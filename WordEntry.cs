using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageChromeExtension2DataExporter
{
    /// <summary>
    /// Представляет запись словаря с переводами и статусом изучения
    /// </summary>
    class WordEntry
    {
        public DateTime Date { get; set; }
        public bool IsLearned { get; set; }
        public List<string>? Translations { get; set; }
        public string? Word { get; set; }
    }
}
