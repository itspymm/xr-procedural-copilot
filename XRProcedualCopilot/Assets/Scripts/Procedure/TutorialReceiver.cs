using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Loads an explicitly published tutorial from the USB-connected Mac.</summary>
    public sealed class TutorialReceiver : MonoBehaviour
    {
        [SerializeField] private ProcedureController controller;
        [SerializeField] private YouTubeProcedurePlayer player;
        [SerializeField] private string companionUrl = "http://127.0.0.1:8765";
        private string loadedRevision = "";
        private string acknowledgedRevision = "";
        private string lastError = "";

        [Serializable] private sealed class Envelope
        {
            public string revision;
            public string procedure_json;
        }
        [Serializable] private sealed class VideoLink { public string video_url; }
        [Serializable] private sealed class Acknowledgement { public string revision; }

        private IEnumerator Start()
        {
            if (controller == null || player == null)
            {
                Debug.LogError("TutorialReceiver needs the controller and YouTube player.", this);
                yield break;
            }
            // Let ProcedurePanel load its bundled fallback before accepting replacements.
            yield return null;
            while (enabled)
            {
                using (var request = UnityWebRequest.Get(companionUrl + "/api/active"))
                {
                    request.timeout = 4;
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Envelope envelope = null;
                        try { envelope = JsonUtility.FromJson<Envelope>(request.downloadHandler.text); }
                        catch (Exception exception) { ReportError(exception.Message); }
                        if (envelope != null && !string.IsNullOrEmpty(envelope.revision))
                        {
                            if (envelope.revision != loadedRevision && controller.State != ProcedurePlaybackState.Help)
                                Load(envelope);
                            if (envelope.revision == loadedRevision && loadedRevision != acknowledgedRevision)
                            {
                                using (var ack = new UnityWebRequest(companionUrl + "/api/ack", "POST"))
                                {
                                    ack.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(
                                        JsonUtility.ToJson(new Acknowledgement { revision = loadedRevision })));
                                    ack.downloadHandler = new DownloadHandlerBuffer();
                                    ack.SetRequestHeader("Content-Type", "application/json");
                                    ack.timeout = 4;
                                    yield return ack.SendWebRequest();
                                    if (ack.result == UnityWebRequest.Result.Success) acknowledgedRevision = loadedRevision;
                                }
                            }
                        }
                    }
                    else ReportError("Waiting for Mac companion: " + request.error);
                }
                yield return new WaitForSecondsRealtime(2f);
            }
        }

        private void Load(Envelope envelope)
        {
            try
            {
                var link = JsonUtility.FromJson<VideoLink>(envelope.procedure_json);
                YouTubeProcedurePlayer.ParseVideoId(link.video_url);
                if (!controller.TryLoadJson(envelope.procedure_json, out string error))
                    throw new ArgumentException(error);
                player.SetTutorialUrl(link.video_url);
                controller.BeginProcedure();
                loadedRevision = envelope.revision;
                lastError = "";
                Debug.Log("[TutorialReceiver] Loaded " + controller.TutorialTitle + " (" + controller.TotalSteps + " steps)", this);
            }
            catch (Exception exception) { ReportError("Tutorial rejected: " + exception.Message); }
        }

        private void ReportError(string message)
        {
            if (message == lastError) return;
            lastError = message;
            Debug.LogWarning("[TutorialReceiver] " + message, this);
        }
    }
}
