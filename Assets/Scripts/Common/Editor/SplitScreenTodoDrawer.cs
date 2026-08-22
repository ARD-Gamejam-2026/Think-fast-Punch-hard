using ThinkFast.Common;
using UnityEditor;
using UnityEngine;

namespace ThinkFast.CommonEditor
{
    /// <summary>
    /// Renders <see cref="SplitScreenTodoAttribute"/> as an info box above the
    /// field it decorates.
    ///
    /// A DecoratorDrawer rather than a PropertyDrawer on purpose: decorators do
    /// not consume the property, so the field still draws with its normal
    /// editor -- sliders, ranges and tooltips all keep working.
    /// </summary>
    [CustomPropertyDrawer(typeof(SplitScreenTodoAttribute))]
    public sealed class SplitScreenTodoDrawer : DecoratorDrawer
    {
        private const string Prefix = "SPLIT-SCREEN TODO\n";
        private const float VerticalPadding = 6f;
        private const float MinimumHeight = 34f;

        /// <summary>Allowance for the help box icon, borders and margins.</summary>
        private const float IconAllowance = 52f;

        private const float LineHeight = 15f;

        /// <summary>Rough width of a character in the default inspector font.</summary>
        private const float ApproximateCharacterWidth = 6.2f;

        /// <summary>
        /// Learned from OnGUI, where the real rect is available. Only used to
        /// estimate wrapping, so the starting guess just needs to be sane.
        /// </summary>
        private float lastKnownWidth = 300f;

        /// <summary>
        /// Deliberately free of every GUI API, including EditorStyles and
        /// EditorGUIUtility.
        ///
        /// Unity 6 builds the Inspector with UIToolkit and calls GetHeight while
        /// BINDING the property, which is not inside OnGUI. Anything that routes
        /// through GUIUtility.CheckOnGUI -- EditorGUIUtility.currentViewWidth
        /// being the obvious trap -- throws there, once per redraw, forever. So
        /// the height is estimated from the text instead.
        /// </summary>
        public override float GetHeight()
        {
            string text = Prefix + ((SplitScreenTodoAttribute)attribute).Note;

            float usableWidth = Mathf.Max(80f, lastKnownWidth - IconAllowance);
            int charactersPerLine = Mathf.Max(10, Mathf.FloorToInt(usableWidth / ApproximateCharacterWidth));

            int lines = 0;
            foreach (string paragraph in text.Split('\n'))
            {
                lines += Mathf.Max(1, Mathf.CeilToInt(paragraph.Length / (float)charactersPerLine));
            }

            return Mathf.Max(MinimumHeight, (lines * LineHeight) + VerticalPadding + 8f);
        }

        public override void OnGUI(Rect position)
        {
            // Inside OnGUI the real width is known, so record it for the next
            // height estimate. First layout may be slightly off; it self-corrects.
            lastKnownWidth = position.width;

            var note = (SplitScreenTodoAttribute)attribute;

            var box = new Rect(
                position.x,
                position.y + (VerticalPadding * 0.5f),
                position.width,
                position.height - VerticalPadding);

            EditorGUI.HelpBox(box, Prefix + note.Note, MessageType.Info);
        }
    }
}
