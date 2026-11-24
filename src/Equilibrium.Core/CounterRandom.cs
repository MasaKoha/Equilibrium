using System;

namespace Equilibrium.Core
{
    /// <summary>Versioned counter-based random words and an unbiased bounded draw.</summary>
    public static class CounterRandom
    {
        /// <summary>Version of the algorithm and its output mapping.</summary>
        public const string Version = "splitmix64-purpose-v1";

        /// <summary>Computes the word at an explicit stream and index.</summary>
        public static ulong Word(ulong seed, uint purpose, ulong index)
        {
            unchecked
            {
                return Mix(seed ^ Mix(0xD1B54A32D192ED03UL + purpose) ^
                    Mix(0x9E3779B97F4A7C15UL + index));
            }
        }

        /// <summary>Draws an unbiased integer in [0, upperBound), advancing only this stream.</summary>
        public static int Below(ulong seed, uint purpose, ref ulong index, int upperBound)
        {
            if (upperBound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound));
            }

            ulong bound = (ulong)upperBound;
            ulong threshold = unchecked(0UL - bound) % bound;
            while (true)
            {
                ulong word = Word(seed, purpose, index);
                index = checked(index + 1);
                if (word >= threshold)
                {
                    return (int)(word % bound);
                }
            }
        }

        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
