using System.Collections.Generic;

namespace Equilibrium.ToyGame
{
    /// <summary>Deterministic rule-based demo opponent.</summary>
    public static class ToyPolicy
    {
        /// <summary>Policy version recorded with the result.</summary>
        public const string Version = "greedy-v1";

        /// <summary>Chooses a legal action using just the active player's observations.</summary>
        public static int Choose(ToyView view, IReadOnlyList<int> actions)
        {
            int largestHit = ToyRules.Pass;
            int largestDamage = 0;
            int heal = ToyRules.Pass;
            foreach (int action in actions)
            {
                if (action == ToyRules.Pass)
                {
                    continue;
                }

                Card card = view.Hand[action];
                int damage = ToyRules.Damage(card);
                if (damage >= view.OpponentLife && damage > 0)
                {
                    return action;
                }

                if (card == Card.Heal && heal == ToyRules.Pass)
                {
                    heal = action;
                }

                if (damage > largestDamage)
                {
                    largestDamage = damage;
                    largestHit = action;
                }
            }

            return view.Life <= 7 && heal != ToyRules.Pass ? heal : largestHit;
        }
    }
}
