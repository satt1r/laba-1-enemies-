using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace laba_1
{
    

    public class CEnemyTemplateList
    {
        private List<CEnemyTemplate> enemies;

        public CEnemyTemplateList()
        {
            enemies = new List<CEnemyTemplate>();
        }

        public void AddEnemy(string name, string iconName, int baseLife,
            double lifeModifier, int baseGold, double goldModifier, double spawnChance)
        {
            enemies.Add(new CEnemyTemplate(name, iconName, baseLife,
                lifeModifier, baseGold, goldModifier, spawnChance));
        }

        public CEnemyTemplate? GetEnemyByName(string name) =>
            enemies.FirstOrDefault(e => e.Name == name);

        public CEnemyTemplate GetEnemyByIndex(int id) => enemies[id];

        public void DeleteEnemyByName(string name)
        {
            var enemy = GetEnemyByName(name);
            if (enemy != null) enemies.Remove(enemy);
        }

        public void DeleteEnemyByIndex(int id) => enemies.RemoveAt(id);

        public List<string> GetListOfEnemyNames() =>
            enemies.Select(e => e.Name).ToList();

        public void SaveToJson(string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // кириллица не превратится в \uXXXX
            };
            File.WriteAllText(path, JsonSerializer.Serialize(enemies, options));
        }

        public void LoadFromJson(string path)
        {
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            enemies = JsonSerializer.Deserialize<List<CEnemyTemplate>>(json)
                      ?? new List<CEnemyTemplate>();
        }
    }
}
