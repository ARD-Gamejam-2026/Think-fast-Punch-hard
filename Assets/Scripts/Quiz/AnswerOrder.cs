using System;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// A display order for a question's answers: a permutation that maps each
    /// on-screen slot to the answer index it shows.
    ///
    /// Ordering happens at display time rather than in the question data because
    /// authored questions are ScriptableObject assets -- shuffling their arrays
    /// in place would rewrite the asset on disk. Generated questions already
    /// randomize where the correct answer lands; running every question through
    /// the same permutation makes authored ones behave the same way, so no fixed
    /// click position can be pre-committed.
    /// </summary>
    public static class AnswerOrder
    {
        /// <summary>
        /// Builds a shuffled order over the slots [0, count), where entry i is
        /// the answer index shown in slot i.
        /// </summary>
        public static int[] Shuffled(int count, Random random)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            int[] order = Identity(count);
            for (int i = count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                int held = order[i];
                order[i] = order[swap];
                order[swap] = held;
            }
            return order;
        }

        /// <summary>Builds the unshuffled order, where every slot shows its own answer index.</summary>
        public static int[] Identity(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }
            return order;
        }

        /// <summary>
        /// Finds the slot displaying the given answer index, or -1 when the
        /// order does not contain it. Used to translate a question's
        /// correctIndex into the slot the player has to click.
        /// </summary>
        public static int SlotOf(int[] order, int answerIndex)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            for (int slot = 0; slot < order.Length; slot++)
            {
                if (order[slot] == answerIndex)
                {
                    return slot;
                }
            }
            return -1;
        }
    }
}
