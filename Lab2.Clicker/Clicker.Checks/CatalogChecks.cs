using Clicker.Core;

namespace Clicker.Checks;

internal static class CatalogChecks
{
    public static int Run()
    {
        int passed = 0;
        const string json = """
            [
              { "Name": "Never", "IconName": "car.png", "BaseLife": 1, "LifeModifier": 1, "BaseGold": 0, "GoldModifier": 1, "SpawnChance": 0 },
              { "Name": "First", "IconName": "car.png", "BaseLife": 100, "LifeModifier": 1.2, "BaseGold": 10, "GoldModifier": 1.5, "SpawnChance": 20 },
              { "Name": "Second", "IconName": "car.png", "BaseLife": 40, "LifeModifier": 2, "BaseGold": 3, "GoldModifier": 0.5, "SpawnChance": 80 },
              { "Name": "NeverLast", "IconName": "car.png", "BaseLife": 1, "LifeModifier": 1, "BaseGold": 0, "GoldModifier": 1, "SpawnChance": 0 }
            ]
            """;

        var catalog = EnemyCatalog.FromJson(json);
        Check(catalog.Count == 4, "All templates must be loaded.");
        Check(Math.Abs(catalog.NormalizedChances.Sum() - 1) < 1e-12, "Normalized probabilities must sum to one.");
        Check(catalog.NormalizedChances[0] == 0, "Zero probabilities must stay zero.");
        Check(Math.Abs(catalog.NormalizedChances[1] - 0.2) < 1e-12, "First normalized probability is incorrect.");
        Check(Math.Abs(catalog.NormalizedChances[2] - 0.8) < 1e-12, "Second normalized probability is incorrect.");
        Check(catalog.CreateEnemy(0).Name == "First", "A zero chance must skip zero-weight templates.");
        Check(catalog.CreateEnemy(0.2 - 1e-12).Name == "First", "The first interval must include its upper interior.");
        Check(catalog.CreateEnemy(0.2).Name == "Second", "An interval boundary must select the next template.");
        Check(catalog.CreateEnemy(double.BitDecrement(1)).Name == "Second", "The final interval must skip trailing zero weights.");

        int firstCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            string name = catalog.CreateEnemy(i / 1000.0).Name;
            Check(name is "First" or "Second", "Zero-weight templates must never be selected.");
            if (name == "First")
                firstCount++;
        }
        Check(firstCount == 200, "A deterministic probability grid must match 20/80 weights.");

        Enemy original = catalog.CreateEnemy(0);
        CheckEnemy(original, "100", "10");
        Check(!original.TakeDamage(new BigNumber("10"), out _), "A small attack must leave the enemy alive.");
        CheckEnemy(catalog.CreateEnemy(0), "100", "10");
        Check(catalog.Stage.ToString() == "1", "Creating enemies must not advance the stage.");
        catalog.Advance();
        Check(catalog.Stage.ToString() == "2", "Victory must advance the stage.");
        CheckEnemy(catalog.CreateEnemy(0), "120", "15");
        CheckEnemy(catalog.CreateEnemy(0.5), "80", "1");
        CheckEnemy(original, "90", "10");
        catalog.Advance();
        CheckEnemy(catalog.CreateEnemy(0), "144", "22");
        CheckEnemy(catalog.CreateEnemy(0.5), "160", "0");

        string hugeText = "1" + new string('0', 80);
        string largeJson = $$"""
            [{ "Name": "Huge", "IconName": "car.png", "BaseLife": {{hugeText}}, "LifeModifier": 1.25,
               "BaseGold": {{hugeText}}, "GoldModifier": 1.5, "SpawnChance": 50 }]
            """;
        var large = EnemyCatalog.FromJson(largeJson);
        CheckEnemy(large.CreateEnemy(0), hugeText, hugeText);
        large.Advance();
        CheckEnemy(large.CreateEnemy(0), "125" + new string('0', 78), "15" + new string('0', 79));

        var tiny = EnemyCatalog.FromJson("""
            [{ "Name": "Tiny", "IconName": "car.png", "BaseLife": 0, "LifeModifier": 0,
               "BaseGold": 0, "GoldModifier": 0, "SpawnChance": 1 }]
            """);
        CheckEnemy(tiny.CreateEnemy(0), "1", "0");
        tiny.Advance();
        CheckEnemy(tiny.CreateEnemy(0), "1", "0");

        var extremeWeights = new EnemyCatalog(new[]
        {
            MakeTemplate("First", double.MaxValue),
            MakeTemplate("Second", double.MaxValue)
        });
        Check(extremeWeights.NormalizedChances[0] == 0.5, "Large weights must not overflow their sum.");
        Check(extremeWeights.CreateEnemy(0.5).Name == "Second", "Large weights must still support selection.");
        Check(catalog.CreateEnemy().Name is "First" or "Second", "Random selection must return a valid template.");

        string samplePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "enemies.sample.json");
        var sample = EnemyCatalog.LoadFromJson(samplePath);
        Check(sample.Count == 1, "The actual editor export must load.");
        Check(sample.CreateEnemy(0).Name == "машина литвина", "Cyrillic names must survive loading.");
        Check(sample.CreateEnemy(0).IconName == "car.png", "Icon filenames must survive loading.");
        CheckEnemy(sample.CreateEnemy(0), "100", "10");

        foreach (double invalid in new[] { -0.1, 1, double.NaN, double.PositiveInfinity })
            ExpectException<ArgumentOutOfRangeException>(() => catalog.CreateEnemy(invalid));
        ExpectException<ArgumentException>(() => new EnemyCatalog(Array.Empty<EnemyTemplate>()));
        ExpectException<ArgumentException>(() => new EnemyCatalog(new[] { MakeTemplate("Never", 0) }));
        ExpectException<ArgumentException>(() => new EnemyCatalog(new EnemyTemplate[] { null! }));
        ExpectException<ArgumentOutOfRangeException>(() => MakeTemplate("Invalid", double.NaN));
        ExpectException<ArgumentOutOfRangeException>(() => MakeTemplate("Invalid", -1));
        ExpectException<ArgumentNullException>(() => EnemyCatalog.FromJson(null!));
        foreach (string invalid in new[]
        {
            "[]", "null", "{}", "[null]", "not json",
            json.Replace("\"SpawnChance\": 20", "\"SpawnChance\": -1"),
            json.Replace("\"Name\": \"First\"", "\"Name\": null"),
            json.Replace("\"BaseLife\": 100", "\"BaseLife\": -1"),
            json.Replace("\"BaseLife\": 100", "\"BaseLife\": 1.5"),
            json.Replace("\"BaseLife\": 100", "\"BaseLife\": \"100\""),
            json.Replace("\"BaseGold\": 10", "\"BaseGold\": -1"),
            json.Replace("\"LifeModifier\": 1.2", "\"LifeModifier\": -1"),
            json.Replace("\"GoldModifier\": 1.5", "\"GoldModifier\": -1"),
            json.Replace("\"BaseLife\": 100,", ""),
            json.Replace("\"SpawnChance\": 20", "\"SpawnChance\": 0").Replace("\"SpawnChance\": 80", "\"SpawnChance\": 0")
        })
            ExpectException<InvalidDataException>(() => EnemyCatalog.FromJson(invalid));

        return passed;

        EnemyTemplate MakeTemplate(string name, double chance) =>
            new(name, "car.png", new BigNumber("1"), 1m, new BigNumber("0"), 1m, chance);

        void CheckEnemy(Enemy enemy, string health, string gold)
        {
            Check(enemy.Health.ToString() == health, $"Expected health {health}, got {enemy.Health}.");
            Check(enemy.GoldReward.ToString() == gold, $"Expected gold {gold}, got {enemy.GoldReward}.");
        }

        void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            passed++;
        }

        void ExpectException<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                passed++;
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
        }
    }
}
