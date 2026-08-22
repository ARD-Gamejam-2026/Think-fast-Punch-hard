using UnityEngine;

namespace ThinkFast.Common
{
    /// <summary>
    /// Marks a serialized field whose value was tuned against a single
    /// full-screen view, and which will need revisiting once the fighter is
    /// confined to one half of a split screen with the riddle UI beside it.
    ///
    /// Draws as an info box directly above the field in the Inspector, so the
    /// note is visible without hovering for a tooltip. The attribute name is also
    /// deliberately distinctive: search the project for "SplitScreenTodo" to find
    /// every affected setting at once.
    ///
    /// Purely informational -- it changes nothing at runtime, and the whole thing
    /// can be deleted once the split-screen layout is settled.
    /// </summary>
    public sealed class SplitScreenTodoAttribute : PropertyAttribute
    {
        public readonly string Note;

        public SplitScreenTodoAttribute(string note)
        {
            Note = note;
        }
    }
}
