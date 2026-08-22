using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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

        private const string QuestionsFolder = "Assets/Quiz/Questions";
        private const string SampleQuestionPath = QuestionsFolder + "/SampleQuestion.asset";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Quiz/Add Quiz Panel To Sample Scene")]
        public static void AddQuizPanelToSampleScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"No prefab at {PrefabPath} — run Tools/Quiz/Create Quiz Panel first.");
                return;
            }

            // Open the scene before touching the question asset: switching
            // scenes unloads assets nothing references yet, which would turn
            // an already-loaded question into a dead object.
            var scene = EditorSceneManager.OpenScene(ScenePath);

            EnsureFolder("Assets/Quiz");
            EnsureFolder(QuestionsFolder);

            var question = AssetDatabase.LoadAssetAtPath<QuizQuestion>(SampleQuestionPath);
            if (question == null)
            {
                question = ScriptableObject.CreateInstance<QuizQuestion>();
                question.questionText = "Who is behind the box?";
                question.answers = new[] { "Bat Man", "Supper Man", "Pac Man", "Pack Man" };
                question.correctIndex = 3;
                question.timeLimitSeconds = 10f;
                AssetDatabase.CreateAsset(question, SampleQuestionPath);
            }

            var existingController = Object.FindFirstObjectByType<QuizController>();
            var controller = existingController != null
                ? existingController
                : ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<QuizController>();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("startingQuestion").objectReferenceValue = question;
            controllerSo.ApplyModifiedProperties();
            // Edits to a prefab instance made from batch code are not always
            // registered as overrides automatically; record them explicitly
            // or the scene save drops them.
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Quiz panel added to SampleScene with sample question.");
        }

        private const string PacManSpritePath = "Assets/Quiz/PacMan.png";
        private const string SecondQuestionPath = QuestionsFolder + "/SampleQuestion2.asset";

        [MenuItem("Tools/Quiz/Add Quiz Flow To Sample Scene")]
        public static void AddQuizFlowToSampleScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            var controller = Object.FindFirstObjectByType<QuizController>();
            if (controller == null)
            {
                Debug.LogError("No QuizController in SampleScene — run Tools/Quiz/Add Quiz Panel To Sample Scene first.");
                return;
            }

            var firstQuestion = AssetDatabase.LoadAssetAtPath<QuizQuestion>(SampleQuestionPath);
            if (firstQuestion == null)
            {
                Debug.LogError($"No question at {SampleQuestionPath} — run Tools/Quiz/Add Quiz Panel To Sample Scene first.");
                return;
            }

            var secondQuestion = AssetDatabase.LoadAssetAtPath<QuizQuestion>(SecondQuestionPath);
            if (secondQuestion == null)
            {
                secondQuestion = ScriptableObject.CreateInstance<QuizQuestion>();
                secondQuestion.questionText = "Think fast! 2 + 2 × 2 = ?";
                secondQuestion.answers = new[] { "8", "6", "4", "22" };
                secondQuestion.correctIndex = 1;
                secondQuestion.timeLimitSeconds = 5f;
                AssetDatabase.CreateAsset(secondQuestion, SecondQuestionPath);
            }

            var flow = controller.GetComponent<QuizFlow>();
            if (flow == null)
                flow = controller.gameObject.AddComponent<QuizFlow>();

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("quiz").objectReferenceValue = controller;
            var questionsProperty = flowSo.FindProperty("questions");
            questionsProperty.arraySize = 2;
            questionsProperty.GetArrayElementAtIndex(0).objectReferenceValue = firstQuestion;
            questionsProperty.GetArrayElementAtIndex(1).objectReferenceValue = secondQuestion;
            flowSo.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(flow);

            // The flow owns which question starts; a startingQuestion on the
            // controller would show the same question a second time.
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("startingQuestion").objectReferenceValue = null;
            controllerSo.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("QuizFlow added to SampleScene with 2 questions, looping forever.");
        }

        [MenuItem("Tools/Quiz/Update Sample Question (Pac-Man image, 5s)")]
        public static void UpdateSampleQuestion()
        {
            var question = AssetDatabase.LoadAssetAtPath<QuizQuestion>(SampleQuestionPath);
            if (question == null)
            {
                Debug.LogError($"No question at {SampleQuestionPath} — run Tools/Quiz/Add Quiz Panel To Sample Scene first.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Sprite>(PacManSpritePath) == null)
            {
                var texture = GeneratePacManTexture(256);
                System.IO.File.WriteAllBytes(PacManSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(PacManSpritePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(PacManSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                // Setting textureType from code does not imply a sprite mode;
                // without Single there are no Sprite sub-assets to load.
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PacManSpritePath);
            if (sprite == null)
            {
                Debug.LogError($"No sprite at {PacManSpritePath} after import — check the texture import settings.");
                return;
            }

            question.image = sprite;
            question.timeLimitSeconds = 5f;
            EditorUtility.SetDirty(question);
            AssetDatabase.SaveAssets();
            Debug.Log($"Sample question updated: Pac-Man image, {question.timeLimitSeconds}s timer.");
        }

        private static Texture2D GeneratePacManTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            var yellow = new Color32(255, 220, 20, 255);
            var black = new Color32(20, 20, 20, 255);
            var clear = new Color32(0, 0, 0, 0);

            float center = (size - 1) / 2f;
            float radius = size * 0.48f;
            float mouthHalfAngle = 35f;
            // Eye sits above the mouth, slightly toward the facing side.
            var eyeCenter = new Vector2(center + radius * 0.25f, center + radius * 0.45f);
            float eyeRadius = radius * 0.12f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    bool insideBody = dx * dx + dy * dy <= radius * radius;
                    bool insideMouth = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) < mouthHalfAngle;
                    bool insideEye = (new Vector2(x, y) - eyeCenter).sqrMagnitude <= eyeRadius * eyeRadius;

                    pixels[y * size + x] = insideBody && !insideMouth
                        ? (insideEye ? black : yellow)
                        : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
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
