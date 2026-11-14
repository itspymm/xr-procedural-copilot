using System;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>
    /// Owns procedure progress and mode. UI and voice input can call these public methods
    /// and listen to the events without owning or changing procedure state themselves.
    /// </summary>
    public sealed class ProcedureController : MonoBehaviour
    {
        [SerializeField] private ProcedureDefinition procedure;

        private int currentStepIndex;
        private ProcedurePlaybackState state = ProcedurePlaybackState.Paused;
        private ProcedurePlaybackState stateBeforeHelp = ProcedurePlaybackState.Paused;
        private bool isReady;

        public event Action<ProcedureStep, int> StepChanged;
        public event Action<ProcedurePlaybackState> StateChanged;
        public event Action<ProcedureStep, int> RepeatRequested;

        public ProcedureDefinition Procedure => procedure;
        public int CurrentStepIndex => currentStepIndex;
        public ProcedureStep CurrentStep => IsReady ? procedure.GetStep(currentStepIndex) : null;
        public ProcedurePlaybackState State => state;
        public bool IsReady => isReady && procedure != null;

        private void Awake()
        {
            string error = null;
            isReady = procedure != null && procedure.TryValidate(out error);
            if (!isReady)
            {
                Debug.LogError(procedure == null
                    ? "ProcedureController needs a ProcedureDefinition asset."
                    : $"Invalid procedure '{procedure.ProcedureTitle}': {error}", this);
                return;
            }

            currentStepIndex = 0;
        }

        /// <summary>Starts at the first step when paused; repeated calls while active do not reset progress.</summary>
        public bool BeginProcedure()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            if (state == ProcedurePlaybackState.Playing)
                return true;

            currentStepIndex = 0;
            StepChanged?.Invoke(CurrentStep, currentStepIndex);
            SetState(ProcedurePlaybackState.Playing);
            return true;
        }

        public bool Play()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            SetState(ProcedurePlaybackState.Playing);
            return true;
        }

        /// <summary>Loads a different authored procedure and resets progress to its first step.</summary>
        public bool TryLoadProcedure(ProcedureDefinition newProcedure, out string error)
        {
            error = null;
            if (state == ProcedurePlaybackState.Help)
            {
                error = "Continue from help before loading another procedure.";
                return false;
            }

            if (newProcedure == null || !newProcedure.TryValidate(out error))
            {
                if (newProcedure == null)
                    error = "A procedure asset is required.";
                return false;
            }

            procedure = newProcedure;
            isReady = true;
            currentStepIndex = 0;
            stateBeforeHelp = ProcedurePlaybackState.Paused;
            SetState(ProcedurePlaybackState.Paused);
            StepChanged?.Invoke(CurrentStep, currentStepIndex);
            error = null;
            return true;
        }

        public bool Pause()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            SetState(ProcedurePlaybackState.Paused);
            return true;
        }

        /// <summary>Moves only on an explicit call; reaching a video timestamp never advances the step.</summary>
        public bool NextStep()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help || currentStepIndex >= procedure.StepCount - 1)
                return false;

            currentStepIndex++;
            StepChanged?.Invoke(CurrentStep, currentStepIndex);
            return true;
        }

        public bool PreviousStep()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help || currentStepIndex <= 0)
                return false;

            currentStepIndex--;
            StepChanged?.Invoke(CurrentStep, currentStepIndex);
            return true;
        }

        /// <summary>Requests replay of the current step's video segment without changing the step.</summary>
        public bool RepeatStep()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            // Repeating is an explicit request to replay the current instruction segment.
            SetState(ProcedurePlaybackState.Playing);
            RepeatRequested?.Invoke(CurrentStep, currentStepIndex);
            return true;
        }

        public bool EnterHelp()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            stateBeforeHelp = state;
            SetState(ProcedurePlaybackState.Help);
            return true;
        }

        /// <summary>Returns from help without changing the step; the video adapter resumes its saved position.</summary>
        public bool ResumeProcedure()
        {
            if (!IsReady || state != ProcedurePlaybackState.Help)
                return false;

            SetState(stateBeforeHelp);
            return true;
        }

        private void SetState(ProcedurePlaybackState newState)
        {
            if (state == newState)
                return;

            state = newState;
            StateChanged?.Invoke(state);
        }
    }
}
