using PoemClient.Source.Tools;
using PoemClient.Source.Tools.IA;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace PoemClient.View
{
    public partial class TranslateWindow : Window
    {
        private readonly Config config;
        private readonly MainWindow _main;
        private static TranslateWindow instance = null;
        public bool canceled = false;
        bool filenameSet = false;
        bool hasTranslated = false;
        private string FromSave = "";
        private string ToSave = "";
        private string PathSave = "";

        public TranslateWindow()
        {
            InitializeComponent();
            _main = MainWindow.main;
            this.config = _main.GetConfig();
            this.Loaded += Window_Loaded;
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = "Traduction";

            this.PathSave = GetFilePath();
            this.Filename.Content = this.PathSave;

            InitializedDataContext();
        }
        #region DataContext
        private void InitializedDataContext()
        {
            string defaultModel = config.GetKeyValue("DefaultModel") ?? "Local";    //Default AI choose
            
            string defaultFrom = config.GetKeyValue("CurrentLanguage") ?? "Français";  //Default language of the application
            string defaultTo = config.GetKeyValue("ToLanguage") ?? "Anglais";       //Default language of traduction
            
            DataContext = new
            {
                //TextLang = "",
                To = config.GetKeyValue("To"),
                Languages = GetLangList(),
                LanguagesAI = GetLangAIList(),
                FilenameText = config.GetKeyValue("Result"),               // JZ 0319
                OkButton = config.GetKeyValue("Execute"),
                SelectedFrom = defaultFrom,
                SelectedTo = defaultTo,
                SelectedAI = defaultModel,
            };
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ToList.Items.Count > 0)
            {
                ToList.SelectedIndex = 0;
                FromList.SelectedIndex = 0;
                if (ToList.Items.Contains(ToSave))
                {
                    ToList.SelectedIndex = ToList.Items.IndexOf(ToSave);
                }
                if (FromList.Items.Contains(FromSave))
                {
                    FromList.SelectedIndex = FromList.Items.IndexOf(FromSave);
                }
            }
        }

        private string[] GetLangList()
        {
            string[] langlist = config.GetKeyValue("LanguageList").Split(',');
            if (langlist == null)
            {
                return null;
            }
            return langlist;
        }

        private string[] GetLangAIList()
        {
            string[] langlist = config.GetKeyValue("ModelList").Split(',');    // JZ 0319
            if (langlist == null)
            {
                return null;
            }
            return langlist;
        }

        private string GetFilePath()
        {
            string[] langlist = config.GetKeyValue("Menu18Static1").Split(',');
            if (langlist == null)
            {
                return null;
            }
            return langlist[langlist.Length-1];
        }

        public static TranslateWindow GetInstance()
        {
            if (instance == null)
            {
                instance = new TranslateWindow();
            }
            return instance;
        }
        private void FromListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string FromSelected = FromList.SelectedItem as string;
            FromSave = FromSelected;
            UpdateConfig("CurrentLanguage", FromSelected);
        }

        private void ToListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ToSelected = ToList.SelectedItem as string;
            ToSave = ToSelected;
            UpdateConfig("ToLanguage", ToSelected);
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

        private void TranslateClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Owner = null;
            if (filenameSet && hasTranslated)
            {
                string[] downloadConfig = "Write,Download,,Download/Client".Split(',');
                downloadConfig[2] = Filename.Content as string;
                _main.translateDownload = downloadConfig;
            }
            if (filenameSet && !hasTranslated) _main.skipDownload = true;
            _main.canDownload = true;
            hasTranslated = false;
            instance = null;
            return;
        }

        private void updateText(string text)
        {
            Task.Run(() =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    this.Loading.Text = text;
                });
            });
        }

        #endregion

        private async void Translate(object sender, RoutedEventArgs e)
        {
            if (!Utils.IsInternetAvailable())
            {
                MessageBox.Show(config.GetKeyValue("NetworkError"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                updateText(config.GetKeyValue("Failure"));
                this.TranslateButton.IsEnabled = true;
                return;
            }

            // Désactive le bouton pendant traitement
            this.TranslateButton.IsEnabled = false;

            string FromLanguage = FromList.SelectedItem as string;
            string ToLanguage = ToList.SelectedItem as string;
            string filename = Filename.Content as string;

            if (!string.IsNullOrEmpty(ToLanguage))
            {
                if (String.Compare(ToLanguage, FromLanguage) == 0)
                {
                    MessageBox.Show(config.GetKeyValue("LanguagesError"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    updateText(config.GetKeyValue("Failure"));
                    this.TranslateButton.IsEnabled = true;
                    return;
                }
            }

            if (string.IsNullOrEmpty(Filename.Content.ToString()))
            {
                MessageBox.Show(config.GetKeyValue("FileError"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                updateText(config.GetKeyValue("Failure"));
                this.TranslateButton.IsEnabled = true;
                return;
            }

            this.filenameSet = true;
            updateText(config.GetKeyValue("Loading"));  // JZ 0407

            ModeleIA modeleIA = null;
            switch (AiModelSelector.SelectedIndex)
            {
                case 0:
                    modeleIA = new Ollama(config);
                    break;
                case 1:
                    string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl");
                    string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel");
                    string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey");
                    modeleIA = new ChatGPTClient(urlAPI, modelAPI, keyAPI);
                    if (_main.WarningAIChatGPT)
                    {
                        _main.WarningAIChatGPT = false;
                        MessageBoxResult resultBox = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (resultBox == MessageBoxResult.No) { this.TranslateButton.IsEnabled = true; return; }
                    }
                    break;
                case 2:
                    string keyAPIGemini = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiKey");
                    string urlAPIGemini = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiUrl");
                    string modelAPIGemini = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiModel");
                    modeleIA = new GeminiClient(urlAPIGemini, modelAPIGemini, keyAPIGemini);
                    if (_main.WarningAIGemini)
                    {
                        _main.WarningAIGemini = false;
                        MessageBoxResult resultBoxGemini = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (resultBoxGemini == MessageBoxResult.No) { this.TranslateButton.IsEnabled = true; return; }
                    }
                    break;
                default:
                    modeleIA = new Ollama(config);
                    break;
            }

            // Récupération des données
            string[] macros = GetMacro();

            StringBuilder finalTranslatedContent = new StringBuilder();
            int batchSize = 200; // Nombre de lignes envoyées à l'IA en même temps
            int totalBatches = (int)Math.Ceiling((double)macros.Length / batchSize);
            int currentBatch = 0;

            try
            {
                for (int i = 0; i < macros.Length; i += batchSize)
                {
                    currentBatch += batchSize;

                    // On extrait le lot actuel
                    var currentMacroBatch = macros.Skip(i).Take(batchSize).ToArray();
                    string textToTranslate = string.Join("\n", currentMacroBatch);

                    updateText($"{currentBatch}/{macros.Length}");     

                    TypeRequete translationService = new TraductionService(textToTranslate, modeleIA, ToLanguage, this.config);
                    string batchResult = await translationService.executerRequete();

                    if (batchResult.StartsWith("Erreur"))      
                    {
                        throw new Exception(batchResult.Substring(config.GetKeyValue("Error").Length).TrimStart());
                        //throw new Exception($"L'IA a renvoyé une erreur au lot {currentBatch} : \n" + batchResult.Substring("Erreur".Length).TrimStart());
                    }

                    // On ajoute le résultat du lot au texte final
                    finalTranslatedContent.AppendLine(batchResult.Trim());
                }

                string rawFilename = Filename.Content.ToString();
                string absolutePath = rawFilename;

                if (!System.IO.Path.IsPathRooted(absolutePath))
                {
                    string pureFileName = System.IO.Path.GetFileName(rawFilename);
                    absolutePath = System.IO.Path.Combine(GetDefaultSaveDirectory(), pureFileName);
                }

                // On écrit tout le contenu d'un coup
                System.IO.File.WriteAllText(absolutePath, finalTranslatedContent.ToString().TrimEnd());

                Console.WriteLine("Le fichier traduit a été sauvegardé localement : " + absolutePath);
                updateText(config.GetKeyValue("Success"));
                this.hasTranslated = true;

                _main.skipDownload = true;
                _main.canDownload = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la traduction ou de la sauvegarde : " + ex.Message);
                MessageBox.Show(ex.Message, config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                updateText(config.GetKeyValue("Failure"));
            }
            finally
            {
                this.TranslateButton.IsEnabled = true;
            }
        }

        private string [] GetMacro()
        {
            string buffer = _main.login.GetServerPort() + "%20" + _main.GetUserId();
            string[] macro = _main.GetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientGetMacro"), buffer);
            macro = macro.Take(macro.Length - 5).Skip(2).ToArray();
            string[] trimmedMacro = Array.ConvertAll(macro, s => s.Replace("\n", "").Replace("\r", "").Trim());
            return trimmedMacro;
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            string currentName = System.IO.Path.GetFileNameWithoutExtension(Filename.Content.ToString());

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Enregistrer le résultat d'extraction",
                Filter = "Fichier CSV (*.csv)|*.csv|Fichier Texte (*.txt)|*.txt",
                FileName = currentName,
                InitialDirectory = GetDefaultSaveDirectory()
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Filename.Content = saveFileDialog.FileName;
            }
        }


        private string GetDefaultSaveDirectory()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(currentDir);

            while (dirInfo != null && !dirInfo.Name.Equals("POEM", StringComparison.OrdinalIgnoreCase))
            {
                dirInfo = dirInfo.Parent;
            }

            string targetPath;
            if (dirInfo != null)
            {
                targetPath = System.IO.Path.Combine(dirInfo.FullName, "Logistics", "Upload", "Client");
            }
            else
            {
                targetPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(currentDir, @"..\..\..\Logistics\Upload\Client"));
            }

            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            return targetPath;
        }
    }
}
