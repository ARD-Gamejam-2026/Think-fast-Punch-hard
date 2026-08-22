using System;

namespace ThinkFast.Quiz
{
    /// <summary>
    /// Deals indices [0, count) in shuffled order, reshuffling after each
    /// full cycle without ever dealing the same index twice in a row.
    /// </summary>
    public class LandmarkDeck
    {
        private readonly int[] order;
        private readonly Random random;
        private int position;

        public LandmarkDeck(int count, Random random)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            this.random = random;
            order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            Shuffle(avoidFirst: -1);
        }

        public int Next()
        {
            if (position >= order.Length)
            {
                int last = order[order.Length - 1];
                position = 0;
                Shuffle(avoidFirst: last);
            }

            return order[position++];
        }

        private void Shuffle(int avoidFirst)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            if (order.Length > 1 && order[0] == avoidFirst)
            {
                int j = random.Next(1, order.Length);
                (order[0], order[j]) = (order[j], order[0]);
            }
        }
    }
}
