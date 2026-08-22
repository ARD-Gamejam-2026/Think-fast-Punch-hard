using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class AnswerIconPrefabTests
    {
        [Test]
        public void Every_answer_button_in_the_panel_prefab_has_an_icon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quiz/QuizPanel.prefab");
            Assert.IsNotNull(prefab, "QuizPanel.prefab must exist");
            AnswerButton[] buttons = prefab.GetComponentsInChildren<AnswerButton>(true);
            Assert.AreEqual(QuizQuestion.AnswerCount, buttons.Length);

            FieldInfo iconField = typeof(AnswerButton).GetField("answerIcon",
                BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var button in buttons)
            {
                var icon = iconField.GetValue(button) as Image;
                Assert.IsNotNull(icon, "each AnswerButton must have answerIcon assigned");
            }
        }
    }
}
