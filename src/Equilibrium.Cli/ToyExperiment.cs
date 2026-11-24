using System;
using System.Collections.Generic;
using System.Globalization;
using Equilibrium.Core;
using Equilibrium.Simulation;
using Equilibrium.ToyGame;

namespace Equilibrium.Cli
{
    /// <summary>Four-way paired demonstration; no product decks or rules.</summary>
    public static class ToyExperiment
    {
        /// <summary>Experiment plan, result definitions and stream allocation version.</summary>
        public const string Version = "single-card-paired-v1";

        /// <summary>Runs four matches per block and records the first actual block for replay.</summary>
        public static ExperimentReport Compare(ulong seed, int blocks)
        {
            if (blocks < 1 || blocks > 100_000)
            {
                throw new ArgumentOutOfRangeException(nameof(blocks));
            }

            var game = new ToyRules();
            string[] deckIds = { "baseline", "replacement" };
            var counts = new OutcomeCounts[4];
            var differences = new List<double>(blocks);
            var replays = new List<ToyReplay>(4);
            Card[] opponent = ToyRules.Deck("opponent");
            Card[][] decks = { ToyRules.Deck(deckIds[0]), ToyRules.Deck(deckIds[1]) };

            for (int block = 0; block < blocks; block++)
            {
                ulong matchSeed = CounterRandom.Word(seed, 100, (ulong)block);
                double difference = 0;
                bool complete = true;
                for (int variant = 0; variant < 2; variant++)
                {
                    for (int startingSeat = 0; startingSeat < 2; startingSeat++)
                    {
                        var initial = game.Start(matchSeed, startingSeat, decks[variant], opponent);
                        var result = MatchRunner.Run(game, initial, ToyPolicy.Choose, capture: block == 0);
                        int cell = variant * 2 + startingSeat;
                        counts[cell] = counts[cell].Add(result.Outcome);
                        if (result.Outcome == Outcome.Limit)
                        {
                            complete = false;
                        }
                        else
                        {
                            double score = result.Outcome == Outcome.SeatZeroWins ? 1 :
                                result.Outcome == Outcome.Draw ? 0.5 : 0;
                            difference += (variant == 0 ? 0.5 : -0.5) * score;
                        }

                        if (block == 0)
                        {
                            replays.Add(new ToyReplay(deckIds[variant], "opponent", startingSeat,
                                Hex(matchSeed), result.Outcome, result.Events));
                        }
                    }
                }

                if (complete)
                {
                    differences.Add(difference);
                }
            }

            return new ExperimentReport(Version, ToyRules.Version, ToyPolicy.Version, CounterRandom.Version,
                Hex(seed), blocks, blocks - differences.Count, counts[0], counts[1], counts[2], counts[3],
                differences.Count == blocks ? PairedStatistics.Summarize(differences) : null,
                PairedStatistics.ProbabilityAtLeastOne(10, 2, 3), replays.AsReadOnly());
        }

        private static string Hex(ulong value) => value.ToString("X16", CultureInfo.InvariantCulture);
    }

    /// <summary>Raw outcomes for the deck being measured.</summary>
    /// <param name="Wins">Seat zero wins.</param>
    /// <param name="Losses">Seat one wins.</param>
    /// <param name="Draws">Natural draws.</param>
    /// <param name="Limits">Matches stopped by the run limit.</param>
    public readonly record struct OutcomeCounts(int Wins, int Losses, int Draws, int Limits)
    {
        /// <summary>Share of available win points; null for an incomplete cell.</summary>
        public double? WinScoreRate => Limits > 0 || Wins + Losses + Draws == 0
            ? null
            : (Wins + 0.5 * Draws) / (Wins + Losses + Draws);

        internal OutcomeCounts Add(Outcome outcome)
        {
            return outcome switch
            {
                Outcome.SeatZeroWins => this with { Wins = Wins + 1 },
                Outcome.SeatOneWins => this with { Losses = Losses + 1 },
                Outcome.Draw => this with { Draws = Draws + 1 },
                Outcome.Limit => this with { Limits = Limits + 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };
        }
    }

    /// <summary>Replay input and observed trace. Rule and RNG versions live in its report.</summary>
    /// <param name="DeckZero">Named deck under evaluation.</param>
    /// <param name="DeckOne">Named opponent deck.</param>
    /// <param name="StartingSeat">Seat that moved first.</param>
    /// <param name="SeedHex">Unsigned seed in lossless hexadecimal notation.</param>
    /// <param name="Outcome">Recorded terminal outcome.</param>
    /// <param name="Events">Public trace including the actions needed for replay.</param>
    public sealed record ToyReplay(string DeckZero, string DeckOne, int StartingSeat, string SeedHex,
        Outcome Outcome, IReadOnlyList<ToyEvent> Events);

    /// <summary>Self-describing experiment result.</summary>
    /// <param name="ExperimentVersion">Version of the experiment plan.</param>
    /// <param name="RulesVersion">Version of rules and fixed decks.</param>
    /// <param name="PolicyVersion">Version of the AI.</param>
    /// <param name="RandomVersion">Version of the random implementation.</param>
    /// <param name="SeedHex">Unsigned experiment seed in hexadecimal.</param>
    /// <param name="Blocks">Number of blocks, four games each.</param>
    /// <param name="IncompleteBlocks">Blocks containing at least one limit result.</param>
    /// <param name="BaselineFirst">Baseline outcomes going first.</param>
    /// <param name="BaselineSecond">Baseline outcomes going second.</param>
    /// <param name="ReplacementFirst">Replacement outcomes going first.</param>
    /// <param name="ReplacementSecond">Replacement outcomes going second.</param>
    /// <param name="BaselineMinusReplacement">Paired estimate, or null when any block was incomplete.</param>
    /// <param name="OpeningHitProbability">Analytic probability for the baseline's two target cards.</param>
    /// <param name="FirstBlockReplays">All four traces from the first measured block.</param>
    public sealed record ExperimentReport(string ExperimentVersion, string RulesVersion, string PolicyVersion,
        string RandomVersion, string SeedHex, int Blocks, int IncompleteBlocks, OutcomeCounts BaselineFirst,
        OutcomeCounts BaselineSecond, OutcomeCounts ReplacementFirst, OutcomeCounts ReplacementSecond,
        DifferenceInterval? BaselineMinusReplacement, double OpeningHitProbability,
        IReadOnlyList<ToyReplay> FirstBlockReplays);
}
