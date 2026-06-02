using MahApps.Metro.Controls;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace PoemClient.Source.View
{
    public partial class GooglePaths : Window
    {
        private readonly MainWindow _main;
        private readonly Config _config;
        private bool all = false;
        private int locationAmount = 0;
        private ProgramBoxUpdate ProgramBox;
        private bool _standalone;

        public GooglePaths(bool standalone = false)
        {
            InitializeComponent();
            _main = MainWindow.main;
            _config = _main.GetConfig();
            _standalone = standalone;

            this.Owner = standalone ? null : _main;
            this.Topmost = false;
            this.ResizeMode = ResizeMode.NoResize;
            ProgramBox = new ProgramBoxUpdate(_main.clientConfig, "ClientGetTour", _main, OnClickSearch, _config.GetKeyValue("Itinerary"));
            contentGrid.Children.Add(ProgramBox);
            this.Title = _config.GetKeyValue("GoogleMapsTours");
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Window_closing(object sender, EventArgs e)
        {
            _main.googlePaths = null;
            if (this.Owner != null)
            {
                this.Owner.Activate(); // Remet la fenêtre MainWindow au premier plan
                this.Owner.Topmost = true;
                this.Owner.Topmost = false;
            }
        }

        private void OnClickSearch()
        {
            if (!PoemClient.Source.Tools.Utils.IsInternetAvailable())
            {
                MessageBox.Show(this, _config.GetKeyValue("NetworkError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> items = ProgramBox.GetSelectedItems();

            if (items.Count < 2 || string.IsNullOrWhiteSpace(items[0]) || string.IsNullOrWhiteSpace(items[1]))
            {
                MessageBox.Show(_config.GetKeyValue("InputError"), _config.GetKeyValue("DataError"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Utiliser directement les items sans les diviser
            string vehicleWithType = items[0];
            string vehicleWithTour = items[1];

            string[] tourParts = vehicleWithTour.Split('-');
            if (tourParts.Length < 2)
            {
                MessageBox.Show("Format des données invalide. Veuillez réessayer une fois les données chargées.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedTour = tourParts[1];
            all = string.IsNullOrWhiteSpace(selectedTour);

            try
            {
                string prebuffer = _main.login.GetServerPort() + "%20" + _main.GetUserId();

                // Nouveau format de buffer correspondant au NCL modifié
                string buffer = $@"%7B""{vehicleWithType}""%7D%20%7B""{vehicleWithTour}""%7D";

                string[] result = _main.GetQueries(ProgramBox.Program.nclProgram, prebuffer + "%20" + buffer);
                string baseUrl = "https://www.google.com/maps/dir";

                var parsedTour = all ? ParseAllTour(result) : ParseTour(result);
                if (string.IsNullOrWhiteSpace(parsedTour))
                {
                    MessageBox.Show(_config.GetKeyValue("ServerError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (locationAmount >= 25)
                {
                    MessageBox.Show(_config.GetKeyValue("DataSizeError"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // return;
                }

                Console.WriteLine("URL GOOGLE PATH : " + baseUrl + parsedTour);
                // _main.GetBrowser().Source = new Uri(baseUrl + parsedTour);

                if (_standalone)
                {
                    BrowserWindow browserWindow = new BrowserWindow();
                    browserWindow.Title = _config.GetKeyValue("GoogleMapsTours");
                    browserWindow.Navigate(baseUrl + parsedTour);
                    browserWindow.Show();
                }
                else
                {
                    _main.GetBrowser().Source = new Uri(baseUrl + parsedTour);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex}");
                MessageBox.Show(_config.GetKeyValue("ServerError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string[] RemoveDucplicates(string[] list)
        {
            List<string> result = new List<string>();

            string previous = null;
            foreach (var item in list)
            {
                if (item != previous)
                {
                    result.Add(item);
                }
                previous = item;
            }

            return result.ToArray();
        }

        private string ParseTour(string[] tour)
        {
            tour = tour.Take(tour.Length - 5).Skip(4).ToArray();

            string locations = "";
            locationAmount = 0;

            foreach (string t in tour)
            {
                if (t.Contains('\t')) continue;

                Match m = Regex.Match(
                    t,
                    @"^\s*([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)"
                );

                if (!m.Success) continue;

                string cleanedTour = $"{m.Groups[1].Value},{m.Groups[2].Value}";

                if (!locations.EndsWith(cleanedTour))
                {
                    locations += "/" + cleanedTour;
                    locationAmount++;
                }
            }

            return locations;
        }

        private string ParseAllTour(string[] tour)
        {
            tour = tour.Take(tour.Length - 5).Skip(4).ToArray();

            string locations = "";
            locationAmount = 0;

            foreach (string t in tour)
            {
                // Extrait uniquement les deux coordonnées (ignore tout le reste)
                Match m = Regex.Match(
                    t,
                    @"^\s*([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)"
                );

                if (!m.Success) continue;

                string cleanedTour = $"{m.Groups[1].Value},{m.Groups[2].Value}";

                if (!locations.EndsWith(cleanedTour))
                {
                    locations += "/" + cleanedTour;
                    locationAmount++;
                }
            }

            return locations;
        }
    }
}
