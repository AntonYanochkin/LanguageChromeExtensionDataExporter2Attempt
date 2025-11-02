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
    /// <summary>
    /// Представляет статический класс для дампа базы данных расширения браузера Chrome в output.JSON файл
    /// </summary>
    public static class Dumper
    {
        /// <summary>
        /// Выполняет дамп всех записей из базы данных LevelDB в JSON-файл
        /// </summary>
        /// <param name="dbPath">Путь к базе LevelDB, из которой нужно считать</param>
        /// <param name="outputPath">Путь к JSON-файлу, куда будут записаны данные из базы</param>
        public static void DumpLevelDbToJson(string dbPath, string outputPath)
        {
            var result = new Dictionary<string, object>();
            //Options — настройки LevelDB: CreateIfMissing = false означает, что база не будет создана, если она отсутствует
            var options = new Options { CreateIfMissing = false }; 

            using (var db = new DB(options, dbPath))
            using (var it = db.CreateIterator())
            {
                //SeekToFirst() — перемещаем итератор на первый элемент
                //IsValid() — проверяем, что элемент существует
                //Next() — переходим к следующему элементу
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

            // Записываем в файл в json формате
            string jsonOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(outputPath, jsonOutput, Encoding.UTF8);
        }
        /// <summary>
        /// Пытаеться распарсить входную строку как JSON.
        /// Если строка-параметр метода явялется JSON, то возвращается объект JToken.
        /// Если строка не являтеся JSON, возвращается сама строка
        /// </summary>
        /// <param name="input"></param>
        /// <returns>
        /// JToken, если входная строка корректный JSON;
        /// иначе исходная строка
        /// </returns>
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
