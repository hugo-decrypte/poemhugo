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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PoemClient.Source.Tools.IA.SpeechRecognition;
using PoemClient.Source.Tools;

namespace PoemClient.Source.View
{
    public partial class PromptWindow : Window
    {
        private readonly MainWindow _main;
        private static PromptWindow instance = null;
        public bool canceled = false;

        private bool microOn = false;
        private ISpeechRecognition recognizer;
        private ModeleIA _ollamaInstance;
        private KeywordMatcher _keywordMatcher;

        private PromptWindow()
        {
            InitializeComponent();

            this._main = MainWindow.main;

            this.Title = MainWindow.config.GetKeyValue("AI Agent");

            recognizer = new WindowsRecognition(MainWindow.config.GetLanguageCode());
            recognizer.MessageReceived += OnSpeechMessageReceived;
            _ollamaInstance = new Ollama(MainWindow.config);
            _keywordMatcher = new KeywordMatcher(MainWindow.config);

            InitializeDataContext();

            // Modèle implicite
            string implicitModel = MainWindow.config.GetKeyValue("ImplicitModel");
            if (!string.IsNullOrWhiteSpace(implicitModel))
            {
                AiModelSelector.Visibility = Visibility.Collapsed;
            }

            // KeyBinding entrée
            Prompt.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter && !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftShift))
                {
                    e.Handled = true;
                    SendPrompt();
                }
            };
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
            string resourcePath = MainWindow.config.GetRessourcePath();
            resourcePath = Path.Combine(resourcePath, splittedRes[1]);
            return resourcePath;
        }

        private void InitializeDataContext()
        {
            string implicitModel = MainWindow.config.GetKeyValue("ImplicitModel");
            string defaultFrom = !string.IsNullOrWhiteSpace(implicitModel)
                ? implicitModel
                : MainWindow.config.GetKeyValue("DefaultModel") ?? "Modèle local";

            DataContext = new
            {
                LanguagesAI = MainWindow.config.GetKeyValue("ModelList").Split(','),
                OkButton = MainWindow.config.GetKeyValue("OK"),
                SelectedFrom = defaultFrom,
                Voice1Icon = BuildResourcePath(MainWindow.config.GetKeyValue("MicrophoneOff")),
                Voice2Icon = BuildResourcePath(MainWindow.config.GetKeyValue("MicrophoneOn")),
            };
        }

        private void AISelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string FromSelected = AiModelSelector.SelectedItem as string;
            UpdateConfig("DefaultModel", FromSelected);
        }

        private bool UpdateConfig(string key, string value)
        {
            if (MainWindow.config.GetKeyValue(key) != value)
            {
                MainWindow.config.SetKeyValueKeepLineBreak(key, value);
                return true;
            }
            return false;
        }

        public static PromptWindow getInstance()
        {
            if (instance == null)
                instance = new PromptWindow();
            return instance;
        }

        #region Analyse Prompt
        private void ConfirmPrompt(object sender, RoutedEventArgs e)
        {
            SendPrompt();
        }

        async private void SendPrompt()
        {
            string userPrompt = Prompt.Text;
            if (userPrompt == "")
            {
                MessageBox.Show(MainWindow.config.GetKeyValue("NoAgentTask"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string reponse;

                if (AiModelSelector.SelectedIndex == 2)
                {
                    // Sans connexion : KeywordMatcher uniquement, aucun appel réseau
                    reponse = _keywordMatcher.TryMatch(userPrompt)
                              ?? "{\"tool\":\"none\",\"arguments\":[]}";
                }
                else
                {
                    // Modèle local (Ollama) ou ChatGPT : passe par le LLM
                    ModeleIA modeleIA;
                    switch (AiModelSelector.SelectedIndex)
                    {
                        case 0:
                            modeleIA = _ollamaInstance;
                            break;
                        default: // case 1 : ChatGPT
                            {
                                string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl");
                                string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel");
                                string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey");
                                modeleIA = new ChatGPTClient(urlAPI, modelAPI, keyAPI);
                                if (_main.WarningAIChatGPT)
                                {
                                    MessageBoxResult resultBox = MessageBox.Show(MainWindow.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (resultBox == MessageBoxResult.No) { Confirm.IsEnabled = true; Speech.IsEnabled = true; return; }
                                    _main.WarningAIChatGPT = false;
                                }
                                break;
                            }
                    }
                    TypeRequete agent = new Agent(MainWindow.config, _main.clientConfig, modeleIA, userPrompt);

                    Console.WriteLine($"[IA] Envoi de la requête au modèle : {userPrompt}");
                    reponse = await agent.executerRequete();
                    Console.WriteLine($"[IA] REPONSE REÇUE :\n{reponse}");
                }

                if (reponse.Contains("\"tool\":\"none\"") || reponse.Contains("\"tool\": \"none\""))
                {
                    MessageBox.Show(MainWindow.config.GetKeyValue("NoAgentResponse"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                    using (var jsonreponse = JsonDocument.Parse(reponse))
                    {
                        JsonElement jsonroot = jsonreponse.RootElement;
                        JsonElement elts = jsonroot.GetProperty("arguments");

                        switch (jsonroot.GetProperty("tool").ToString())
                        {
                            case "static":
                                await StaticFunctions.RunAction($"{elts.Deserialize<string[]>()[0]}".Split(','));
                                break;
                            case "view":
                                {
                                    foreach (var elt in elts.EnumerateArray())
                                    {
                                        string viewName = elt.ToString();
                                        if (!MainWindow.GetViews().Contains(viewName))
                                        {
                                            MessageBox.Show(MainWindow.config.GetKeyValue("NoAgentResponse"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                                            break;
                                        }
                                        new OpenView(viewName, _main).Show();
                                        break;
                                    }
                                    break;
                                }
                            case "menu":
                                foreach (var script in elts.EnumerateArray())
                                {
                                    await _main.ContextMenu_Click(script.ToString());
                                }
                                break;
                            case "script":
                                {
                                    foreach (var script in elts.EnumerateArray())
                                    {
                                        string[] values = _main.GetQueries(
                                            script.ToString(),
                                            _main.login.GetServerPort() + "%20" + _main.GetUserId()
                                        );

                                        foreach (string value in values)
                                        {
                                            if (value.Contains("MESSAGE="))
                                            {
                                                string view = value.Split('=').Last().Split(',')[0];
                                                new OpenView(view, _main).Show();
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }
                            default:
                                MessageBox.Show(MainWindow.config.GetKeyValue("NoAgentTask"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
            }
            catch (PingException)
            {
                throw new PingException("Connexion impossible");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"[Erreur IA] {exc.Message}");
                MessageBox.Show(MainWindow.config.GetKeyValue("TaskNotWorking"), MainWindow.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PromptClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (microOn)
            {
                StopMicrophone();
            }
            this.Owner = null;
            instance = null;
            recognizer.stop();
        }

        #endregion

        private static string SplitCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1]))
                    result.Append(' ');
                result.Append(s[i]);
            }
            return result.ToString();
        }

        #region Speech
        protected virtual void OnSpeechMessageReceived(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            Console.WriteLine(msg);
            switch (msg.Split(':')[0])
            {
                case "RESULT":
                    Dispatcher.Invoke(() =>
                    {
                        if (microOn)
                        {
                            Prompt.Text = msg.Split(':')[1];
                            if (Prompt.Text.ToLower().Contains("ok"))
                            {
                                Prompt.Text = Prompt.Text.ToLower().Split(new string[] { "ok" }, StringSplitOptions.None)[0];
                                StopMicrophone();
                                SendPrompt();
                            }
                        }
                    });
                    break;
                case "LISTENING":
                    Dispatcher.Invoke(() =>
                    {
                        microOn = true;
                        Speech.IsEnabled = true;
                        changerMicro();
                    });
                    break;
                case "WARNING":
                    Dispatcher.Invoke(() =>
                    {
                        StopMicrophone();
                        MessageBox.Show(MainWindow.config.GetKeyValue(msg.Split(':')[1]), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    break;
            }
        }

        private void MonBoutonImage_Click(object sender, RoutedEventArgs e)
        {
            if (!microOn)
            {
                StartMicrophone();
            }
            else
            {
                StopMicrophone();
                Prompt.Text = "";
            }
        }

        private void StartMicrophone()
        {
            microOn = true;
            Prompt.Text = "";
            recognizer.startListen();
        }

        private void StopMicrophone()
        {
            microOn = false;
            changerMicro();
            recognizer.stopListen();
        }

        private string[] _images = new string[]
        {
            "MicrophoneOff",
            "MicrophoneOn"
        };

        private void changerMicro()
        {
            try
            {
                Image monImage = (Image)Speech.Template.FindName("Image", Speech);
                Border micBorder = (Border)Speech.Template.FindName("MicBorder", Speech);

                if (monImage != null)
                {
                    int indexImage = microOn ? 1 : 0;
                    monImage.Source = new BitmapImage(new Uri(BuildResourcePath(MainWindow.config.GetKeyValue(_images[indexImage]))));
                }

                if (micBorder != null)
                {
                    micBorder.Background = microOn
                        ? new SolidColorBrush(Color.FromRgb(227, 242, 253))
                        : new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    micBorder.BorderBrush = microOn
                        ? new SolidColorBrush(Color.FromRgb(144, 202, 249))
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224));
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
