using PoemClient.Source.Controls;
using PoemClientWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoemClient.Source
{
    public partial class EnhancedProgramBox : UserControl
    {
        public ProgramParser Program;
        private List<MultiSelectComboBox> multiBoxes = new List<MultiSelectComboBox>();
        private List<List<string>> programResults;
        private MainWindow _main;
        private Action Action;
        private bool initialized = false;
        private Button actionButton; // Référence au bouton d'action
        private bool isLoadingTours = false; // Indicateur de chargement
        private bool vehicleSelectionPending = false; // Indique qu'il faut recharger les tournées

        public EnhancedProgramBox(string config, string programTitle, MainWindow main, Action action = null, string buttonContent = "", bool showButton = true)
        {
            InitializeComponent();
            Program = new ProgramParser(config, programTitle);
            _main = main;
            this.Loaded += WindowLoaded;
            InitalizeComboBox(buttonContent, showButton);
            programResults = Enumerable.Range(0, Program.programNumber).Select(_ => new List<string>()).ToList();
            Action = action;
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await LoadBoxContent(0); // charge véhicule
            // charge ensuite les autres (LoadBoxContent s'appelle récursivement pour préfetch)
            initialized = true;
        }

        private void InitalizeComboBox(string buttonContent, bool showButton)
        {
            // assume ComboBoxPanel is now a Grid (voir XAML ci-dessus)
            if (!(ComboBoxPanel is Grid parentGrid)) return;

            for (int i = 0; i < Program.programNumber; i++)
            {
                // ajouter une nouvelle ligne
                parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Label
                Label label = new Label
                {
                    Content = Program.programTitles[i],
                    Margin = new Thickness(0, 10, 0, 0),
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(label, i);
                Grid.SetColumn(label, 0);
                parentGrid.Children.Add(label);

                // MultiSelectComboBox
                var multi = new MultiSelectComboBox
                {
                    Margin = new Thickness(0, 10, 0, 0),
                    Tag = $"{i}",
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                multi.SelectionChanged += async () => await Multi_SelectionChangedAsync(multi);

                // Événement de fermeture de popup pour la combobox des véhicules
                if (i == 0)
                {
                    multi.PopupClosed += async () => await OnVehiclePopupClosed();
                }

                multiBoxes.Add(multi);

                Grid.SetRow(multi, i);
                Grid.SetColumn(multi, 1);
                parentGrid.Children.Add(multi);

                // bouton (seulement sur la dernière ligne si demandé)
                if (showButton && i + 1 == Program.programNumber)
                {
                    actionButton = new Button
                    {
                        Content = buttonContent,
                        Margin = new Thickness(10, 10, 10, 0),
                        Width = 100
                    };
                    actionButton.Click += Button_OnClick;

                    Grid.SetRow(actionButton, i);
                    Grid.SetColumn(actionButton, 3);
                    parentGrid.Children.Add(actionButton);
                }
            }
        }

        private async Task OnVehiclePopupClosed()
        {
            // Si une modification de véhicule est en attente, recharger les tournées
            if (vehicleSelectionPending && multiBoxes.Count > 1)
            {
                vehicleSelectionPending = false;
                await LoadBoxContent(1);
            }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Action?.Invoke();
        }

        private async Task LoadBoxContent(int index)
        {
            if (index >= multiBoxes.Count) return;

            // Si on charge l'index 1 (tournées) et que plusieurs véhicules sont sélectionnés -> requête groupée avec IN
            if (index == 1 && multiBoxes.Count > 0)
            {
                // Bloquer l'interface pendant le chargement des tournées
                isLoadingTours = true;
                UpdateUIState();

                var selectedVehicles = multiBoxes[0].SelectedItems.ToList();

                if (selectedVehicles.Count > 1)
                {
                    // Construire la requête avec IN au lieu de plusieurs requêtes
                    string query = BuildQueryForIndexWithMultipleVehicles(index, selectedVehicles);
                    string[] result = await _main.CallSQLRequest(query);

                    multiBoxes[index].ItemsSource = result ?? new string[0];
                }
                else
                {
                    // pas de véhicules sélectionnés -> requête normale (sans override)
                    string q = BuildQueryForIndex(index, programResults);
                    string[] result = await _main.CallSQLRequest(q);
                    multiBoxes[index].ItemsSource = result ?? new string[0];
                }

                // Ne pas sélectionner automatiquement le premier élément
                programResults[index] = new List<string>();

                // Débloquer l'interface
                isLoadingTours = false;
                UpdateUIState();
            }
            else
            {
                // comportement standard : remplacer $i par le premier élément correspondant (si présent)
                string query = BuildQueryForIndex(index, programResults);
                string[] result = await _main.CallSQLRequest(query);
                multiBoxes[index].ItemsSource = result ?? new string[0];
                programResults[index] = new List<string>(); // ne pas auto-cocher
            }

            // Sélection par défaut du premier véhicule et du premier tour
            var items = (multiBoxes[index].ItemsSource as IEnumerable<string>)?.ToArray();

            if (index == 0 && items != null && items.Length > 0)
            {
                var firstVehicle = items[0];
                multiBoxes[index].SetSelectedItems(new[] { firstVehicle });
                programResults[index] = new List<string> { firstVehicle };
            }
            else if (index == 1 && items != null && items.Length > 0)
            {
                var firstTour = items[0];
                multiBoxes[index].SetSelectedItems(new[] { firstTour });
                programResults[index] = new List<string> { firstTour };
            }

            // précharger l'index suivant (comme avant)
            await LoadBoxContent(index + 1);
        }

        private void UpdateUIState()
        {
            // Bloquer/débloquer la combobox des véhicules (index 0)
            if (multiBoxes.Count > 0)
            {
                multiBoxes[0].IsEnabled = !isLoadingTours;
            }

            // Bloquer/débloquer la combobox des tournées (index 1)
            if (multiBoxes.Count > 1)
            {
                multiBoxes[1].IsEnabled = !isLoadingTours;
            }

            // Bloquer/débloquer le bouton d'action
            if (actionButton != null)
            {
                actionButton.IsEnabled = !isLoadingTours;
            }
        }

        private string BuildQueryForIndexWithMultipleVehicles(int index, List<string> vehicles)
        {
            // Clone programResults
            var clone = programResults.Select(list => list.ToList()).ToList();

            string result = Program.programs[index];

            for (int i = 1; i < clone.Count; i++)
            {
                if (i == 1) // C'est le paramètre du véhicule (index 0 dans programResults = $1 dans la requête)
                {
                    // Construire la liste pour IN ('P071', 'P072', 'P073')
                    var escapedVehicles = vehicles.Select(v => v.Replace("'", "''")).ToList();
                    string inClause = string.Join(", ", escapedVehicles.Select(v => $"'{v}'"));

                    // Remplacer = '$1' par IN (...)
                    // On suppose que la requête contient RESOURCE.name='$1' ou similar
                    result = result.Replace("=$1", $" IN ({inClause})");
                    result = result.Replace("= $1", $" IN ({inClause})");
                }
                else
                {
                    // Autres paramètres : comportement normal
                    string repl = "";
                    if (clone[i - 1] != null && clone[i - 1].Count > 0)
                        repl = clone[i - 1][0].Replace("'", "''");
                    result = result.Replace($"${i}", $"'{repl}'");
                }
            }

            if (result.Contains("USERID"))
                result = result.Replace("USERID", $@"'{_main.login.GetIdUser()}'");

            return result;
        }

        private string BuildQueryForIndex(int index, List<List<string>> results)
        {
            string result = Program.programs[index];
            for (int i = 1; i < results.Count; i++)
            {
                string repl = "";
                if (results[i - 1] != null && results[i - 1].Count > 0)
                    repl = results[i - 1][0].Replace("'", "''");
                // remplace $i par 'valeur' — si empty, devient ''
                result = result.Replace($"${i}", $"'{repl}'");
            }

            if (result.Contains("USERID"))
                result = result.Replace("USERID", $@"'{_main.login.GetIdUser()}'");

            return result;
        }

        private async Task Multi_SelectionChangedAsync(MultiSelectComboBox multi)
        {
            if (!initialized) return;

            int index = int.Parse(multi.Tag as string);
            if (index >= programResults.Count) return;

            programResults[index] = multi.SelectedItems.ToList();

            // Si on a changé la sélection des véhicules (index 0), marquer qu'un rechargement est nécessaire
            // mais NE PAS recharger immédiatement (attendre la fermeture du popup)
            if (index == 0 && multiBoxes.Count > 1)
            {
                vehicleSelectionPending = true;
            }
            else
            {
                // recharge suivant (compatibilité)
                await LoadBoxContent(index + 1);
            }
        }

        // Méthodes publiques pour HerePaths (compatibilité)
        public List<string> GetSelectedVehicles()
            => (multiBoxes.Count > 0) ? multiBoxes[0].SelectedItems.ToList() : new List<string>();

        public List<string> GetSelectedTours()
            => (multiBoxes.Count > 1) ? multiBoxes[1].SelectedItems.ToList() : new List<string>();

        public List<(string vehicle, string tour)> GetSelectedVehicleTourPairs()
        {
            var vehicles = GetSelectedVehicles();
            var tours = GetSelectedTours();
            var pairs = new List<(string vehicle, string tour)>();

            if (vehicles.Count == 0 && tours.Count == 0) return pairs;

            if (vehicles.Count == 0)
            {
                foreach (var t in tours) pairs.Add((vehicle: "", tour: t));
                return pairs;
            }

            foreach (var v in vehicles)
            {
                var prefix = v.Split(' ')[0];
                var matching = tours.Where(t => t.StartsWith(prefix + "-")).ToList();
                if (matching.Count > 0)
                {
                    foreach (var mt in matching) pairs.Add((v, mt));
                }
                else
                {
                    pairs.Add((v, ""));
                }
            }
            return pairs;
        }
    }
}