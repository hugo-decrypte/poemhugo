using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace PoemClient.Source.View
{
    public partial class ExcelImport : Window
    {
        private readonly Config config;

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
        public ExcelImport()
        {
            if ((config = MainWindow.config) == null)
            {
                return;
            }

            DataContext = new
            {
                windowTitle = config.GetKeyValue("ExcelImport"),         // JZ
                windowInputLabel = config.GetKeyValue("ExcelFile"),
                windowOutputLabel = config.GetKeyValue("SavedTo"),
            };

            InitializeComponent();

            _main = MainWindow.main;

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
                    InitialDirectory = GetSafeInitialDirectory(savedPaths[Convert.ToInt16(button.Name == "Output")]),
                };

                if (fileDialog.ShowDialog() == true && string.IsNullOrEmpty(fileDialog.FileName) == false)
                {
                    SetFile(button, savedPaths[Convert.ToInt16(button.Name == "Output")] = fileDialog.FileName, button.Name == "Input");
                }
            }
        }

        private string GetSafeInitialDirectory(string savedPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(savedPath))
                {
                    string directory = Path.GetDirectoryName(savedPath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        return directory;
                }
            }
            catch
            {
                // Ignore les erreurs de chemin invalide
            }

            // Fallback vers le répertoire courant ou le path par défaut
            return Directory.Exists(path) ? path : Environment.CurrentDirectory;
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

        private async void StartImportButton_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(config.GetKeyValue("InputError"),
                                DataContext.ToString().Split('=')[1].Split(',')[0],
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _main.InteractionBlocker.Visibility = Visibility.Visible;
            var dynamicBox = new MessageFormExcel("Excel Import", "");
            dynamicBox.Show();

            string inputPath = Input.Content.ToString();
            string outputPath = Output.Content.ToString();

            bool success = false;

            try
            {
                await Task.Factory.StartNew(() => success = ConvertExcelToText(inputPath, outputPath, dynamicBox))
                .ContinueWith(task =>
                {
                    dynamicBox.SetMessage(config.GetKeyValue("Terminated"));

                }, dynamicBox.Token, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            } catch (OperationCanceledException)
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
                if (!success)
                    _main.runActionFailed = true;
            }
            catch
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
            }
            finally
            {
                _main.InteractionBlocker.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Parses each cells of each sheets from an Excel file and writes its content to an output file
        /// </summary>
        /// <param name="truncate">Whether or not to truncate any existing file, defaults to true</param>
        /// <returns>true if operation succeed, false otherwise</returns>
        private bool ConvertExcelToText(string inputPath, string outputPath, MessageFormExcel dynamicBox, bool truncate = true)
        {
            try
            {
                string outputBuffer = string.Empty;

                if (truncate)
                {
                    File.Delete(outputPath);
                }

                IWorkbook workbook;
                using (FileStream file = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // Added FileShare.ReadWrite to allow the import file being opened while importing
                {
                    if (DocumentFactoryHelper.HasOOXMLHeader(file)) // Modification de methode obsolète POIXMLDocument.HasOOXMLHeader
                        workbook = new XSSFWorkbook(file);
                    else
                    {
                        file.Position = 0;
                        workbook = new HSSFWorkbook(file);
                    }

                    outputBuffer += config.GetKeyValue("SheetNumberPrefix") +
                                    workbook.NumberOfSheets + SEPARATOR + Environment.NewLine;
                    int totalSheet = workbook.NumberOfSheets;

                    foreach (ISheet sheet in workbook)
                    {
                        try
                        {
                            outputBuffer += config.GetKeyValue("SheetNamePrefix") +
                                            sheet.SheetName + SEPARATOR +
                                            (sheet.GetRow(0).LastCellNum) + SEPARATOR +
                                            sheet.LastRowNum + SEPARATOR + Environment.NewLine;

                            int totalRow = sheet.LastRowNum;

                            for (int row = sheet.FirstRowNum; row <= sheet.LastRowNum; row++, outputBuffer += Environment.NewLine)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    dynamicBox.SetMessage($"{config.GetKeyValue("SheetNumber")} : {totalSheet},  {config.GetKeyValue("LineNumber")} : {totalRow}");
                                });
                                for (int cell = sheet.GetRow(0).FirstCellNum; cell < (sheet.GetRow(0).LastCellNum); cell++)
                                {
                                    if (sheet.GetRow(row) == null)
                                    {
                                        continue;
                                    }

                                    outputBuffer += (sheet.GetRow(row).GetCell(cell) == null ? "" : ConvertCellCoordinatesToNCLFormat(sheet.GetRow(row).GetCell(cell), sheet.GetRow(row).GetCell(cell).ToString(), sheet.LastRowNum)) + SEPARATOR;
                                }
                                totalRow--;
                            }
                            totalSheet--;

                            outputBuffer += Environment.NewLine;
                        }
                        catch (Exception ex){
                            Console.WriteLine(ex);
                        }
                    }
                }

                File.Delete(outputPath);
                using (StreamWriter output = new StreamWriter(new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write), Encoding.Default))
                {
                    output.Write(outputBuffer);
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message,
                                    DataContext.ToString().Split('=')[1].Split(',')[0],
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                });

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
            return data.Contains('(') == true ? (data = config.GetKeyValue("ExcelFormulaPrefix") + data).Remove(data.IndexOf('(')) + nclCoordinates : (config.GetKeyValue("ExcelFormulaPrefix") + nclCoordinates);
        }
    }

    public class MessageFormExcel : Window
    {
        private readonly TextBlock messageText;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly MainWindow _main;

        public CancellationToken Token => _cts.Token;

        public MessageFormExcel(string title, string message)
        {
            this.Width = 350;
            this.Height = 120;
            this.Topmost = false;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Title = title;
            this.ResizeMode = ResizeMode.NoResize;
            this._main = MainWindow.main;

            messageText = new TextBlock
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontFamily = new System.Windows.Media.FontFamily(MainWindow.config.GetKeyValue("FontFamily")),
                FontSize = double.Parse(MainWindow.config.GetKeyValue("TitleFontSize")),
                Foreground = Brushes.Black,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };

            var background = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                Child = CreateLayout()
            };

            this.Content = background;
            messageText.Text = message;

            this.Closed += (s, e) =>
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            };
        }

        private UIElement CreateLayout()
        {
            var grid = new Grid();
            grid.Children.Add(messageText);
            return grid;
        }
        public void SetMessage(string message) => messageText.Text = message;
        public void ShowMessage() => this.Show();
        public void CloseMessage() => this.Close();
    }
}
