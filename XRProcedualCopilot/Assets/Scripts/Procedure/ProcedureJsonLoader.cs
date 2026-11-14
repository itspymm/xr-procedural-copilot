using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Maps Role 3 tutorial JSON into a fully validated runtime procedure.</summary>
    public static class ProcedureJsonLoader
    {
        [Serializable]
        private sealed class TutorialJson
        {
            public string tutorial_id;
            public string title;
            public string video_url;
            public TutorialStepJson[] steps;
        }

        [Serializable]
        private sealed class TutorialStepJson
        {
            public int step_number;
            public string title;
            public string instruction;
            public int start_time;
            public int end_time;
            public string additional_guidance;
        }

        private enum JsonValueKind
        {
            String,
            Array,
            Integer
        }

        private sealed class FieldRule
        {
            public readonly int ExpectedCount;
            public readonly JsonValueKind Kind;
            public int SeenCount;

            public FieldRule(int expectedCount, JsonValueKind kind)
            {
                ExpectedCount = expectedCount;
                Kind = kind;
            }
        }

        /// <summary>
        /// Parses JSON without changing controller state. The returned ScriptableObject is
        /// temporary runtime data; the controller takes ownership after a successful load.
        /// </summary>
        public static bool TryCreateDefinition(string json, out ProcedureDefinition definition, out string error)
        {
            definition = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Tutorial JSON is empty.";
                return false;
            }

            json = json.TrimStart('\uFEFF');

            TutorialJson data;
            try
            {
                data = JsonUtility.FromJson<TutorialJson>(json);
            }
            catch (Exception exception)
            {
                error = $"Tutorial JSON could not be parsed: {exception.Message}";
                return false;
            }

            if (data == null)
            {
                error = "Tutorial JSON must contain a top-level object.";
                return false;
            }

            int stepCount = data.steps == null ? 0 : data.steps.Length;
            if (!ValidateRootFields(json, out int stepsArrayStart, out error))
                return false;

            if (!TryExtractStepObjects(json, stepsArrayStart, out List<string> stepObjects, out error))
                return false;

            if (stepObjects.Count != stepCount)
            {
                error = $"The steps array contains {stepObjects.Count} object(s), but Unity parsed {stepCount} step(s).";
                return false;
            }

            if (stepCount == 0)
            {
                error = "Tutorial JSON must contain at least one step.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.tutorial_id))
            {
                error = "Field 'tutorial_id' must not be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.title))
            {
                error = "Field 'title' must not be empty.";
                return false;
            }

            // video_url is required by the schema but may be empty while the video is unavailable.
            if (data.video_url == null)
            {
                error = "Field 'video_url' must be a string (it may be empty).";
                return false;
            }

            var procedureSteps = new List<ProcedureStep>(stepCount);
            double previousEnd = 0d;
            for (int i = 0; i < stepCount; i++)
            {
                TutorialStepJson step = data.steps[i];
                if (!ValidateStepFields(stepObjects[i], i, out error))
                    return false;

                int expectedNumber = i + 1;
                if (step.step_number != expectedNumber)
                {
                    error = $"Step at position {expectedNumber} has step_number {step.step_number}; step numbers must be sequential starting at 1.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.title))
                {
                    error = $"Step {expectedNumber} needs a non-empty title.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(step.instruction))
                {
                    error = $"Step {expectedNumber} needs a non-empty instruction.";
                    return false;
                }

                if (step.additional_guidance == null)
                {
                    error = $"Step {expectedNumber} needs an 'additional_guidance' string (it may be empty).";
                    return false;
                }

                if (step.start_time < 0 || step.end_time <= step.start_time)
                {
                    error = $"Step {expectedNumber} must have a non-negative start_time and an end_time after it.";
                    return false;
                }

                if (i > 0 && step.start_time < previousEnd)
                {
                    error = $"Step {expectedNumber} overlaps or is out of order; its start_time must be at or after step {i}'s end_time.";
                    return false;
                }

                previousEnd = step.end_time;
                procedureSteps.Add(new ProcedureStep(
                    step.step_number,
                    step.title,
                    step.instruction,
                    step.additional_guidance,
                    step.start_time,
                    step.end_time));
            }

            ProcedureDefinition candidate = ScriptableObject.CreateInstance<ProcedureDefinition>();
            candidate.InitializeFromJson(data.tutorial_id, data.title, data.video_url, procedureSteps);
            if (!candidate.TryValidate(out error))
            {
                UnityEngine.Object.DestroyImmediate(candidate);
                definition = null;
                return false;
            }

            definition = candidate;
            error = null;
            return true;
        }

        private static bool ValidateRootFields(string json, out int stepsArrayStart, out string error)
        {
            var fields = new Dictionary<string, FieldRule>(StringComparer.Ordinal)
            {
                { "tutorial_id", new FieldRule(1, JsonValueKind.String) },
                { "title", new FieldRule(1, JsonValueKind.String) },
                { "video_url", new FieldRule(1, JsonValueKind.String) },
                { "steps", new FieldRule(1, JsonValueKind.Array) }
            };

            if (!ValidateDirectFields(json, fields, out stepsArrayStart, out error))
                return false;

            return true;
        }

        private static bool ValidateStepFields(string stepJson, int stepIndex, out string error)
        {
            var fields = new Dictionary<string, FieldRule>(StringComparer.Ordinal)
            {
                { "step_number", new FieldRule(1, JsonValueKind.Integer) },
                { "title", new FieldRule(1, JsonValueKind.String) },
                { "instruction", new FieldRule(1, JsonValueKind.String) },
                { "start_time", new FieldRule(1, JsonValueKind.Integer) },
                { "end_time", new FieldRule(1, JsonValueKind.Integer) },
                { "additional_guidance", new FieldRule(1, JsonValueKind.String) }
            };

            if (!ValidateDirectFields(stepJson, fields, out _, out error))
                error = $"Step {stepIndex + 1}: {error}";

            return error == null;
        }

        /// <summary>Checks required direct object keys and their JSON value types.</summary>
        private static bool ValidateDirectFields(string json, Dictionary<string, FieldRule> fields,
            out int stepsArrayStart, out string error)
        {
            stepsArrayStart = -1;
            error = null;
            int objectDepth = 0;
            int arrayDepth = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char current = json[i];
                if (current == '"')
                {
                    int stringEnd = FindStringEnd(json, i);
                    if (stringEnd < 0)
                    {
                        error = "Tutorial JSON contains an unterminated string.";
                        return false;
                    }

                    int colonIndex = SkipWhitespace(json, stringEnd + 1);
                    if (objectDepth == 1 && arrayDepth == 0
                        && colonIndex < json.Length && json[colonIndex] == ':')
                    {
                        string key = json.Substring(i + 1, stringEnd - i - 1);
                        if (fields.TryGetValue(key, out FieldRule rule))
                        {
                            rule.SeenCount++;
                            int valueStart = SkipWhitespace(json, colonIndex + 1);
                            if (valueStart >= json.Length || !HasExpectedType(json, valueStart, rule.Kind))
                            {
                                error = $"Field '{key}' has the wrong JSON value type.";
                                return false;
                            }

                            if (key == "steps")
                                stepsArrayStart = valueStart;
                        }
                    }

                    i = stringEnd;
                    continue;
                }

                if (current == '{') objectDepth++;
                else if (current == '}') objectDepth--;
                else if (current == '[') arrayDepth++;
                else if (current == ']') arrayDepth--;
            }

            foreach (KeyValuePair<string, FieldRule> field in fields)
            {
                if (field.Value.SeenCount != field.Value.ExpectedCount)
                {
                    error = $"Expected {field.Value.ExpectedCount} '{field.Key}' field(s), found {field.Value.SeenCount}.";
                    return false;
                }
            }

            return true;
        }

        private static bool HasExpectedType(string json, int valueStart, JsonValueKind kind)
        {
            switch (kind)
            {
                case JsonValueKind.String:
                    return json[valueStart] == '"';
                case JsonValueKind.Array:
                    return json[valueStart] == '[';
                case JsonValueKind.Integer:
                    return IsIntegerToken(json, valueStart);
                default:
                    return false;
            }
        }

        private static bool IsIntegerToken(string json, int start)
        {
            int index = start;
            if (index < json.Length && json[index] == '-')
                index++;

            int digitStart = index;
            while (index < json.Length && char.IsDigit(json[index]))
                index++;

            if (index == digitStart)
                return false;

            if (index < json.Length && !char.IsWhiteSpace(json[index])
                && json[index] != ',' && json[index] != '}' && json[index] != ']')
                return false;

            string token = json.Substring(start, index - start);
            return int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
        }

        private static bool TryExtractStepObjects(string json, int arrayStart, out List<string> stepObjects, out string error)
        {
            stepObjects = new List<string>();
            error = null;
            if (arrayStart < 0 || arrayStart >= json.Length || json[arrayStart] != '[')
            {
                error = "Field 'steps' must be an array.";
                return false;
            }

            int index = SkipWhitespace(json, arrayStart + 1);
            if (index < json.Length && json[index] == ']')
                return true;

            while (index < json.Length)
            {
                if (json[index] != '{')
                {
                    error = $"Step {stepObjects.Count + 1} must be a JSON object.";
                    return false;
                }

                int objectEnd = FindObjectEnd(json, index);
                if (objectEnd < 0)
                {
                    error = $"Step {stepObjects.Count + 1} is not a complete JSON object.";
                    return false;
                }

                stepObjects.Add(json.Substring(index, objectEnd - index + 1));
                index = SkipWhitespace(json, objectEnd + 1);

                if (index < json.Length && json[index] == ',')
                {
                    index = SkipWhitespace(json, index + 1);
                    continue;
                }

                if (index < json.Length && json[index] == ']')
                    return true;

                error = "The steps array has invalid separators.";
                return false;
            }

            error = "The steps array is not terminated.";
            return false;
        }

        private static int FindObjectEnd(string json, int objectStart)
        {
            int objectDepth = 0;
            for (int i = objectStart; i < json.Length; i++)
            {
                if (json[i] == '"')
                {
                    int stringEnd = FindStringEnd(json, i);
                    if (stringEnd < 0)
                        return -1;
                    i = stringEnd;
                }
                else if (json[i] == '{')
                {
                    objectDepth++;
                }
                else if (json[i] == '}' && --objectDepth == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindStringEnd(string json, int quoteStart)
        {
            for (int i = quoteStart + 1; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (json[i] == '"')
                    return i;
            }

            return -1;
        }

        private static int SkipWhitespace(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            return index;
        }
    }
}
