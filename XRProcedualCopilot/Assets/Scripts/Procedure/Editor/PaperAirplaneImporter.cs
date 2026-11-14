using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XRProceduralCopilot.Procedure.Editor
{
    /// <summary>Converts the team's reviewed JSON into the controller's authored asset format.</summary>
    public static class PaperAirplaneImporter
    {
        [Serializable] private sealed class Tutorial
        {
            public string title;
            public Step[] steps;
        }

        [Serializable] private sealed class Step
        {
            public string title;
            public string instruction;
            public double start_time;
            public double end_time;
        }

        [MenuItem("Tools/XR Procedural Copilot/Import Paper Airplane Tutorial")]
        public static void Import()
        {
            const string folder = "Assets/Role3_TutorialData";
            const string assetPath = folder + "/PaperAirplane.asset";
            var data = JsonUtility.FromJson<Tutorial>(File.ReadAllText(folder + "/paper_airplane.json"));
            if (data == null || data.steps == null || data.steps.Length == 0)
                throw new InvalidDataException("Tutorial JSON has no steps.");

            // Validate a temporary asset before touching an existing authored asset.
            var candidate = ScriptableObject.CreateInstance<ProcedureDefinition>();
            var serialized = new SerializedObject(candidate);
            serialized.FindProperty("procedureTitle").stringValue = data.title;
            var steps = serialized.FindProperty("steps");
            steps.arraySize = data.steps.Length;
            for (int i = 0; i < data.steps.Length; i++)
            {
                var source = data.steps[i];
                if (source == null) throw new InvalidDataException($"Tutorial step {i + 1} is empty.");
                var target = steps.GetArrayElementAtIndex(i);
                target.FindPropertyRelative("title").stringValue = source.title;
                target.FindPropertyRelative("instruction").stringValue = source.instruction;
                target.FindPropertyRelative("videoStartSeconds").doubleValue = source.start_time;
                target.FindPropertyRelative("videoEndSeconds").doubleValue = source.end_time;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!candidate.TryValidate(out string error))
            {
                UnityEngine.Object.DestroyImmediate(candidate);
                throw new InvalidDataException(error);
            }
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
