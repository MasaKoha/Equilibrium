using System.Collections.Generic;

namespace Equilibrium.Core
{
    /// <summary>Result from the point of view of named seats, not turn order.</summary>
    public enum Outcome
    {
        /// <summary>The game is still running.</summary>
        None = 0,
        /// <summary>Seat zero wins.</summary>
        SeatZeroWins,
        /// <summary>Seat one wins.</summary>
        SeatOneWins,
        /// <summary>A draw under the game rules.</summary>
        Draw,
        /// <summary>A run limit, not a game-rule draw.</summary>
        Limit
    }

    /// <summary>One-step pure, synchronous game contract.</summary>
    public interface IGame<TState, TView, TAction, TEvent>
    {
        /// <summary>Reads the current actor.</summary>
        int ActiveSeat(TState state);
        /// <summary>Reads the current outcome.</summary>
        Outcome GetOutcome(TState state);
        /// <summary>Builds the active player's view without hidden opponent data.</summary>
        TView Observe(TState state);
        /// <summary>Returns legal actions in stable order.</summary>
        IReadOnlyList<TAction> LegalActions(TState state);
        /// <summary>Applies a legal action without changing the input state.</summary>
        (TState State, TEvent Event) Apply(TState state, TAction action);
    }
}
