using System;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>
    /// Owns procedure progress and mode. UI and voice input call its public methods
    /// and read its properties rather than keeping a separate copy of the procedure state.
    /// </summary>
    public sealed class ProcedureController : MonoBehaviour
    {
        [SerializeField] private ProcedureDefinition procedure;

        private int currentStepIndex;
        private ProcedurePlaybackState state = ProcedurePlaybackState.Paused;
        private ProcedurePlaybackState stateBeforeHelp = ProcedurePlaybackState.Paused;
        private bool isReady;
        private ProcedureDefinition runtimeOwnedProcedure;

        /// <summary>Raised when the active step or loaded procedure changes. The integer is one-based.</summary>
        public event Action<ProcedureStep, int> StepChanged;

        /// <summary>Raised only when the playback state actually changes.</summary>
        public event Action<ProcedurePlaybackState> StateChanged;

        /// <summary>Raised when the active step is explicitly requested again.</summary>
        public event Action<ProcedureStep, int> RepeatRequested;

        public ProcedureDefinition Procedure => IsReady ? procedure : null;
        public int CurrentStepIndex => CurrentStep == null ? -1 : currentStepIndex;
        public ProcedureStep CurrentStep => IsReady ? procedure.GetStep(currentStepIndex) : null;
        public int TotalSteps => IsReady ? procedure.StepCount : 0;
        public int CurrentStepNumber => CurrentStep == null ? 0 : CurrentStep.StepNumber;
        public float Progress => TotalSteps == 0 || CurrentStepNumber == 0
            ? 0f
            : (float)CurrentStepNumber / TotalSteps;
        public string TutorialTitle => IsReady ? procedure.ProcedureTitle ?? string.Empty : string.Empty;
        public string TutorialId => IsReady ? procedure.TutorialId : string.Empty;
        public string VideoUrl => IsReady ? procedure.VideoUrl : string.Empty;
        public ProcedurePlaybackState State => state;
        public bool IsReady => isReady && procedure != null;

        private void Awake()
        {
            // A controller with no authored asset is valid when another component will load JSON at runtime.
            if (procedure == null)
            {
                currentStepIndex = 0;
                return;
            }

            string error = null;
            isReady = procedure.TryValidate(out error);
            if (!isReady)
            {
                Debug.LogError($"Invalid procedure '{procedure.ProcedureTitle}': {error}", this);
                return;
            }

            currentStepIndex = 0;
        }

        private void OnDestroy()
        {
            DestroyRuntimeDefinition(runtimeOwnedProcedure);
        }

        /// <summary>Starts at the first step when paused; repeated calls while active do not reset progress.</summary>
        public bool BeginProcedure()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            if (state == ProcedurePlaybackState.Playing)
                return true;

            bool stepChanged = currentStepIndex != 0;
            currentStepIndex = 0;
            if (stepChanged)
                RaiseStepChanged();

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

        /// <summary>Loads and validates an authored ProcedureDefinition asset.</summary>
        public bool TryLoadProcedure(ProcedureDefinition newProcedure, out string error)
        {
            return TryReplaceProcedure(newProcedure, false, out error);
        }

        /// <summary>Loads JSON text after validating the complete replacement before changing controller state.</summary>
        public bool TryLoadJson(string json, out string error)
        {
            if (!ProcedureJsonLoader.TryCreateDefinition(json, out ProcedureDefinition parsed, out error))
                return false;

            if (TryReplaceProcedure(parsed, true, out error))
                return true;

            DestroyRuntimeDefinition(parsed);
            return false;
        }

        /// <summary>Loads a bundled JSON TextAsset, suitable for assignment in the Unity Inspector.</summary>
        public bool TryLoadJson(TextAsset jsonAsset, out string error)
        {
            if (jsonAsset == null)
            {
                error = "A tutorial JSON TextAsset is required.";
                return false;
            }

            return TryLoadJson(jsonAsset.text, out error);
        }

        private bool TryReplaceProcedure(ProcedureDefinition newProcedure, bool takeOwnership, out string error)
        {
            error = null;
            if (state == ProcedurePlaybackState.Help)
            {
                error = "Continue from help before loading another procedure.";
                return false;
            }

            if (newProcedure == null)
            {
                error = "A valid ProcedureDefinition is required.";
                return false;
            }

            if (!newProcedure.TryValidate(out error))
                return false;

            bool sameContent = IsReady && procedure.HasSameContent(newProcedure);
            if (sameContent)
            {
                // Identical reloads are no-ops: retain the active step, playback state, and object references.
                if (takeOwnership && newProcedure != procedure)
                    DestroyRuntimeDefinition(newProcedure);

                error = null;
                return true;
            }

            ProcedureDefinition previousRuntimeProcedure = runtimeOwnedProcedure;

            procedure = newProcedure;
            isReady = true;
            runtimeOwnedProcedure = takeOwnership ? newProcedure : null;
            currentStepIndex = 0;
            stateBeforeHelp = ProcedurePlaybackState.Paused;
            SetState(ProcedurePlaybackState.Paused);
            RaiseStepChanged();

            if (previousRuntimeProcedure != newProcedure)
                DestroyRuntimeDefinition(previousRuntimeProcedure);

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
            RaiseStepChanged();
            return true;
        }

        public bool PreviousStep()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help || currentStepIndex <= 0)
                return false;

            currentStepIndex--;
            RaiseStepChanged();
            return true;
        }

        /// <summary>Requests replay of the current step's video segment without changing the step.</summary>
        public bool RepeatStep()
        {
            if (!IsReady || state == ProcedurePlaybackState.Help)
                return false;

            SetState(ProcedurePlaybackState.Playing);
            RepeatRequested?.Invoke(CurrentStep, CurrentStepNumber);
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

        /// <summary>Returns from help without changing the step; the synchronizer restores its saved playhead.</summary>
        public bool ResumeProcedure()
        {
            if (!IsReady || state != ProcedurePlaybackState.Help)
                return false;

            SetState(stateBeforeHelp);
            return true;
        }

        private void RaiseStepChanged()
        {
            StepChanged?.Invoke(CurrentStep, CurrentStepNumber);
        }

        private void SetState(ProcedurePlaybackState newState)
        {
            if (state == newState)
                return;

            state = newState;
            StateChanged?.Invoke(state);
        }

        private static void DestroyRuntimeDefinition(ProcedureDefinition definition)
        {
            if (definition == null)
                return;

            if (Application.isPlaying)
                Destroy(definition);
            else
                DestroyImmediate(definition);
        }
    }
}
