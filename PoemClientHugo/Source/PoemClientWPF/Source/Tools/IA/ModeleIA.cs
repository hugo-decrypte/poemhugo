using Org.BouncyCastle.Asn1.Mozilla;
using PoemClient.Source.Tools.IA;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace PoemClientWPF.Tools.IA
{
    //
    // Objet pour communiquer avec un LLM
    //
    public abstract class ModeleIA
    {

        protected string model;//model de l'api
        protected string urlAPI;//Url de l'api
        protected string keyAPI;//key de l'api
        protected static readonly HttpClient _httpClient = new HttpClient();
        protected static readonly int MaxTokens = 3000;

        public ModeleIA(string urlAPI, string model, string keyAPI)
        {
            this.urlAPI = urlAPI;
            this.model = model;
            this.keyAPI = keyAPI;
        }

        public abstract Task<string> contacterIA(string prompt, string donnees, double temp);

        /*
        public static ModeleIA loadModel()
        {
            ModeleIA res;
            switch (AiModelSelector.SelectedIndex)
            {
                case 0:
                    res = new Ollama(_main.config);
                    break;
                case 1:
                    {
                        string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl");
                        string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel");
                        string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey");
                        res = new ChatGPTClient(urlAPI, modelAPI, keyAPI);
                        if (_main.WarningAIChatGPT)
                        {
                            MessageBoxResult resultBox = MessageBox.Show(_main.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (resultBox == MessageBoxResult.No) { Confirm.IsEnabled = true;Speech.IsEnabled = true; return; }
                            _main.WarningAIChatGPT = false;
                        }

                        break;
                    }

                case 2:
                    {
                        string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiKey");
                        string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiUrl");
                        string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiModel");
                        res = new GeminiClient(urlAPI, modelAPI, keyAPI);
                        if (_main.WarningAIGemini)
                        {
                            MessageBoxResult resultBox = MessageBox.Show(_main.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (resultBox == MessageBoxResult.No) { Confirm.IsEnabled = true; Speech.IsEnabled = true; return; }
                            _main.WarningAIGemini = false;
                        }

                        break;
                    }
            }
        }
        */


    }
}