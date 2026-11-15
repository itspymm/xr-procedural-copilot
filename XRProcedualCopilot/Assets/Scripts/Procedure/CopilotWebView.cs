using TLab.WebView;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Compatibility adapter for the pinned TLab Android library.</summary>
    public sealed class CopilotWebView : WebView
    {
        public void LoadBundledHtml(string html, string origin)
        {
            if (state != State.Initialized)
                throw new System.InvalidOperationException("Browser is not initialized.");
#if UNITY_ANDROID && !UNITY_EDITOR
            // The pinned AAR exports LoadHtml; its C# LoadHTML wrapper calls LoadHTML.
            m_NativePlugin.Call("LoadHtml", html, origin);
#endif
        }
    }
}
