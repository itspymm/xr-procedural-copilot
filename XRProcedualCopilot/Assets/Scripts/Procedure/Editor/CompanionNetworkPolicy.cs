using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace XRProceduralCopilot.Procedure.Editor
{
    /// <summary>Only development builds allow unencrypted traffic, only to loopback.</summary>
    public sealed class CompanionNetworkPolicy : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        private const string DevelopmentBuildKey = "Copilot.CompanionDevelopmentBuild";
        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report)
        {
            // SessionState survives the domain reload between preprocessing and Gradle.
            UnityEditor.SessionState.SetBool(DevelopmentBuildKey,
                (report.summary.options & UnityEditor.BuildOptions.Development) != 0);
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!UnityEditor.SessionState.GetBool(DevelopmentBuildKey, false)) return;
            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            var manifest = new XmlDocument();
            manifest.Load(manifestPath);
            var application = (XmlElement)manifest.SelectSingleNode("/manifest/application");
            const string android = "http://schemas.android.com/apk/res/android";
            application.SetAttribute("networkSecurityConfig", android, "@xml/copilot_network_security");
            application.SetAttribute("usesCleartextTraffic", android, "false");
            manifest.Save(manifestPath);
            string resources = Path.Combine(path, "src/main/res/xml");
            Directory.CreateDirectory(resources);
            File.WriteAllText(Path.Combine(resources, "copilot_network_security.xml"),
                "<network-security-config><base-config cleartextTrafficPermitted=\"false\"/>" +
                "<domain-config cleartextTrafficPermitted=\"true\"><domain>127.0.0.1</domain>" +
                "<domain>localhost</domain></domain-config></network-security-config>");
        }
    }
}
