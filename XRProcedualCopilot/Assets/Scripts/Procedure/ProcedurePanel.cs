using TMPro;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Displays controller state and provides temporary controller/keyboard inputs.</summary>
    public sealed class ProcedurePanel : MonoBehaviour
    {
        [SerializeField] private ProcedureController controller;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text stepCounter;
        private bool nextHeld, backHeld, repeatHeld;

        private void OnEnable()
        {
            if (controller == null) return;
            controller.StepChanged += DisplayStep;
            controller.RepeatRequested += DisplayStep;
            nextHeld = backHeld = repeatHeld = true;
        }

        private void Start()
        {
            if (controller == null || instructionText == null || stepCounter == null)
            {
                Debug.LogError("ProcedurePanel needs its controller and both text references.", this);
                enabled = false;
                return;
            }
            controller.BeginProcedure();
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.StepChanged -= DisplayStep;
            controller.RepeatRequested -= DisplayStep;
        }

        private void DisplayStep(ProcedureStep step, int index)
        {
            if (step == null || instructionText == null || stepCounter == null) return;
            instructionText.text = step.Title + "\n" + step.Instruction;
            stepCounter.text = $"STEP {index + 1} OF {controller.Procedure.StepCount}";
        }

        private void Update()
        {
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool next);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool back);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool repeat);
            bool nextPressed = next && !nextHeld;
            bool backPressed = back && !backHeld;
            bool repeatPressed = repeat && !repeatHeld;
            nextHeld = next;
            backHeld = back;
            repeatHeld = repeat;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                nextPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
                backPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
                repeatPressed |= Keyboard.current.rKey.wasPressedThisFrame;
            }
#endif
            if (nextPressed) controller.NextStep();
            else if (backPressed) controller.PreviousStep();
            else if (repeatPressed) controller.RepeatStep();
        }
    }
}
