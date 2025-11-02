using Newtonsoft.Json; // Для Docx
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace LanguageChromeExtension2DataExporter
{
    /// <summary>
    /// Считывает данные из дамп-файла базы данных (расширения Chrome бразера) output.json и экспортирует необходимые переводы в новый .DOCX файл
    /// </summary>
    public static class Exporter
    {
        static string jsonContent = "";
        /// <summary>
        /// Генерирует строку с текущей датой и временем формата "yyyy-MM-dd_HH-mm-ss".
        /// Используется для создания уникальных имён файлов или идентификаторов на основе текущего момента
        /// </summary>
        /// <returns>Строка с текущей датой и временем формата "yyyy-MM-dd_HH-mm-ss"</returns>
        public static string GenerateName()
        {
            DateTime now = DateTime.Now;
            string fileName = now.ToString("yyyy-MM-dd_HH-mm-ss");
            return fileName;
        }
        /// <summary>
        /// Считывает "output.json" и создает новый .DOCX файл учитывая параметры метода
        /// </summary>
        /// <param name="fromDate">В файл будут добавлены слова сохренные ОТ такого-то числа</param>
        /// <param name="toDate">В файл будут добавлены слова сохренные ДО такого-то числа</param>
        /// <param name="savePath">Полный путь к папке в которой будет сохранен файл</param>
        /// <param name="fileName">Имя файла</param>
        public static void ReadAndCreate(DateTime? fromDate = null, DateTime? toDate = null, string? savePath = null, string? fileName = null)
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "output.json");
            jsonContent = File.ReadAllText(jsonPath);
            Dictionary<string, WordEntry>? words = JsonConvert.DeserializeObject<Dictionary<string, WordEntry>>(jsonContent);

            if (words != null)
            {
                Dictionary<string, WordEntry> filteredWords = new Dictionary<string, WordEntry>();
                foreach (KeyValuePair<string, WordEntry> pair in words)
                {
                    DateTime wordDate = pair.Value.Date.Date;
                    //даты не заданы - значит вся история переводов
                    if (fromDate == null && toDate == null) 
                    {
                        filteredWords.Add(pair.Key, pair.Value); 
                    }
                    //одна дата - либо от либо до
                    else if (fromDate != null && toDate == null) //от
                    {
                        if (wordDate >= fromDate.Value.Date)
                        {
                            filteredWords.Add(pair.Key, pair.Value);
                        }
                    }
                    else if (toDate != null && fromDate == null) //до
                    {
                        if (wordDate <= toDate.Value.Date)
                        {
                            filteredWords.Add(pair.Key, pair.Value);
                        }
                    }
                    //2 даты - от и до
                    else if (fromDate != null && toDate != null) //диапозон дат
                    {
                        if (wordDate >= fromDate.Value.Date && wordDate <= toDate.Value.Date) 
                        {
                            filteredWords.Add(pair.Key, pair.Value);
                        }
                    }                    
                }
                if (filteredWords.Count == 0)
                {
                    Console.WriteLine("Нет записей по заданной дате или диапазону.");
                    return;
                }
                //Нет переданной директории - ссохраняем туда же где бинарник программы
                savePath = string.IsNullOrEmpty(savePath) ? Directory.GetCurrentDirectory() :  savePath;
                //Если не передано имя файла - используем функцию GenerateName() - имя будет форматом("yyyy-MM-dd_HH-mm-ss");
                fileName = string.IsNullOrEmpty(fileName) ? $"{GenerateName()}.docx" : $"{fileName}.docx";
                string finalPath = Path.Combine(savePath, fileName);
                //savePath = string.IsNullOrEmpty(savePath) 
                //    ? Path.Combine(Directory.GetCurrentDirectory(), "words1.docx") 
                //    : Path.Combine(savePath, "words1.docx");

                CreateWordDoc(finalPath, filteredWords);
            }
            else
                Console.WriteLine($"Программа не смогла прочитать {jsonPath}");
        }
        /// <summary>
        /// Создает документ Word с таблицей, содержащей слова и их переводы.
        /// Каждое слово помещается в первую колонку, а соответствующие переводы — во вторую колонку.
        /// </summary>
        /// <param name="filePath">Имя для документа который надо создать</param>
        /// <param name="words">Словарь из слов и иих переводов (WordEntry)</param>
        static void CreateWordDoc(string filePath, Dictionary<string, WordEntry> words)
        {
            //Body, Paragraph, Run, Text — элементы OpenXml, описывающие структуру документа:
            //Body — тело.
            //Paragraph — абзац.
            //Run — «прогон» — часть абзаца с одинаковым стилем.
            //Text — текст.

            //Document
            // └── Body
            //      └── Paragraph
            //           └── Run
            //                └── Text

            using (var wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart(); // не разумею зачем AddMainDocumentPart, но в гайде так
                mainPart.Document = new Document(); //корневой элемент
                Body body = new Body(); // создал тело документа
                mainPart.Document.Append(body);


                //В Word таблица — это:
                //< Table > — сама таблица
                //<TableRow> — строка таблицы
                //< TableCell > — ячейка
                //А внутри TableCell лежат Paragraph, Run, Text, как в обычном тексте

                //А теперь создаем таблицу
                Table tableWords = new Table();

                TableProperties tableWordsProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 5 },
                        new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    ),
                    new TableWidth()
                    {
                        Width = "6000",
                        Type = TableWidthUnitValues.Pct
                    }
                );
                tableWords.Append(tableWordsProperties);

                //заполним таблицу
                foreach(KeyValuePair<string, WordEntry> pair in words)
                {
                    TableRow row = new TableRow();
                    for(int j = 0; j < 2; j++)
                    {
                        //TableColumn column = new Column( new Paragraph( new Run(new Text("ss"))));
                        TableCell cell;
                        if (j == 1)
                        {
                            var translations = pair.Value.Translations ?? new List<string>();
                            string joined = string.Join(", ", translations.Select((item, index) => $"{item}"));
                            cell = CreateCell(joined, true);
                        }
                        else cell = CreateCell($"{pair.Key}");
                        cell.Append(new TableCellProperties(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }));
                        row.Append(cell);
                    }
                    tableWords.Append(row);
                }

                //добавим таблицу в документ
                body.Append(tableWords);
            }
        }
        /// <summary>
        /// Создает ячейку таблицы для документа Word с заданным текстом
        /// </summary>
        /// <param name="text">Текст, который будет помещен в ячейку</param>
        /// <param name="alignRight">Если true, текст будет выровнен по правому краю; по умолчанию выравнивание слева</param>
        /// <returns>Объект TableCell, готовый для добавления в таблицу Word</returns>
        static TableCell CreateCell(string text, bool alignRight = false)
        {
            Paragraph paragraph = new Paragraph();

            if (alignRight)
            {
                paragraph.PrependChild(new ParagraphProperties(
                    new Justification() { Val = JustificationValues.Right }));
            }

            paragraph.Append(new Run(new Text(text)));

            TableCell cell = new TableCell(paragraph);
            cell.Append(new TableCellProperties(
                new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }));

            return cell;
        }
    }
}
    
