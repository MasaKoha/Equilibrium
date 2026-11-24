using System;
using System.Collections.Generic;

namespace Equilibrium.Simulation
{
    /// <summary>Difference in expected half-point win score and conservative bounds.</summary>
    /// <param name="Count">Number of paired trial blocks.</param>
    /// <param name="Difference">Mean baseline-minus-replacement score, in [-1, 1].</param>
    /// <param name="Lower95">Lower bound at 95% confidence.</param>
    /// <param name="Upper95">Upper bound at 95% confidence.</param>
    public sealed record DifferenceInterval(int Count, double Difference, double Lower95, double Upper95);

    /// <summary>Small, dependency-free statistical checks for the first experiment.</summary>
    public static class PairedStatistics
    {
        /// <summary>Two-sided Hoeffding interval for independent bounded block differences.</summary>
        public static DifferenceInterval Summarize(IReadOnlyList<double> differences)
        {
            if (differences.Count == 0)
            {
                throw new ArgumentException("At least one block is required.", nameof(differences));
            }

            double total = 0;
            foreach (double difference in differences)
            {
                if (!double.IsFinite(difference) || difference < -1 || difference > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(differences));
                }

                total += difference;
            }

            double mean = total / differences.Count;
            double radius = Math.Sqrt(2 * Math.Log(40) / differences.Count);
            return new DifferenceInterval(differences.Count, mean, Math.Max(-1, mean - radius),
                Math.Min(1, mean + radius));
        }

        /// <summary>Probability of drawing at least one target, without replacement.</summary>
        public static double ProbabilityAtLeastOne(int deckSize, int targetCount, int handSize)
        {
            if (deckSize < 0 || targetCount < 0 || targetCount > deckSize ||
                handSize < 0 || handSize > deckSize)
            {
                throw new ArgumentOutOfRangeException(nameof(deckSize), "Invalid deck distribution.");
            }

            double miss = 1;
            for (int draw = 0; draw < handSize; draw++)
            {
                miss *= (double)Math.Max(0, deckSize - targetCount - draw) / (deckSize - draw);
            }

            return 1 - miss;
        }
    }
}
