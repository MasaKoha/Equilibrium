using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Equilibrium.Cli;
using Equilibrium.Core;
using Equilibrium.Simulation;
using Equilibrium.ToyGame;

namespace Equilibrium.Smoke
{
    internal static class Program
    {
        private static int Main()
        {
            var tests = new (string Name, Action Run)[]
            {
                ("opening probability 8/15", OpeningProbability),
                ("paired mean and Hoeffding bounds", PairedMean),
                ("counter random stability and domain separation", Random),
                ("pure steps and legal actions", Steps),
                ("exact replay and malformed replay refusal", Replay),
                ("seat balance and deterministic report", Report),
                ("common shuffles and one-card difference", CommonShuffles),
                ("deck exhaustion is a rule-defined loss", DeckExhaustion)
            };
            foreach (var test in tests)
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }

            Console.WriteLine($"{tests.Length} checks passed.");
            return 0;
        }

        private static void OpeningProbability()
        {
            Near(PairedStatistics.ProbabilityAtLeastOne(10, 2, 3), 8.0 / 15.0);
            Near(PairedStatistics.ProbabilityAtLeastOne(10, 2, 0), 0);
            Near(PairedStatistics.ProbabilityAtLeastOne(10, 2, 9), 1);
            Throws<ArgumentOutOfRangeException>(() => PairedStatistics.ProbabilityAtLeastOne(3, 4, 2));
            int hits = 0;
            int combinations = 0;
            for (int i = 0; i < 10; i++)
            {
                for (int j = i + 1; j < 10; j++)
                {
                    for (int k = j + 1; k < 10; k++)
                    {
                        combinations++;
                        if (i < 2)
                        {
                            hits++;
                        }
                    }
                }
            }

            Require(hits == 64 && combinations == 120);
        }

        private static void PairedMean()
        {
            var differences = Enumerable.Repeat(0.5, 75).Concat(Enumerable.Repeat(-0.5, 25)).ToArray();
            var interval = PairedStatistics.Summarize(differences);
            Require(interval.Count == 100);
            Near(interval.Difference, 0.25);
            Near(interval.Lower95, -0.02162030314812391);
            Near(interval.Upper95, 0.5216203031481239);
            Throws<ArgumentOutOfRangeException>(() => PairedStatistics.Summarize(new[] { double.NaN }));
        }

        private static void Random()
        {
            Require(CounterRandom.Word(2026, 1, 0) == 0xf4e0e576d00df067UL);
            Require(CounterRandom.Word(2026, 1, 0) != CounterRandom.Word(2026, 2, 0));
            ulong indexOne = 0;
            ulong indexTwo = 0;
            for (int i = 0; i < 500; i++)
            {
                int a = CounterRandom.Below(2026, 1, ref indexOne, 10);
                CounterRandom.Word(2026, 2, (ulong)i);
                int b = CounterRandom.Below(2026, 1, ref indexTwo, 10);
                Require(a == b && a >= 0 && a < 10);
            }

            Require(indexOne == indexTwo);
        }

        private static void Steps()
        {
            var game = new ToyRules();
            var initial = Start(game);
            var view = game.Observe(initial);
            Require(view.Hand.Count == 4 && view.OpponentHandCount == 3 && view.Life == 12);
            Require(game.LegalActions(initial).SequenceEqual(new[] { 0, 1, 2, 3, -1 }));
            Require(game.ActiveSeat(game.Apply(initial, 0).State) == 1);
            Require(game.Observe(initial).Hand.SequenceEqual(view.Hand));
            Require(game.Observe(initial).OpponentLife == 12);
            Throws<ArgumentOutOfRangeException>(() => game.Apply(initial, 4));
        }

        private static void Replay()
        {
            var game = new ToyRules();
            var initial = Start(game);
            var result = MatchRunner.Run(game, initial, ToyPolicy.Choose, capture: true);
            var actions = result.Events.Select(e => e.Action).ToArray();
            Require(result.Events.SequenceEqual(MatchRunner.Replay(game, initial, actions)));
            Require(result.Events[^1].Outcome == result.Outcome);
            Throws<ArgumentException>(() => MatchRunner.Replay(game, initial, actions.SkipLast(1)));
            Throws<ArgumentException>(() => MatchRunner.Replay(game, initial, actions.Append(ToyRules.Pass)));
            Throws<ArgumentOutOfRangeException>(() => MatchRunner.Replay(game, initial, new[] { 999 }));
        }

        private static void Report()
        {
            var report = ToyExperiment.Compare(2026, 120);
            Require(report.FirstBlockReplays.Count == 4 && report.IncompleteBlocks == 0);
            var cells = new[] { report.BaselineFirst, report.BaselineSecond,
                report.ReplacementFirst, report.ReplacementSecond };
            foreach (var cell in cells)
            {
                Require(cell.Wins + cell.Losses + cell.Draws + cell.Limits == 120);
            }

            var interval = report.BaselineMinusReplacement ?? throw new Exception("Missing interval.");
            double gap = (Score(cells[0]) + Score(cells[1]) - Score(cells[2]) - Score(cells[3])) / 240.0;
            Near(interval.Difference, gap);
            Require(report.FirstBlockReplays.Select(r => r.StartingSeat).SequenceEqual(new[] { 0, 1, 0, 1 }));
            Require(report.FirstBlockReplays.Select(r => r.SeedHex).Distinct().Count() == 1);
            var game = new ToyRules();
            foreach (var replay in report.FirstBlockReplays)
            {
                var initial = game.Start(ulong.Parse(replay.SeedHex, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture), replay.StartingSeat,
                    ToyRules.Deck(replay.DeckZero), ToyRules.Deck(replay.DeckOne));
                Require(replay.Events.SequenceEqual(MatchRunner.Replay(game, initial, replay.Events.Select(e => e.Action))));
            }

            Require(JsonSerializer.Serialize(report) == JsonSerializer.Serialize(ToyExperiment.Compare(2026, 120)));
        }

        private static void CommonShuffles()
        {
            var game = new ToyRules();
            var baseline = ToyRules.Deck("baseline");
            var variant = ToyRules.Deck("replacement");
            var opponent = ToyRules.Deck("opponent");
            Require(baseline.Zip(variant).Count(pair => pair.First != pair.Second) == 1);
            for (ulong seed = 0; seed < 24; seed++)
            {
                var before = game.Start(seed, 0, baseline, opponent);
                var after = game.Start(seed, 0, variant, opponent);
                int mismatches = 0;
                while (game.GetOutcome(before) == Outcome.None)
                {
                    var a = game.Observe(before);
                    var b = game.Observe(after);
                    Require(a.Seat == b.Seat);
                    var differences = a.Hand.Zip(b.Hand).Where(pair => pair.First != pair.Second).ToArray();
                    Require(a.Hand.Count == b.Hand.Count);
                    if (a.Seat == 0)
                    {
                        mismatches = Math.Max(mismatches, differences.Length);
                        Require(differences.All(pair => pair.First == Card.Burst && pair.Second == Card.Strike));
                    }
                    else
                    {
                        Require(differences.Length == 0);
                    }

                    before = game.Apply(before, ToyRules.Pass).State;
                    after = game.Apply(after, ToyRules.Pass).State;
                }

                Require(mismatches == 1);
            }
        }

        private static void DeckExhaustion()
        {
            var game = new ToyRules();
            var one = MatchRunner.Run(game, Start(game), (view, actions) => ToyRules.Pass, capture: true);
            Require(one.Events.Count == 14 && one.Outcome == Outcome.SeatOneWins &&
                one.Events[^1].EndReason == "deck-empty");
        }

        private static double Score(OutcomeCounts counts) => counts.Wins + 0.5 * counts.Draws;

        private static ToyState Start(ToyRules game) =>
            game.Start(2026, 0, ToyRules.Deck("baseline"), ToyRules.Deck("opponent"));

        private static void Near(double actual, double expected) =>
            Require(Math.Abs(actual - expected) < 1e-12);

        private static void Require(bool condition)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Smoke check failed.");
            }
        }

        private static void Throws<TError>(Action action) where TError : Exception
        {
            try
            {
                action();
            }
            catch (TError)
            {
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(TError).Name}.");
        }
    }
}
