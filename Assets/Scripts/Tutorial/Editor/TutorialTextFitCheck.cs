using System.Collections.Generic;
using ThinkFast.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThinkFast.TutorialEditor
{
    /// <summary>
    /// Measures every line of tutorial copy against the label it has to fit
    /// inside, using the real font at the real width.
    ///
    /// This exists because "the text does not fit" is the one UI fault that is
    /// invisible until someone plays the step where it happens -- step eight of
    /// ten, several minutes in. Re-wording a step is also the single most likely
    /// change anyone will make to this screen. Measuring is a second and catches
    /// it at the point of the edit.
    ///
    /// The card shrinks type that overruns rather than clipping it, so an
    /// overflow here is a legibility warning, not a broken build: it means that
    /// step will be read at a smaller size than the rest.
    /// </summary>
    public static class TutorialTextFitCheck
    {
        private const string ScenePath = "Assets/Scenes/Scene_Tutorial.unity";

        [MenuItem("Tools/Think Fast/Check Tutorial Text Fits")]
        public static void Check()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open {ScenePath}. Run Tools > Think Fast > Build Tutorial Scene first.");
                return;
            }

            var coach = Object.FindAnyObjectByType<TutorialCoach>(FindObjectsInactive.Include);
            if (coach == null)
            {
                Debug.LogError("No TutorialCoach in the tutorial scene, so there is nothing to measure against.");
                return;
            }

            TMP_Text body = FindLabel(coach.transform, "Card/Body");
            TMP_Text title = FindLabel(coach.transform, "Card/Title");
            TMP_Text objective = FindLabel(coach.transform, "Card/Objective/Label");

            if (body == null || title == null || objective == null)
            {
                Debug.LogError("The coach card is missing one of its labels, so the fit could not be measured.");
                return;
            }

            List<TutorialStepDefinition> steps = TutorialScript.Build();
            int tight = 0;

            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStepDefinition step = steps[i];
                string label = $"step {i + 1} ({step.Title})";

                tight += Measure(label, "title", title, step.Title);
                tight += Measure(label, "body", body, step.Body);
                tight += Measure(label, "objective", objective, step.Objective);
            }

            if (tight == 0)
            {
                Debug.Log($"Tutorial text fits: all {steps.Count} steps sit inside their labels at full size.");
                return;
            }

            Debug.LogWarning($"Tutorial text: {tight} label(s) overrun and will be auto-shrunk. See the warnings above.");
        }

        /// <summary>
        /// Returns 1 when the text needs more room than the label has.
        ///
        /// Measured at the label's authored size rather than its auto-size
        /// minimum, because the question is not "can this be squeezed in" -- the
        /// card can always squeeze it -- but "does this read at the size the rest
        /// of the tutorial reads at".
        /// </summary>
        private static int Measure(string step, string part, TMP_Text label, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            Rect rect = label.rectTransform.rect;

            float authoredSize = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
            float previousSize = label.fontSize;
            bool previousAutoSize = label.enableAutoSizing;

            label.enableAutoSizing = false;
            label.fontSize = authoredSize;

            Vector2 needed = label.GetPreferredValues(text, rect.width, 0f);

            label.enableAutoSizing = previousAutoSize;
            label.fontSize = previousSize;

            // A whisker of tolerance: TMP's preferred height rounds up past the
            // rect by a fraction on text that visually fits exactly.
            if (needed.y <= rect.height + 1f)
            {
                return 0;
            }

            // A label without auto-sizing has no way to recover: it overflows its
            // rect and is clipped. That is a different severity from shrinking,
            // and saying "it will shrink" about a label that cannot would send
            // the next reader looking in the wrong place.
            string consequence = previousAutoSize ? "so it will shrink" : "and CANNOT shrink, so it will be clipped";

            Debug.LogWarning(
                $"Tutorial {step}: {part} needs {needed.y:0}px of the {rect.height:0}px it has at {authoredSize:0}pt, {consequence}.");
            return 1;
        }

        private static TMP_Text FindLabel(Transform root, string path)
        {
            Transform found = root.Find(path);
            if (found == null)
            {
                return null;
            }

            return found.GetComponent<TMP_Text>();
        }
    }
}
