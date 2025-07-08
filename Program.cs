using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LanguageChromeExtension2DataExporter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string extensionId = "oliignefgaockfigpmjgkbgbifehhmic"; // замени на реальный ID
            string userDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                                    + @"\Google\Chrome\User Data\Profile 1\Local Extension Settings\" + extensionId;

            if (!Directory.Exists(userDataPath))
            {
                Console.WriteLine("❌ Не найдена папка расширения. Проверь ID!");
                return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), "ChromeLevelDBDump");
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);
            foreach (var file in Directory.GetFiles(userDataPath))
                File.Copy(file, Path.Combine(tempPath, Path.GetFileName(file)));

            Console.WriteLine("📦 Копия LevelDB создана: " + tempPath);

            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output.json");
            Dumper.DumpLevelDbToJson(tempPath, outputPath);

            Console.WriteLine("✅ Готово! Данные сохранены в:");
            Console.WriteLine(outputPath);
        }
    }
}
