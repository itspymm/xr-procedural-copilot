using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
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
        private TutorialReceiver receiver;
        private ProcedurePanel panel;
        private bool answering;
        private int conversationVersion;
        private readonly List<ChatTurn> history = new List<ChatTurn>();
        [Serializable] private sealed class ChatTurn
        {
            public string role;
            public string content;
        }
        [Serializable] private sealed class ChatRequest
        {
            public string revision;
            public int step_number;
            public string question;
            public ChatTurn[] history;
        }
        [Serializable] private sealed class ChatResponse
        {
            public string answer;
            public string error;
            public string revision;
            public int step_number;
        }
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
            receiver = GetComponent<TutorialReceiver>();
            panel = GetComponent<ProcedurePanel>();
            ready = true;
            nextActivation = Time.unscaledTime + 0.5f;
        }

        private void Update()
        {
            if (listening && Time.unscaledTime >= feedbackUntil && statusText != null)
                statusText.text = answering ? "Thinking about your question…" : "Listening · speak when you’re ready";
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
            if (Time.unscaledTime >= feedbackUntil) Show(controller != null && controller.State == ProcedurePlaybackState.Help
                ? "Ask a question / Continue tutorial" : "Next step / Go back / Repeat / Help", 0f);
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
            if (panel != null && (command == "read more" || command == "next page" || command == "previous page"))
            {
                bool moved = panel.ChangePage(command == "previous page" ? -1 : 1);
                Show(moved ? "Page changed" : "No more pages", 2f);
                lastCommandTime = Time.unscaledTime;
                return;
            }
            // Clarification is enabled only in the companion scene.
            if (receiver != null && panel != null)
            {
                if (command == "continue tutorial")
                {
                    conversationVersion++;
                    history.Clear();
                    controller.ResumeProcedure();
                    panel.RestoreInstruction();
                    Show("Tutorial resumed", 2f);
                    lastCommandTime = Time.unscaledTime;
                    return;
                }
                if (controller.State == ProcedurePlaybackState.Help)
                {
                    if (answering) { Show("Still thinking…", 2f); return; }
                    if (transcript.Length > 1000) { Show("Please ask a shorter question", 2f); return; }
                    StartCoroutine(AskQuestion(transcript));
                    lastCommandTime = Time.unscaledTime;
                    return;
                }
                if (command == "help")
                {
                    if (string.IsNullOrEmpty(receiver.LoadedRevision))
                    {
                        Show("Send a tutorial from the Mac before asking for help", 5f);
                        return;
                    }
                    controller.EnterHelp();
                    conversationVersion++;
                    history.Clear();
                    panel.ShowClarification("What would you like to know about this step?\n\nAsk a question or say ‘continue tutorial’ to return.");
                    Show("Ask about this step", 2f);
                    lastCommandTime = Time.unscaledTime;
                    return;
                }
            }
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

        private IEnumerator AskQuestion(string question)
        {
            answering = true;
            int version = conversationVersion;
            string revision = receiver.LoadedRevision;
            int step = controller.CurrentStepNumber;
            panel.ShowClarification("Thinking about your question…", question);
            var body = new ChatRequest { revision = revision, step_number = step,
                question = question, history = history.ToArray() };
            using (var request = new UnityWebRequest(receiver.CompanionUrl + "/api/chat", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 100;
                yield return request.SendWebRequest();
                answering = false;
                if (version != conversationVersion || controller.State != ProcedurePlaybackState.Help
                    || receiver.LoadedRevision != revision || controller.CurrentStepNumber != step) yield break;
                ChatResponse response = null;
                try { response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text); }
                catch (Exception) { }
                if (request.result != UnityWebRequest.Result.Success || response == null
                    || string.IsNullOrWhiteSpace(response.answer) || response.revision != revision
                    || response.step_number != step)
                {
                    string error = response != null && !string.IsNullOrEmpty(response.error)
                        ? response.error : "Could not reach the Mac companion. Check USB and the server.";
                    panel.ShowClarification(error + "\n\nAsk again, or say ‘continue tutorial’.", question);
                    yield break;
                }
                history.Add(new ChatTurn { role = "user", content = question });
                history.Add(new ChatTurn { role = "assistant", content = response.answer });
                while (history.Count > 8) history.RemoveRange(0, 2);
                panel.ShowClarification(response.answer, question);
                Show("Answer ready", 2f);
            }
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
            conversationVersion++;
            answering = false;
            StopAllCoroutines();
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
