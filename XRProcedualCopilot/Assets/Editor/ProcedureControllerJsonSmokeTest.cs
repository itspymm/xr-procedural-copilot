using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XRProceduralCopilot.Procedure;

/// <summary>Editor-only, non-scene smoke test for the Role 3 JSON and procedure controller.</summary>
public static class ProcedureControllerJsonSmokeTest
{
    private const string TutorialAssetPath = "Assets/Role3_TutorialData/paper_airplane.json";
    private const string MenuPath = "Tools/XR Procedural Copilot/Run Procedure Controller JSON Test";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Run this smoke test in Edit Mode, not while entering or in Play Mode.");
            return;
        }

        GameObject temporaryObject = null;
        ProcedureDefinition temporaryDefinition = null;

        try
        {
            TextAsset tutorialJson = AssetDatabase.LoadAssetAtPath<TextAsset>(TutorialAssetPath);
            Require(tutorialJson != null, $"Could not load '{TutorialAssetPath}' as a TextAsset.");

            temporaryObject = new GameObject("[Temporary] ProcedureController JSON Test")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            ProcedureController controller = temporaryObject.AddComponent<ProcedureController>();
            var stepEventNumbers = new List<int>();
            var stateEvents = new List<ProcedurePlaybackState>();
            int repeatEventCount = 0;

            controller.StepChanged += (step, number) => stepEventNumbers.Add(number);
            controller.StateChanged += newState => stateEvents.Add(newState);
            controller.RepeatRequested += (step, number) => repeatEventCount++;

            Require(controller.TryLoadJson(tutorialJson, out string error), $"TryLoadJson failed: {error}");
            temporaryDefinition = controller.Procedure;

            Require(controller.TutorialId == "paper_airplane_01", "Tutorial ID did not load.");
            Require(controller.TutorialTitle == "How to Make a Paper Airplane", "Tutorial title did not load.");
            Require(controller.VideoUrl == string.Empty, "The empty video_url value was not preserved.");
            Require(controller.TotalSteps == 3, "Expected three steps.");

            AssertStep(controller.Procedure.GetStep(0), 1, "Create the Center Crease",
                "Fold the paper in half lengthwise, then unfold it.",
                "Press firmly along the fold.", 0d, 10d);
            AssertStep(controller.Procedure.GetStep(1), 2, "Fold the Top Corners",
                "Fold both top corners inward to meet the center crease.",
                "Keep both sides symmetrical.", 10d, 20d);
            AssertStep(controller.Procedure.GetStep(2), 3, "Create the Wings",
                "Fold the paper in half along the center crease, then fold each side down to form wings.",
                "Make both wings approximately equal in size.", 20d, 35d);

            Require(controller.CurrentStepNumber == 1, "The initial current step number should be 1.");
            RequireApproximately(controller.Progress, 1f / 3f, "Initial progress should be 1/3.");
            Require(stepEventNumbers.Count == 1 && stepEventNumbers[0] == 1,
                "Loading a new procedure should emit one StepChanged event for step 1.");
            Require(stateEvents.Count == 0, "Loading while initially Paused should not emit StateChanged.");

            Require(controller.BeginProcedure(), "BeginProcedure failed.");
            Require(controller.BeginProcedure(), "Repeated BeginProcedure should be safe.");
            Require(controller.State == ProcedurePlaybackState.Playing, "BeginProcedure should enter Playing.");
            Require(stateEvents.Count == 1 && stateEvents[0] == ProcedurePlaybackState.Playing,
                "StateChanged should fire once for Paused to Playing.");
            Require(stepEventNumbers.Count == 1, "Beginning on step 1 should not duplicate StepChanged.");

            Require(controller.RepeatStep(), "RepeatStep failed on the active step.");
            Require(repeatEventCount == 1 && controller.CurrentStepNumber == 1,
                "RepeatStep should signal the same step without changing it.");

            Require(controller.NextStep() && controller.CurrentStepNumber == 2, "NextStep did not select step 2.");
            Require(stepEventNumbers.Count == 2 && stepEventNumbers[1] == 2,
                "NextStep should emit one StepChanged event with step number 2.");
            RequireApproximately(controller.Progress, 2f / 3f, "Step 2 progress should be 2/3.");

            Require(controller.EnterHelp(), "EnterHelp failed from Playing.");
            Require(controller.State == ProcedurePlaybackState.Help && controller.CurrentStepNumber == 2,
                "Help should retain step 2.");
            Require(!controller.NextStep() && !controller.PreviousStep() && !controller.RepeatStep(),
                "Step actions should be blocked during Help.");
            Require(controller.CurrentStepNumber == 2 && stepEventNumbers.Count == 2 && repeatEventCount == 1,
                "Blocked actions changed the step or emitted duplicate events during Help.");

            Require(controller.ResumeProcedure(), "ResumeProcedure failed from Help.");
            Require(controller.State == ProcedurePlaybackState.Playing && controller.CurrentStepNumber == 2,
                "ResumeProcedure should restore the prior Playing state and step.");
            Require(stateEvents.Count == 3
                && stateEvents[1] == ProcedurePlaybackState.Help
                && stateEvents[2] == ProcedurePlaybackState.Playing,
                "Help and resume should each emit one StateChanged event.");

            Require(controller.NextStep() && controller.CurrentStepNumber == 3, "NextStep did not select step 3.");
            RequireApproximately(controller.Progress, 1f, "Final-step progress should be 1.");
            Require(!controller.NextStep(), "NextStep should return false at the last step.");
            Require(controller.PreviousStep() && controller.CurrentStepNumber == 2, "PreviousStep did not select step 2.");
            Require(controller.PreviousStep() && controller.CurrentStepNumber == 1, "PreviousStep did not select step 1.");
            Require(!controller.PreviousStep(), "PreviousStep should return false at the first step.");
            Require(stepEventNumbers.Count == 5
                && stepEventNumbers[2] == 3 && stepEventNumbers[3] == 2 && stepEventNumbers[4] == 1,
                "StepChanged notifications were missing, duplicated, or out of order.");

            Require(controller.Pause(), "Pause failed before testing paused Help restoration.");
            Require(controller.EnterHelp() && controller.ResumeProcedure(), "Paused Help/resume cycle failed.");
            Require(controller.State == ProcedurePlaybackState.Paused, "ResumeProcedure should restore Paused state.");
            Require(controller.RepeatStep(), "RepeatStep failed from Paused.");
            Require(controller.State == ProcedurePlaybackState.Playing && repeatEventCount == 2,
                "RepeatStep from Paused should request replay and enter Playing.");
            Require(stateEvents.Count == 7, "Unexpected number of StateChanged notifications.");

            int priorStepNumber = controller.CurrentStepNumber;
            ProcedurePlaybackState priorState = controller.State;
            Require(!controller.TryLoadJson("{}", out error), "Incomplete JSON should be rejected.");
            Require(controller.CurrentStepNumber == priorStepNumber && controller.State == priorState,
                "A failed JSON load changed the active procedure or playback state.");

            Debug.Log("PASS: paper airplane JSON, controller navigation, progress, Help, Repeat, and events.");
        }
        catch (Exception exception)
        {
            Debug.LogError("FAIL: ProcedureController JSON smoke test.");
            Debug.LogException(exception);
        }
        finally
        {
            if (temporaryObject != null)
                UnityEngine.Object.DestroyImmediate(temporaryObject);

            // The controller normally releases this runtime ScriptableObject in OnDestroy.
            // This fallback also covers Edit Mode lifecycle differences.
            if (temporaryDefinition != null)
                UnityEngine.Object.DestroyImmediate(temporaryDefinition);
        }
    }

    private static void AssertStep(ProcedureStep step, int number, string title, string instruction,
        string guidance, double start, double end)
    {
        Require(step != null, $"Step {number} is null.");
        Require(step.StepNumber == number, $"Step {number} has the wrong step number.");
        Require(step.Title == title, $"Step {number} title did not load.");
        Require(step.Instruction == instruction, $"Step {number} instruction did not load.");
        Require(step.AdditionalGuidance == guidance, $"Step {number} additional guidance did not load.");
        Require(step.VideoStartSeconds == start && step.VideoEndSeconds == end,
            $"Step {number} timestamps did not load.");
    }

    private static void RequireApproximately(float actual, float expected, string message)
    {
        Require(Mathf.Abs(actual - expected) < 0.0001f, message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
