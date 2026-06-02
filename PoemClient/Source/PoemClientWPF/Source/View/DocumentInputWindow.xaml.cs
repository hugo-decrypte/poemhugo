using Microsoft.Win32;
using NPOI.SS.Formula.Functions;


// Bibliothèques pour l'extraction locale
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
using Org.BouncyCastle.Security;
using PoemClient.Source.Tools;
using PoemClient.Source.Tools.IA;
using PoemClient.Source.View;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
// Packages pour les PDF et Images
using UglyToad.PdfPig;
using ICell = NPOI.SS.UserModel.ICell;

namespace PoemClient.View
{
    // Pour stocker nos zones découpées spatialement
    public class DocumentZones
    {
        public string HeaderText { get; set; }
        public string TableText { get; set; }
    }

    public partial class DocumentInputWindow : Window
    {
        private readonly Config config;
        private readonly MainWindow _main;
        private static DocumentInputWindow instance = null;
        public bool canceled = false;
        private bool isProcessing = false;
        private DataFormatConfig _formatRules;

        private string[] keyValues;

        public DocumentInputWindow(string[] args)
        {
            InitializeComponent();
            _main = MainWindow.main;
            this.config = _main.GetConfig();

            this.keyValues = args;

            InitializedDataContext();

            string implicitModel = config.GetKeyValue("ImplicitModel");
            if (!string.IsNullOrWhiteSpace(implicitModel))
            {
                AiModelSelector.Visibility = Visibility.Collapsed;
                AiSeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void InitializedDataContext()
        {
            string implicitModel = config.GetKeyValue("ImplicitModel");
            string defaultFrom = !string.IsNullOrWhiteSpace(implicitModel)
                ? implicitModel
                : config.GetKeyValue("DefaultModel") ?? "Local";

            DataContext = new
            {
                DocumentReader = config.GetKeyValue("DocumentInput"),
                ToAnalyse = config.GetKeyValue("Document"),
                Format = config.GetKeyValue("Format"),
                File = config.GetKeyValue("File"),
                Folder = config.GetKeyValue("Folder"),
                Save = config.GetKeyValue("Save"),
                ToSave = config.GetKeyValue("Result"),
                Result = config.GetKeyValue("Message"),
                AnalyseSave = config.GetKeyValue("Execute"),
                SelectedFrom = defaultFrom,
                LanguagesAI = GetLangAIList(),
                PathFile = GetPathFromFileText(),
                PathSave = GetPathToFileText(),
                PathFormatFile = GetPathFormatFileText()
            };
        }

        private string[] GetLangAIList()
        {
            string[] langlist = config.GetKeyValue("ModelList")?.Split(',');
            return langlist;
        }

        public string GetPathFormatFileText()
        {
            for (int i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i].Contains(".cfg")) return keyValues[i];
            }
            return null;
        }

        public string GetPathFromFileText()
        {
            if (keyValues != null && keyValues.Length >= 2) return keyValues[keyValues.Length - 2];
            return "Fichier introuvable";
        }


        public string GetPathToFileText()
        {
            if (keyValues != null && keyValues.Length >= 1) return keyValues[keyValues.Length - 1];
            return "Dossier introuvable";
        }

        public static DocumentInputWindow GetInstance(string[] args)
        {
            if (instance == null) instance = new DocumentInputWindow(args);
            if (args != null)
            {
                instance.keyValues = args;
            }
            return instance;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isProcessing)
            {
                var result = MessageBox.Show("Une analyse est en cours. Voulez-vous vraiment quitter ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            instance = null;
            canceled = true;
        }

        private void updateText(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { this.Loading.Text = text; });
        }

        private void AISelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string FromSelected = AiModelSelector.SelectedItem as string;
            UpdateConfig("DefaultModel", FromSelected);
        }

        private bool UpdateConfig(string key, string value)
        {
            if (config.GetKeyValue(key) != value)
            {
                config.SetKeyValueKeepLineBreak(key, value);
                return true;
            }
            return false;
        }

        #region UI Event Handlers

        private void BtnBrowseFormatFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichiers de configuration|*.cfg;*.txt;*.ini|Tous les fichiers (*.*)|*.*";
            openFileDialog.Title = "Sélectionnez le fichier de format des tables";
            if (openFileDialog.ShowDialog() == true)
            {
                txtFormatFilePath.Text = openFileDialog.FileName;
            }
        }

        private void BtnBrowseInputFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Supported Documents|*.pdf;*.png;*.jpg;*.jpeg;*.xlsx;*.xls;*.csv;*.docx;*.bmp;*.tif;*.tiff;*.gif|All files (*.*)|*.*";
            openFileDialog.Title = "Sélectionnez un document à analyser";
            if (openFileDialog.ShowDialog() == true)
            {
                txtFilePath.Text = openFileDialog.FileName;
                updateText(config.GetKeyValue("Loading"));
            }
        }

        private void BtnBrowseInputFolder_Click(object sender, RoutedEventArgs e)
            {
                using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderDialog.Description = "Sélectionnez un dossier à analyser";
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        txtFilePath.Text = folderDialog.SelectedPath;
                        updateText(config.GetKeyValue("Loading"));
                    }
                }
            }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Enregistrer le résultat d'extraction",
                Filter = "Fichier Texte (*.txt)|*.txt|Fichier CSV (*.csv)|*.csv",
                DefaultExt = ".txt",
                FileName = "Resultat_Extraction"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                txtOutputPath.Text = saveFileDialog.FileName;

            }
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            string rawOutputPath = txtOutputPath.Text ?? "";
            string rawFilePath = txtFilePath.Text ?? "";

            string absoluteFilePath = ConvertToAbsolutePath(rawFilePath);
            string absoluteOutputPath = ConvertToAbsolutePath(rawOutputPath);

            bool isDirectory = Directory.Exists(absoluteFilePath);
            bool isFile = File.Exists(absoluteFilePath);

            if (!isDirectory && !isFile)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un fichier ou un dossier source valide.", config.GetKeyValue("Info"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(absoluteOutputPath))
            {
                MessageBox.Show("Veuillez choisir un emplacement pour sauvegarder le résultat.", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Utils.IsInternetAvailable())
            {
                MessageBox.Show(config.GetKeyValue("NetworkError"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            isProcessing = true;
            this.btnAnalyze.IsEnabled = false;
            this.txtFilePath.IsEnabled = false;
            this.txtOutputPath.IsEnabled = false;
            this.txtFormatFilePath.IsEnabled = false;
            this.AiModelSelector.IsEnabled = false;

            try
            {
                string rawFormatPath = txtFormatFilePath.Text?.ToString() ?? "";
                string absoluteFormatPath = ConvertToAbsolutePath(rawFormatPath);

                string defaultText = config.GetKeyValue("PathFormatFile") ?? "Sélectionner un fichier (Optionnel)";

                if (!string.IsNullOrWhiteSpace(rawFormatPath) && rawFormatPath != defaultText && File.Exists(absoluteFormatPath))
                {
                    _formatRules = FormatParser.LoadConfig(absoluteFormatPath);
                }
                else
                {
                    string defaultPath = ConvertToAbsolutePath(@"Doc\Examples\DataFormatOrder.cfg");
                    if (File.Exists(defaultPath))
                    {
                        _formatRules = FormatParser.LoadConfig(defaultPath);
                    }
                    else
                    {
                        _formatRules = null;
                        Console.WriteLine("dataFormat.cfg introuvable. Traitement 100% IA activé.");
                    }
                }

                string safeFilePath = absoluteFilePath;
                string rawResult = string.Empty;

                ModeleIA iaClient = null;
                int selectedAi = -1;
                Dispatcher.Invoke(() => { selectedAi = AiModelSelector.SelectedIndex; });

                if (selectedAi == 1)
                {
                    iaClient = new ChatGPTClient(MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl"), MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel"), MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey"));
                    if (_main.WarningAIChatGPT)
                    {
                        _main.WarningAIChatGPT = false;
                        MessageBoxResult result = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No) { ResetUI(); return; }
                    }
                }
                else if (selectedAi == 2)
                {
                    iaClient = new GeminiClient(MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiUrl"), MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiModel"), MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiKey"));
                    if (_main.WarningAIGemini)
                    {
                        _main.WarningAIGemini = false;
                        MessageBoxResult result = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No) { ResetUI(); return; }
                    }
                }
                else if (selectedAi == 0)
                {
                    iaClient = new Ollama(config);
                }

                updateText(config.GetKeyValue("Running"));

                if (isFile)
                {
                    rawResult = await ExtractTextFromFile(safeFilePath, iaClient);
                }
                else if (isDirectory)
                {
                    rawResult = await ProcessFolder(safeFilePath, iaClient);
                }

                if (rawResult.Contains("Erreur :"))
                {
                    updateText(config.GetKeyValue("Failure"));
                    MessageBox.Show($"{rawResult}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.WriteAllText(absoluteOutputPath, rawResult, Encoding.UTF8);
                updateText(config.GetKeyValue("Success"));
            }
            catch (Exception ex)
            {
                updateText(config.GetKeyValue("Failure"));
                StringBuilder sb = new StringBuilder();
                Exception curr = ex;
                while (curr != null)
                {
                    sb.AppendLine($"[{curr.GetType().Name}] : {curr.Message}");
                    //sb.AppendLine(curr.StackTrace);
                    //sb.AppendLine(new string('-', 50));
                    curr = curr.InnerException;
                }
                //MessageBox.Show(sb.ToString() + "J'ai écrit le rapport de crash complet dans la grande zone de texte 'Résultat'.\n\nFerme ce message, copie tout le texte et envoie-le moi !", "Diagnostic", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show(sb.ToString(), config.GetKeyValue("Warning"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ResetUI();
            }
        }

        private void ResetUI()
        {
            isProcessing = false;
            this.btnAnalyze.IsEnabled = true;
            this.txtFilePath.IsEnabled = true;
            this.txtOutputPath.IsEnabled = true;
            this.txtFormatFilePath.IsEnabled = true;
            this.AiModelSelector.IsEnabled = true;
        }

        #endregion

        #region Batch Processing (Dossiers)

        private async Task<string> ProcessFolder(string folderPath, ModeleIA iaClient)
        {
            string[] allFiles = Directory.GetFiles(folderPath);
            int totalSheets = 0;
            StringBuilder combinedContent = new StringBuilder();

            foreach (string file in allFiles)
            {
                if (Path.GetFileName(file) == Path.GetFileName(txtOutputPath.Text.ToString())) continue;
                if (!IsSupportedExtension(file)) continue;
                updateText($"{Path.GetFileName(file)}");

                string fileResult = await ExtractTextFromFile(file, iaClient);

                if (fileResult.StartsWith("SHEET_NUMBER="))
                {
                    int firstLineEnd = fileResult.IndexOf('\n');
                    if (firstLineEnd == -1) firstLineEnd = fileResult.Length;

                    string firstLine = fileResult.Substring(0, firstLineEnd);
                    string numStr = firstLine.Replace("SHEET_NUMBER=", "").Replace(";", "").Trim();
                    if (int.TryParse(numStr, out int count))
                    {
                        totalSheets += count;
                    }

                    if (firstLineEnd < fileResult.Length)
                    {
                        combinedContent.Append(fileResult.Substring(firstLineEnd + 1));
                        if (!fileResult.EndsWith("\n")) combinedContent.AppendLine();
                    }
                }
                else
                {
                    combinedContent.AppendLine(fileResult);
                }
            }

            if (combinedContent.Length == 0) return "SHEET_NUMBER=0;\n";
            return $"SHEET_NUMBER={totalSheets};\n" + combinedContent.ToString();
        }

        private bool IsSupportedExtension(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".csv" || ext == ".xlsx" || ext == ".xls" || ext == ".tif" || ext == ".tiff" || ext == ".gif" ||
                   ext == ".docx" || ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }

        #endregion

        #region Routing Principal

        private async Task<string> ExtractTextFromFile(string filePath, ModeleIA iaClient)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                //case ".txt":
                case ".csv":
                    return await VerifyAndFormatTxtCsvAsync(filePath, iaClient);
                case ".xlsx":
                case ".xls":
                    return await ExtractExcelTextAsync(filePath, iaClient);
                case ".docx":
                    return await ExtractWordTextAsync(filePath, iaClient);
                case ".pdf":
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                case ".tif":
                case ".tiff":
                case ".gif":
                    return await ProcessPdfOrImageLocallyAsync(filePath, iaClient);
                default:
                    throw new NotSupportedException($"Le format {extension} n'est pas supporté.");
            }
        }

        private string FormatCsvCell(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string cleanedText = text.Replace("\r", "").Replace("\n", " ").Replace("\"", "\"\"");
            if (cleanedText.Contains(";")) return $"\"{cleanedText}\"";
            return cleanedText;
        }

        private string CleanAiResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse)) return "";
            string[] lines = aiResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (!line.StartsWith("SHEET_NUMBER=")) sb.AppendLine(line);
            }
            return sb.ToString();
        }

        #endregion

        #region Extracteurs Hybrides (Local + IA)

        private async Task<string> VerifyAndFormatTxtCsvAsync(string filePath, ModeleIA iaClient)
        {
            string content = File.ReadAllText(filePath, Encoding.Default);
            string title = Path.GetFileNameWithoutExtension(filePath);
            
            if (_formatRules != null && _formatRules.Sheets.ContainsKey(title))
            {
                return FormatRawTextToStrictCsv(content, title, _formatRules.Sheets[title]);
            }
            return await FormatRawTextWithAI(content, title, iaClient);
        }

        private async Task<string> ExtractWordTextAsync(string filePath, ModeleIA iaClient)
        {
            StringBuilder finalOutput = new StringBuilder();
            int tableCount = 0;
            int textBlockCount = 0;
            int totalSheetsDetected = 0;
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                XWPFDocument document = new XWPFDocument(file);
                List<string> currentTextParagraphs = new List<string>();

                foreach (IBodyElement element in document.BodyElements)
                {
                    if (element is XWPFParagraph paragraph && !string.IsNullOrWhiteSpace(paragraph.Text))
                    {
                        currentTextParagraphs.Add(FormatCsvCell(paragraph.Text) + ";");
                    }
                    else if (element is XWPFTable table)
                    {
                        if (currentTextParagraphs.Count > 0)
                        {
                            textBlockCount++;
                            string blockName = $"Texte_{textBlockCount}";
                            totalSheetsDetected++;

                            StringBuilder txtSb = new StringBuilder();
                            foreach (var p in currentTextParagraphs) txtSb.AppendLine(p);
                            string aiResult = await FormatRawTextWithAI(txtSb.ToString(), blockName, iaClient);
                            finalOutput.Append(CleanAiResponse(aiResult));
                            currentTextParagraphs.Clear();
                        }

                        tableCount++;
                        string tableName = $"Tableau_{tableCount}";
                        totalSheetsDetected++;

                        StringBuilder rawTableText = new StringBuilder();
                        foreach (XWPFTableRow row in table.Rows)
                        {
                            foreach (XWPFTableCell cell in row.GetTableCells())
                            {
                                rawTableText.Append($"{FormatCsvCell(cell.GetText())};");
                            }
                            rawTableText.AppendLine();
                        }

                        if (_formatRules != null && _formatRules.Sheets.ContainsKey(tableName))
                        {
                            finalOutput.AppendLine($"SHEET_NAME={tableName};{_formatRules.Sheets[tableName].ExpectedColumns};{table.Rows.Count};");
                            finalOutput.Append(rawTableText.ToString());
                        }
                        else
                        {
                            string aiResult = await FormatRawTextWithAI(rawTableText.ToString(), tableName, iaClient);
                            finalOutput.Append(CleanAiResponse(aiResult));
                        }
                    }
                }

                if (currentTextParagraphs.Count > 0)
                {
                    textBlockCount++;
                    string blockName = $"Texte_{textBlockCount}";
                    totalSheetsDetected++;
                    StringBuilder txtSb = new StringBuilder();
                    foreach (var p in currentTextParagraphs) txtSb.AppendLine(p);

                    string aiResult = await FormatRawTextWithAI(txtSb.ToString(), blockName, iaClient);
                    finalOutput.Append(CleanAiResponse(aiResult));
                }
            }

            return $"SHEET_NUMBER={totalSheetsDetected};\n" + finalOutput.ToString();
        }

        private async Task<string> ExtractExcelTextAsync(string filePath, ModeleIA iaClient)
        {
            StringBuilder finalOutput = new StringBuilder();
            IWorkbook workbook;

            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = WorkbookFactory.Create(file);
                DataFormatter formatter = new DataFormatter();
                finalOutput.AppendLine($"SHEET_NUMBER={workbook.NumberOfSheets};");

                foreach (ISheet sheet in workbook)
                {
                    string sheetName = sheet.SheetName;
                    if (_formatRules != null && _formatRules.Sheets.TryGetValue(sheetName, out SheetConfig configFeuille))
                    {
                        StringBuilder sheetBody = new StringBuilder();
                        int numRows = 0;
                        for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum; rowIdx++)
                        {
                            IRow row = sheet.GetRow(rowIdx);
                            if (row == null) continue;

                            numRows++;
                            for (int colIdx = 0; colIdx < configFeuille.ExpectedColumns; colIdx++)
                            {
                                ICell cell = row.GetCell(colIdx, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                                string cellValue = cell == null ? "" : formatter.FormatCellValue(cell);
                                sheetBody.Append(FormatCsvCell(cellValue) + ";");
                            }
                            sheetBody.AppendLine();
                        }

                        finalOutput.AppendLine($"SHEET_NAME={sheetName};{configFeuille.ExpectedColumns};{numRows};");
                        finalOutput.Append(sheetBody.ToString());
                    }
                    else
                    {
                        StringBuilder rawSheetText = new StringBuilder();
                        for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum; rowIdx++)
                        {
                            IRow row = sheet.GetRow(rowIdx);
                            if (row == null) continue;
                            for (int colIdx = 0; colIdx < (row.LastCellNum > 0 ? row.LastCellNum : 1); colIdx++)
                            {
                                ICell cell = row.GetCell(colIdx);
                                rawSheetText.Append((cell == null ? "" : formatter.FormatCellValue(cell)) + ";");
                            }
                            rawSheetText.AppendLine();
                        }

                        string aiResult = await FormatRawTextWithAI(rawSheetText.ToString(), sheetName, iaClient);
                        finalOutput.Append(CleanAiResponse(aiResult));
                    }
                }
            }
            return finalOutput.ToString();
        }

        #endregion

        #region Formatage IA & OCR

        private string FormatRawTextToStrictCsv(string content, string title, SheetConfig config)
        {
            if (content.TrimStart().StartsWith("SHEET_NUMBER=")) {
                return content;
            }
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("SHEET_NUMBER=1;");
            sb.AppendLine($"SHEET_NAME={title};{config.ExpectedColumns};{lines.Length};");
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string processedLine = Regex.Replace(line.Trim(), @"\t+|\s{2,}", ";");
                string[] cols = processedLine.Split(';');

                for (int i = 0; i < config.ExpectedColumns; i++)
                {
                    string val = i < cols.Length ? cols[i] : "";
                    sb.Append(val + ";");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private SheetConfig DetectTableFromContent(string content, DataFormatConfig rules)
        {
            if (rules == null || rules.Sheets.Count == 0 || string.IsNullOrWhiteSpace(content)) return null;
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int searchLimit = Math.Min(15, lines.Length);
            string headerContext = string.Join("\n", lines.Take(searchLimit)).ToLower();

            SheetConfig bestMatch = null;
            int maxMatches = 0;
            foreach (var sheet in rules.Sheets.Values)
            {
                if (headerContext.Contains(sheet.SheetName.ToLower()))
                {
                    return sheet;
                }

                int matches = 0;
                foreach (var col in sheet.Columns)
                {
                    if (!string.IsNullOrWhiteSpace(col.Label) && headerContext.Contains(col.Label.ToLower()))
                    {
                        matches++;
                    }
                }

                if (matches > maxMatches && matches >= 2)
                {
                    maxMatches = matches;
                    bestMatch = sheet;
                }
            }

            return bestMatch;
        }

        private async Task<string> ProcessPdfOrImageLocallyAsync(string filePath, ModeleIA iaClient)
        {
            string title = Path.GetFileNameWithoutExtension(filePath);

            string rawText = await Task.Run(() => ExtractRawImageText(filePath, _formatRules));
            SheetConfig detectedConfig = DetectTableFromContent(rawText, _formatRules);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return "SHEET_NUMBER=1;\nSHEET_NAME=Erreur_Lecture;1;1;\nLe document est vide.;\n";
            }

            return await FormatRawTextWithAI(rawText, title, iaClient, detectedConfig);
        }


        public class TableCell
        {
            public string Text { get; set; }
            public double MinY { get; set; }
            public double MaxY { get; set; }
            public double CentroidY => (MinY + MaxY) / 2.0; // Le centre vertical exact du bloc
        }

        // Ancienne façon de procéder pour les pdf (voir aussi ExtractRawPdfText)
        private DocumentZones SplitDocumentByHeaders(UglyToad.PdfPig.Content.Page page, SheetConfig config)
        {
            var words = page.GetWords().ToList();
            var zones = new DocumentZones { HeaderText = "", TableText = "" };
            if (!words.Any()) return zones;

            // Groupement basique pour trouver la ligne d'en-tête principale
            var lines = words.GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 7.0) * 7.0)
                             .OrderByDescending(g => g.Key).ToList();

            double splitY = -1;

            // 1. DÉTECTION INTELLIGENTE DE L'EN-TÊTE (Le "Scoring")
            if (config != null)
            {
                // On découpe les labels, on enlève les apostrophes (ex: d'achat -> achat) et on garde les mots > 2 lettres (prix, qté)
                var configKeywords = config.Columns
                                           .SelectMany(c => c.Label.ToLower().Split(new[] { ' ', '\'', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                                           .Where(s => s.Length > 2)
                                           .Distinct() // On ne compte pas les doublons
                                           .ToList();

                int maxScore = 0;

                foreach (var line in lines)
                {
                    string text = string.Join(" ", line.Select(w => w.Text)).ToLower();

                    // On calcule combien de mots-clés uniques sont présents sur cette ligne
                    int currentScore = configKeywords.Count(kw => text.Contains(kw));

                    // Si cette ligne bat le record précédent, elle devient notre candidat numéro 1
                    if (currentScore > maxScore && currentScore >= 2)
                    {
                        maxScore = currentScore;
                        splitY = line.Key; // On sauvegarde la hauteur de cette ligne gagnante
                    }
                }
            }

            // Mode de secours si on ne trouve aucun en-tête
            if (splitY == -1)
            {
                zones.TableText = string.Join("\n", lines.Select(l => string.Join(" ", l.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))));
                return zones;
            }

            // 2. CRÉATION DE LA "BANDE D'EN-TÊTE" (Réglage fin de la hauteur)
            // On utilise Centroid.Y avec +/- 12.0 pour attraper les doubles lignes sans toucher aux données
            var headerBandWords = words
                .Where(w => w.BoundingBox.Centroid.Y <= splitY + 12.0 && w.BoundingBox.Centroid.Y >= splitY - 12.0)
                .OrderBy(w => w.BoundingBox.Left)
                .ToList();

            List<double> columnStarts = new List<double>();
            List<string> columnNames = new List<string>();

            if (headerBandWords.Any())
            {
                columnStarts.Add(-1000.0);
                List<UglyToad.PdfPig.Content.Word> currentColWords = new List<UglyToad.PdfPig.Content.Word> { headerBandWords[0] };

                for (int i = 0; i < headerBandWords.Count - 1; i++)
                {
                    double gap = headerBandWords[i + 1].BoundingBox.Left - headerBandWords[i].BoundingBox.Right;
                    bool isOverlapping = headerBandWords[i + 1].BoundingBox.Left < (headerBandWords[i].BoundingBox.Right - 2.0);

                    if (gap > 4.0 && !isOverlapping)
                    {
                        string colName = string.Join(" ", currentColWords
                            .GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 3.0) * 3.0)
                            .OrderByDescending(g => g.Key)
                            .SelectMany(g => g.OrderBy(w => w.BoundingBox.Left))
                            .Select(w => w.Text));

                        columnNames.Add(colName);

                        double midpointBoundary = headerBandWords[i].BoundingBox.Right + (gap / 2.0);
                        columnStarts.Add(midpointBoundary);

                        currentColWords.Clear();
                        currentColWords.Add(headerBandWords[i + 1]);
                    }
                    else
                    {
                        currentColWords.Add(headerBandWords[i + 1]);
                    }
                }

                string finalColName = string.Join(" ", currentColWords
                    .GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 3.0) * 3.0)
                    .OrderByDescending(g => g.Key)
                    .SelectMany(g => g.OrderBy(w => w.BoundingBox.Left))
                    .Select(w => w.Text));
                columnNames.Add(finalColName);
                columnStarts.Add(10000.0);
            }

            // 3. RÉCUPÉRATION DE TOUS LES MOTS DU TABLEAU (Sous l'en-tête)
            var tableWords = words.Where(w => w.BoundingBox.Centroid.Y < splitY - 12.0).ToList();

            // === NOUVEAU BOUCLIER ANTI-FOOTER (La Coupe Nette) ===
            var linesForFooter = tableWords.GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 7.0) * 7.0)
                                           .OrderByDescending(g => g.Key).ToList();

            double bottomCutoffY = -10000.0;
            foreach (var line in linesForFooter)
            {
                string text = string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).ToLower();
                // Si on repère la fin du tableau
                if (text.Contains("total") || text.Contains("s.a.r.l") || text.Contains("siret") || text.Contains("tva"))
                {
                    bottomCutoffY = line.Key + 10.0; // On crée un mur infranchissable juste au-dessus
                    break;
                }
            }

            // On détruit de la liste TOUS les mots qui se trouvent en dessous du mur
            tableWords = tableWords.Where(w => w.BoundingBox.Centroid.Y > bottomCutoffY).ToList();
            // =======================================================

            // 4. TROUVER LES ANCRES (Clé primaire)
            var anchorWords = tableWords
                .Where(w => (w.BoundingBox.Left + w.BoundingBox.Width / 2.0) >= columnStarts[0] &&
                            (w.BoundingBox.Left + w.BoundingBox.Width / 2.0) < columnStarts[1])
                .GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 7.0) * 7.0)
                .OrderByDescending(g => g.Key)
                .Select(g => new {
                    Text = string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)),
                    Y = g.Key
                })
                .ToList();

            if (!anchorWords.Any()) return zones;
            // 5. CALCUL DES FRONTIÈRES HORIZONTALES (La Guillotine)
            List<double> rowBoundaries = new List<double>();
            rowBoundaries.Add(10000.0); // Plafond
            for (int i = 0; i < anchorWords.Count - 1; i++)
            {
                double midpoint = (anchorWords[i].Y + anchorWords[i + 1].Y) / 2.0;
                rowBoundaries.Add(midpoint);
            }
            rowBoundaries.Add(-10000.0); // Plancher

            StringBuilder tableSb = new StringBuilder();

            // 6. RANGEMENT DES MOTS DANS LEURS CASES (Découpage spatial total)
            for (int i = 0; i < anchorWords.Count; i++)
            {
                double topBoundary = rowBoundaries[i];
                double bottomBoundary = rowBoundaries[i + 1];

                // On isole les mots de la ligne
                var wordsInRow = tableWords
                    .Where(w => w.BoundingBox.Centroid.Y <= topBoundary && w.BoundingBox.Centroid.Y > bottomBoundary)
                    .ToList();

                // On prépare les listes pour chaque cellule
                List<UglyToad.PdfPig.Content.Word>[] colWords = new List<UglyToad.PdfPig.Content.Word>[columnStarts.Count - 1];
                for (int c = 0; c < colWords.Length; c++) colWords[c] = new List<UglyToad.PdfPig.Content.Word>();

                // On trie chaque mot dans la bonne colonne selon son point central (midX)
                foreach (var w in wordsInRow)
                {
                    double midX = w.BoundingBox.Left + (w.BoundingBox.Width / 2.0);
                    for (int c = 0; c < columnStarts.Count - 1; c++)
                    {
                        if (midX >= columnStarts[c] && midX < columnStarts[c + 1])
                        {
                            colWords[c].Add(w);
                            break;
                        }
                    }
                }

                // Création des balises [Colonne: Valeur]
                List<string> rowTags = new List<string>();
                for (int c = 0; c < columnStarts.Count - 1; c++)
                {
                    if (colWords[c].Any())
                    {
                        // Tri Magique de la donnée (Haut en bas, puis gauche à droite avec tolérance 3.0)
                        string val = string.Join(" ", colWords[c]
                                .GroupBy(w => Math.Round(w.BoundingBox.Centroid.Y / 2.5) * 2.5) // Précision accrue
                                .OrderByDescending(g => g.Key) // Du haut vers le bas
                                .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))) // De gauche à droite
                            ).Trim();

                        rowTags.Add($"[{columnNames[c]}: {val}]");
                    }
                }
                tableSb.AppendLine(string.Join(" ", rowTags));
            }

            // Le texte au-dessus de l'en-tête (pour l'IA générale si besoin)
            zones.HeaderText = string.Join("\n", lines.Where(l => l.Key > splitY + 15.0).Select(l => string.Join(" ", l.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))));
            zones.TableText = tableSb.ToString();

            return zones;
        }

        // Ancienne façon de procéder pour les pdf (voir aussi SplitDocumentByHeaders) 
        private string ExtractRawPdfText(string filePath, SheetConfig config)
        {
            StringBuilder sb = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    // On utilise maintenant la détection par lignes vectorielles
                    var zones = SplitDocumentByHeaders(page, config);

                    sb.AppendLine("=== ZONE EN-TETE (IA) ===");
                    sb.AppendLine(zones.HeaderText);
                    sb.AppendLine("=== ZONE TABLEAU (GRID PAR LIGNES NOIRES) ===");
                    sb.AppendLine(zones.TableText);
                }
            }
            //Console.WriteLine("\n extractpdf sb :");
            //Console.WriteLine(sb);
            return sb.ToString();
        }

        private static string NormalizeLabel(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return s.Normalize(NormalizationForm.FormC)  // unifie é précomposé vs e + accent combinant
                    .Trim()
                    .ToLowerInvariant()
                    .Replace(".", "")                     // "H.T." == "H.T"
                    .Replace(" ", "");                    // tolère les espaces parasites de l'OCR ou l'absence d'espace
        }

        private async Task<string> FormatRawTextWithAI(string rawText, string title, ModeleIA iaClient, SheetConfig expectedConfig = null)
        {
            Console.WriteLine("\nrawtext in formatrawtextwithAI :");    
            Console.WriteLine(rawText);

            try
            {
                if (expectedConfig == null)
                {
                    TypeRequete textRequest = new DocumentInput(this.config, iaClient, rawText);
                    string result = await textRequest.executerRequete();
                    return CleanAiResponse(result).Replace("```csv", "").Replace("```", "").Trim();
                }

                // Extraction des labels OCR uniques + exemple de valeur
                var ocrLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // On parcourt la zone tableau et on collecte les labels distincts avec une valeur exemple
                bool inZone = false;
                foreach (var line in rawText.Split('\n')) 
                {
                    if (line.Contains("=== ZONE TABLEAU ===")) { inZone = true; continue; }
                    if (!inZone) continue;
                    foreach (System.Text.RegularExpressions.Match m in Regex.Matches(line, @"\[([^\]:]+):\s*([^\]]*)\]"))
                    {
                        string label = m.Groups[1].Value.Trim();
                        string value = m.Groups[2].Value.Trim();
                        if (!ocrLabels.ContainsKey(label))
                            ocrLabels[label] = value;
                    }
                }

                // Exact match
                var mapping = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var labelsNonMatches = new List<string>();

                foreach (string label in ocrLabels.Keys)    // Pour chaque label OCR, on cherche une colonne config dont le label est identique
                {
                    string labelNorm = NormalizeLabel(label);
                    int exactIndex = expectedConfig.Columns
                        .FindIndex(c => NormalizeLabel(c.Label) == labelNorm);

                    if (exactIndex >= 0)
                        mapping[label] = exactIndex;  // match direct, pas besoin de l'IA
                    else
                        labelsNonMatches.Add(label);  // à soumettre à l'IA
                }

                var colonnesDejaUtilisees = mapping.Values.ToHashSet(); // ex: {0, 2, 3}
                var targetColsDisponibles = expectedConfig.Columns  // ex: ["1: Référence", "5: Matière", ...]
                    .Select((c, i) => new { i, c })
                    .Where(x => !colonnesDejaUtilisees.Contains(x.i))
                    .Select(x => $"{x.i}: {x.c.Label}")
                    .ToList();

                Console.WriteLine("labels matchés en C# : " + string.Join(", ", mapping.Keys));
                Console.WriteLine("labels ambigus pour l'IA : " + string.Join(", ", labelsNonMatches));

                // Appel IA uniquement pour les labels ambigus 
                if (labelsNonMatches.Count > 0 && targetColsDisponibles.Count > 0)
                {
                    var labelsAvecExemples = labelsNonMatches
                        .Select(l => ocrLabels.TryGetValue(l, out string v) ? $"{l} (ex: {v})" : l)
                        .ToList();

                    StringBuilder sbMapping = new StringBuilder();
                    sbMapping.AppendLine($"Labels OCR non identifiés : {string.Join(", ", labelsAvecExemples)}");
                    sbMapping.AppendLine($"Colonnes cibles disponibles (index: nom) : {string.Join(" | ", targetColsDisponibles)}");
                    sbMapping.AppendLine();
                    sbMapping.AppendLine("Mappe chaque label à l'index de la colonne cible la plus proche sémantiquement.");
                    sbMapping.AppendLine("Utilise les exemples de valeurs pour lever les ambiguïtés : par exemple, un label dont les valeurs sont des identifiants produit ne peut pas correspondre à un numéro de commande.");
                    sbMapping.AppendLine("IMPORTANT : si la correspondance n'est pas claire et évidente, ne mappe PAS le label — il vaut mieux laisser une colonne vide que de mapper incorrectement.");
                    sbMapping.AppendLine("Retourne UN SEUL objet JSON. Exemple : {\"Référence\": 1, \"Désignation\": 2, \"Matière\": 7}");

                    string mappingPrompt = sbMapping.ToString();
                    Console.WriteLine("\nmappingPrompt in FormatRawTextWithAI :");
                    Console.WriteLine(mappingPrompt);

                    string systemPromptMapping = "Tu es un robot de mapping. Réponds UNIQUEMENT avec un objet JSON valide, sans explication, sans balise markdown.";
                    string mappingResponse = await iaClient.contacterIA(systemPromptMapping, mappingPrompt, 0.1);

                    Console.WriteLine("mappingResponse in FormatRawTextWithAI :");
                    Console.WriteLine(mappingResponse);

                    try
                    {
                        string cleanJson = mappingResponse.Replace("```json", "").Replace("```", "").Trim();
                        var mappingIA = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int?>>(cleanJson)
                                        ?? new Dictionary<string, int?>();

                        var indicesPris = new HashSet<int>(mapping.Values);

                        foreach (var kvp in mappingIA)
                        {
                            if (mapping.ContainsKey(kvp.Key)) continue;

                            if (!kvp.Value.HasValue) continue;
                            int idx = kvp.Value.Value;

                            if (idx < 0 || idx >= expectedConfig.ExpectedColumns) continue;

                            if (indicesPris.Contains(idx)) continue;

                            mapping[kvp.Key] = idx;
                            indicesPris.Add(idx);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("AVERTISSEMENT : mapping IA invalide, JSON mal formé.");
                    }
                }

                // Parsing déterministe des lignes + construction du CSV
                var dataLines = new List<string>();
                bool inTableZone = false;
                foreach (var line in rawText.Split('\n'))
                {
                    if (line.Contains("=== ZONE TABLEAU ===")) { inTableZone = true; continue; }
                    if (!inTableZone) continue;
                    if (line.Trim().StartsWith("["))
                        dataLines.Add(line.Trim());
                }

                StringBuilder finalOutput = new StringBuilder();
                finalOutput.AppendLine("SHEET_NUMBER=1;");
                finalOutput.AppendLine($"SHEET_NAME={expectedConfig.SheetName};{expectedConfig.ExpectedColumns};{dataLines.Count + 2};");

                foreach (var col in expectedConfig.Columns) finalOutput.Append(col.Label + ";");
                finalOutput.AppendLine();
                foreach (var col in expectedConfig.Columns) finalOutput.Append(col.InternalName + ";");
                finalOutput.AppendLine();
                foreach (var col in expectedConfig.Columns) finalOutput.Append(col.DataType + ";");
                finalOutput.AppendLine();

                foreach (string dataLine in dataLines)
                {
                    string[] row = new string[expectedConfig.ExpectedColumns];

                    foreach (System.Text.RegularExpressions.Match m in Regex.Matches(dataLine, @"\[([^\]:]+):\s*([^\]]*)\]"))
                    {
                        string label = m.Groups[1].Value.Trim();
                        string value = m.Groups[2].Value.Trim();

                        if (mapping.TryGetValue(label, out int colIndex)
                            && colIndex >= 0
                            && colIndex < expectedConfig.ExpectedColumns)
                        {
                            row[colIndex] = value;
                        }
                    }

                    finalOutput.AppendLine(string.Join(";", row) + ";");
                }

                Console.WriteLine("\nfinalOutput in FormatRawTextWithAI :");
                Console.WriteLine(finalOutput.ToString());

                return finalOutput.ToString();
            }
            catch (Exception ex)
            {
                return $"SHEET_NUMBER=1;\nSHEET_NAME=Erreur_IA;1;1;\nErreur lors du formatage : {ex.Message};\n";
            }
            
        }

        private List<string> ExtractDataLinesOnly(string aiResponse)
        {
            string[] lines = aiResponse.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> dataLines = new List<string>();
            int headersToSkip = 0;

            foreach (string line in lines)
            {
                if (line.StartsWith("```")) continue;
                if (line.StartsWith("SHEET_NUMBER")) continue;

                if (line.StartsWith("SHEET_NAME"))
                {
                    headersToSkip = 3;
                    continue;
                }

                if (headersToSkip > 0)
                {
                    headersToSkip--;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    dataLines.Add(line);
                }
            }
            return dataLines;
        }

        private string ExtractRawImageText(string filePath, DataFormatConfig formatRules)
        {
            string cleanFilePath = filePath.Replace("\"", "").Replace("'", "").Trim(' ', '\r', '\n', '\t');

            if (cleanFilePath.StartsWith("file:///"))
            {
                cleanFilePath = new Uri(cleanFilePath).LocalPath;
            }

            if (!File.Exists(cleanFilePath))
                throw new FileNotFoundException($"Le fichier image est introuvable :\n{cleanFilePath}");

            var configKeywords = formatRules.Sheets.Values      // on donne les mots clés de toutes les sheets du .cfg
                                    .SelectMany(s => s.Columns)
                                    .Where(c => !string.IsNullOrWhiteSpace(c.Label))
                                    .SelectMany(c => c.Label.ToLowerInvariant().Split(new[] { ' ', '\'', '-', '_' }, StringSplitOptions.RemoveEmptyEntries))
                                    .Where(s => s.Length > 2)
                                    .Distinct()
                                    .ToList();

            if (configKeywords.Count == 0)
                throw new InvalidOperationException(
                    "Aucun mot-clé exploitable extrait du .cfg (labels vides ou tokens trop courts). " +
                    "Le script OCR ne peut pas détecter d'en-tête sans mots-clés — vérifiez la configuration.");

            string keywords = string.Join(",", configKeywords);

            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Source", "PoemClientWPF", "Source", "Tools", "Python", "extract_table_glmocr.py");

            var psi = new ProcessStartInfo
            {
                // FileName = "python",
                FileName = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Source",
                    "PoemClientWPF",
                    "Source",
                    "Tools",
                    "python",
                    "venv",
                    "Scripts",
                    "python.exe"
                ),
                Arguments = $"\"{scriptPath}\" \"{cleanFilePath}\" \"{keywords}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                string output = outputTask.Result;   // bloque jusqu'à la fin réelle de la lecture
                string error = errorTask.Result;

                if (process.ExitCode != 0)
                    throw new Exception($"Le script Python a échoué (code {process.ExitCode}) :\n{error}");

                if (string.IsNullOrWhiteSpace(output))  // TODO : ne pas renvoyer une exception dans le cas où on analyse un dossier car ça bloque la chaine entière
                    throw new Exception("Le script Python n'a retourné aucun résultat.");

                return output;
            }

        }

        #endregion

        private string ConvertToAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            if (System.IO.Path.IsPathRooted(path)) return path;

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(currentDir);
            while (dirInfo != null && !dirInfo.Name.Equals("POEM", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = dirInfo.Parent;
            }

            string basePath = dirInfo != null
                ? System.IO.Path.Combine(dirInfo.FullName, "Logistics")
                : System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDir, @"..\..\..\Logistics"));
            return System.IO.Path.Combine(basePath, path).Replace("/", "\\");
        }
    }

}
