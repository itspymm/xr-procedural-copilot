using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XRProceduralCopilot.Procedure
{
    /// <summary>Displays controller state and provides temporary controller/keyboard inputs.</summary>
    public sealed class ProcedurePanel : MonoBehaviour
    {
        [SerializeField] private ProcedureController controller;
        [SerializeField] private TextAsset tutorialJson;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text stepCounter;
        private TMP_FontAsset interfaceFont;
        private TMP_Text heading, questionLabel, footer, brand;
        private Image progressFill;
        private bool clarification;
        private int visiblePage = 1;
        private float nextPageInput;
        private bool nextHeld, backHeld, repeatHeld;

        private void OnEnable()
        {
            if (controller == null) return;
            controller.StepChanged += DisplayStep;
            controller.RepeatRequested += DisplayStep;
            nextHeld = backHeld = repeatHeld = true;
        }

        private void Start()
        {
            if (controller == null || instructionText == null || stepCounter == null)
            {
                Debug.LogError("ProcedurePanel needs its controller and both text references.", this);
                enabled = false;
                return;
            }
            ConfigureLayout();
            if (tutorialJson != null && !controller.TryLoadJson(tutorialJson, out string error))
            {
                instructionText.text = "Unable to load this tutorial.";
                stepCounter.text = "TUTORIAL ERROR";
                Debug.LogError(error, this);
                enabled = false;
                return;
            }
            controller.BeginProcedure();
            DisplayStep(controller.CurrentStep, controller.CurrentStepNumber);
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.StepChanged -= DisplayStep;
            controller.RepeatRequested -= DisplayStep;
        }

        private void DisplayStep(ProcedureStep step, int stepNumber)
        {
            if (step == null || instructionText == null || stepCounter == null) return;
            clarification = false;
            instructionText.text = step.Instruction;
            instructionText.pageToDisplay = visiblePage = 1;
            stepCounter.text = $"STEP {stepNumber} / {controller.TotalSteps}";
            if (heading != null) heading.text = step.Title;
            if (questionLabel != null) questionLabel.gameObject.SetActive(false);
            if (brand != null) brand.text = "COPILOT  /  GUIDED TUTORIAL";
            Position(instructionText.rectTransform, 32, 120, 696, 258);
            if (progressFill != null) progressFill.fillAmount = controller.Progress;
            RefreshPagination();
        }

        public void ShowClarification(string text, string question = null)
        {
            if (instructionText == null) return;
            clarification = true;
            instructionText.text = text;
            instructionText.pageToDisplay = visiblePage = 1;
            if (heading != null) heading.text = "Let's make it clear.";
            if (brand != null) brand.text = "COPILOT  /  ASK ABOUT THIS STEP";
            bool hasQuestion = !string.IsNullOrEmpty(question);
            if (questionLabel != null)
            {
                questionLabel.gameObject.SetActive(hasQuestion);
                questionLabel.text = "YOU ASKED  /  " + question;
            }
            Position(instructionText.rectTransform, 32, hasQuestion ? 174 : 120, 696, hasQuestion ? 204 : 258);
            RefreshPagination();
        }

        public bool ChangePage(int direction)
        {
            instructionText.ForceMeshUpdate();
            int pages = Mathf.Max(1, instructionText.textInfo.pageCount);
            int page = Mathf.Clamp(visiblePage + direction, 1, pages);
            if (page == visiblePage) return false;
            instructionText.pageToDisplay = visiblePage = page;
            RefreshPagination();
            return true;
        }

        private void RefreshPagination()
        {
            if (footer == null) return;
            instructionText.ForceMeshUpdate();
            int pages = Mathf.Max(1, instructionText.textInfo.pageCount);
            string hint = clarification ? "Say ‘continue tutorial’ to return" : "A Next   /   B Back   /   X Repeat   /   Say ‘help’";
            footer.text = pages > 1
                ? $"{visiblePage} / {pages}   ·   Stick ↑↓ / ‘read more’" + (clarification ? "   ·   ‘continue tutorial’ exits" : "")
                : hint;
        }

        public void RestoreInstruction() => DisplayStep(controller.CurrentStep, controller.CurrentStepNumber);


        private void ConfigureLayout()
        {
            // Load at runtime without changing the serialized layout of existing scene components.
            interfaceFont = Resources.Load<TMP_FontAsset>("Copilot/AtkinsonHyperlegible SDF");
            RectTransform root = instructionText.rectTransform.parent as RectTransform;
            if (root == null) return;
            root.sizeDelta = new Vector2(760, 480);
            // Preserve the existing distance and head-relative presentation.
            root.anchoredPosition = new Vector2(0, 0.19f);
            var background = root.Find("BackgroundPanel")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.035f, 0.065f, 0.10f, 0.97f);
                background.raycastTarget = false;
                background.sprite = RoundedCard();
                background.type = Image.Type.Sliced;
            }
            Style(instructionText, 28, new Color(0.93f, 0.96f, 0.99f));
            instructionText.overflowMode = TextOverflowModes.Page;
            instructionText.lineSpacing = 8;
            instructionText.paragraphSpacing = 12;
            Style(stepCounter, 17, new Color(0.46f, 0.87f, 0.76f));
            stepCounter.alignment = TextAlignmentOptions.TopRight;
            Position(stepCounter.rectTransform, 550, 28, 178, 28);
            brand = Label(root, "CopilotBrand", 17, new Color(0.46f, 0.87f, 0.76f), 32, 28, 510, 28);
            brand.characterSpacing = 1.2f;
            heading = Label(root, "StepHeading", 30, Color.white, 32, 68, 696, 42);
            heading.fontStyle = FontStyles.Bold;
            questionLabel = Label(root, "Question", 20, new Color(0.62f, 0.73f, 0.83f), 32, 116, 696, 52);
            footer = Label(root, "ControlHints", 18, new Color(0.62f, 0.73f, 0.83f), 32, 447, 696, 27);
            var status = root.Find("VoiceStatus")?.GetComponent<TMP_Text>();
            if (status != null)
            {
                Style(status, 20, new Color(0.46f, 0.87f, 0.76f));
                Position(status.rectTransform, 32, 414, 696, 28);
            }
            var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(root, false);
            Position(track.GetComponent<RectTransform>(), 32, 396, 696, 3);
            track.GetComponent<Image>().color = new Color(0.17f, 0.24f, 0.30f);
            track.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Progress", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var rect = fill.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            progressFill = fill.GetComponent<Image>();
            progressFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.color = new Color(0.46f, 0.87f, 0.76f);
            progressFill.raycastTarget = false;
        }

        private TMP_Text Label(RectTransform parent, string name, float size, Color color,
            float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            Style(text, size, color);
            Position(text.rectTransform, x, y, width, height);
            return text;
        }

        private void Style(TMP_Text text, float size, Color color)
        {
            if (interfaceFont != null) text.font = interfaceFont;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = color;
            text.richText = false;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
        }

        private static void Position(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private Sprite cardSprite;
        private Texture2D cardTexture;
        private Sprite RoundedCard()
        {
            cardTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(16 - x, x - 47);
                    float dy = Mathf.Max(16 - y, y - 47);
                    float distance = new Vector2(Mathf.Max(0, dx), Mathf.Max(0, dy)).magnitude;
                    cardTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(16 - distance)));
                }
            cardTexture.Apply();
            cardSprite = Sprite.Create(cardTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 100, 0,
                SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
            return cardSprite;
        }

        private void OnDestroy()
        {
            Release(cardSprite);
            Release(cardTexture);
            if (progressFill != null) Release(progressFill.sprite);
        }

        private static void Release(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }

        private void Update()
        {
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 stick)
                && Mathf.Abs(stick.y) > 0.65f && Time.unscaledTime >= nextPageInput)
            {
                ChangePage(stick.y < 0 ? 1 : -1);
                nextPageInput = Time.unscaledTime + 0.35f;
            }
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool next);
            right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool back);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool repeat);
            bool nextPressed = next && !nextHeld;
            bool backPressed = back && !backHeld;
            bool repeatPressed = repeat && !repeatHeld;
            nextHeld = next;
            backHeld = back;
            repeatHeld = repeat;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                nextPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame;
                backPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame;
                repeatPressed |= Keyboard.current.rKey.wasPressedThisFrame;
            }
#endif
            if (nextPressed) controller.NextStep();
            else if (backPressed) controller.PreviousStep();
            else if (repeatPressed) controller.RepeatStep();
        }
    }
}
