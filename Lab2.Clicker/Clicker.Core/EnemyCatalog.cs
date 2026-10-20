using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clicker.Core;

public sealed class EnemyCatalog
{
    private static readonly BigNumber One = new("1");
    private static readonly BigNumber Zero = new("0");
    private readonly Entry[] entries;
    private readonly double[] chances;

    public int Count => entries.Length;
    public BigNumber Stage { get; private set; } = One;
    public IReadOnlyList<double> NormalizedChances { get; }

    public EnemyCatalog(IEnumerable<EnemyTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        EnemyTemplate[] items = templates.ToArray();
        if (items.Length == 0)
            throw new ArgumentException("Список шаблонов пуст.", nameof(templates));
        if (items.Any(item => item is null))
            throw new ArgumentException("Список содержит пустой шаблон.", nameof(templates));

        entries = items.Select(item => new Entry(item)).ToArray();
        double largestChance = items.Max(item => item.SpawnChance);
        if (largestChance == 0)
            throw new ArgumentException("Хотя бы один шанс появления должен быть больше нуля.", nameof(templates));

        // Масштабирование на максимум защищает сумму больших весов от переполнения.
        double sum = items.Sum(item => item.SpawnChance / largestChance);
        chances = items.Select(item => item.SpawnChance / largestChance / sum).ToArray();
        NormalizedChances = Array.AsReadOnly(chances);
    }

    public static EnemyCatalog LoadFromJson(string path) => FromJson(File.ReadAllText(path));

    public static EnemyCatalog FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            List<TemplateData?>? data = JsonSerializer.Deserialize<List<TemplateData?>>(json);
            if (data is null)
                throw new InvalidDataException("В JSON должен быть массив шаблонов.");

            var templates = new List<EnemyTemplate>();
            foreach (TemplateData? item in data)
            {
                if (item is null)
                    throw new InvalidDataException("JSON содержит пустой шаблон.");

                templates.Add(new EnemyTemplate(item.Name, item.IconName,
                    ReadNumber(item.BaseLife), item.LifeModifier,
                    ReadNumber(item.BaseGold), item.GoldModifier, item.SpawnChance));
            }

            return new EnemyCatalog(templates);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException)
        {
            throw new InvalidDataException("Некорректный JSON шаблонов: " + exception.Message, exception);
        }
    }

    public Enemy CreateEnemy() => CreateEnemy(Random.Shared.NextDouble());

    public Enemy CreateEnemy(double chance)
    {
        if (!double.IsFinite(chance) || chance < 0 || chance >= 1)
            throw new ArgumentOutOfRangeException(nameof(chance), "Нужно число от 0 включительно до 1 не включительно.");

        double sum = 0;
        int lastPositive = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (chances[i] == 0)
                continue;

            lastPositive = i;
            sum += chances[i];
            if (chance < sum)
                return entries[i].CreateEnemy();
        }

        // Округление double может оставить небольшой зазор перед единицей.
        return entries[lastPositive].CreateEnemy();
    }

    // Вызывается после победы, до создания следующего противника.
    public void Advance()
    {
        foreach (Entry entry in entries)
            entry.Advance();
        Stage = Stage + One;
    }

    private static BigNumber ReadNumber(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException("Здоровье и золото должны быть целыми неотрицательными числами.");

        // Читаем все цифры, не преобразуя значение в int, long или double.
        return new BigNumber(value.GetRawText());
    }

    private sealed class Entry
    {
        private readonly EnemyTemplate template;
        private BigNumber health;
        private BigNumber gold;

        public Entry(EnemyTemplate template)
        {
            this.template = template;
            health = template.BaseLife == Zero ? One : template.BaseLife;
            gold = template.BaseGold;
        }

        public Enemy CreateEnemy() => new(template.Name, template.IconName, health, gold);

        public void Advance()
        {
            health = template.ScaleLife(health);
            gold = template.ScaleGold(gold);
        }
    }

    private sealed class TemplateData
    {
        [JsonRequired] public string Name { get; set; } = "";
        [JsonRequired] public string IconName { get; set; } = "";
        [JsonRequired] public JsonElement BaseLife { get; set; }
        [JsonRequired] public decimal LifeModifier { get; set; }
        [JsonRequired] public JsonElement BaseGold { get; set; }
        [JsonRequired] public decimal GoldModifier { get; set; }
        [JsonRequired] public double SpawnChance { get; set; }
    }
}
