using System;
using System.IO;
using System.Windows;
using Microsoft.VisualBasic.FileIO;

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using PoemClientWPF;
using PoemClientWPF.Tools;

namespace PoemClient.Source.View
{
    public partial class ExcelExport : Window
    {
        private readonly Config configuration;
        private static readonly string[] savedPaths = { null, null };
        private readonly string path = null;

        public bool canceled = false;

        MainWindow _main;

        /// <summary>
        /// Class constructor
        /// </summary>
        public ExcelExport(MainWindow main, Config config = null)
        {
            if ((configuration = config) == null)
            {
                return;
            }
            _main = main;

            DataContext = new
            {
                windowTitle = configuration.GetKeyValue("ExcelExport"),         // JZ
                windowInputLabel = configuration.GetKeyValue("ExcelFile"),
                windowOutputLabel = configuration.GetKeyValue("SavedTo"),

                //windowTitle = "Excel Export",
                //windowInputLabel = configuration.GetKeyValue("ExcelExportSource"),
                //windowOutputLabel = configuration.GetKeyValue("ExcelExportDestination"),
            };

            InitializeComponent();

            this.Owner = main;

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
                Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = (button.Name == "Input" ? "Text files|*.txt|CSV files|*.csv|" : "Excel files|*.xlsx;*.xls|") + "All files|*.*",
                    CheckFileExists = button.Name == "Input",
                    InitialDirectory = (Path.GetDirectoryName(savedPaths[Convert.ToInt16(button.Name == "Output")]) ?? (Directory.Exists(path) == false ? Environment.CurrentDirectory : path)),
                };
                if (fileDialog.ShowDialog() == true && string.IsNullOrEmpty(fileDialog.FileName) == false)
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
                file = configuration.GetIniValue(configKey) + "/" + file;
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
        public void ResetSavedPaths()
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

        /// <summary>
        /// Translation caller that check saved paths
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void StartImport(object sender, RoutedEventArgs e)
        {
            if (Input.Content == null || Output.Content == null)
            {
                MessageBox.Show(configuration.GetKeyValue("SelectFile"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ConvertCsvToExcel() == true)
            {
                MessageBox.Show(_main, configuration.GetKeyValue("Terminated"), configuration.GetKeyValue("ExcelExport"), MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }

        }

        /// <summary>
        /// Parses each lines of a CSV file an converts it to an Excel file
        /// </summary>
        /// <param name="truncate">Whether or not to truncate any existing file, defaults to true</param>
        /// <returns></returns>
        private bool ConvertCsvToExcel(bool truncate = true)
        {
            try
            {
                if (truncate == true)
                {
                    File.Delete(Output.Content.ToString());
                }

                // If FileStream is created inside XSSFWorkbook() constructor and an exception raises, it doesn't close itself
                using (FileStream output = new FileStream(Output.Content.ToString(), FileMode.CreateNew, FileAccess.Write) { Position = 0 })
                {
                    IWorkbook workbook = new XSSFWorkbook();
                    ISheet currentSheet = null;
                    IRow currentRow = null;

                    string formulaPrefix = configuration.GetKeyValue("ExcelFormulaPrefix");

                    using (TextFieldParser csvParser = new TextFieldParser(Input.Content.ToString()) { TextFieldType = FieldType.Delimited })
                    {
                        csvParser.SetDelimiters(";");
                        string[] fields = null;

                        for (int row = 0; (fields = csvParser.ReadFields()) != null;)
                        {
                            // Skipping first line (number of sheets)
                            if (fields[0].StartsWith(configuration.GetKeyValue("ExcelSheetNumberPrefix")) == true)
                            {
                                continue;
                            }

                            // Retrieving current sheet name
                            if (fields[0].StartsWith(configuration.GetKeyValue("ExcelSheetNamePrefix")) == true)
                            {
                                currentSheet = workbook.CreateSheet(fields[0].Replace(configuration.GetKeyValue("ExcelSheetNamePrefix"), ""));
                                row = 0;
                                continue;
                            }

                            if (currentSheet == null)
                            {
                                currentSheet = workbook.CreateSheet();
                            }

                            currentRow = currentSheet.CreateRow(row);

                            for (int cell = 0; cell < fields.Length; cell++)
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
                MessageBox.Show(exception.Message, DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);

                if (IsActive == false)
                {
                    ShowDialog();
                }
                return false;
            }
            return true;
        }
    }
}
