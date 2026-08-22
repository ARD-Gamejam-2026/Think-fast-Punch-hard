using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Quiz.EditorTools
{
    /// <summary>
    /// One-time generator for the quiz panel prefab. After generation the
    /// prefab is a normal asset — restyle it in the editor, not here.
    /// Re-running overwrites the prefab.
    /// </summary>
    public static class QuizPanelBuilder
    {
        public const string PrefabPath = "Assets/Quiz/QuizPanel.prefab";

        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.20f, 0.95f);
        private static readonly Color LetterBoxColor = new Color(0.16f, 0.16f, 0.40f);
        private static readonly Color TimerBackColor = new Color(0.05f, 0.05f, 0.12f);
        private static readonly Color TimerFillColor = new Color(0.95f, 0.75f, 0.10f);
        private static readonly Color ImageFrameColor = new Color(0.55f, 0.45f, 0.05f);

        [MenuItem("Tools/Quiz/Create Quiz Panel")]
        public static void CreateQuizPanel()
        {
            EnsureTmpEssentials();
            EnsureFolder("Assets/Quiz");

            var root = new GameObject("QuizPanel",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                BuildHierarchy(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Quiz panel prefab saved to {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildHierarchy(GameObject root)
        {
            var container = CreateUIObject("Container", root.transform);
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = new Vector2(640, 0);
            container.anchoredPosition = Vector2.zero;
            container.gameObject.AddComponent<Image>().color = PanelColor;

            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var questionLabel = CreateText("QuestionLabel", container, "Question?", 40, FontStyles.Bold);
            questionLabel.alignment = TextAlignmentOptions.Center;
            SetLayout(questionLabel.gameObject, minHeight: 60);

            var imagePanel = CreateUIObject("ImagePanel", container);
            imagePanel.gameObject.AddComponent<Image>().color = ImageFrameColor;
            SetLayout(imagePanel.gameObject, preferredHeight: 320);

            var questionImageRect = CreateUIObject("QuestionImage", imagePanel);
            Stretch(questionImageRect, margin: 12);
            var questionImage = questionImageRect.gameObject.AddComponent<Image>();
            questionImage.preserveAspect = true;

            var timerBar = CreateUIObject("TimerBar", container);
            timerBar.gameObject.AddComponent<Image>().color = TimerBackColor;
            SetLayout(timerBar.gameObject, minHeight: 20);

            var timerFillRect = CreateUIObject("TimerFill", timerBar);
            Stretch(timerFillRect, margin: 3);
            var timerFill = timerFillRect.gameObject.AddComponent<Image>();
            timerFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerFill.fillAmount = 1f;
            timerFill.color = TimerFillColor;

            var answerButtons = new AnswerButton[QuizQuestion.AnswerCount];
            for (int i = 0; i < answerButtons.Length; i++)
                answerButtons[i] = CreateAnswerButton(container, i);

            var view = root.AddComponent<QuizView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("questionLabel").objectReferenceValue = questionLabel;
            viewSo.FindProperty("imagePanel").objectReferenceValue = imagePanel.gameObject;
            viewSo.FindProperty("questionImage").objectReferenceValue = questionImage;
            viewSo.FindProperty("timerFill").objectReferenceValue = timerFill;
            var buttonsProperty = viewSo.FindProperty("answerButtons");
            buttonsProperty.arraySize = answerButtons.Length;
            for (int i = 0; i < answerButtons.Length; i++)
                buttonsProperty.GetArrayElementAtIndex(i).objectReferenceValue = answerButtons[i];
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var controller = root.AddComponent<QuizController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("view").objectReferenceValue = view;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnswerButton CreateAnswerButton(RectTransform parent, int index)
        {
            string letter = ((char)('A' + index)).ToString();

            var buttonRect = CreateUIObject($"AnswerButton{letter}", parent);
            SetLayout(buttonRect.gameObject, minHeight: 72);

            var background = buttonRect.gameObject.AddComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;

            var button = buttonRect.gameObject.AddComponent<Button>();
            // AnswerButton drives the background color directly; a color-tint
            // transition would fight it, so disable Selectable transitions.
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;

            var rowLayout = buttonRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 16, 8, 8);
            rowLayout.spacing = 16;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            var letterBox = CreateUIObject("LetterBox", buttonRect);
            letterBox.gameObject.AddComponent<Image>().color = LetterBoxColor;
            SetLayout(letterBox.gameObject, minWidth: 56, minHeight: 56);

            var letterLabel = CreateText("LetterLabel", letterBox, letter, 32, FontStyles.Bold);
            letterLabel.alignment = TextAlignmentOptions.Center;
            Stretch(letterLabel.rectTransform, margin: 0);

            var answerLabel = CreateText("AnswerLabel", buttonRect, "Answer", 30, FontStyles.Normal);
            answerLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var answerLayout = answerLabel.gameObject.AddComponent<LayoutElement>();
            answerLayout.flexibleWidth = 1f;

            var answerButton = buttonRect.gameObject.AddComponent<AnswerButton>();
            var so = new SerializedObject(answerButton);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("letterLabel").objectReferenceValue = letterLabel;
            so.FindProperty("answerLabel").objectReferenceValue = answerLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            answerButton.SetVisualState(AnswerButton.VisualState.Normal);
            return answerButton;
        }

        private static RectTransform CreateUIObject(string name, Component parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent.transform, worldPositionStays: false);
            return rect;
        }

        private static TMP_Text CreateText(
            string name, Component parent, string text, float size, FontStyles style)
        {
            var rect = CreateUIObject(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = Color.white;
            if (label.font == null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetLayout(GameObject go,
            float minWidth = -1, float minHeight = -1, float preferredHeight = -1)
        {
            var element = go.AddComponent<LayoutElement>();
            element.minWidth = minWidth;
            element.minHeight = minHeight;
            element.preferredHeight = preferredHeight;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string leaf = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        private static void EnsureTmpEssentials()
        {
            if (TmpEssentialsPresent())
                return;

            AssetDatabase.ImportPackage(TmpEssentialsPackage, interactive: false);
            AssetDatabase.Refresh();
        }

        private const string TmpEssentialsPackage =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        private static bool TmpEssentialsPresent()
        {
            return AssetDatabase.LoadAssetAtPath<Object>(
                "Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;
        }

        /// <summary>
        /// Batch-mode entry point: AssetDatabase.ImportPackage is asynchronous,
        /// so a -quit session exits before the import lands. Run this WITHOUT
        /// -quit; it exits the editor itself once the import completes.
        /// </summary>
        public static void ImportTmpEssentialsBatch()
        {
            if (TmpEssentialsPresent())
            {
                Debug.Log("TMP essentials already present.");
                EditorApplication.Exit(0);
                return;
            }

            AssetDatabase.importPackageCompleted += packageName =>
            {
                Debug.Log($"TMP essentials imported: {packageName}");
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (packageName, error) =>
            {
                Debug.LogError($"TMP essentials import failed: {error}");
                EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(TmpEssentialsPackage, interactive: false);
        }
    }
}
