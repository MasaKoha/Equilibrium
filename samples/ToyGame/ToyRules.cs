using System;
using System.Collections.Generic;
using Equilibrium.Core;

namespace Equilibrium.ToyGame
{
    /// <summary>Cards supported by the demonstration only.</summary>
    public enum Card
    {
        /// <summary>No card.</summary>
        None = 0,
        /// <summary>Deal one damage.</summary>
        Jab,
        /// <summary>Deal two damage.</summary>
        Strike,
        /// <summary>Deal four damage.</summary>
        Burst,
        /// <summary>Recover three life.</summary>
        Heal
    }

    /// <summary>Immutable active-player view. There is no opponent hand or deck order.</summary>
    public sealed class ToyView
    {
        internal ToyView(int turn, int seat, int life, int opponentLife, Card[] hand,
            int opponentHandCount, int deckCount, int opponentDeckCount)
        {
            Turn = turn;
            Seat = seat;
            Life = life;
            OpponentLife = opponentLife;
            Hand = Array.AsReadOnly(hand);
            OpponentHandCount = opponentHandCount;
            DeckCount = deckCount;
            OpponentDeckCount = opponentDeckCount;
        }

        /// <summary>One-based action turn.</summary>
        public int Turn { get; }
        /// <summary>Identity of the active seat.</summary>
        public int Seat { get; }
        /// <summary>Active seat's remaining life.</summary>
        public int Life { get; }
        /// <summary>Opponent's remaining life.</summary>
        public int OpponentLife { get; }
        /// <summary>Active seat's hand only.</summary>
        public IReadOnlyList<Card> Hand { get; }
        /// <summary>Opponent's hand size.</summary>
        public int OpponentHandCount { get; }
        /// <summary>Active seat's remaining deck size.</summary>
        public int DeckCount { get; }
        /// <summary>Opponent's remaining deck size.</summary>
        public int OpponentDeckCount { get; }
    }

    /// <summary>Internal simulation state; agents do not receive this.</summary>
    public sealed class ToyState
    {
        internal ToyState(int turn, int seat, Outcome outcome, int[] life, Card[][] decks,
            int[] cursors, Card[][] hands)
        {
            Turn = turn;
            Seat = seat;
            Outcome = outcome;
            Life = life;
            Decks = decks;
            Cursors = cursors;
            Hands = hands;
        }

        internal int Turn { get; }
        internal int Seat { get; }
        internal Outcome Outcome { get; }
        internal int[] Life { get; }
        internal Card[][] Decks { get; }
        internal int[] Cursors { get; }
        internal Card[][] Hands { get; }
    }

    /// <summary>A deterministic replay event for a played card or pass.</summary>
    public readonly struct ToyEvent
    {
        internal ToyEvent(int turn, int seat, int action, Card played, int lifeZero, int lifeOne,
            Outcome outcome, string endReason)
        {
            Turn = turn;
            Seat = seat;
            Action = action;
            Played = played;
            LifeZero = lifeZero;
            LifeOne = lifeOne;
            Outcome = outcome;
            EndReason = endReason;
        }

        /// <summary>One-based action turn.</summary>
        public int Turn { get; }
        /// <summary>Actor seat.</summary>
        public int Seat { get; }
        /// <summary>Hand index, or -1 for pass.</summary>
        public int Action { get; }
        /// <summary>The public card played, or None.</summary>
        public Card Played { get; }
        /// <summary>Life of seat zero after the action.</summary>
        public int LifeZero { get; }
        /// <summary>Life of seat one after the action.</summary>
        public int LifeOne { get; }
        /// <summary>Outcome after the action and any next-turn draw.</summary>
        public Outcome Outcome { get; }
        /// <summary>Terminal reason; empty while a game is running.</summary>
        public string EndReason { get; }
    }

    /// <summary>Rules for the deliberately small demonstration game.</summary>
    public sealed class ToyRules : IGame<ToyState, ToyView, int, ToyEvent>
    {
        /// <summary>Rule and manifest version.</summary>
        public const string Version = "toy-v1";
        /// <summary>Explicit pass action.</summary>
        public const int Pass = -1;
        private const int MaximumLife = 12;
        private const int TurnLimit = 24;
        private const int HandSize = 3;

        /// <summary>Creates a detached ten-card deck for a named fixture.</summary>
        public static Card[] Deck(string id)
        {
            switch (id)
            {
                case "baseline":
                    return new[] { Card.Burst, Card.Jab, Card.Strike, Card.Heal, Card.Strike,
                        Card.Burst, Card.Jab, Card.Strike, Card.Heal, Card.Strike };
                case "replacement":
                    Card[] replaced = Deck("baseline");
                    replaced[0] = Card.Strike;
                    return replaced;
                case "opponent":
                    return new[] { Card.Strike, Card.Jab, Card.Burst, Card.Heal, Card.Strike,
                        Card.Strike, Card.Burst, Card.Jab, Card.Heal, Card.Strike };
                default:
                    throw new ArgumentException("Unknown toy deck ID.", nameof(id));
            }
        }

        /// <summary>Starts a match with both starting hands and the first turn's draw.</summary>
        public ToyState Start(ulong seed, int startingSeat, Card[] deckZero, Card[] deckOne)
        {
            if (startingSeat != 0 && startingSeat != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(startingSeat));
            }

            Card[][] decks = { Shuffle(deckZero, seed, 1), Shuffle(deckOne, seed, 2) };
            var hands = new[] { new Card[HandSize], new Card[HandSize] };
            int[] cursors = { HandSize, HandSize };
            Array.Copy(decks[0], hands[0], HandSize);
            Array.Copy(decks[1], hands[1], HandSize);
            Draw(decks, cursors, hands, startingSeat);
            return new ToyState(1, startingSeat, Outcome.None, new[] { MaximumLife, MaximumLife },
                decks, cursors, hands);
        }

        /// <inheritdoc/>
        public int ActiveSeat(ToyState state) => state.Seat;

        /// <inheritdoc/>
        public Outcome GetOutcome(ToyState state) => state.Outcome;

        /// <inheritdoc/>
        public ToyView Observe(ToyState state)
        {
            int seat = state.Seat;
            int other = 1 - seat;
            return new ToyView(state.Turn, seat, state.Life[seat], state.Life[other],
                state.Hands[seat], state.Hands[other].Length,
                state.Decks[seat].Length - state.Cursors[seat],
                state.Decks[other].Length - state.Cursors[other]);
        }

        /// <inheritdoc/>
        public IReadOnlyList<int> LegalActions(ToyState state)
        {
            if (state.Outcome != Outcome.None)
            {
                return Array.Empty<int>();
            }

            int size = state.Hands[state.Seat].Length;
            var actions = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                actions[i] = i;
            }

            actions[size] = Pass;
            return actions;
        }

        /// <inheritdoc/>
        public (ToyState State, ToyEvent Event) Apply(ToyState state, int action)
        {
            int seat = state.Seat;
            int other = 1 - seat;
            if (state.Outcome != Outcome.None || action < Pass || action >= state.Hands[seat].Length)
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Illegal action.");
            }

            int[] life = (int[])state.Life.Clone();
            int[] cursors = (int[])state.Cursors.Clone();
            Card[][] hands = (Card[][])state.Hands.Clone();
            Card played = Card.None;
            if (action != Pass)
            {
                played = hands[seat][action];
                var remaining = new Card[hands[seat].Length - 1];
                Array.Copy(hands[seat], 0, remaining, 0, action);
                Array.Copy(hands[seat], action + 1, remaining, action, remaining.Length - action);
                hands[seat] = remaining;
                if (played == Card.Heal)
                {
                    life[seat] = Math.Min(MaximumLife, life[seat] + 3);
                }
                else
                {
                    life[other] = Math.Max(0, life[other] - Damage(played));
                }
            }

            Outcome outcome = Outcome.None;
            string reason = string.Empty;
            if (life[other] == 0)
            {
                outcome = Winner(seat);
                reason = "life-zero";
            }
            else if (state.Turn == TurnLimit)
            {
                outcome = Outcome.Limit;
                reason = "turn-limit";
            }
            else if (!Draw(state.Decks, cursors, hands, other))
            {
                outcome = Winner(seat);
                reason = "deck-empty";
            }

            var next = new ToyState(state.Turn + (outcome == Outcome.None ? 1 : 0),
                outcome == Outcome.None ? other : seat, outcome, life, state.Decks, cursors, hands);
            return (next, new ToyEvent(state.Turn, seat, action, played, life[0], life[1], outcome, reason));
        }

        internal static int Damage(Card card)
        {
            return card switch
            {
                Card.Jab => 1,
                Card.Strike => 2,
                Card.Burst => 4,
                _ => 0
            };
        }

        private static Outcome Winner(int seat) => seat == 0 ? Outcome.SeatZeroWins : Outcome.SeatOneWins;

        private static Card[] Shuffle(Card[] cards, ulong seed, uint purpose)
        {
            if (cards == null || cards.Length != 10 ||
                Array.Exists(cards, card => card < Card.Jab || card > Card.Heal))
            {
                throw new ArgumentException("A toy deck requires ten valid cards.", nameof(cards));
            }

            var shuffled = (Card[])cards.Clone();
            ulong index = 0;
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = CounterRandom.Below(seed, purpose, ref index, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }

        private static bool Draw(Card[][] decks, int[] cursors, Card[][] hands, int seat)
        {
            if (cursors[seat] == decks[seat].Length)
            {
                return false;
            }

            Card[] before = hands[seat];
            var after = new Card[before.Length + 1];
            Array.Copy(before, after, before.Length);
            after[before.Length] = decks[seat][cursors[seat]++];
            hands[seat] = after;
            return true;
        }
    }
}
