using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LightningDB;
using LevelDB;

namespace LanguageChromeExtension2DataExporter
{
    public static class Dumper
    {
        public static void DumpLevelDbToJson(string dbPath, string outputPath)
        {
            var result = new Dictionary<string, object>();

            var options = new Options { CreateIfMissing = false };

            using (var db = new DB(options, dbPath))
            using (var it = db.CreateIterator())
            {
                for (it.SeekToFirst(); it.IsValid(); it.Next())
                {
                    try
                    {
                        string key = Encoding.UTF8.GetString(it.Key());

                        // Пытаемся прочитать значение как UTF8 строку
                        string rawValue;
                        try
                        {
                            rawValue = Encoding.UTF8.GetString(it.Value());
                        }
                        catch
                        {
                            // Если не удалось, сохраняем значение как Base64
                            rawValue = Convert.ToBase64String(it.Value());
                        }

                        // Пытаемся распарсить как JSON
                        object value = TryParseJson(rawValue);

                        result[key] = value;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при чтении записи: {ex.Message}");
                    }
                }
            }

            // Записываем в файл в красивом формате
            string jsonOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(outputPath, jsonOutput, Encoding.UTF8);
        }

        private static object TryParseJson(string input)
        {
            try
            {
                return JToken.Parse(input);
            }
            catch
            {
                return input;
            }
        }
    }
}
