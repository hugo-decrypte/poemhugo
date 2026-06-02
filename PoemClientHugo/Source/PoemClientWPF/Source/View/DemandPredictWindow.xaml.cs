using PoemClient.Source.Tools;
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
    public partial class DemandPredictWindow : Window
    {
        private readonly Config config;
        private readonly MainWindow _main;
        private static DemandPredictWindow instance = null;
        public bool canceled = false;

        private List<CheckableItem> productItems = new List<CheckableItem>();
        private List<CheckableItem> countryItems = new List<CheckableItem>();
        private List<CheckableItem> periodItems = new List<CheckableItem>();

        public DemandPredictWindow()
        {
            InitializeComponent();
            _main = MainWindow.main;
            this.config = _main.GetConfig();
            this.ResizeMode = ResizeMode.NoResize;
            this.Title = config.GetKeyValue("DemandPrediction");

            LoadDataFromDatabase();
            InitializedDataContext();
        }

        private void InitializedDataContext()
        {
            //var CFGPredAll = config.GetKeyValue("SelectAll");
            string defaultFrom = config.GetKeyValue("DefaultModel") ?? "ChatGPT";   // JZ 0328
            if (defaultFrom.Equals("Local"))
            {
                defaultFrom = "ChatGPT";
            }

            DataContext = new
            {
                CFGDemandPredict = config.GetKeyValue("DemandPrediction"),
                //CFGPredAll = config.GetKeyValue("SelectAll"),
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

            // On filtre pour exclure "Local" (sensible à la casse)
            // .Trim() est ajouté par sécurité pour gérer d'éventuels espaces après la virgule
            return langlist.Where(lang => lang.Trim() != "Local").ToArray();
        }

        public static DemandPredictWindow GetInstance()
        {
            if (instance == null) instance = new DemandPredictWindow();
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
            UpdateConfig("DefaultModel", FromSelected);         // JZ 0328
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
                // --- TOUT EST SÉLECTIONNÉ PAR DÉFAUT (IsSelected = true) ---
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
                MessageBox.Show($"{config.GetKeyValue("DatabaseError")}:\n{ex.Message}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);  // JZ 0403
            }
        }

        // Nouvelle méthode générique pour gérer le clic dans la ListBox
        private void OnItemChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var clickedItem = checkBox?.DataContext as CheckableItem;
            if (clickedItem == null) return;

            // Déterminer quelle liste est concernée
            List<CheckableItem> currentList = null;
            ListBox currentListBox = null;

            if (productItems.Contains(clickedItem)) { currentList = productItems; currentListBox = lbProducts; }
            else if (countryItems.Contains(clickedItem)) { currentList = countryItems; currentListBox = lbCountries; }
            else if (periodItems.Contains(clickedItem)) { currentList = periodItems; currentListBox = lbPeriods; }

            if (currentList == null) return;

            // Si on a cliqué sur "Tout sélectionner" (le premier item)
            if (clickedItem == currentList[0])
            {
                foreach (var item in currentList)
                {
                    item.IsSelected = clickedItem.IsSelected;
                }
            }
            else
            {
                // Si on décoche un item normal, on décoche "Tout"
                if (!clickedItem.IsSelected) currentList[0].IsSelected = false;
                
                // Si on coche tous les items normaux, on coche "Tout"
                if (currentList.Skip(1).All(i => i.IsSelected)) currentList[0].IsSelected = true;
            }

            currentListBox.Items.Refresh();
        }

        private bool SaveDemandPredictionToDatabase(List<ArticleItem> products, string dateDeb, string dateFin, List<MarketEntry> aiResults)
        {
            try
            {
                string buffer = _main.login.GetServerPort() + "%20" + _main.GetUserId() + "%20" + products.Count;

                foreach (ArticleItem product in products)
                {
                    var aiData = aiResults?.FirstOrDefault(r => !string.IsNullOrEmpty(r.product_name) && r.product_name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)) ?? aiResults?.FirstOrDefault();

                    string idProd = product.Id?.Replace(" ", "_").Replace("\r", "").Replace("\n", ""); int minQuantity = aiData?.min_quantity ?? 10;
                    int maxQuantity = aiData?.max_quantity ?? 100;
                    double minWeight = aiData?.min_weight ?? 1.0;
                    double maxWeight = aiData?.max_weight ?? 10.0;
                    double minVolume = aiData?.min_volume ?? 0.5;
                    double maxVolume = aiData?.max_volume ?? 5.0;
                    double probability = aiData?.probability ?? 0.75;

                    buffer += $"%20\"{idProd}\"%20\"{dateDeb}\"%20\"{dateFin}\"%20{minQuantity}%20{minWeight.ToString(CultureInfo.InvariantCulture)}%20{minVolume.ToString(CultureInfo.InvariantCulture)}%20{maxQuantity}%20{maxWeight.ToString(CultureInfo.InvariantCulture)}%20{maxVolume.ToString(CultureInfo.InvariantCulture)}%20{probability.ToString(CultureInfo.InvariantCulture)}";
                }

                string nclScript = MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientSetDemandPrediction");

                if (string.IsNullOrEmpty(nclScript))
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Erreur : La ligne 'ClientSetDemandPrediction' est introuvable dans ton fichier clientConfig !\nImpossible de sauvegarder.", config.GetKeyValue("ConfigError"), MessageBoxButton.OK, MessageBoxImage.Error));
                    return false;
                }

                bool success = _main.SetQueries(nclScript, buffer);
                if (!success)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Le serveur a refusé de sauvegarder les données de demande.\nIl y a un problème de format ou la requête a échoué.\n\nBuffer envoyé :\n" + buffer, config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Warning));
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
            if (!Utils.IsInternetAvailable())
            {
                MessageBox.Show(config.GetKeyValue("NetworkError"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);   // JZ 0408
                //MessageBox.Show("Network Error", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Désactive le bouton pendant traitement
            this.PredictButton.IsEnabled = false;
            // Utilisez .Skip(1) pour ignorer l'item "Tout"
            var selectedProductsList = productItems.Skip(1)
                                                .Where(p => p.IsSelected)
                                                .Select(p => (ArticleItem)p.Value)
                                                .ToList();

            var selectedCountriesList = countryItems.Skip(1)
                                                    .Where(c => c.IsSelected)
                                                    .Select(c => c.Name)
                                                    .ToList();

            var selectedPeriodsList = periodItems.Skip(1)
                                                .Where(p => p.IsSelected)
                                                .Select(p => p.Name)
                                                .ToList();

            if (!selectedProductsList.Any() || !selectedCountriesList.Any() || !selectedPeriodsList.Any())
            {
                MessageBox.Show("Please select at least one Product, one Country, and one Period.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.PredictButton.IsEnabled = true;

                return;
            }


            int isGpt = cmbAiModel.SelectedIndex;
            string jsonResult = "";
            ModeleIA iaClient = null;
            switch (isGpt)
            {
                case 1:
                    {
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
                    }
                default:
                    {
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
            }
            try
            {
                int batchSize = 5;
                int totalBatchesPerPeriod = (int)Math.Ceiling((double)selectedProductsList.Count / batchSize);
                int totalGlobalBatches = totalBatchesPerPeriod * selectedPeriodsList.Count;
                int currentGlobalBatch = 0;
                int totalItemsSaved = 0;

                // 1. BOUCLE PRINCIPALE : Pour chaque période cochée
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

                    // 2. SOUS-BOUCLE : Par lots de produits pour la période en cours
                    for (int i = 0; i < selectedProductsList.Count; i += batchSize)
                    {
                        currentGlobalBatch++;
                        var currentBatch = selectedProductsList.Skip(i).Take(batchSize).ToList();
                        string batchProductNames = string.Join(", ", currentBatch.Select(p => p.Name));

                        updateText($"{dateDeb} {currentGlobalBatch}/{totalGlobalBatches} "); //MG

                        // Appel à DemandPredict au lieu de SupplyPredict !
                        TypeRequete predictionService = new DemandPredict(iaClient, batchProductNames, selectedCountriesList, dateDeb, dateFin,this.config);
                        jsonResult = await predictionService.executerRequete();

                        jsonResult = jsonResult.Replace("```json", "").Replace("```", "").Trim();

                        if (jsonResult.StartsWith("E", StringComparison.OrdinalIgnoreCase) ||
                           (!jsonResult.StartsWith("[") && !jsonResult.StartsWith("{")))
                        {
                            throw new Exception($"L'IA a renvoyé une erreur pour la période {dateDeb}.\nRéponse brute :\n{jsonResult}");
                        }

                        var batchResults = JsonSerializer.Deserialize<List<MarketEntry>>(jsonResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        bool isSaved = SaveDemandPredictionToDatabase(currentBatch, dateDeb, dateFin, batchResults);
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
    }
}