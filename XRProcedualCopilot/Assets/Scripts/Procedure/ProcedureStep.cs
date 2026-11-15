using System;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>One instruction and the part of a tutorial video that demonstrates it.</summary>
    [Serializable]
    public sealed class ProcedureStep
    {
        [SerializeField, Min(1)] private int stepNumber = 1;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string instruction;
        [SerializeField, TextArea(2, 5)] private string additionalGuidance;
        [SerializeField, Min(0f)] private double videoStartSeconds;
        [SerializeField, Min(0f)] private double videoEndSeconds;

        public int StepNumber => stepNumber;
        public string Title => title;
        public string Instruction => instruction;
        public string AdditionalGuidance => additionalGuidance ?? string.Empty;
        public double VideoStartSeconds => videoStartSeconds;
        public double VideoEndSeconds => videoEndSeconds;

        public ProcedureStep()
        {
        }

        public ProcedureStep(string title, string instruction, double videoStartSeconds, double videoEndSeconds)
            : this(1, title, instruction, string.Empty, videoStartSeconds, videoEndSeconds)
        {
        }

        public ProcedureStep(int stepNumber, string title, string instruction, string additionalGuidance,
            double videoStartSeconds, double videoEndSeconds)
        {
            this.stepNumber = stepNumber;
            this.title = title;
            this.instruction = instruction;
            this.additionalGuidance = additionalGuidance;
            this.videoStartSeconds = videoStartSeconds;
            this.videoEndSeconds = videoEndSeconds;
        }
    }
}
