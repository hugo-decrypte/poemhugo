using PoemClient.Source.Tools.IA;
using PoemClient.View;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using PoemClientWPF.View;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Speech.Recognition;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PoemClient.Source.View
{
    public partial class PromptWindow : Window
    {
        private readonly MainWindow _main;
        private static PromptWindow instance = null;
        public bool canceled = false;

        private bool microOn = false;
        private DispatcherTimer _timer;
        private int _indexImage = 0;

        private SpeechRecognitionEngine _recognizer;
        private string _finalizedText = "";

        private PromptWindow(MainWindow main)
        {
            InitializeComponent();
            this._main = main;
            this.Owner = main;

            this.Title = _main.config.GetKeyValue("Agent");
            InitializeTimer();
            InitializeDataContext();

            InitSpeechEngine();
        }

        private void InitSpeechEngine()
        {
            try
            {
                _recognizer = new SpeechRecognitionEngine();
                _recognizer.SetInputToDefaultAudioDevice();

                // --- METHODE 1 : La dictée libre (Ce que tu as actuellement) ---
                //_recognizer.LoadGrammar(new DictationGrammar());

                // --- METHODE 2 : Le vocabulaire ultra-précis ---
                Choices commandes = new Choices();
                commandes.Add(SplitForSpeechRecognition());
                Console.WriteLine("ICI COMMANDES");
                Console.WriteLine(SplitForSpeechRecognition()[1]);
                Console.WriteLine("FIN COMMANDES");

                GrammarBuilder gb = new GrammarBuilder(commandes);
                Grammar dictionnairePerso = new Grammar(gb);
                _recognizer.LoadGrammar(dictionnairePerso);
                // ------------------------------------------------

                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur d'initialisation : {ex.Message}");
            }
        }
        public string[] SplitForSpeechRecognition()
        {
            List<string> listeName = new List<string>();
            string text = _main.config.GetKeyValue("APIAgent");
            string motif = @"['""]name['""]\s*:\s*['""]([^'""]+)['""]";

            Regex regex = new Regex(motif, RegexOptions.IgnoreCase);
            MatchCollection correspondances = regex.Matches(text);

            foreach (Match match in correspondances)
            {
                // On récupère uniquement la valeur capturée dans les parenthèses de la Regex
                if (match.Groups.Count > 1)
                {
                    listeName.Add(match.Groups[1].Value);
                }
            }
            listeName.Add("ouvre");
            return listeName.ToArray();
        }

        private void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (e.Result != null)
            {
                Console.WriteLine($"[Micro] Entendu (En cours) : {e.Result.Text}");
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Confidence > 0.5f)
            {
                Console.WriteLine($"[Micro] Validé ({e.Result.Confidence * 100:0}%) : {e.Result.Text}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _finalizedText += e.Result.Text + " ";
                    Prompt.Text = _finalizedText;
                    Prompt.CaretIndex = Prompt.Text.Length;
                });
            }
            else
            {
                Console.WriteLine("[Micro] Phrase rejetée (Confiance trop faible).");
            }
        }

        private string BuildResourcePath(string resource)
        {
            resource = resource.Replace("\\", "/");
            resource = resource.Replace("//", "/");
            if (resource.StartsWith("/"))
            {
                resource = resource.Substring(1);
            }
            string[] splittedRes = resource.Split('/');
            string resourcePath = _main.config.GetRessourcePath();
            resourcePath = Path.Combine(resourcePath, splittedRes[1]);
            return resourcePath;
        }

        private void InitializeDataContext()
        {
            string defaultFrom = _main.config.GetKeyValue("DefaultModel") ?? "Local";

            DataContext = new
            {
                LanguagesAI = _main.config.GetKeyValue("ModelList").Split(','),
                OkButton = _main.config.GetKeyValue("OK"),
                SelectedFrom = defaultFrom,
                Voice1Icon = BuildResourcePath(_main.config.GetKeyValue("Voice1Icon")),
                Voice2Icon = BuildResourcePath(_main.config.GetKeyValue("Voice2Icon")),
            };
        }

        private void AISelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string FromSelected = AiModelSelector.SelectedItem as string;
            UpdateConfig("DefaultModel", FromSelected);
        }

        private bool UpdateConfig(string key, string value)
        {
            if (_main.config.GetKeyValue(key) != value)
            {
                _main.config.SetKeyValueKeepLineBreak(key, value);
                return true;
            }
            return false;
        }

        public static PromptWindow getInstance(MainWindow main)
        {
            if (instance == null)
                instance = new PromptWindow(main);
            return instance;
        }

        #region Analyse Prompt
        private async void ConfirmPrompt(object sender, RoutedEventArgs e)
        {
            ModeleIA modeleIA = null;
            string userPrompt = Prompt.Text;
            if (userPrompt == "")
            {
                MessageBox.Show(_main.config.GetKeyValue("NoAgentTask"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.Confirm.IsEnabled = false;

            switch (AiModelSelector.SelectedIndex)
            {
                case 0:
                    modeleIA = new Ollama(_main.config);
                    break;
                case 1:
                    {
                        string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl");
                        string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel");
                        string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey");
                        modeleIA = new ChatGPTClient(urlAPI, modelAPI, keyAPI);
                        if (_main.WarningAIChatGPT)
                        {
                            MessageBoxResult resultBox = MessageBox.Show(_main.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (resultBox == MessageBoxResult.No) { Confirm.IsEnabled = true; return; }
                            _main.WarningAIChatGPT = false;
                        }

                        break;
                    }

                case 2:
                    {
                        string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiKey");
                        string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiUrl");
                        string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiModel");
                        modeleIA = new GeminiClient(urlAPI, modelAPI, keyAPI);
                        if (_main.WarningAIGemini)
                        {
                            MessageBoxResult resultBox = MessageBox.Show(_main.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (resultBox == MessageBoxResult.No) { Confirm.IsEnabled = true; return; }
                            _main.WarningAIGemini = false;
                        }

                        break;
                    }
            }

            TypeRequete agent = new Agent(this._main.config, modeleIA, userPrompt);

            try
            {
                Console.WriteLine($"[IA] Envoi de la requête au modèle : {userPrompt}");
                string reponse = await agent.executerRequete();
                Console.WriteLine($"[IA] REPONSE REÇUE :\n{reponse}");

                if (reponse.Contains("\"tool\": \"none\""))
                {
                    MessageBox.Show(_main.config.GetKeyValue("NoAgentResponse"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                    using (var jsonreponse = JsonDocument.Parse(reponse))
                    {
                        JsonElement jsonroot = jsonreponse.RootElement;
                        bool success = true;

                        switch (jsonroot.GetProperty("tool").ToString())
                        {
                            case "documentinput": OpenDocumentInput(); break;
                            case "demandpredict": OpenDemandPredict(); break;
                            case "supplypredict": OpenSupplyPredict(); break;
                            case "translation": OpenTranslation(); break;
                            case "view":
                                {JsonElement elts = jsonroot.GetProperty("arguments");
                                foreach (var elt in elts.EnumerateArray())
                                {
                                    OpenTreeItemView(elt.ToString());
                                    break;
                                }
                                    break;
                                }
                            case "excelToDaba": OpenExcelImport(); break;
                            case "excelexport": OpenExcelExport(); break;
                            case "databaseToExcel": OpenDownload(); break;
                            case "excelToDatabase": OpenUpload(); break;
                            case "script":
                                {JsonElement elts = jsonroot.GetProperty("arguments");
                                foreach (var elt in elts.EnumerateArray())
                                {
                                    OpenScript();
                                    break;
                                }
                                    break;
                                }
                            default:
                                MessageBox.Show(_main.config.GetKeyValue("NoAgentTask"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                                success = false;
                                break;
                        }

                        if (success) PromptClosing(null, null);
                    }
            }
            catch (PingException)
            {
                throw new PingException("Connexion impossible");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"[Erreur IA] {exc.Message}");
                MessageBox.Show(_main.config.GetKeyValue("NetworkError"), _main.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Confirm.IsEnabled = true;
            }
        }

        private void PromptClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (microOn)
            {
                StopMicrophone();
            }

            _recognizer?.Dispose();
            this.Owner = null;
            instance = null;
        }
        //
        // Methodes d'execution d'api
        //
        private void OpenDocumentInput() { DocumentInputWindow.GetInstance(this._main).Show(); }
        private void OpenSupplyPredict() { SupplyPredictWindow.GetInstance(this._main).Show(); }
        private void OpenDemandPredict() { DemandPredictWindow.GetInstance(this._main).Show(); }
        private void OpenTranslation() { TranslateWindow.GetInstance(this._main).Show(); }
        private void OpenTreeItemView(string viewname) {new OpenView(viewname, _main).Show(); }
        private void OpenExcelImport() { new ExcelImport(_main, _main.config).Show(); }
        private void OpenExcelExport() { new ExcelExport(_main, _main.config).Show(); }
        private void OpenDownload() { new Download(_main.login.GetDownloadDirectory(), _main.login.GetHttpServer(), _main.login.GetServerPort(), _main, _main.config).Show(); }
        private void OpenUpload() { new Upload(_main.login.GetUploadDirectory(), _main.login.GetHttpServer(), _main.login.GetServerPort(), _main, _main.config).Show(); }
        private void OpenScript()
        {
            // A modifier pour mieux configurer


            // Récupération fichier de config agent scripts
            string configAgent = File.ReadAllText("..\\Logistics\\Boot\\Admin\\Agent.cfg");


            Console.WriteLine(configAgent);
            Console.WriteLine(MainWindow.GetLineBelowParameter(configAgent, "GetProductView"));
            
            string[] values = _main.GetQueries(MainWindow.GetLineBelowParameter(configAgent, "GetProductView"), _main.login.GetServerPort() + "%20" + _main.GetUserId());
            
            foreach (string str in values)
            {
                Console.WriteLine(str+"\n");
            }

            Console.WriteLine(MainWindow.GetLineBelowParameter(configAgent, "GetClientView"));

            values = _main.GetQueries(MainWindow.GetLineBelowParameter(configAgent, "GetClientView"), _main.login.GetServerPort() + "%20" + _main.GetUserId());
            foreach (string str in values)
            {
                Console.WriteLine(str + "\n");
            }
        }

        #endregion

        #region Speech

        private void MonBoutonImage_Click(object sender, RoutedEventArgs e)
        {
            if (_recognizer == null)
            {
                MessageBox.Show("Le moteur vocal de Windows n'est pas disponible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!microOn)
            {
                StartMicrophone();
            }
            else
            {
                StopMicrophone();
            }
        }

        private void StartMicrophone()
        {
            try
            {
                _finalizedText = string.IsNullOrWhiteSpace(Prompt.Text) ? "" : Prompt.Text;
                if (!string.IsNullOrEmpty(_finalizedText) && !_finalizedText.EndsWith(" "))
                {
                    _finalizedText += "     ";
                }

                Console.WriteLine("[Micro] DÉMARRAGE DE L'ÉCOUTE...");
                _recognizer.RecognizeAsync(RecognizeMode.Multiple);

                microOn = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erreur Micro] {ex.Message}");
                MessageBox.Show($"Erreur d'écoute : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                StopMicrophone();
            }
        }

        private void StopMicrophone()
        {
            try
            {
                Console.WriteLine("[Micro] ARRÊT DE L'ÉCOUTE.");
                _recognizer?.RecognizeAsyncCancel();
            }
            catch { }

            microOn = false;
            _timer.Stop();

            if (_indexImage == 1)
            {
                _indexImage = 0;
                changerMicro();
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.3);
            _timer.Tick += TimerTick;
        }

        private string[] _images = new string[]
        {
            "Voice1Icon",
            "Voice2Icon"
        };

        private void TimerTick(object sender, EventArgs e)
        {
            _indexImage++;
            if (_indexImage >= _images.Length)
            {
                _indexImage = 0;
            }
            changerMicro();
        }

        private void changerMicro()
        {
            try
            {
                Image monImage = (Image)Speech.Template.FindName("Image", Speech);
                if (monImage != null)
                {
                    monImage.Source = new BitmapImage(new Uri(BuildResourcePath(_main.config.GetKeyValue(_images[_indexImage]))));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion
    }
}