using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkFast.Quiz
{
    /// <summary>Builds the four shuffled answer labels for a place question.</summary>
    public static class PlaceAnswerBuilder
    {
        /// <summary>
        /// Builds the four shuffled answers: the correct entry's display name
        /// plus three distinct names drawn from the other entries.
        /// correctIndex receives the position of the correct answer.
        /// </summary>
        public static string[] Build(
            LandmarkList.Entry[] entries, int correctEntryIndex, Random random, out int correctIndex)
        {
            if (entries == null || entries.Length < QuizQuestion.AnswerCount)
            {
                throw new ArgumentException(
                    $"Need at least {QuizQuestion.AnswerCount} landmark entries", nameof(entries));
            }

            var picked = new List<int> { correctEntryIndex };
            while (picked.Count < QuizQuestion.AnswerCount)
            {
                int candidate = random.Next(entries.Length);
                if (!picked.Contains(candidate))
                {
                    picked.Add(candidate);
                }
            }

            for (int i = picked.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (picked[i], picked[j]) = (picked[j], picked[i]);
            }

            correctIndex = picked.IndexOf(correctEntryIndex);
            return picked.Select(index => entries[index].displayName).ToArray();
        }
    }
}
