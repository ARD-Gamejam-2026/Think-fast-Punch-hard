using ThinkFast.Quiz;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThinkFast.Quiz.EditorTools
{
    /// <summary>Editor commands that add per-answer shape icons to the quiz panel.</summary>
    public static class QuizPanelSequenceSetup
    {
        private const string PrefabPath = "Assets/Quiz/QuizPanel.prefab";

        /// <summary>Adds a disabled AnswerIcon image to each answer button in the panel prefab.</summary>
        [MenuItem("Tools/Quiz/Add Answer Icons To Panel")]
        public static void AddAnswerIcons()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var button in root.GetComponentsInChildren<AnswerButton>(true))
                {
                    EnsureAnswerIcon(button);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Creates and wires an AnswerIcon image on the button when absent.</summary>
        public static void EnsureAnswerIcon(AnswerButton button)
        {
            var serialized = new SerializedObject(button);
            SerializedProperty iconProperty = serialized.FindProperty("answerIcon");
            if (iconProperty.objectReferenceValue != null)
            {
                return;
            }

            var iconObject = new GameObject("AnswerIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.1f);
            rect.anchorMax = new Vector2(0.8f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = iconObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;

            iconProperty.objectReferenceValue = image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Enables the sequence bucket on the sample scene's QuizFlow.</summary>
        [MenuItem("Tools/Quiz/Add Sequence Questions To Sample Scene")]
        public static void AddSequenceQuestionsToSampleScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/Scenes/SampleScene.unity");
            QuizFlow flow = Object.FindAnyObjectByType<QuizFlow>();
            if (flow == null)
            {
                Debug.LogWarning("No QuizFlow in SampleScene; run Add Quiz Flow To Sample Scene first.");
                return;
            }
            var serialized = new SerializedObject(flow);
            serialized.FindProperty("sequenceWeight").floatValue = 2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
    }
}
