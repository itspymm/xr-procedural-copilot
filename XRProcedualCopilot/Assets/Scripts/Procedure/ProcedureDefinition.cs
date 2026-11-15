using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Authored procedure data, separate from the UI and playback implementation.</summary>
    [CreateAssetMenu(fileName = "Procedure", menuName = "XR Procedural Copilot/Procedure")]
    public sealed class ProcedureDefinition : ScriptableObject
    {
        [SerializeField] private string tutorialId;
        [SerializeField] private string procedureTitle = "New Procedure";
        [SerializeField] private string videoUrl;
        [SerializeField] private List<ProcedureStep> steps = new List<ProcedureStep>();

        public string TutorialId => tutorialId ?? string.Empty;
        public string ProcedureTitle => procedureTitle;
        public string VideoUrl => videoUrl ?? string.Empty;
        public int StepCount => steps == null ? 0 : steps.Count;

        public ProcedureStep GetStep(int index)
        {
            if (steps == null || index < 0 || index >= steps.Count)
                return null;

            return steps[index];
        }

        /// <summary>Checks the timestamp ordering before a controller starts the procedure.</summary>
        public bool TryValidate(out string error)
        {
            if (steps == null || steps.Count == 0)
            {
                error = "The procedure must contain at least one step.";
                return false;
            }

            double previousEnd = 0d;
            for (int i = 0; i < steps.Count; i++)
            {
                ProcedureStep step = steps[i];
                if (step == null)
                {
                    error = $"Step {i + 1} is empty.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.Title) || string.IsNullOrWhiteSpace(step.Instruction))
                {
                    error = $"Step {i + 1} needs both a title and instruction.";
                    return false;
                }

                if (step.StepNumber != i + 1)
                {
                    error = $"Step {i + 1} has step_number {step.StepNumber}; step numbers must be sequential and start at 1.";
                    return false;
                }

                if (double.IsNaN(step.VideoStartSeconds) || double.IsInfinity(step.VideoStartSeconds)
                    || double.IsNaN(step.VideoEndSeconds) || double.IsInfinity(step.VideoEndSeconds)
                    || step.VideoStartSeconds < 0d || step.VideoEndSeconds <= step.VideoStartSeconds)
                {
                    error = $"Step {i + 1} must have a non-negative start time and an end time after its start.";
                    return false;
                }

                if (i > 0 && step.VideoStartSeconds < previousEnd)
                {
                    error = $"Step {i + 1} starts before step {i} ends. Video ranges must be chronological and non-overlapping.";
                    return false;
                }

                previousEnd = step.VideoEndSeconds;
            }

            error = null;
            return true;
        }

        internal void InitializeFromJson(string id, string title, string url, List<ProcedureStep> procedureSteps)
        {
            tutorialId = id;
            procedureTitle = title;
            videoUrl = url;
            steps = procedureSteps;
        }

        internal bool HasSameContent(ProcedureDefinition other)
        {
            if (other == null || TutorialId != other.TutorialId || ProcedureTitle != other.ProcedureTitle
                || VideoUrl != other.VideoUrl || StepCount != other.StepCount)
                return false;

            for (int i = 0; i < StepCount; i++)
            {
                ProcedureStep left = GetStep(i);
                ProcedureStep right = other.GetStep(i);
                if (left == null || right == null
                    || left.StepNumber != right.StepNumber
                    || left.Title != right.Title
                    || left.Instruction != right.Instruction
                    || left.AdditionalGuidance != right.AdditionalGuidance
                    || left.VideoStartSeconds != right.VideoStartSeconds
                    || left.VideoEndSeconds != right.VideoEndSeconds)
                    return false;
            }

            return true;
        }
    }
}
