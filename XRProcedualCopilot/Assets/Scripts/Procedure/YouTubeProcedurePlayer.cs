using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using TLab.WebView;
using UnityEngine;
using UnityEngine.Scripting;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Streams an official YouTube iframe through the Android browser texture.</summary>
    public sealed class YouTubeProcedurePlayer : MonoBehaviour
    {
        [SerializeField] private ProcedureController controller;
        [SerializeField] private CopilotWebView browser;
        [SerializeField] private TextAsset playerHtml;
        [SerializeField] private string youtubeUrl = "https://youtu.be/1PVA2nPU60U";
        private bool ready;
        private bool loaded;
        private bool hadHelp;
        private bool tapHeld;
        private float deadline;
        public string Status { get; private set; } = "Starting browser";

        public static string ParseVideoId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                throw new ArgumentException("Use an HTTPS YouTube link.");
            string id = null;
            string host = uri.Host.ToLowerInvariant();
            if (host == "youtu.be") id = uri.AbsolutePath.Trim('/');
            else if (host == "youtube.com" || host == "www.youtube.com" || host == "m.youtube.com")
            {
                if (uri.AbsolutePath == "/watch")
                {
                    foreach (string pair in uri.Query.TrimStart('?').Split('&'))
                    {
                        string[] field = pair.Split(new[] { '=' }, 2);
                        if (field.Length == 2 && field[0] == "v") id = Uri.UnescapeDataString(field[1]);
                    }
                }
                else
                {
                    string[] parts = uri.AbsolutePath.Trim('/').Split('/');
                    if (parts.Length == 2 && (parts[0] == "embed" || parts[0] == "shorts")) id = parts[1];
                }
            }
            if (id == null || !Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{11}\z"))
                throw new ArgumentException("The YouTube link does not contain a valid video ID.");
            return id;
        }

        private void OnEnable()
        {
            if (controller == null) return;
            controller.StepChanged += OnStep;
            controller.RepeatRequested += OnStep;
            controller.StateChanged += OnState;
        }

        private IEnumerator Start()
        {
            if (controller == null || browser == null || playerHtml == null)
            {
                Report("Missing YouTube player references");
                yield break;
            }
            try { ParseVideoId(youtubeUrl); }
            catch (ArgumentException exception) { Report(exception.Message); yield break; }
            if (Application.platform != RuntimePlatform.Android)
            {
                Report("Embedded browser requires an Android/Quest build");
                yield break;
            }
            browser.Init();
            deadline = Time.realtimeSinceStartup + 30f;
            while (browser.state != FragmentCapture.State.Initialized && Time.realtimeSinceStartup < deadline) yield return null;
            if (browser.state != FragmentCapture.State.Initialized) { Report("Browser initialization timed out"); yield break; }
            browser.eventCallback.onPageFinish.AddListener(OnPageFinished);
            LoadPage();
        }

        public void SetTutorialUrl(string url)
        {
            ParseVideoId(url);
            youtubeUrl = url;
            if (browser != null && browser.state == FragmentCapture.State.Initialized) LoadPage();
        }

        private void LoadPage()
        {
            ready = false;
            hadHelp = false;
            // Google recommends the app ID as HTTPS base URL for bundled player HTML.
            string origin = "https://" + Application.identifier.ToLowerInvariant();
            string html = playerHtml.text.Replace("__VIDEO_ID__", ParseVideoId(youtubeUrl))
                .Replace("__ORIGIN__", origin).Replace("__UNITY_OBJECT__", gameObject.name);
            browser.LoadBundledHtml(html, origin + "/");
            loaded = true;
            deadline = Time.realtimeSinceStartup + 45f;
            Report("Loading YouTube");
        }

        private void Update()
        {
            if (browser == null) return;
            browser.UpdateFrame();
            browser.DispatchMessageQueue();
            FragmentCapture.GarbageCollect();
            if (loaded && !ready && Time.realtimeSinceStartup > deadline)
            {
                Report("YouTube did not become ready. Check Wi-Fi and the player display.");
                loaded = false;
            }
            // Left Y taps the visible player center if YouTube asks for a user gesture.
            var left = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            bool tap = left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool pressed) && pressed;
            if (tap && !tapHeld && browser.IsInitialized())
            {
                long downTime = browser.TouchEvent(browser.viewSize.x / 2, browser.viewSize.y / 2, 0, 0);
                browser.TouchEvent(browser.viewSize.x / 2, browser.viewSize.y / 2, 1, downTime);
            }
            tapHeld = tap;
        }

        [Preserve]
        public void OnYouTubeEvent(string message)
        {
            Report(message);
            if (message != "ready") return;
            ready = true;
            ApplyCurrentStep();
        }

        private void OnStep(ProcedureStep step, int number) => ApplyCurrentStep();

        private void OnPageFinished(string url)
        {
            Report("Browser page loaded");
            browser.EvaluateJS("if(window.tlab)window.tlab.unitySendMessage('" + gameObject.name +
                "','OnYouTubeEvent','page scripts active; iframe API: '+typeof window.YT);");
        }

        private void ApplyCurrentStep()
        {
            if (!ready || !controller.IsReady) return;
            var step = controller.CurrentStep;
            hadHelp = false;
            browser.EvaluateJS("window.copilot.segment(" +
                step.VideoStartSeconds.ToString(CultureInfo.InvariantCulture) + "," +
                step.VideoEndSeconds.ToString(CultureInfo.InvariantCulture) + "," +
                (controller.State == ProcedurePlaybackState.Playing ? "true" : "false") + ");");
        }

        private void OnState(ProcedurePlaybackState state)
        {
            if (!ready) return;
            if (state == ProcedurePlaybackState.Help)
            {
                hadHelp = true;
                browser.EvaluateJS("window.copilot.pause();");
            }
            else
            {
                browser.EvaluateJS(state == ProcedurePlaybackState.Playing
                    ? "window.copilot.resume();" : "window.copilot.pause();");
                hadHelp = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!ready) return;
            browser.EvaluateJS(paused || hadHelp || controller.State != ProcedurePlaybackState.Playing
                ? "window.copilot.pause();" : "window.copilot.resume();");
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.StepChanged -= OnStep;
                controller.RepeatRequested -= OnStep;
                controller.StateChanged -= OnState;
            }
            if (ready && browser != null) browser.EvaluateJS("window.copilot.pause();");
            if (browser != null && browser.eventCallback != null)
                browser.eventCallback.onPageFinish.RemoveListener(OnPageFinished);
        }

        private void Report(string message)
        {
            Status = message;
            Debug.Log("[YouTube] " + message, this);
        }
    }
}
