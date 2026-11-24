using System;
using System.Collections.Generic;
using Equilibrium.Core;

namespace Equilibrium.Simulation
{
    /// <summary>Runs or replays a match using the same pure transition contract.</summary>
    public static class MatchRunner
    {
        /// <summary>Runs a match, optionally retaining all events.</summary>
        public static (Outcome Outcome, IReadOnlyList<TEvent> Events) Run<TState, TView, TAction, TEvent>(
            IGame<TState, TView, TAction, TEvent> game, TState state,
            Func<TView, IReadOnlyList<TAction>, TAction> choose, bool capture)
        {
            var events = capture ? new List<TEvent>() : null;
            while (game.GetOutcome(state) == Outcome.None)
            {
                TAction action = choose(game.Observe(state), game.LegalActions(state));
                var step = game.Apply(state, action);
                state = step.State;
                events?.Add(step.Event);
            }

            IReadOnlyList<TEvent> result = events == null ? Array.Empty<TEvent>() : events.AsReadOnly();
            return (game.GetOutcome(state), result);
        }

        /// <summary>Replays an exact sequence; incomplete or extra actions are invalid.</summary>
        public static IReadOnlyList<TEvent> Replay<TState, TView, TAction, TEvent>(
            IGame<TState, TView, TAction, TEvent> game, TState state, IEnumerable<TAction> actions)
        {
            var events = new List<TEvent>();
            foreach (TAction action in actions)
            {
                if (game.GetOutcome(state) != Outcome.None)
                {
                    throw new ArgumentException("Replay has actions after the game ended.", nameof(actions));
                }

                var step = game.Apply(state, action);
                events.Add(step.Event);
                state = step.State;
            }

            if (game.GetOutcome(state) == Outcome.None)
            {
                throw new ArgumentException("Replay ended before the game did.", nameof(actions));
            }

            return events.AsReadOnly();
        }
    }
}
