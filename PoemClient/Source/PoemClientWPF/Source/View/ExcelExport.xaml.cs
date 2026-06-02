using Microsoft.VisualBasic.FileIO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PoemClient.Source.View
{
    public partial class ExcelExport : Window
    {
        private readonly Config config;
        private static readonly string[] savedPaths = { null, null };
        private readonly string path = null;

        public bool canceled = false;

        MainWindow _main;

        /// <summary>
        /// Class constructor
        /// </summary>
        public ExcelExport()
        {
            if ((config = MainWindow.config) == null)
            {
                return;
            }

            _main = MainWindow.main;

            DataContext = new
            {
                windowTitle = config.GetKeyValue("ExcelExport"),
                windowInputLabel = config.GetKeyValue("TextFile"),
                windowOutputLabel = config.GetKeyValue("SavedTo"),
            };

            InitializeComponent();

            path = $"{config.GetIniValue("DIRECTORY")}".Replace('/', '\\');
            path += path.EndsWith("\\") ? "" : "\\";

            Input.Content = savedPaths[0];
            Output.Content = savedPaths[1];
        }

        /// <summary>
        /// Opens a new FileDialog to browse files
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        private void BrowseFile(object sender, RoutedEventArgs _e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                string fullPath = savedPaths[Convert.ToInt16(button.Name == "Output")];
                string initialDir = Directory.Exists(path) ? path : Environment.CurrentDirectory;

                // Vérifie que le chemin est non vide et bien formé
                if (!string.IsNullOrWhiteSpace(fullPath) && Path.IsPathRooted(fullPath))
                {
                    try
                    {
                        string candidate = Path.GetDirectoryName(fullPath);
                        if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                        {
                            initialDir = candidate;
                        }
                    }
                    catch (ArgumentException) { /* Ignore et garde initialDir par défaut */ }
                }

                Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = (button.Name == "Input" ? "Text files|*.txt|CSV files|*.csv|" : "Excel files|*.xlsx;*.xls|") + "All files|*.*",
                    CheckFileExists = button.Name == "Input",
                    InitialDirectory = initialDir,
                };

                if (fileDialog.ShowDialog() == true && !string.IsNullOrEmpty(fileDialog.FileName))
                {
                    SetFile(button, savedPaths[Convert.ToInt16(button.Name == "Output")] = fileDialog.FileName, button.Name == "Input");
                }
            }
        }

        /// <summary>
        /// Sets a button content and save file path internally
        /// </summary>
        /// <param name="button">Button to set content of</param>
        /// <param name="file">File path</param>
        /// <param name="checkFile">Whether or not to verify if given file exists</param>
        /// <param name="configKey">Config key to check relative path from</param>
        /// <returns>true if file exists, false otherwise</returns>
        public bool SetFile(System.Windows.Controls.Button button, string file, bool checkFile = true, string configKey = "DIRECTORY")
        {
            if (string.IsNullOrEmpty(file) == true)
            {
                button.Content = file;
                savedPaths[Convert.ToInt16(button.Name == "Output")] = file;
                return false;
            }

            if (Directory.Exists(Path.GetPathRoot(file)) == false)
            {
                file = config.GetIniValue(configKey) + "/" + file;
            }

            if (Directory.Exists(Path.GetPathRoot(file)) == false)
            {
                file = AppDomain.CurrentDomain.BaseDirectory + file;
            }

            if (checkFile == true)
            {
                try
                {
                    if (File.Exists(file) == false)
                    {
                        throw new FileNotFoundException();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show((exception.Message + $" ({file})"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            button.Content = file;
            savedPaths[Convert.ToInt16(button.Name == "Output")] = file;
            return true;
        }

        /// <summary>
        /// Unsets saved file paths
        /// </summary>
        public static void ResetSavedPaths()
        {
            for (int path = 0; path < savedPaths.Length; path++)
            {
                savedPaths[path] = null;
            }
        }

        /// <summary>
        /// Closes ExcelInput Window
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void CancelImport(object _sender, RoutedEventArgs _e)
        {
            canceled = true;
            Close();
        }

        private async void StartExportButton_Click(object sender, RoutedEventArgs e)
        {
            await StartImport();
        }

        /// <summary>
        /// Translation caller that check saved paths
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public async Task StartImport()
        {
            this.Hide();
            if (Input.Content == null || Output.Content == null)
            {
                MessageBox.Show(config.GetKeyValue("InputError"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _main.InteractionBlocker.Visibility = Visibility.Visible;
            var dynamicBox = new MessageFormExcel("Excel Export", config.GetKeyValue("Running"));
            dynamicBox.Show();

            bool success = false;
            string outputContent = Output.Content?.ToString();
            string inputContent = Input.Content?.ToString();
            try
            {
                await Task.Factory.StartNew(() => success = ConvertCsvToExcel(outputContent, inputContent))
                .ContinueWith(task =>
                {
                    dynamicBox.SetMessage(config.GetKeyValue("Terminated"));

                }, dynamicBox.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (OperationCanceledException)
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
                if (!success)
                    _main.runActionFailed = true;
            }
            catch (Exception ex)
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Parses each lines of a CSV file an converts it to an Excel file
        /// </summary>
        /// <param name="truncate">Whether or not to truncate any existing file, defaults to true</param>
        /// <returns></returns>
        private bool ConvertCsvToExcel(string outputFile, string inputFile, bool truncate = true)
        {
            try
            {
                if (truncate == true)
                {
                    File.Delete(outputFile);
                }

                // If FileStream is created inside XSSFWorkbook() constructor and an exception raises, it doesn't close itself
                using (FileStream output = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None) { Position = 0 })
                {
                    IWorkbook workbook = new XSSFWorkbook();
                    ISheet currentSheet = null;
                    IRow currentRow = null;

                    string formulaPrefix = config.GetKeyValue("SheetFormulaPrefix");

                    using (StreamReader sr = new StreamReader(inputFile, Encoding.GetEncoding(1252)))
                    using (TextFieldParser csvParser = new TextFieldParser(sr) { TextFieldType = FieldType.Delimited })
                    {
                        csvParser.SetDelimiters(";");
                        string[] fields = null;

                        for (int row = 0; (fields = csvParser.ReadFields()) != null;)
                        {
                            // Skipping first line (number of sheets)
                            if (fields[0].StartsWith(config.GetKeyValue("SheetNumberPrefix")))
                                continue;

                            // Retrieving current sheet name
                            if (fields[0].StartsWith(config.GetKeyValue("SheetNamePrefix")))
                            {
                                currentSheet = workbook.CreateSheet(fields[0].Replace(config.GetKeyValue("SheetNamePrefix"), ""));
                                row = 0;
                                continue;
                            }

                            if (currentSheet == null)
                                currentSheet = workbook.CreateSheet();

                            currentRow = currentSheet.CreateRow(row);

                            int lastNonEmptyCell = fields.Length - 1;
                            while (lastNonEmptyCell >= 0 && string.IsNullOrWhiteSpace(fields[lastNonEmptyCell]))
                            {
                                lastNonEmptyCell--;
                            }

                            // Créer uniquement les cellules jusqu'à la dernière non-vide
                            for (int cell = 0; cell <= lastNonEmptyCell; cell++)
                            {
                                currentRow.CreateCell(cell).SetCellValue(fields[cell].Replace(formulaPrefix, ""));
                            }

                            row++;
                        }

                        workbook.Write(output);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(exception.Message, DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);
                });

                /*
                Application.Current.Dispatcher.Invoke(() =>
                    {
                    if (IsActive == false)
                    {
                        ShowDialog();
                    }
                });
                */

                return false;
            }
            return true;
        }
    }
}
