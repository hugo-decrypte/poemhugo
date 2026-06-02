using System;
using System.IO;
using System.Linq;
using System.Windows;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using PoemClientWPF;

using PoemClientWPF.Tools;

namespace PoemClient.Source.View
{
    public partial class ExcelImport : Window
    {
        private readonly Config configuration;

        private readonly string path = null;

        private const int MAX_EXCEL_DIMENSION = 2; // maximum number of letters in a cell name, like cell 'FA'
        private const string SEPARATOR = ";";

        private const int NUMBER_OF_LETTERS_IN_ALPHABET = 26;
        private const char FIRST_LETTER_IN_ALPHABET = 'A';
        private const char LAST_LETTER_IN_ALPHABET = 'Z';

        private static readonly string[] savedPaths = { null, null };
        MainWindow _main;

        public bool canceled = false;

        /// <summary>
        /// Class constructor
        /// </summary>
        public ExcelImport(MainWindow main, Config config = null)
        {
            if ((configuration = config) == null)
            {
                return;
            }

            DataContext = new
            {
                windowTitle = configuration.GetKeyValue("ExcelImport"),         // JZ
                windowInputLabel = configuration.GetKeyValue("ExcelFile"),
                windowOutputLabel = configuration.GetKeyValue("SavedTo"),
            };

            InitializeComponent();

            _main = main;
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
                    Filter = (button.Name == "Input" ? "Excel files|*.xlsx;*.xls|" : "Text files|*.txt|CSV files|*.csv|") + "All files|*.*",
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

            if (ConvertExcelToText() == true)
            {
                MessageBox.Show(_main, configuration.GetKeyValue("Terminated"), configuration.GetKeyValue("ExcelImport"), MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }

        }

        /// <summary>
        /// Parses each cells of each sheets from an Excel file and writes its content to an output file
        /// </summary>
        /// <param name="truncate">Whether or not to truncate any existing file, defaults to true</param>
        /// <returns>true if operation succeed, false otherwise</returns>
        private bool ConvertExcelToText(bool truncate = true)
        {
            try
            {
                string outputBuffer = string.Empty;
                
                if (truncate == true)
                {
                    File.Delete(Output.Content.ToString());
                }

                IWorkbook worksheet;
                string filePath = Input.Content.ToString();

                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet = new HSSFWorkbook(file); // for .xls
                    }
                    else if (Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet = new XSSFWorkbook(file); // for .xlsx
                    } else
                    {
                        MessageBox.Show(_main, "Unsupported excel format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    outputBuffer += configuration.GetKeyValue("ExcelSheetNumberPrefix") + worksheet.NumberOfSheets + SEPARATOR + Environment.NewLine;

                    foreach (ISheet sheet in worksheet)
                    {
                        try
                        {
                            outputBuffer += configuration.GetKeyValue("ExcelSheetNamePrefix") + sheet.SheetName + SEPARATOR + sheet.GetRow(0).LastCellNum + SEPARATOR + sheet.LastRowNum + SEPARATOR + Environment.NewLine;

                            for (int row = sheet.FirstRowNum; row <= sheet.LastRowNum; row++, outputBuffer += Environment.NewLine)
                            {
                                for (int cell = sheet.GetRow(0).FirstCellNum; cell < sheet.GetRow(0).LastCellNum; cell++)
                                {
                                    if (sheet.GetRow(row) == null)
                                    {
                                        continue;
                                    }

                                    outputBuffer += (sheet.GetRow(row).GetCell(cell) == null ? "" : ConvertCellCoordinatesToNCLFormat(sheet.GetRow(row).GetCell(cell), sheet.GetRow(row).GetCell(cell).ToString(), sheet.LastRowNum)) + SEPARATOR;
                                }
                            }
                            outputBuffer += Environment.NewLine;
                        } catch { }
                    }
                }

                using (StreamWriter output = new StreamWriter(new FileStream(Output.Content.ToString(), FileMode.OpenOrCreate, FileAccess.Write) { Position = 0 }))
                {
                    output.Write(outputBuffer);
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

        /// <summary>
        /// Converts Excel cell coordinates to NCL coordinates
        /// </summary>
        /// <example>=SUM(C7,A5) becomes =SUM((3,7),(1,5))</example>
        /// <param name="data">Content of a cell which may contains an Excel formula with coordinates in it</param>
        /// <param name="maxRow">Maximum number of rows in the current Excel Sheet</param>
        /// <returns>Input content with NCL coordinates converted from Excel coordinates, if given string contains so</returns>
        string ConvertCellCoordinatesToNCLFormat(ICell cell, string data, int maxRow)
        {
            if (cell == null || cell.CellType != CellType.Formula)
            {
                return data;
            }

            string nclCoordinates = data.Contains('(') == true ? data.Substring(data.IndexOf('(')) : data;
            char[] columns = Enumerable.Repeat((char)(LAST_LETTER_IN_ALPHABET + 1), MAX_EXCEL_DIMENSION).ToArray<char>();

            while (columns.Length != 0)
            {
                for (int row = maxRow + 1, column = 0; row != 0; row--, column = 0)
                {
                    if (nclCoordinates.Contains(new string(columns) + row.ToString()) == false)
                    {
                        continue;
                    }

                    // Converts 1+ cells names (like cell 'BA', 'FX', 'ZA') to plain integer, for example, column FX becomes column number 180
                    for (int index = 0; index < columns.Length; index++)
                    {
                        column += (int)(columns[index] - FIRST_LETTER_IN_ALPHABET + 1) * (index + 1 == columns.Length ? 1 : NUMBER_OF_LETTERS_IN_ALPHABET);
                    }

                    nclCoordinates = nclCoordinates.Replace(new string(columns) + row.ToString(), $"({column},{row})");
                }

                if (columns.All(letter => letter.Equals(FIRST_LETTER_IN_ALPHABET)))
                {
                    Array.Resize(ref columns, columns.Length - 1);
                    columns = Enumerable.Repeat((char)(LAST_LETTER_IN_ALPHABET + 1), columns.Length).ToArray<char>();
                }

                if (columns.Length > 0)
                {
                    if (columns[columns.Length - 1] == FIRST_LETTER_IN_ALPHABET)
                    {
                        columns[columns.Length - 2]--;
                        columns[columns.Length - 1] = (char)(LAST_LETTER_IN_ALPHABET + 1);
                    }

                    columns[columns.Length - 1]--;
                }
            }

            return data.Contains('(') == true ? (data = configuration.GetKeyValue("ExcelFormulaPrefix") + data).Remove(data.IndexOf('(')) + nclCoordinates : (configuration.GetKeyValue("ExcelFormulaPrefix") + nclCoordinates);
        }
    }
}
