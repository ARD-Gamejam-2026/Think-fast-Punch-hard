using System.Collections.Generic;
using System.IO;
using ThinkFast.Quiz;
using UnityEditor;
using UnityEngine;

namespace ThinkFast.TutorialEditor
{
    /// <summary>
    /// The handful of questions the tutorial asks.
    ///
    /// Deliberately its own folder rather than
    /// <c>Assets/Quiz/Questions</c>: the fight scene loads every authored
    /// question under that folder, and a tutorial question turning up mid-match
    /// would be a strange, easy thing to ship by accident.
    ///
    /// They are easy on purpose. The player is being taught what the timer bar
    /// means and where the Action Point went, not tested -- and the Flow lesson
    /// only works if answering inside the green zone is actually achievable
    /// while reading the card that explains it. The generous time limit is part
    /// of the same idea.
    /// </summary>
    public static class TutorialQuestions
    {
        public const string Folder = "Assets/Quiz/TutorialQuestions";

        /// <summary>Long enough to read the coach card and still beat the green zone.</summary>
        private const float TimeLimitSeconds = 12f;

        /// <summary>
        /// One question as authored here: the prompt, the right answer first,
        /// then the three wrong ones. Display order is shuffled by the
        /// controller, so first place carries no meaning at runtime.
        /// </summary>
        private readonly struct Entry
        {
            public readonly string Name;
            public readonly string Question;
            public readonly string[] Answers;

            public Entry(string name, string question, params string[] answers)
            {
                Name = name;
                Question = question;
                Answers = answers;
            }
        }

        private static readonly Entry[] Entries =
        {
            new Entry("Tutorial_Add", "What is 7 + 5?", "12", "10", "11", "13"),
            new Entry("Tutorial_Planet", "Which planet is closest to the Sun?", "Mercury", "Venus", "Earth", "Mars"),
            new Entry("Tutorial_Hexagon", "How many sides does a hexagon have?", "6", "5", "7", "8"),
            new Entry("Tutorial_Mix", "Blue and yellow paint make...", "Green", "Purple", "Orange", "Grey"),
            new Entry("Tutorial_Times", "What is 9 x 3?", "27", "21", "24", "29"),
            new Entry("Tutorial_Mammal", "Which of these is a mammal?", "Dolphin", "Shark", "Octopus", "Tuna"),
            new Entry("Tutorial_Minutes", "How many minutes are in an hour?", "60", "30", "90", "100"),
            new Entry("Tutorial_Capital", "What is the capital of France?", "Paris", "Rome", "Madrid", "Berlin"),
        };

        /// <summary>
        /// Creates any missing tutorial question and returns the whole set.
        /// Existing assets are left alone, so re-running the scene builder never
        /// overwrites a re-worded question.
        /// </summary>
        public static List<QuizQuestion> EnsureAll()
        {
            EnsureFolder();

            var questions = new List<QuizQuestion>(Entries.Length);

            foreach (Entry entry in Entries)
            {
                questions.Add(EnsureOne(entry));
            }

            AssetDatabase.SaveAssets();
            return questions;
        }

        private static QuizQuestion EnsureOne(Entry entry)
        {
            string path = $"{Folder}/{entry.Name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<QuizQuestion>(path);
            if (existing != null)
            {
                return existing;
            }

            var question = ScriptableObject.CreateInstance<QuizQuestion>();
            question.questionText = entry.Question;
            question.answers = (string[])entry.Answers.Clone();

            // The right answer is authored first and the controller shuffles the
            // slots on display, so this index is about the asset, not about where
            // the player will see it.
            question.correctIndex = 0;
            question.timeLimitSeconds = TimeLimitSeconds;

            AssetDatabase.CreateAsset(question, path);
            return question;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                return;
            }

            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
        }
    }
}
