using PoemClient.Source.Tools.IA;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ArticleItem = PoemClientWPF.Tools.IA.ArticleItem;
using MessageBox = System.Windows.MessageBox;

namespace PoemClient.View
{
    public partial class SupplyPredictWindow : Window
    {
        private bool _isInitialized = false;

        private readonly Config config;
        private readonly MainWindow _main;
        private static SupplyPredictWindow instance = null;
        public bool canceled = false;

        private List<CheckableItem> productItems = new List<CheckableItem>();
        private List<CheckableItem> countryItems = new List<CheckableItem>();
        private List<CheckableItem> periodItems = new List<CheckableItem>();

        public SupplyPredictWindow()
        {
            InitializeComponent();
            _main = MainWindow.main;
            this.config = _main.GetConfig();
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = config.GetKeyValue("SupplyPrediction");

        }

        private void InitializedDataContext()
        {
            string defaultFrom = config.GetKeyValue("DefaultModel") ?? "ChatGPT";
            if (defaultFrom.Equals("Local"))
            {
                defaultFrom = "ChatGPT";
            }

            DataContext = new
            {
                CFGSupplyPredict = config.GetKeyValue("SupplyPrediction"),
                // Ajout des variables pour correspondre à la première classe
                CFGPredProduct = config.GetKeyValue("Items"),
                CFGPredZones = config.GetKeyValue("Zones"),
                CFGPredPeriodes = config.GetKeyValue("Periods"),

                CFGPredictButton = config.GetKeyValue("Execute"),
                LanguagesAI = GetLangAIList(),
                SelectedFrom = defaultFrom,
            };
        }

        private string[] GetLangAIList()
        {
            string[] langlist = config.GetKeyValue("ModelList")?.Split(',');

            if (langlist == null)
            {
                return null;
            }

            return langlist.Where(lang => lang.Trim() != "Local").ToArray();
        }

        public static SupplyPredictWindow GetInstance()
        {
            if (instance == null) instance = new SupplyPredictWindow();
            return instance;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Owner = null;
            instance = null;
        }

        private void updateText(string text)
        {
            Application.Current.Dispatcher.Invoke(() => { this.Loading.Text = text; });
        }

        private void AISelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string FromSelected = cmbAiModel.SelectedItem as string;
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

        private void OnItemChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var clickedItem = checkBox?.DataContext as CheckableItem;
            if (clickedItem == null) return;

            List<CheckableItem> currentList = null;
            ListBox currentListBox = null;

            if (productItems.Contains(clickedItem)) { currentList = productItems; currentListBox = lbProducts; }
            else if (countryItems.Contains(clickedItem)) { currentList = countryItems; currentListBox = lbCountries; }
            else if (periodItems.Contains(clickedItem)) { currentList = periodItems; currentListBox = lbPeriods; }

            if (currentList == null) return;

            if (clickedItem == currentList[0])
            {
                foreach (var item in currentList) item.IsSelected = clickedItem.IsSelected;
            }
            else
            {
                if (!clickedItem.IsSelected) currentList[0].IsSelected = false;
                if (currentList.Skip(1).All(i => i.IsSelected)) currentList[0].IsSelected = true;
            }

            currentListBox.Items.Refresh();
        }

        private string SanitizeForServer(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Unknown";

            // 1. Remplace les espaces par des underscores
            string safeStr = input.Replace(" ", "_");

            // 2. Supprime les retours à la ligne
            safeStr = safeStr.Replace("\r", "").Replace("\n", "");

            // 3. LE COUPABLE : Remplace le & qui casse les requêtes web !
            safeStr = safeStr.Replace("&", "et");

            // 4. Supprime les guillemets internes parasites (au cas où l'IA en rajoute)
            safeStr = safeStr.Replace("\"", "");

            // 5. Optionnel mais très recommandé pour les vieux serveurs : retirer les accents
            safeStr = safeStr.Replace("é", "e").Replace("è", "e").Replace("ê", "e")
                             .Replace("à", "a").Replace("â", "a")
                             .Replace("ô", "o").Replace("ù", "u").Replace("ç", "c");

            return safeStr;
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                string[] values = _main.GetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientGetItem"), _main.login.GetServerPort() + "%20" + _main.GetUserId());

                if (values == null || values.Length < 2)
                {
                    MessageBox.Show("Failed to load data from database.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                List<string> lines = new List<string>();
                bool startCollecting = false;

                for (int i = 0; i < values.Length; i++)
                {
                    string cleanedLine = values[i].Trim();

                    if (string.IsNullOrWhiteSpace(cleanedLine) || cleanedLine == ".") continue;

                    if (!startCollecting)
                    {
                        if (cleanedLine.Contains("MESSAGE="))
                        {
                            startCollecting = true;
                            cleanedLine = cleanedLine.Substring(cleanedLine.IndexOf("MESSAGE=") + "MESSAGE=".Length).Trim();
                            if (!string.IsNullOrWhiteSpace(cleanedLine)) lines.Add(cleanedLine);
                        }
                        else if (cleanedLine.ToLower().Contains("message buffer"))
                        {
                            startCollecting = true;
                            string remainder = cleanedLine.Substring(cleanedLine.ToLower().IndexOf("message buffer") + "message buffer".Length).Trim();
                            if (!string.IsNullOrWhiteSpace(remainder)) lines.Add(remainder);
                        }
                    }
                    else
                    {
                        lines.Add(cleanedLine);
                    }
                }

                if (lines.Count == 0) throw new Exception("Aucune donnée valide trouvée dans la réponse du serveur.");

                int index = 0;

                string zoneStr = lines[index++].Replace("\"", "").Trim();
                if (!int.TryParse(zoneStr, out int nbZone))
                    throw new Exception($"Impossible de lire le nombre de pays. Texte reçu : '{zoneStr}'");

                List<string> zones = new List<string>();
                for (int i = 0; i < nbZone; i++) zones.Add(lines[index++].Replace("\"", "").Trim());

                string periodStr = lines[index++].Replace("\"", "").Trim();
                if (!int.TryParse(periodStr, out int nbPeriod))
                    throw new Exception($"Impossible de lire le nombre de périodes. Texte reçu : '{periodStr}'");

                List<string> periods = new List<string>();
                for (int i = 0; i < nbPeriod; i++) periods.Add(lines[index++].Replace("\"", "").Replace("'", "").Trim());

                string articleStr = lines[index++].Replace("\"", "").Trim();
                if (!int.TryParse(articleStr, out int nbArticle))
                    throw new Exception($"Impossible de lire le nombre d'articles. Texte reçu : '{articleStr}'");

                List<ArticleItem> articles = new List<ArticleItem>();
                for (int i = 0; i < nbArticle; i++)
                {
                    string id = lines[index++].Replace("\"", "").Trim();
                    string name = lines[index++].Replace("\"", "").Trim();
                    string techName = lines[index++].Replace("\"", "").Trim();
                    articles.Add(new ArticleItem { Id = id, Name = name, TechName = techName });
                }

                productItems = articles.Select(a => new CheckableItem { Name = a.Name, Value = a, IsSelected = true }).ToList();
                productItems.Insert(0, new CheckableItem { Name = config.GetKeyValue("Items"), IsSelected = true });

                countryItems = zones.Select(z => new CheckableItem { Name = z, Value = z, IsSelected = true }).ToList();
                countryItems.Insert(0, new CheckableItem { Name = config.GetKeyValue("Zones"), IsSelected = true });

                periodItems = periods.Select(p => new CheckableItem { Name = p, Value = p, IsSelected = true }).ToList();
                periodItems.Insert(0, new CheckableItem { Name = config.GetKeyValue("Periods"), IsSelected = true });

                lbProducts.ItemsSource = productItems;
                lbCountries.ItemsSource = countryItems;
                lbPeriods.ItemsSource = periodItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{config.GetKeyValue("DatabaseError")}:\n{ex.Message}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SaveSupplyPredictionToDatabase(List<ArticleItem> products, string dateDeb, string dateFin, List<MarketEntry> aiResults)
        {
            try
            {
                string buffer = _main.login.GetServerPort() + "%20" + _main.GetUserId() + "%20" + products.Count;

                foreach (ArticleItem product in products)
                {
                    var aiData = aiResults?.FirstOrDefault(r => !string.IsNullOrEmpty(r.product_name) && r.product_name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)) ?? aiResults?.FirstOrDefault();

                    string idProd = product.Id;
                    double minPrice = aiData?.min_data?.price ?? 0.0;
                    string producersMin = "Unknown";
                    if (aiData?.min_data?.suppliers != null)
                    {
                        producersMin = SanitizeForServer(string.Join(",", aiData.min_data.suppliers));
                    }
                    int quantityMin = aiData?.min_data?.quantity ?? 10;
                    double weightMin = aiData?.min_data?.weight ?? 1.0;
                    double volumeMin = aiData?.min_data?.volume ?? 0.5;

                    double maxPrice = aiData?.max_data?.price ?? 0.0;
                    string producersMax = "Unknown";
                    if (aiData?.max_data?.suppliers != null)
                    {
                        producersMax = SanitizeForServer(string.Join(",", aiData.max_data.suppliers));
                    }
                    int quantityMax = aiData?.max_data?.quantity ?? 100;
                    double weightMax = aiData?.max_data?.weight ?? 10.0;
                    double volumeMax = aiData?.max_data?.volume ?? 5.0;

                    double probability = aiData?.probability ?? 0.75;

                    buffer += $"%20\"{idProd}\"%20\"{dateDeb}\"%20\"{dateFin}\"%20{minPrice.ToString(CultureInfo.InvariantCulture)}%20\"{producersMin}\"%20{quantityMin}%20{weightMin.ToString(CultureInfo.InvariantCulture)}%20{volumeMin.ToString(CultureInfo.InvariantCulture)}%20{maxPrice.ToString(CultureInfo.InvariantCulture)}%20\"{producersMax}\"%20{quantityMax}%20{weightMax.ToString(CultureInfo.InvariantCulture)}%20{volumeMax.ToString(CultureInfo.InvariantCulture)}%20{probability.ToString(CultureInfo.InvariantCulture)}";
                }

                string nclScript = MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientSetSupplyPrediction");

                // Alignement avec la première classe pour gérer proprement l'erreur serveur/script introuvable
                if (string.IsNullOrEmpty(nclScript))
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Erreur : La ligne 'ClientSetSupplyPrediction' est introuvable dans ton fichier clientConfig !\nImpossible de sauvegarder.", config.GetKeyValue("ConfigError"), MessageBoxButton.OK, MessageBoxImage.Error));
                    return false;
                }

                bool success = _main.SetQueries(nclScript, buffer);
                if (!success)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Le serveur a refusé de sauvegarder les données d'approvisionnement.\nIl y a un problème de format ou la requête a échoué.\n\nBuffer envoyé :\n" + buffer, config.GetKeyValue("ServerError"), MessageBoxButton.OK, MessageBoxImage.Warning));
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur BDD : " + ex.Message);
                return false;
            }
        }

        private async void Predict_Click(object sender, RoutedEventArgs e)
        {
            if (!PoemClient.Source.Tools.Utils.IsInternetAvailable())
            {
                MessageBox.Show(config.GetKeyValue("NetworkError"), config.GetKeyValue("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.PredictButton.IsEnabled = false;
            ModeleIA iaClient = null;
            int index = cmbAiModel.SelectedIndex;

            // Alignement complet du switch avec la première classe
            switch (index)
            {
                case 1:
                    iaClient = new GeminiClient(MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiUrl"), MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiModel"), MainWindow.GetLineBelowParameter(_main.clientConfig, "GeminiKey"));
                    if (_main.WarningAIGemini)
                    {
                        _main.WarningAIGemini = false;
                        MessageBoxResult result = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), config.GetKeyValue("Warning"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                        {
                            this.PredictButton.IsEnabled = true;
                            return;
                        }
                        break;
                    }
                    break;
                default:
                    iaClient = new ChatGPTClient(MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl"), MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel"), MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey"));
                    if (_main.WarningAIChatGPT)
                    {
                        _main.WarningAIChatGPT = false;
                        MessageBoxResult result = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), config.GetKeyValue("Warning"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.No)
                        {
                            this.PredictButton.IsEnabled = true;
                            return;
                        }
                        break;
                    }
                    break;
            }

            var selectedProductsList = productItems.Skip(1).Where(p => p.IsSelected).Select(p => (ArticleItem)p.Value).ToList();
            var selectedCountriesList = countryItems.Skip(1).Where(c => c.IsSelected).Select(c => c.Name).ToList();
            var selectedPeriodsList = periodItems.Skip(1).Where(p => p.IsSelected).Select(p => p.Name).ToList();

            if (!selectedProductsList.Any() || !selectedCountriesList.Any() || !selectedPeriodsList.Any())
            {
                MessageBox.Show("Please select at least one Product, one Country, and one Period.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.PredictButton.IsEnabled = true;
                return;
            }

            string jsonResult = "";

            try
            {
                int batchSize = 5;
                int totalBatchesPerPeriod = (int)Math.Ceiling((double)selectedProductsList.Count / batchSize);
                int totalGlobalBatches = totalBatchesPerPeriod * selectedPeriodsList.Count;
                int currentGlobalBatch = 0;
                int totalItemsSaved = 0;

                foreach (string periodText in selectedPeriodsList)
                {
                    string dateDeb = "";
                    string dateFin = "";

                    MatchCollection matches = Regex.Matches(periodText, @"\d{1,2}/\d{1,2}/\d{4}");
                    if (matches.Count >= 2)
                    {
                        dateDeb = DateTime.Parse(matches[0].Value, new CultureInfo("fr-FR")).ToString("dd/MM/yyyy");
                        dateFin = DateTime.Parse(matches[1].Value, new CultureInfo("fr-FR")).ToString("dd/MM/yyyy");
                    }
                    else if (matches.Count == 1)
                    {
                        dateDeb = DateTime.Parse(matches[0].Value, new CultureInfo("fr-FR")).ToString("dd/MM/yyyy");
                        dateFin = dateDeb;
                    }
                    else
                    {
                        throw new Exception($"Aucune date au format attendu n'a pu être extraite de la période : {periodText}");
                    }

                    for (int i = 0; i < selectedProductsList.Count; i += batchSize)
                    {
                        currentGlobalBatch++;
                        var currentBatch = selectedProductsList.Skip(i).Take(batchSize).ToList();
                        string batchProductNames = string.Join(", ", currentBatch.Select(p => p.Name));

                        updateText($"{dateDeb} {currentGlobalBatch}/{totalGlobalBatches} "); // MG

                        TypeRequete predictionService = new SupplyPredict(iaClient, batchProductNames, selectedCountriesList, dateDeb, dateFin, config);
                        jsonResult = await predictionService.executerRequete();

                        jsonResult = jsonResult.Replace("```json", "").Replace("```", "").Trim();

                        if (jsonResult.StartsWith("E", StringComparison.OrdinalIgnoreCase) ||
                           (!jsonResult.StartsWith("[") && !jsonResult.StartsWith("{")))
                        {
                            throw new Exception($"L'IA a renvoyé une erreur pour la période {dateDeb}.\nRéponse brute :\n{jsonResult}");
                        }

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString |
                                             System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                        };

                        var batchResults = JsonSerializer.Deserialize<List<MarketEntry>>(jsonResult, options);
                        bool isSaved = SaveSupplyPredictionToDatabase(currentBatch, dateDeb, dateFin, batchResults);
                        if (isSaved && batchResults != null) totalItemsSaved += currentBatch.Count;
                    }
                }

                updateText(config.GetKeyValue("Success"));
            }
            catch (Exception ex)
            {
                updateText(config.GetKeyValue("Failure"));

                MessageBox.Show($"{config.GetKeyValue("ApiError")}:\n{ex.Message}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error); // JZ 0408
            }
            finally
            {
                this.PredictButton.IsEnabled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            LoadDataFromDatabase();
            InitializedDataContext();
        }
    }
}