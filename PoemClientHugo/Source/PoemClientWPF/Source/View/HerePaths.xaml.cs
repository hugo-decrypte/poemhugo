using PoemClient.Source.Tools;
using PoemClientWPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;

namespace PoemClient.Source.View
{
    public partial class HerePaths : Window
    {
        private readonly MainWindow _main;
        private readonly PoemClientWPF.Tools.Config _config;
        private bool loading = false;
        private int locationAmount = 0;
        // private ProgramBoxUpdate ProgramBox; // Mettre EnhancedProgramBox pour multitournée
        private EnhancedProgramBox ProgramBox;
        bool _standalone;


        // inside HerePaths class
        private class TourInfo
        {
            public string VehicleId;          // peut être null si absent
            public string TourIndex;          // index de la tournée (si présent)
            public List<string> Coordinates;  // liste "lat,lng"
            public List<string> LocationNames;
        }

        private List<TourInfo> ParseToursFromServerResult(string[] result)
        {
            // Regex pour coordonnées avec nom optionnel: lat lon "nom"
            var coordRegex = new Regex(@"^-?\d+\.\d+\s+-?\d+\.\d+(?:\s+""[^""]*"")?$");

            var lines = result.Select(l => Regex.Replace(l ?? "", @"\t", " "))
                              .Select(l => Regex.Replace(l, @"\s+", " ").Trim())
                              .Where(l => !string.IsNullOrEmpty(l))
                              .ToArray();

            var tours = new List<TourInfo>();
            int i = 0;
            string currentVehicle = null;

            while (i < lines.Length)
            {
                var line = lines[i];

                // Candidate: "number number" (vehicle header or route header)
                if (Regex.IsMatch(line, @"^\d+\s+\d+$"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int a = int.Parse(parts[0]);
                    int b = int.Parse(parts[1]);

                    // Case A: next line is a coordinate -> current line is a route header
                    if (i + 1 < lines.Length && coordRegex.IsMatch(lines[i + 1]))
                    {
                        var coords = new List<string>();
                        var names = new List<string>();
                        int maxToRead = Math.Min(b, lines.Length - (i + 1));

                        for (int k = 0; k < maxToRead; k++)
                        {
                            var candidate = lines[i + 1 + k];
                            if (!coordRegex.IsMatch(candidate)) break;

                            // Extraire coordonnées et nom
                            var match = Regex.Match(candidate, @"^(-?\d+\.\d+)\s+(-?\d+\.\d+)(?:\s+""([^""]*)"")?$");
                            if (match.Success)
                            {
                                string lat = match.Groups[1].Value;
                                string lng = match.Groups[2].Value;
                                string name = match.Groups[3].Success ? match.Groups[3].Value : "";

                                coords.Add($"{lat},{lng}");
                                names.Add(name);
                            }
                        }

                        if (coords.Count > 0)
                        {
                            tours.Add(new TourInfo
                            {
                                VehicleId = currentVehicle,
                                TourIndex = a.ToString(),
                                Coordinates = coords,
                                LocationNames = names
                            });
                            i += 1 + coords.Count;
                            continue;
                        }
                    }

                    // Case B: vehicle header
                    if (i + 1 < lines.Length && Regex.IsMatch(lines[i + 1], @"^\d+\s+\d+$") &&
                        i + 2 < lines.Length && coordRegex.IsMatch(lines[i + 2]))
                    {
                        currentVehicle = parts[0].PadLeft(3, '0');
                        i++;
                        continue;
                    }

                    i++;
                    continue;
                }

                // If line is a coordinate, collect a block
                if (coordRegex.IsMatch(line))
                {
                    var coords = new List<string>();
                    var names = new List<string>();

                    while (i < lines.Length && coordRegex.IsMatch(lines[i]))
                    {
                        var match = Regex.Match(lines[i], @"^(-?\d+\.\d+)\s+(-?\d+\.\d+)(?:\s+""([^""]*)"")?$");
                        if (match.Success)
                        {
                            string lat = match.Groups[1].Value;
                            string lng = match.Groups[2].Value;
                            string name = match.Groups[3].Success ? match.Groups[3].Value : "";

                            coords.Add($"{lat},{lng}");
                            names.Add(name);
                        }
                        i++;
                    }

                    if (coords.Count > 0)
                    {
                        tours.Add(new TourInfo
                        {
                            VehicleId = currentVehicle,
                            TourIndex = null,
                            Coordinates = coords,
                            LocationNames = names
                        });
                    }
                    continue;
                }

                i++;
            }

            return tours;
        }

        public HerePaths(MainWindow main, bool standalone = false)
        {
            InitializeComponent();
            _standalone = standalone;
            _main = main;
            _config = main.GetConfig();
            this.Owner = standalone ? null : main;
            this.Topmost = false;
            this.ResizeMode = ResizeMode.NoResize;
            // ProgramBox = new ProgramBoxUpdate(_main.clientConfig, "ClientGetTour", _main, OnClickSearch, _config.GetKeyValue("Itinerary")); // Mettre EnhancedProgramBox pour multitournée
            ProgramBox = new EnhancedProgramBox(_main.clientConfig, "ClientGetTour", _main, OnClickSearch, _config.GetKeyValue("Itinerary")); // Mettre EnhancedProgramBox pour multitournée
            contentGrid.Children.Add(ProgramBox);
            this.Title = _config.GetKeyValue("HereMapsTours");
            this.Closing += Window_Closing;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private async void OnClickSearch()
        {
            if (!Utils.IsInternetAvailable())
            {
                MessageBox.Show(this, _config.GetKeyValue("NetworkError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var (paths, intermediates, vehicleIds, allLocationNames) = await LoadRouteAsync();

            if (paths != null && paths.Count > 0)
            {
                if (paths.Count == 1)
                {
                    var firstNames = allLocationNames?.FirstOrDefault();
                    string html = await HEREMaps.GenerateLeafletMapHtmlAsync(_main, paths[0], intermediates?.FirstOrDefault(), firstNames, _config);

                    if (_standalone)
                    {
                        BrowserWindow browserWindow = new BrowserWindow();
                        browserWindow.Title = _config.GetKeyValue("HereMapsTours");
                        browserWindow.NavigateToString(html);
                        browserWindow.Show();
                    }
                    else
                    {
                        _main.GetBrowser().NavigateToString(html);
                    }
                }
                else
                {
                    try
                    {
                        var filteredPaths = new List<List<LatLngZ>>();
                        foreach (var subList in paths)
                        {
                            var reduced = new List<LatLngZ>();
                            for (int i = 0; i < subList.Count; i += 5)
                                reduced.Add(subList[i]);

                            if (subList.Count > 0 && reduced.Last() != subList.Last())
                                reduced.Add(subList.Last());

                            filteredPaths.Add(reduced);
                        }

                        string html = await HEREMaps.GenerateLeafletMapHtmlMultipleAsync(_main, filteredPaths, vehicleIds, intermediates, allLocationNames, _config);

                        if (_standalone)
                        {
                            BrowserWindow browserWindow = new BrowserWindow();
                            browserWindow.Title = _config.GetKeyValue("HereMapsTours");
                            browserWindow.NavigateToString(html);
                            browserWindow.Show();
                        }
                        else
                        {
                            _main.GetBrowser().NavigateToString(html);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show(this,
                            _config.GetKeyValue("DataSizeError"),
                            _config.GetKeyValue("SystemError"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
        }


        // Méthode async qui fait la vraie logique, retourne la liste
        // Note : signature modifiée pour renvoyer plusieurs trajets
        public async Task<(List<List<LatLngZ>> paths, List<List<string>> intermediates, List<string> vehicleIds, List<List<string>> allLocationNames)> LoadRouteAsync()
        {
            var vehicles = ProgramBox.GetSelectedVehicles();
            var tours = ProgramBox.GetSelectedTours();

            if ((vehicles == null || vehicles.Count == 0) && (tours == null || tours.Count == 0))
            {
                MessageBox.Show(_config.GetKeyValue("InputError"), _config.GetKeyValue("DataError"),
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return (null, null, null, null);
            }

            if (loading) return (null, null, null, null);
            loading = true;

            try
            {
                string prebuffer = _main.login.GetServerPort() + "%20" + _main.GetUserId();

                string EncodeList(IEnumerable<string> items)
                {
                    if (items == null || !items.Any()) return "%7B%7D";
                    var quoted = items.Select(s => $"\"{s}\"");
                    return "%7B" + string.Join("%2C%20", quoted) + "%7D";
                }

                string vehiclesBlock = EncodeList(vehicles);
                string toursBlock = EncodeList(tours);
                string buffer = $"{vehiclesBlock}%20{toursBlock}";

                string[] result = _main.GetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientGetTour"), prebuffer + "%20" + buffer);

                // Parse all tours from the server result
                var parsedTours = ParseToursFromServerResult(result);
                if (parsedTours == null || parsedTours.Count == 0)
                {
                    MessageBox.Show(_config.GetKeyValue("DataError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (null, null, null, null);
                }

                // truck params (kept as before, you can adapt to per-vehicle if server returns per-vehicle)
                var truckParams = ParseTruckParams(result);

                // Ensure HERE key once (avoid re-prompting inside BuildHereUrl)
                bool okKey = KeyInputWindow.EnsureValidKeys(
                    KeyInputMode.HereMaps,
                    _main,
                    MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsKey"),
                    MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsPassword"),
                    (theApiKey, _) => KeyInputWindow.TestHereMapsCredentials(theApiKey, _main),
                    out string finalKey,
                    out string _
                );

                if (!okKey)
                {
                    MessageBox.Show("Clé HERE invalide ou non fournie.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (null, null, null, null);
                }

                var allPaths = new List<List<LatLngZ>>();
                var allIntermediates = new List<List<string>>();
                var allLocationNames = new List<List<string>>();
                var vehicleIds = new List<string>();

                for (int tIndex = 0; tIndex < parsedTours.Count; tIndex++)
                {
                    var tourInfo = parsedTours[tIndex];
                    if (tourInfo?.Coordinates == null || tourInfo.Coordinates.Count < 2)
                        continue;

                    string tourPathString = string.Join("|", tourInfo.Coordinates);
                    string apiUrl = BuildHereUrl(tourPathString, truckParams, finalKey);

                    var path = await GetHereRouteAsync(apiUrl);
                    if (path == null || path.Count == 0)
                    {
                        Console.WriteLine($"HERE route failed for tour {tIndex} (vehicle {tourInfo.VehicleId})");
                        continue;
                    }

                    allPaths.Add(path);

                    // intermediate stops (sans premier/dernier)
                    var intermediates = tourInfo.Coordinates
                        .Skip(1)
                        .Take(Math.Max(0, tourInfo.Coordinates.Count - 2))
                        .ToList();
                    allIntermediates.Add(intermediates);

                    // NOUVEAU: stocker les noms (sans premier/dernier)
                    var intermediateNames = tourInfo.LocationNames
                        .Skip(1)
                        .Take(Math.Max(0, tourInfo.LocationNames.Count - 2))
                        .ToList();
                    allLocationNames.Add(intermediateNames);

                    vehicleIds.Add(vehicles[tIndex]);

                    await Task.Delay(300);
                }

                return (allPaths, allIntermediates, tours, allLocationNames);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur : {ex}");
                MessageBox.Show(_config.GetKeyValue("ServerError"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return (null, null, null, null);
            }
            finally
            {
                loading = false;
            }
        }


        private string ParseTour(string[] tour)
        {
            // Enlève l'entête (4 premières lignes) + pied de réponse (5 dernières lignes)
            tour = tour.Skip(4).Take(tour.Length - 9).ToArray();

            locationAmount = 0;
            List<string> locations = new List<string>();

            foreach (string line in tour)
            {
                // Nettoie la ligne : supprime tab, doubles espaces, retours chariot
                string cleaned = Regex.Replace(line, @"\s+", " ").Trim();

                // Vérifie si c'est une ligne GPS au format "lat lon"
                if (Regex.IsMatch(cleaned, @"^-?\d+\.\d{3,}\s-?\d+\.\d{3,}$"))
                {
                    string coord = cleaned.Replace(" ", ",");
                    locations.Add(coord);
                    locationAmount++;

                }
            }

            return string.Join("|", locations);
        }


        private string ParseAllTour(string[] tour)
        {
            tour = tour.Take(tour.Length - 5).Skip(4).ToArray();
            string locations = "";
            locationAmount = 0;

            foreach (string t in tour)
            {
                if (t.Contains('.'))
                {
                    string cleanedTour = Regex.Replace(t, @"\s+", " ").Trim().Replace(" ", ",");
                    locations += cleanedTour + "|";
                    locationAmount++;
                }
            }

            return locations.TrimEnd('|');
        }

        private Dictionary<string, string> ParseTruckParams(string[] result)
        {
            string line = result[2];
            var parts = line.Split('\t');

            int length = (int)Math.Round(double.Parse(parts[2].Replace(',', '.'), CultureInfo.InvariantCulture) * 100);
            int width = (int)Math.Round(double.Parse(parts[3].Replace(',', '.'), CultureInfo.InvariantCulture) * 100);
            int height = (int)Math.Round(double.Parse(parts[4].Replace(',', '.'), CultureInfo.InvariantCulture) * 100);
            int grossWeight = (int)Math.Round(double.Parse(parts[5].Replace(',', '.'), CultureInfo.InvariantCulture) * 1000);

            var truckParams = new Dictionary<string, string>
            {
                // ["truck[length]"] = length.ToString(),
                // ["truck[width]"] = width.ToString(),
                // ["truck[height]"] = height.ToString(),
                // ["truck[grossWeight]"] = grossWeight.ToString(),
                // ["truck[tunnelCategory]"] = parts[6],
                // ["truck[trailerCount]"] = parts[7],
                // ["truck[shippedHazardousGoods]"] = parts[1]
            };

            return truckParams;
        }

        private string BuildHereUrl(string waypoints, Dictionary<string, string> truckParams)
        {
            var coordinates = waypoints.Split('|');
            if (coordinates.Length < 2) return "";

            string apiKey = MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsKey");

            bool ok = KeyInputWindow.EnsureValidKeys(
            KeyInputMode.HereMaps,
            _main,
            MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsKey"),
            MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsPassword"),
            (theApiKey, _) => KeyInputWindow.TestHereMapsCredentials(theApiKey, _main), // <-- adaptation avec lambda
            out string finalKey,
            out string _
            );

            string origin = coordinates.First();
            string destination = coordinates.Last();
            var vias = coordinates.Skip(1).Take(coordinates.Length - 2);

            var uri = new StringBuilder($"https://router.hereapi.com/v8/routes?");
            uri.Append($"apiKey={finalKey}");
            uri.Append("&transportMode=truck");
            uri.Append($"&origin={origin}&destination={destination}");

            foreach (var via in vias)
                uri.Append($"&via={via}");

            // Ajout des paramètres camion (s'ils existent)
            if (truckParams != null)
            {
                foreach (var param in truckParams)
                {
                    if (param.Key == "truck[tunnelCategory]" && param.Value != "")
                        uri.Append($"&truck[tunnelCategory]={param.Value}");
                    else if (param.Key == "truck[trailerCount]" && param.Value != "" && param.Value != "0")
                        uri.Append($"&truck[trailerCount]={param.Value}");
                    else if (param.Key == "truck[shippedHazardousGoods]" && param.Value != "")
                        uri.Append($"&truck[shippedHazardousGoods]={param.Value}");
                    else
                    {
                        // Nettoyage de la valeur (virgule → point pour les décimaux)
                        string value = param.Value.Replace(',', '.');
                        uri.Append($"&{param.Key}={value}");
                    }
                }
            }

            uri.Append("&return=polyline,summary");
            Console.WriteLine("url Tour: " + uri);
            return uri.ToString();
        }

        private string BuildHereUrl(string waypoints, Dictionary<string, string> truckParams, string finalKey)
        {
            var coordinates = waypoints.Split('|');
            if (coordinates.Length < 2) return "";

            string origin = coordinates.First();
            string destination = coordinates.Last();

            var uri = new StringBuilder($"https://router.hereapi.com/v8/routes?");
            uri.Append($"apiKey={finalKey}");
            uri.Append("&transportMode=truck");
            uri.Append($"&origin={origin}&destination={destination}");

            foreach (var via in coordinates.Skip(1).Take(coordinates.Length - 2))
                uri.Append($"&via={via}");

            if (truckParams != null)
            {
                foreach (var param in truckParams)
                {
                    if (param.Key == "truck[tunnelCategory]" && param.Value != "")
                        uri.Append($"&truck[tunnelCategory]={param.Value}");
                    else if (param.Key == "truck[trailerCount]" && param.Value != "" && param.Value != "0")
                        uri.Append($"&truck[trailerCount]={param.Value}");
                    else if (param.Key == "truck[shippedHazardousGoods]" && param.Value != "")
                        uri.Append($"&truck[shippedHazardousGoods]={param.Value}");
                    else
                    {
                        string value = param.Value.Replace(',', '.');
                        uri.Append($"&{param.Key}={value}");
                    }
                }
            }

            uri.Append("&return=polyline,summary");
            Console.WriteLine("url Tour: " + uri);
            return uri.ToString();
        }



        public static List<LatLngZ> DecodePolyline(string encoded)
        {
            var decodedPoints = PoemClient.Source.Tools.PolylineEncoderDecoder.Decode(encoded);
            return decodedPoints;
        }

        public async Task<List<LatLngZ>> GetHereRouteAsync(string url)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if ((int)response.StatusCode == 429)
                    {
                        MessageBox.Show(_main, MainWindow.config.GetKeyValue("TooManyRequests"), "Erreur Quota", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Erreur HTTP HERE : " + response.StatusCode + " - " + response.ReasonPhrase);
                        return null;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;

                        if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                        {
                            MessageBox.Show("Aucune route trouvée dans la réponse HERE.");
                            return null;
                        }

                        var firstRoute = routes[0];
                        if (!firstRoute.TryGetProperty("sections", out var sections) || sections.GetArrayLength() == 0)
                        {
                            MessageBox.Show("Aucune section trouvée dans la première route.");
                            return null;
                        }

                        var fullPath = new List<LatLngZ>();

                        foreach (var section in sections.EnumerateArray())
                        {
                            if (section.TryGetProperty("polyline", out var polylineProp))
                            {
                                string polyline = polylineProp.GetString();
                                var segmentPath = DecodePolyline(polyline);
                                if (segmentPath != null)
                                    fullPath.AddRange(segmentPath);
                            }
                        }

                        if (fullPath.Count == 0)
                        {
                            MessageBox.Show("Erreur de décodage des polylines.");
                            return null;
                        }

                        return fullPath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_main, $"{MainWindow.config.GetKeyValue("SystemError")}:\n{ex.Message}", MainWindow.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error); // JZ 0408
                   //MessageBox.Show("Exception lors de l’appel HERE : " + ex.Message);
                    return null;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _main.herePaths = null;
            this.Owner?.Activate(); // Ramène la fenêtre principale au premier plan
        }
    }
}
