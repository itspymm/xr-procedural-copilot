using System;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>One instruction and the part of a tutorial video that demonstrates it.</summary>
    [Serializable]
    public sealed class ProcedureStep
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string instruction;
        [SerializeField, Min(0f)] private double videoStartSeconds;
        [SerializeField, Min(0f)] private double videoEndSeconds;

        public string Title => title;
        public string Instruction => instruction;
        public double VideoStartSeconds => videoStartSeconds;
        public double VideoEndSeconds => videoEndSeconds;

        public ProcedureStep()
        {
        }

        public ProcedureStep(string title, string instruction, double videoStartSeconds, double videoEndSeconds)
        {
            this.title = title;
            this.instruction = instruction;
            this.videoStartSeconds = videoStartSeconds;
            this.videoEndSeconds = videoEndSeconds;
        }
    }
}
