using System.Collections.Generic;
using ThinkFast.CameraRig;
using ThinkFast.Economy;
using ThinkFast.Quiz;
using ThinkFast.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThinkFast.UIEditor
{
    /// <summary>
    /// Puts the two halves of the game on screen together: the fight in the left
    /// viewport, the quiz in the right one, and the bridge that turns a solved
    /// question into Action Points and Flow.
    ///
    /// Generated rather than hand-placed, for the same reason the test rig and
    /// the HUD are: re-runnable, reviewable as code, and no scene YAML edited by
    /// hand. Everything it makes lives under two named roots that are separate
    /// from the fighter rig's, so rebuilding the rig does not remove the quiz and
    /// rebuilding the quiz does not remove the stage.
    /// </summary>
    public static class SplitScreenFightBuilder
    {
        private const string ScenePath = "Assets/Scenes/PlayerControllerTest.unity";
        private const string QuizPrefabPath = "Assets/Quiz/QuizPanel.prefab";
        private const string QuestionsFolder = "Assets/Quiz/Questions";

        private const string QuizRootName = "--- Quiz (generated) ---";
        private const string SplitRootName = "--- Split Screen (generated) ---";

        private const string PanelContainerName = "Container";

        private static readonly Color BackdropColour = new Color(0.04f, 0.04f, 0.10f, 1f);
        private static readonly Color SeamColour = new Color(0.16f, 0.16f, 0.40f, 1f);

        [MenuItem("Tools/Think Fast/Build Split Screen Fight")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuizPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"No quiz panel at {QuizPrefabPath}. Run Tools > Quiz > Create Quiz Panel first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExisting(scene);

            QuizController quiz = BuildQuizHalf(prefab);
            SplitScreenLayout layout = BuildSplitScreen(quiz);
            EnsureEventSystem();
            StandDownDebugRiddleDriver();

            // Applied here as well as at runtime, so the Game view shows the real
            // layout without entering Play mode.
            layout.Apply();
            RecordPanelOverride(quiz);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Built the split screen fight: fight on the left, quiz on the right, solves paying into AP and Flow. Press Play and answer a question inside the green zone to build Flow.");
        }

        private static void RemoveExisting(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == QuizRootName || go.name == SplitRootName)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        /// <summary>
        /// Instantiates the quiz panel and the two components that drive it: the
        /// endless question flow, and the bridge that pays solves into the
        /// fighter's economy.
        /// </summary>
        private static QuizController BuildQuizHalf(GameObject prefab)
        {
            var root = new GameObject(QuizRootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Split Screen Fight");

            var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            panel.transform.SetParent(root.transform, worldPositionStays: false);

            var quiz = panel.GetComponent<QuizController>();
            if (quiz == null)
            {
                Debug.LogError("The quiz panel prefab has no QuizController on its root.", panel);
                return null;
            }

            // The flow drives the controller from the moment the scene starts, so
            // a starting question would be shown twice.
            SetObjectField(quiz, "startingQuestion", null);
            PrefabUtility.RecordPrefabInstancePropertyModifications(quiz);

            // Both components go on the generated root rather than on the prefab
            // instance: added components on a prefab instance are overrides, and
            // neither of these needs to sit on the panel to do its job.
            AddQuizFlow(root, quiz);
            AddRewardBridge(root, quiz, panel.GetComponentInChildren<QuizView>());

            return quiz;
        }

        private static void AddQuizFlow(GameObject root, QuizController quiz)
        {
            var flow = root.AddComponent<QuizFlow>();

            var so = new SerializedObject(flow);
            so.FindProperty("quiz").objectReferenceValue = quiz;

            List<QuizQuestion> questions = LoadAuthoredQuestions();
            SerializedProperty list = so.FindProperty("questions");
            list.arraySize = questions.Count;
            for (int i = 0; i < questions.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = questions[i];
            }

            // Authored and generated maths only. Wikipedia sources need the
            // network and a moment to prefetch, so they are opt-in: add a
            // WikipediaQuestionSource and register it in this flow by hand.
            so.FindProperty("authoredWeight").floatValue = 1f;
            so.FindProperty("mathWeight").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRewardBridge(GameObject root, QuizController quiz, QuizView view)
        {
            var bridge = root.AddComponent<QuizRewardBridge>();

            var so = new SerializedObject(bridge);
            so.FindProperty("quiz").objectReferenceValue = quiz;
            so.FindProperty("view").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (view == null)
            {
                Debug.LogWarning("No QuizView found under the quiz panel, so the bridge falls back to its own fast-solve threshold.", bridge);
            }
        }

        private static List<QuizQuestion> LoadAuthoredQuestions()
        {
            var questions = new List<QuizQuestion>();

            if (!AssetDatabase.IsValidFolder(QuestionsFolder))
            {
                return questions;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:QuizQuestion", new[] { QuestionsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var question = AssetDatabase.LoadAssetAtPath<QuizQuestion>(path);
                if (question != null)
                {
                    questions.Add(question);
                }
            }

            return questions;
        }

        /// <summary>
        /// Builds the backdrop the quiz half needs and the layout component that
        /// owns the split. The backdrop is not decoration: a camera whose
        /// viewport covers half the screen never clears the other half, so
        /// something opaque has to be drawn there.
        /// </summary>
        private static SplitScreenLayout BuildSplitScreen(QuizController quiz)
        {
            var root = new GameObject(SplitRootName, typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(root, "Build Split Screen Fight");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Behind both the quiz panel and the fighter HUD, which sit at 0.
            // Deliberately no GraphicRaycaster: a full-height image over the quiz
            // half would otherwise swallow every answer click.
            canvas.sortingOrder = -100;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform backdrop = CreateImage(root.transform, "Quiz Backdrop", BackdropColour);
            RectTransform seam = CreateImage(root.transform, "Seam", SeamColour);

            var layout = root.AddComponent<SplitScreenLayout>();
            Wire(layout, backdrop, seam, FindQuizPanelContainer(quiz));

            return layout;
        }

        private static void Wire(SplitScreenLayout layout, RectTransform backdrop, RectTransform seam, RectTransform container)
        {
            var so = new SerializedObject(layout);
            so.FindProperty("fighterCamera").objectReferenceValue = FindFighterCamera();
            so.FindProperty("quizBackdrop").objectReferenceValue = backdrop;
            so.FindProperty("seam").objectReferenceValue = seam;
            so.FindProperty("quizPanel").objectReferenceValue = container;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateImage(Transform parent, string name, Color colour)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();

            image.transform.SetParent(parent, worldPositionStays: false);
            image.color = colour;
            image.sprite = null;
            image.raycastTarget = false;

            return image.rectTransform;
        }

        private static RectTransform FindQuizPanelContainer(QuizController quiz)
        {
            if (quiz == null)
            {
                return null;
            }

            Transform container = quiz.transform.Find(PanelContainerName);
            if (container == null)
            {
                Debug.LogWarning($"No '{PanelContainerName}' under the quiz panel, so the panel will not be moved into its half.", quiz);
                return null;
            }

            return container as RectTransform;
        }

        /// <summary>
        /// The camera the fight is framed by. Prefers the one already carrying a
        /// FollowCamera, since that is the one the rig builder set up.
        /// </summary>
        private static Camera FindFighterCamera()
        {
            var follow = Object.FindAnyObjectByType<FollowCamera>();
            if (follow != null)
            {
                return follow.GetComponent<Camera>();
            }

            return Object.FindAnyObjectByType<Camera>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            // The answer buttons are clicked with the mouse, which needs an event
            // system. The fighter half reads its own actions directly and is
            // unaffected by this.
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(events, "Build Split Screen Fight");
        }

        /// <summary>
        /// Switches off the debug stand-in for the quiz. Leaving it enabled would
        /// mean two things paying Flow into the same fighter, and its readout
        /// draws over the real HUD.
        /// </summary>
        private static void StandDownDebugRiddleDriver()
        {
            var driver = Object.FindAnyObjectByType<DebugRiddleDriver>();
            if (driver == null || !driver.enabled)
            {
                return;
            }

            driver.enabled = false;
            EditorUtility.SetDirty(driver);
            Debug.Log("Disabled DebugRiddleDriver: the real quiz drives the economy now. Re-tick it on the player to get the 1/2/3 solve keys back.", driver);
        }

        private static void RecordPanelOverride(QuizController quiz)
        {
            RectTransform container = FindQuizPanelContainer(quiz);
            if (container == null)
            {
                return;
            }

            // Laying the panel out moved a prefab instance's RectTransform.
            // Property changes made from batch code are not always registered as
            // overrides on their own, and the scene save would drop them.
            PrefabUtility.RecordPrefabInstancePropertyModifications(container);
        }

        private static void SetObjectField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
