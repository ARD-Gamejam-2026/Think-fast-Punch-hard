using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ThinkFast.Quiz;

namespace ThinkFast.Quiz.Tests
{
    public class AnswerButtonToggleTests
    {
        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(target, value);
        }

        [Test]
        public void SetAnswerImage_shows_icon_and_hides_text()
        {
            var go = new GameObject("answer");
            var button = go.AddComponent<AnswerButton>();
            var icon = new GameObject("icon").AddComponent<Image>();
            var label = new GameObject("label").AddComponent<TextMeshProUGUI>();
            SetPrivate(button, "answerIcon", icon);
            SetPrivate(button, "answerLabel", label);

            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            button.SetAnswerImage(sprite);
            Assert.IsTrue(icon.enabled);
            Assert.IsFalse(label.enabled);

            button.SetAnswerText("hello");
            Assert.IsFalse(icon.enabled);
            Assert.IsTrue(label.enabled);
            Assert.AreEqual("hello", label.text);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(icon.gameObject);
            Object.DestroyImmediate(label.gameObject);
        }
    }
}
