using System.IO;
using UnityEditor;
using UnityEngine;

namespace XRProceduralCopilot.Procedure.Editor
{
    /// <summary>Converts the team's reviewed JSON into the controller's authored asset format.</summary>
    public static class PaperAirplaneImporter
    {
        [MenuItem("Tools/XR Procedural Copilot/Import Paper Airplane Tutorial")]
        public static void Import()
        {
            const string folder = "Assets/Role3_TutorialData";
            const string assetPath = folder + "/PaperAirplane.asset";
            if (!ProcedureJsonLoader.TryCreateDefinition(
                    File.ReadAllText(folder + "/paper_airplane_transcript_generated.json"),
                    out ProcedureDefinition candidate, out string error))
                throw new InvalidDataException(error);
            var existing = AssetDatabase.LoadAssetAtPath<ProcedureDefinition>(assetPath);
            if (existing == null) AssetDatabase.CreateAsset(candidate, assetPath);
            else
            {
                Undo.RecordObject(existing, "Import paper airplane tutorial");
                EditorUtility.CopySerialized(candidate, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(candidate);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
