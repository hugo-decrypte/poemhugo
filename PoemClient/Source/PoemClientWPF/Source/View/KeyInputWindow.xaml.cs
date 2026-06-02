using PoemClientWPF;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;

namespace PoemClient.Source.View
{
    public enum KeyInputMode
    {
        GoogleMaps,
        ChatGPT,
        HereMaps
    }

    public partial class KeyInputWindow : Window
    {
        // Propriétés statiques pour stocker les clés et mots de passe (persistantes pendant l'exécution)
        public static string GoogleMapsKey { get; private set; }
        public static string GoogleMapsPassword { get; private set; }

        public static string ChatGptKey { get; private set; }
        public static string ChatGptPassword { get; private set; }
        public static string HereMapsKey { get; private set; }

        public string Key1 => Key1TextBox.Text;
        public string Key2 => Key2PasswordBox.Password;
        public string Key3 => Key3TextBox.Text;
        public string Key4 => Key4PasswordBox.Password;

        private readonly KeyInputMode _mode;
        public MainWindow _main;
        private PoemClientWPF.Tools.Config config;

        public KeyInputWindow(KeyInputMode mode, MainWindow main)
        {
            InitializeComponent();
            this.Owner = main;
            this._main = main;
            _mode = mode;
            config = MainWindow.config;
            SetupUI();

            //this.config = PoemClientWPF.Tools.Config.GetInstance("Config\\poemclient.config");
            ValidateButton.Content = config.GetKeyValue("OK");
            CancelButton.Content = config.GetKeyValue("Cancel"); ;
        }

        private void SetupUI()
        {
            switch (_mode)
            {
                case KeyInputMode.GoogleMaps:
                    Title = this.config.GetKeyValue("GoogleMapsKey");

                    LabelKey1.Text = this.config.GetKeyValue("Key") + ": ";
                    LabelKey2.Text = this.config.GetKeyValue("Password") + ": ";
                    LabelKey3.Text = this.config.GetKeyValue("Key") + ": ";
                    LabelKey4.Text = this.config.GetKeyValue("Password") + ": ";

                    LabelKey1.Visibility = Key1TextBox.Visibility = Visibility.Visible;
                    LabelKey2.Visibility = Key2PasswordBox.Visibility = Visibility.Visible;
                    LabelKey3.Visibility = Key3TextBox.Visibility = Visibility.Collapsed;
                    LabelKey4.Visibility = Key4PasswordBox.Visibility = Visibility.Collapsed;

                    // Priorité : valeur en mémoire _main > propriété statique > vide (et pas masquée)
                    Key1TextBox.Text =
                        !string.IsNullOrWhiteSpace(_main.googleApiKey) && !Regex.IsMatch(_main.googleApiKey, @"^[Xx]+$")
                            ? _main.googleApiKey
                            : (!string.IsNullOrWhiteSpace(GoogleMapsKey) && !Regex.IsMatch(GoogleMapsKey, @"^[Xx]+$")
                                ? GoogleMapsKey
                                : "");

                    if (!string.IsNullOrWhiteSpace(_main.googlePassword) && !Regex.IsMatch(_main.googlePassword, @"^[Xx]+$"))
                    {
                        Key2PasswordBox.Password = _main.googlePassword;
                    }
                    else if (!string.IsNullOrWhiteSpace(GoogleMapsPassword) && !Regex.IsMatch(GoogleMapsPassword, @"^[Xx]+$"))
                    {
                        Key2PasswordBox.Password = GoogleMapsPassword;
                    }
                    else
                    {
                        Key2PasswordBox.Password = "";
                    }
                    break;

                case KeyInputMode.ChatGPT:
                    Title = this.config.GetKeyValue("ChatGptKey");

                    LabelKey1.Visibility = Key1TextBox.Visibility = Visibility.Collapsed;
                    LabelKey2.Visibility = Key2PasswordBox.Visibility = Visibility.Collapsed;
                    LabelKey3.Visibility = Key3TextBox.Visibility = Visibility.Visible;
                    LabelKey4.Visibility = Key4PasswordBox.Visibility = Visibility.Visible;

                    Key3TextBox.Text =
                        !string.IsNullOrWhiteSpace(_main.gptApiKey) && !Regex.IsMatch(_main.gptApiKey, @"^[Xx]+$")
                            ? _main.gptApiKey
                            : (!string.IsNullOrWhiteSpace(ChatGptKey) && !Regex.IsMatch(ChatGptKey, @"^[Xx]+$")
                                ? ChatGptKey
                                : "");

                    if (!string.IsNullOrWhiteSpace(_main.gptPassword) && !Regex.IsMatch(_main.gptPassword, @"^[Xx]+$"))
                    {
                        Key4PasswordBox.Password = _main.gptPassword;
                    }
                    else if (!string.IsNullOrWhiteSpace(ChatGptPassword) && !Regex.IsMatch(ChatGptPassword, @"^[Xx]+$"))
                    {
                        Key4PasswordBox.Password = ChatGptPassword;
                    }
                    else
                    {
                        Key4PasswordBox.Password = "";
                    }
                    break;
                case KeyInputMode.HereMaps:
                    Title = this.config.GetKeyValue("HereMapsKey");

                    LabelKey1.Visibility = Key1TextBox.Visibility = Visibility.Visible;
                    LabelKey2.Visibility = Key2PasswordBox.Visibility = Visibility.Collapsed;
                    LabelKey3.Visibility = Key3TextBox.Visibility = Visibility.Collapsed;
                    LabelKey4.Visibility = Key4PasswordBox.Visibility = Visibility.Collapsed;

                    Key1TextBox.Text =
                        !string.IsNullOrWhiteSpace(_main.hereApiKey) && !Regex.IsMatch(_main.hereApiKey, @"^[Xx]+$")
                            ? _main.hereApiKey
                            : (!string.IsNullOrWhiteSpace(HereMapsKey) && !Regex.IsMatch(HereMapsKey, @"^[Xx]+$")
                                ? HereMapsKey
                                : "");
                    break;
            }
        }

        public static bool EnsureValidKeys(KeyInputMode mode, MainWindow mainWindow, string keyFromConfig, string passwordFromConfig, Func<string, string, bool> validator, out string finalKey, out string finalPassword)
        {
            finalKey = "";
            finalPassword = "";

            // bool skipPassword = (mode == KeyInputMode.HereMaps);     SKIP GOOGLE MDP
            bool skipPassword = (mode == KeyInputMode.HereMaps || mode == KeyInputMode.GoogleMaps);

            // --- 0) helper to normalise "empty / Xxxx" checks
            bool IsMeaningful(string s) => !string.IsNullOrWhiteSpace(s) && !Regex.IsMatch(s, @"^[Xx]+$");

            // --- 1) Try runtime memory first (MainWindow fields, then static backup)
            string runtimeKey = "";
            string runtimePwd = "";

            switch (mode)
            {
                case KeyInputMode.GoogleMaps:
                    runtimeKey = (mainWindow != null && IsMeaningful(mainWindow.googleApiKey)) ? mainWindow.googleApiKey : GoogleMapsKey;
                    runtimePwd = (mainWindow != null && IsMeaningful(mainWindow.googlePassword)) ? mainWindow.googlePassword : GoogleMapsPassword;
                    break;
                case KeyInputMode.ChatGPT:
                    runtimeKey = (mainWindow != null && IsMeaningful(mainWindow.gptApiKey)) ? mainWindow.gptApiKey : ChatGptKey;
                    runtimePwd = (mainWindow != null && IsMeaningful(mainWindow.gptPassword)) ? mainWindow.gptPassword : ChatGptPassword;
                    break;
                case KeyInputMode.HereMaps:
                    runtimeKey = (mainWindow != null && IsMeaningful(mainWindow.hereApiKey)) ? mainWindow.hereApiKey : HereMapsKey;
                    runtimePwd = "";
                    break;
            }

            if (IsMeaningful(runtimeKey))
            {
                // validate runtime key
                if (validator(runtimeKey, skipPassword ? "" : runtimePwd))
                {
                    finalKey = runtimeKey;
                    finalPassword = skipPassword ? "" : runtimePwd;

                    // make sure runtime + static caches are synchronized
                    switch (mode)
                    {
                        case KeyInputMode.GoogleMaps:
                            if (mainWindow != null) { mainWindow.googleApiKey = finalKey; mainWindow.googlePassword = finalPassword; }
                            GoogleMapsKey = finalKey;
                            GoogleMapsPassword = finalPassword;
                            break;
                        case KeyInputMode.ChatGPT:
                            if (mainWindow != null) { mainWindow.gptApiKey = finalKey; mainWindow.gptPassword = finalPassword; }
                            ChatGptKey = finalKey;
                            ChatGptPassword = finalPassword;
                            break;
                        case KeyInputMode.HereMaps:
                            if (mainWindow != null) { mainWindow.hereApiKey = finalKey; }
                            HereMapsKey = finalKey;
                            break;
                    }
                    return true;
                }
                // if runtime key exists but validator fails, fall through to try config / popup
            }

            // --- 2) Try value from config (existing behaviour)
            bool keyValid = IsMeaningful(keyFromConfig);
            bool pwdValid = skipPassword || !string.IsNullOrWhiteSpace(passwordFromConfig);

            if (keyValid && pwdValid)
            {
                finalKey = keyFromConfig;
                finalPassword = skipPassword ? "" : passwordFromConfig;

                if (validator(finalKey, finalPassword))
                {
                    // update memory caches
                    switch (mode)
                    {
                        case KeyInputMode.GoogleMaps:
                            if (mainWindow != null) { mainWindow.googleApiKey = finalKey; mainWindow.googlePassword = finalPassword; }
                            GoogleMapsKey = finalKey;
                            GoogleMapsPassword = finalPassword;
                            break;
                        case KeyInputMode.ChatGPT:
                            if (mainWindow != null) { mainWindow.gptApiKey = finalKey; mainWindow.gptPassword = finalPassword; }
                            ChatGptKey = finalKey;
                            ChatGptPassword = finalPassword;
                            break;
                        case KeyInputMode.HereMaps:
                            if (mainWindow != null) { mainWindow.hereApiKey = finalKey; }
                            HereMapsKey = finalKey;
                            break;
                    }
                    return true;
                }
                else
                {
                    // keep the message but do not return — we will fall back to popup
                    MessageBox.Show("La clé ou le mot de passe est invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // --- 3) Fallback : boucle popup (identique comportement précédent)
            while (true)
            {
                var popup = new KeyInputWindow(mode, mainWindow);
                bool? popupResult = popup.ShowDialog();

                if (popupResult != true)
                {
                    finalKey = null;
                    finalPassword = null;
                    return false;
                }

                // Récupère les clés saisies (préférer le runtime mainWindow s'il a été mis à jour par la popup)
                switch (mode)
                {
                    case KeyInputMode.GoogleMaps:
                        finalKey = (mainWindow != null && IsMeaningful(mainWindow.googleApiKey)) ? mainWindow.googleApiKey : GoogleMapsKey;
                        finalPassword = (mainWindow != null && IsMeaningful(mainWindow.googlePassword)) ? mainWindow.googlePassword : GoogleMapsPassword;
                        break;
                    case KeyInputMode.ChatGPT:
                        finalKey = (mainWindow != null && IsMeaningful(mainWindow.gptApiKey)) ? mainWindow.gptApiKey : ChatGptKey;
                        finalPassword = (mainWindow != null && IsMeaningful(mainWindow.gptPassword)) ? mainWindow.gptPassword : ChatGptPassword;
                        break;
                    case KeyInputMode.HereMaps:
                        finalKey = (mainWindow != null && IsMeaningful(mainWindow.hereApiKey)) ? mainWindow.hereApiKey : HereMapsKey;
                        finalPassword = "";
                        break;
                }

                if (validator(finalKey, finalPassword))
                {
                    // MAJ mémoire runtime (au cas où)
                    switch (mode)
                    {
                        case KeyInputMode.GoogleMaps:
                            if (mainWindow != null) { mainWindow.googleApiKey = finalKey; mainWindow.googlePassword = finalPassword; }
                            GoogleMapsKey = finalKey;
                            GoogleMapsPassword = finalPassword;
                            break;
                        case KeyInputMode.ChatGPT:
                            if (mainWindow != null) { mainWindow.gptApiKey = finalKey; mainWindow.gptPassword = finalPassword; }
                            ChatGptKey = finalKey;
                            ChatGptPassword = finalPassword;
                            break;
                        case KeyInputMode.HereMaps:
                            if (mainWindow != null) { mainWindow.hereApiKey = finalKey; }
                            HereMapsKey = finalKey;
                            break;
                    }
                    return true;
                }
                else
                {
                    MessageBox.Show("La clé ou le mot de passe est invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public static bool TestGoogleMapsCredentials(string apiKey, string password)
        {
            // if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(password))        SKIP GOOGLE MDP
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://maps.googleapis.com/maps/api/geocode/json?address=Paris&key={apiKey}";
                    var response = client.GetAsync(url).Result;

                    if (!response.IsSuccessStatusCode)
                        return false;

                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    return responseContent.Contains("\"status\" : \"OK\"");
                }
            }
            catch
            {
                return false;
            }
        }
        public static bool TestChatGptCredentials(string apiKey, string password)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) //  || string.IsNullOrWhiteSpace(password)
                return false;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    // requête simple pour lister les modèles disponibles
                    var response = client.GetAsync("https://api.openai.com/v1/models").Result;

                    if (!response.IsSuccessStatusCode)
                        return false;

                    string content = response.Content.ReadAsStringAsync().Result;
                    return content.Contains("gpt"); // contenu très probable si clé valide
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_mode)
            {
                case KeyInputMode.GoogleMaps:
                    GoogleMapsKey = Key1;
                    GoogleMapsPassword = Key2;

                    // MAJ mémoire principale pour garder les clés à jour partout
                    _main.googleApiKey = GoogleMapsKey;
                    _main.googlePassword = GoogleMapsPassword;
                    break;

                case KeyInputMode.ChatGPT:
                    ChatGptKey = Key3;
                    ChatGptPassword = Key4;

                    _main.gptApiKey = ChatGptKey;
                    _main.gptPassword = ChatGptPassword;
                    break;
                case KeyInputMode.HereMaps:
                    HereMapsKey = Key1;
                    _main.hereApiKey = HereMapsKey;
                    break;
            }

            DialogResult = true;
            Close();
        }
        public static bool TestHereMapsCredentials(string apiKey, MainWindow main)
        {
            if (string.IsNullOrWhiteSpace(apiKey) || Regex.IsMatch(apiKey, @"^[Xx]+$"))
                return false;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Test simple avec un itinéraire court
                    string testUrl = $"https://router.hereapi.com/v8/routes?transportMode=truck&origin=48.8566,2.3522&destination=48.8570,2.3525&return=summary&apikey={apiKey}";
                    var response = client.GetAsync(testUrl).Result;
                    if ((int)response.StatusCode == 429)
                    {
                        MessageBox.Show(main, MainWindow.config.GetKeyValue("TooManyRequests"), MainWindow.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);    // JZ 0409
                        return false;
                    }
                    if (!response.IsSuccessStatusCode)
                        return false;

                    string content = response.Content.ReadAsStringAsync().Result;

                    // Vérifie que la réponse contient bien une section routes
                    return content.Contains("\"routes\"") || content.Contains("\"sections\"");
                }
            }
            catch
            {
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
