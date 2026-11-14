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
                videoPlayer.prepareCompleted -= HandleVideoPrepared;

            isSubscribed = false;
        }

        private void Update()
        {
            if (controller == null || videoPlayer == null || !controller.IsReady || reachedCurrentStepEnd)
                return;

            ProcedureStep step = controller.CurrentStep;
            if (step == null || !videoPlayer.isPrepared || !videoPlayer.isPlaying)
                return;

            if (videoPlayer.time >= step.VideoEndSeconds - endToleranceSeconds)
            {
                videoPlayer.Pause();
                videoPlayer.time = step.VideoEndSeconds;
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
                helpVideoPosition = videoPlayer.isPrepared
                    ? videoPlayer.time
                    : (pendingSeek ? pendingSeekTime : videoPlayer.time);
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
                if (!reachedCurrentStepEnd && videoPlayer.isPrepared)
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
            if (videoPlayer == null || !videoPlayer.isPrepared)
                return;

            if (pendingSeek)
            {
                videoPlayer.time = pendingSeekTime;
                pendingSeek = false;
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

            if (videoPlayer.isPrepared)
            {
                videoPlayer.time = helpVideoPosition;
                pendingSeek = false;
            }
            else
            {
                pendingSeekTime = helpVideoPosition;
                pendingSeek = true;
            }
        }
    }
}
