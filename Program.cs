using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Runtime.InteropServices;

namespace LanguageChromeExtension2DataExporter
{
    internal class Program
    {

        private const string TempDumpFolderName = "ChromeLevelDBDump";
        /// <summary>
        /// Разбирает аргументы командной строки и преобразует их в словарь ключ–значение.
        /// Поддерживает параметры в формате <c>--ключ значение</c> или флаги <c>--ключ</c>
        /// </summary>
        /// <param name="args">Массив аргументов командной строки, переданных в приложение</param>
        /// <returns>
        /// Словарь, где ключ — имя параметра без префикса "--", 
        /// а значение — указанное значение или "true" для флагов
        /// </returns>
        static Dictionary<string, string> GetCommandLineOptions(string[] args)
        {

            Dictionary<string, string> commandLineOptions = new Dictionary<string, string>();

            for(int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    //обрезать --to до to (или --from, --path или что-то другое) и записать в key
                    string key = args[i].ToLower().Substring(2);
                    //взять i+1 и записать в value
                    string value = (i + 1 < args.Length && !(args[i + 1].StartsWith("-"))) ? args[++i] : "true";// если флаг. сейчас флагов у меня нет. может потом надо будет добавить
                    commandLineOptions[key] = value;
                }
            }
            return commandLineOptions;
        }
        /// <summary>
        /// Метод для создания DOCX файла
        /// </summary>
        /// <param name="args"></param>
        /// <param name="savePath"></param>
        static void CreateDocxFile(string[] args, string? savePath)
        {
            Dictionary<string, string> commandLineOptions = Program.GetCommandLineOptions(args);
            //получаем аргументы командной строки
            commandLineOptions.TryGetValue("to", out string? toStr);
            commandLineOptions.TryGetValue("from", out string? fromStr);
            commandLineOptions.TryGetValue("path", out string? pathStr);
            commandLineOptions.TryGetValue("name", out string? nameStr);
            //finalPath равен:
            //pathStr  если передан аргумент приложения
            //savePath аргумент метода
            //иначе будет пустой и метод завершится
            string finalPath = pathStr ??  savePath ?? string.Empty;  // желательно всегда в appsettings.json SavePath определять
            if (string.IsNullOrEmpty(finalPath))
            {
                Console.WriteLine("Путь для сохранения не указан. Пути решения:\n   1. Добавьте в appsettings.json \"SavePath\": \"путь к папке в которую будете сохранять\"\n    2. Передайте путь к папке в которой сохраните файл в параметрах программы через --path \"путь к папке в которую будете сохранять\"");
                return; //ну а куда сохранять?
            }
            //преобразуем в DateTime
            bool isToDate = DateTime.TryParse(toStr, out DateTime toDate);
            bool isFromDate = DateTime.TryParse(fromStr, out DateTime fromDate);
            //Создаем .DOCX  
            if (isFromDate && isToDate) Exporter.ReadAndCreate(fromDate: fromDate, toDate: toDate, savePath: finalPath, fileName: nameStr);
            else if (isFromDate) Exporter.ReadAndCreate(fromDate: fromDate, savePath: finalPath, fileName: nameStr);
            else if (isToDate) Exporter.ReadAndCreate( toDate: toDate, savePath: finalPath, fileName: nameStr); 
            else Exporter.ReadAndCreate(savePath: finalPath, fileName: nameStr); 
        }

        static void Main(string[] args)
        {
            //настроил получение настроек из appsetting.json в ConfigurationHelper(у него стат конструктор)
            string? savePath = ConfigurationHelper.GetSavePath();

            string extensionId = ConfigurationHelper.GetSetting("ExtensionId") 
                ?? throw new InvalidOperationException("Не найден ExtensionId в appsettings.json");


            string? pathToExtension = ConfigurationHelper.GetSetting("ExtensionPath");
            pathToExtension = pathToExtension ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Chrome", "User Data", "Default", "Local Extension Settings");
            string userExtensionPath = Path.Combine(pathToExtension, extensionId);

            if (!Directory.Exists(userExtensionPath))
            {
                Console.WriteLine("❌ Не найдена папка расширения. Проверь ID!");
                return;
            }
            string tempPath;
            try
            {
                tempPath = Path.Combine(Path.GetTempPath(), TempDumpFolderName);         

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                Directory.CreateDirectory(tempPath);

                foreach (var file in Directory.GetFiles(userExtensionPath))
                    File.Copy(file, Path.Combine(tempPath, Path.GetFileName(file)));
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Нет доступа к файлам: {ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка при копировании файлов: {ex.Message}");
                return;
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Неожиданная ошибка: {ex.ToString()}");
                return;
            }

            Console.WriteLine("Копия LevelDB создана: " + tempPath);

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output.json");
            Dumper.DumpLevelDbToJson(tempPath, outputPath);

            Console.WriteLine($"Готово! Данные сохранены в: {outputPath}");

            CreateDocxFile(args, savePath);
        }
    }
}
