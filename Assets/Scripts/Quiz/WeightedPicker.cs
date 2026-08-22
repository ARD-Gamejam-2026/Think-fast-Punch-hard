namespace ThinkFast.Quiz
{
    /// <summary>
    /// Picks one bucket index in proportion to the buckets' weights.
    /// </summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Picks a bucket index in proportion to its weight. unitRoll is a
        /// value in [0, 1] (e.g. UnityEngine.Random.value). Buckets with a
        /// non-positive weight are never picked. Returns -1 when no bucket
        /// has a positive weight. Random.value is inclusive of 1, so a
        /// boundary roll must never fall past the end: the last positive
        /// bucket stays picked when the cumulative loop runs out.
        /// </summary>
        public static int Pick(float[] weights, float unitRoll)
        {
            float total = 0f;
            foreach (float weight in weights)
            {
                if (weight > 0f)
                {
                    total += weight;
                }
            }

            if (total <= 0f)
            {
                return -1;
            }

            float roll = unitRoll * total;
            float cumulative = 0f;
            int selected = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f)
                {
                    continue;
                }
                cumulative += weights[i];
                selected = i;
                if (roll < cumulative)
                {
                    break;
                }
            }
            return selected;
        }
    }
}
