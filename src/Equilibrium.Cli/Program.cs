using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Equilibrium.Core;
using Equilibrium.Simulation;
using Equilibrium.ToyGame;

namespace Equilibrium.Cli
{
    internal static class Program
    {
        private const string Usage = "Usage: Equilibrium.Cli compare <blocks: 1..100000> <unsigned seed>\n" +
            "       Equilibrium.Cli replay <report.json> <first-block index: 0..3>";
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--help")
            {
                Console.WriteLine(Usage);
                return 0;
            }

            if (args.Length == 3 && args[0] == "compare" &&
                int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int blocks) &&
                blocks >= 1 && blocks <= 100_000 &&
                ulong.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
            {
                Console.WriteLine(JsonSerializer.Serialize(ToyExperiment.Compare(seed, blocks), Options));
                return 0;
            }

            if (args.Length == 3 && args[0] == "replay" &&
                int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out int index) &&
                index >= 0 && index < 4)
            {
                return VerifyReplay(args[1], index);
            }

            Console.Error.WriteLine(Usage);
            return 2;
        }

        private static int VerifyReplay(string path, int index)
        {
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement report = json.RootElement;
                if (report.GetProperty("ExperimentVersion").GetString() != ToyExperiment.Version ||
                    report.GetProperty("RulesVersion").GetString() != ToyRules.Version ||
                    report.GetProperty("RandomVersion").GetString() != CounterRandom.Version)
                {
                    throw new InvalidDataException("Unsupported report, rule or RNG version.");
                }

                JsonElement replay = report.GetProperty("FirstBlockReplays")[index];
                JsonElement expected = replay.GetProperty("Events");
                var game = new ToyRules();
                var initial = game.Start(
                    ulong.Parse(replay.GetProperty("SeedHex").GetString() ?? "", NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture),
                    replay.GetProperty("StartingSeat").GetInt32(),
                    ToyRules.Deck(replay.GetProperty("DeckZero").GetString() ?? ""),
                    ToyRules.Deck(replay.GetProperty("DeckOne").GetString() ?? ""));
                var actual = MatchRunner.Replay(game, initial,
                    expected.EnumerateArray().Select(evt => evt.GetProperty("Action").GetInt32()));
                if (actual.Count == 0 ||
                    !JsonElement.DeepEquals(expected, JsonSerializer.SerializeToElement(actual, Options)) ||
                    !JsonElement.DeepEquals(replay.GetProperty("Outcome"),
                        JsonSerializer.SerializeToElement(actual[^1].Outcome, Options)))
                {
                    throw new InvalidDataException("Replay does not match the recorded events and outcome.");
                }

                Console.WriteLine($"Replay {index} verified: {actual.Count} actions, {actual[^1].Outcome}.");
                return 0;
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
                FormatException or OverflowException or InvalidOperationException or
                System.Collections.Generic.KeyNotFoundException or ArgumentException or IndexOutOfRangeException)
            {
                Console.Error.WriteLine($"Replay failed: {error.Message}");
                return 1;
            }
        }
    }
}
