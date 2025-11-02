using Microsoft.Extensions.Configuration;

namespace LanguageChromeExtension2DataExporter
{
    /// <summary>
    /// Загружает настройки из файла <c>appsettings.json</c> при первом обращении 
    /// и предоставляет методы для получения отдельных параметров приложения
    /// </summary>
    class ConfigurationHelper
    {
        private static IConfigurationRoot? _configuration;
        public static IConfigurationRoot? Configuration => _configuration;
        /// <summary>
        /// Конструктор, который инициализирует конфигурацию (<see cref="_configuration"/>),
        /// загружая её из файла <c>appsettings.json</c> в базовой директории приложения
        /// </summary>
        static ConfigurationHelper()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
        }
        /// <summary>
        /// Получает путь для сохранения данных из конфигурации (<see cref="Configuration"/>)
        /// </summary>
        /// <returns>Путь для сохранения данных или <c>null</c>, если конфигурация не загружена или параметр отсутствует</returns>
        public static string? GetSavePath()
        {
            if (Configuration == null) return null;
            return Configuration["SavePath"];
        }
        /// <summary>
        /// Возвращает значение произвольной настройки по ключу.
        /// </summary>
        /// <param name="key">Ключ параметра конфигурации (например, <c>Logging:LogLevel:Default</c>).</param>
        /// <returns>Значение параметра или <c>null</c>, если конфигурация не загружена или ключ не найден.</returns>
        public static string? GetSetting(string key)
        {
            if(Configuration == null) return null;
            return Configuration[key];
        }
    }
}