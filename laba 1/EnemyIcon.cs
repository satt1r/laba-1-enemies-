using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba_1
{
    public class EnemyIcon
    {
        public EnemyIcon(string name) => Name = name;

        public string Name { get; set; } = "";       // имя файла с расширением
        public string ImagePath { get; set; } = "";  // полный путь до файла
    }
}
