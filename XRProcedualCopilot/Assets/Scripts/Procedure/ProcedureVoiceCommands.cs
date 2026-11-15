using System.Collections;
using System.Text.RegularExpressions;
using Oculus.Voice;
using TMPro;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Routes completed speech transcriptions to the same actions as controller input.</summary>
    public sealed class ProcedureVoiceCommands : MonoBehaviour
    {
        [SerializeField] private AppVoiceExperience voice;
        [SerializeField] private ProcedureController controller;
        [SerializeField] private TMP_Text statusText;
        private bool ready;
        private float nextActivation;
        private float feedbackUntil;
        private float lastCommandTime = -10f;
        private int consecutiveErrors;
        private bool listening;
        private float micLevel;

        private void OnEnable()
        {
            if (voice == null) return;
            voice.VoiceEvents.OnFullTranscription.AddListener(HandleTranscription);
            voice.VoiceEvents.OnStartListening.AddListener(HandleListening);
            voice.VoiceEvents.OnStoppedListening.AddListener(HandleStopped);
            voice.VoiceEvents.OnRequestCompleted.AddListener(HandleCompleted);
            voice.VoiceEvents.OnError.AddListener(HandleError);
            voice.VoiceEvents.OnMicLevelChanged.AddListener(HandleMicLevel);
        }

        private IEnumerator Start()
        {
            if (voice == null || controller == null || voice.Configuration == null)
            {
                Show("Voice setup needed", 30f);
                yield break;
            }
            Show("Allow microphone access to use voice", 2f);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                Show("Microphone blocked — use A / B / X", 30f);
                yield break;
            }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Show("Microphone blocked — use A / B / X", 30f);
                yield break;
            }
#else
            // Desktop microphone access is requested by the SDK's native microphone input.
            // RequestUserAuthorization/HasUserAuthorization do not describe macOS permission.
            yield return null;
#endif
            ready = true;
            nextActivation = Time.unscaledTime + 0.5f;
        }

        private void Update()
        {
            if (listening && Time.unscaledTime >= feedbackUntil && statusText != null)
                statusText.text = $"Listening • mic level {micLevel:0.00}";
#if !UNITY_EDITOR
            if (!Application.isFocused) return;
#endif
            if (!ready || voice == null || Time.unscaledTime < nextActivation
                || voice.Active || voice.IsRequestActive || !voice.CanActivateAudio()) return;
            nextActivation = Time.unscaledTime + 2f;
            voice.Activate();
        }

        private void HandleListening()
        {
            listening = true;
            micLevel = 0f;
            if (Time.unscaledTime >= feedbackUntil) Show("Listening: Next step / Go back / Repeat", 0f);
        }

        private void HandleStopped()
        {
            listening = false;
            if (Time.unscaledTime >= feedbackUntil) Show("Processing voice…", 0f);
        }

        private void HandleCompleted() { nextActivation = Mathf.Max(nextActivation, Time.unscaledTime + 0.5f); }

        private void HandleError(string code, string message)
        {
            listening = false;
            consecutiveErrors++;
            float delay = Mathf.Min(15f, 2f * consecutiveErrors);
            nextActivation = Time.unscaledTime + delay;
            Show("Voice unavailable — retrying; A / B / X still work", delay);
        }

        private void HandleMicLevel(float level) { micLevel = level; }

        public void HandleTranscription(string transcript)
        {
            if (controller == null || string.IsNullOrWhiteSpace(transcript)) return;
            string command = Regex.Replace(transcript.ToLowerInvariant(), @"[^a-z\s]", "");
            command = Regex.Replace(command, @"\s+", " ").Trim();
            consecutiveErrors = 0;
            if (Time.unscaledTime - lastCommandTime < 0.8f) return;
            bool changed;
            switch (command)
            {
                case "next": case "next step":
                    changed = controller.NextStep();
                    Show(changed ? "Next step ✓" : "Already at the last step", 2f);
                    break;
                case "back": case "go back": case "previous step":
                    changed = controller.PreviousStep();
                    Show(changed ? "Previous step ✓" : "Already at the first step", 2f);
                    break;
                case "repeat": case "repeat step":
                    changed = controller.RepeatStep();
                    Show(changed ? "Repeat ✓" : "Repeat unavailable", 2f);
                    break;
                default:
                    Show("Say: Next step / Go back / Repeat", 2f);
                    return;
            }
            lastCommandTime = Time.unscaledTime;
        }

        private void Show(string message, float duration)
        {
            if (statusText != null) statusText.text = message;
            feedbackUntil = Time.unscaledTime + duration;
        }

        private void OnApplicationFocus(bool focused)
        {
#if !UNITY_EDITOR
            if (!focused && voice != null) voice.DeactivateAndAbortRequest();
#endif
        }

        private void OnDisable()
        {
            if (voice == null) return;
            voice.VoiceEvents.OnFullTranscription.RemoveListener(HandleTranscription);
            voice.VoiceEvents.OnStartListening.RemoveListener(HandleListening);
            voice.VoiceEvents.OnStoppedListening.RemoveListener(HandleStopped);
            voice.VoiceEvents.OnRequestCompleted.RemoveListener(HandleCompleted);
            voice.VoiceEvents.OnError.RemoveListener(HandleError);
            voice.VoiceEvents.OnMicLevelChanged.RemoveListener(HandleMicLevel);
            voice.DeactivateAndAbortRequest();
        }
    }
}
