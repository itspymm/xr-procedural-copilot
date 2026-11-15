using UnityEngine;
using UnityEngine.Video;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Adapts procedure events to a Unity VideoPlayer without owning procedure progress.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class ProcedureVideoSynchronizer : MonoBehaviour
    {
        [SerializeField] private ProcedureController controller;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField, Min(0f)] private float endToleranceSeconds = 0.03f;

        private bool reachedCurrentStepEnd;
        private bool pendingSeek;
        private bool seekInProgress;
        private bool seekCompleted;
        private bool seekFrameReady;
        private double activeSeekTime;
        private double renderedTime = -1d;
        private bool pendingPlay;
        private double pendingSeekTime;
        private bool durationWarningShown;
        private bool hasHelpPosition;
        private double helpVideoPosition;
        private bool isSubscribed;
        private ProcedureDefinition durationValidatedFor;

        private void Reset()
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        private void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                Debug.LogError("ProcedureVideoSynchronizer needs a ProcedureController reference.", this);
                return;
            }

            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("ProcedureVideoSynchronizer needs a VideoPlayer on the same GameObject.", this);
                return;
            }

            controller.StepChanged += HandleStepChanged;
            controller.StateChanged += HandleStateChanged;
            controller.RepeatRequested += HandleRepeatRequested;
            videoPlayer.prepareCompleted += HandleVideoPrepared;
            videoPlayer.seekCompleted += HandleSeekCompleted;
            videoPlayer.frameReady += HandleFrameReady;
            videoPlayer.sendFrameReadyEvents = true;
            isSubscribed = true;

            if (controller.IsReady)
                SeekToCurrentStepStart(controller.State == ProcedurePlaybackState.Playing);

            if (HasVideoSource() && !videoPlayer.isPrepared)
                videoPlayer.Prepare();
            else if (videoPlayer.isPrepared)
                ValidateVideoDuration();
        }

        private void OnDisable()
        {
            if (!isSubscribed)
                return;

            if (controller != null)
            {
                controller.StepChanged -= HandleStepChanged;
                controller.StateChanged -= HandleStateChanged;
                controller.RepeatRequested -= HandleRepeatRequested;
            }

            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= HandleVideoPrepared;
                videoPlayer.seekCompleted -= HandleSeekCompleted;
                videoPlayer.frameReady -= HandleFrameReady;
            }

            isSubscribed = false;
            seekInProgress = false;
        }

        private void Update()
        {
            if (controller == null || videoPlayer == null || !controller.IsReady || reachedCurrentStepEnd
                || seekInProgress || pendingSeek)
                return;

            ProcedureStep step = controller.CurrentStep;
            if (step == null || !videoPlayer.isPrepared || !videoPlayer.isPlaying)
                return;

            if (renderedTime >= step.VideoEndSeconds - endToleranceSeconds)
            {
                videoPlayer.Pause();
                reachedCurrentStepEnd = true;
            }
        }

        private void HandleStepChanged(ProcedureStep step, int stepNumber)
        {
            if (controller == null)
                return;

            ValidateVideoDuration();
            SeekToCurrentStepStart(controller.State == ProcedurePlaybackState.Playing);
        }

        private void HandleStateChanged(ProcedurePlaybackState newState)
        {
            if (videoPlayer == null)
                return;

            if (newState == ProcedurePlaybackState.Help)
            {
                // Snapshot before pausing so Help can never change the saved playhead.
                helpVideoPosition = seekInProgress || pendingSeek ? pendingSeekTime : videoPlayer.time;
                hasHelpPosition = true;
                pendingPlay = false;
                videoPlayer.Pause();
                return;
            }

            if (hasHelpPosition)
            {
                RestoreHelpPosition();
                hasHelpPosition = false;
            }

            if (newState == ProcedurePlaybackState.Playing)
            {
                pendingPlay = true;
                if (!reachedCurrentStepEnd && !seekInProgress && !pendingSeek && videoPlayer.isPrepared)
                    videoPlayer.Play();
            }
            else
            {
                pendingPlay = false;
                videoPlayer.Pause();
            }
        }

        private void HandleRepeatRequested(ProcedureStep step, int stepNumber)
        {
            if (controller == null)
                return;

            SeekToCurrentStepStart(controller.State == ProcedurePlaybackState.Playing);
        }

        private void SeekToCurrentStepStart(bool play)
        {
            if (controller == null || videoPlayer == null || !controller.IsReady)
                return;

            ProcedureStep step = controller.CurrentStep;
            if (step == null)
                return;

            reachedCurrentStepEnd = false;
            pendingSeekTime = step.VideoStartSeconds;
            pendingSeek = true;
            pendingPlay = play;
            if (videoPlayer.isPrepared)
                ApplyPendingSeek();
        }

        private bool HasVideoSource()
        {
            if (videoPlayer == null)
                return false;

            return videoPlayer.source == VideoSource.VideoClip
                ? videoPlayer.clip != null
                : !string.IsNullOrWhiteSpace(videoPlayer.url);
        }

        private void HandleVideoPrepared(VideoPlayer source)
        {
            ValidateVideoDuration();
            ApplyPendingSeek();
        }

        private void HandleSeekCompleted(VideoPlayer source)
        {
            seekCompleted = true;
            if (seekFrameReady) CompleteSeek();
        }

        private void HandleFrameReady(VideoPlayer source, long frameIndex)
        {
            if (source.frameRate <= 0) return;
            renderedTime = frameIndex / source.frameRate;
            if (!seekInProgress || System.Math.Abs(renderedTime - activeSeekTime) > 0.15d) return;
            seekFrameReady = true;
            if (seekCompleted) CompleteSeek();
        }

        private void CompleteSeek()
        {
            seekInProgress = false;
            ApplyPendingSeek();
        }

        private void ValidateVideoDuration()
        {
            if (controller == null || !controller.IsReady || videoPlayer == null || !videoPlayer.isPrepared)
                return;

            if (durationValidatedFor != controller.Procedure)
            {
                durationValidatedFor = controller.Procedure;
                durationWarningShown = false;
            }

            if (durationWarningShown)
                return;

            ProcedureStep lastStep = controller.Procedure.GetStep(controller.Procedure.StepCount - 1);
            if (lastStep != null && lastStep.VideoEndSeconds > videoPlayer.length + endToleranceSeconds)
            {
                Debug.LogError(
                    $"Procedure video is {videoPlayer.length:0.##} seconds long, but the final step ends at {lastStep.VideoEndSeconds:0.##} seconds.",
                    this);
                durationWarningShown = true;
            }
        }

        private void ApplyPendingSeek()
        {
            if (videoPlayer == null || !videoPlayer.isPrepared || seekInProgress)
                return;

            if (pendingSeek)
            {
                pendingSeek = false;
                double frameTolerance = videoPlayer.frameRate > 0 ? 1d / videoPlayer.frameRate + 0.005d : 0.04d;
                // Adjacent segments can share the currently paused frame. Some decoders do
                // not issue seek callbacks when asked to seek to that same frame.
                if (renderedTime < 0 || System.Math.Abs(renderedTime - pendingSeekTime) > frameTolerance)
                {
                    seekInProgress = true;
                    seekCompleted = false;
                    seekFrameReady = false;
                    activeSeekTime = pendingSeekTime;
                    videoPlayer.Pause();
                    videoPlayer.time = activeSeekTime;
                    return;
                }
            }

            if (pendingPlay && controller != null && controller.State == ProcedurePlaybackState.Playing
                && !reachedCurrentStepEnd)
                videoPlayer.Play();
            else
                videoPlayer.Pause();
        }

        private void RestoreHelpPosition()
        {
            if (videoPlayer == null)
                return;

            pendingSeekTime = helpVideoPosition;
            pendingSeek = true;
            ApplyPendingSeek();
        }
    }
}
