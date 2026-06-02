using PoemClientWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoemClient.Source
{
    public partial class GoogleMapsProgramBox : UserControl
    {
        public ProgramParser Program;
        private List<String> programResults;
        private List<ComboBox> comboBoxes = new List<ComboBox>();
        private MainWindow _main;
        private Action Action;
        private bool initialized = false;
        private string[][] _cachedResults;

        public GoogleMapsProgramBox(string config, string programTitle, Action action = null, string buttonContent = "", bool showButton = true)
        {
            InitializeComponent();
            Program = new ProgramParser(config, programTitle);
            _main = MainWindow.main;
            this.Loaded += WindowLoaded;
            InitalizeComboBox(buttonContent, showButton);
            programResults = Enumerable.Repeat(string.Empty, comboBoxes.Count).ToList();
            _cachedResults = new string[Program.programNumber][];
            Action = action;
        }

        private void InitializeComponent()
        {
            // Créer la structure de base du UserControl
            Grid mainGrid = new Grid();
            mainGrid.Background = new SolidColorBrush(Colors.White);

            StackPanel comboBoxPanel = new StackPanel();
            comboBoxPanel.Name = "ComboBoxPanel";

            mainGrid.Children.Add(comboBoxPanel);
            this.Content = mainGrid;
            this.ComboBoxPanel = comboBoxPanel;
        }

        private StackPanel ComboBoxPanel;

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await LoadBoxContent(0);
            initialized = true;
        }

        private void InitalizeComboBox(string buttonContent, bool showButton)
        {
            // Créer un Grid principal avec la même structure que GoogleMaps
            Grid mainGrid = new Grid();
            mainGrid.Margin = new Thickness(10, 20, 10, 10);

            // Définir les colonnes comme dans GoogleMaps.xaml
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            for (int i = 0; i < Program.programNumber; i++)
            {
                // Définir la ligne pour chaque ComboBox
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Label aligné comme dans GoogleMaps
                Label label = new Label();
                label.Content = Program.programTitles[i];
                label.Margin = new Thickness(0, 10, 0, 0);
                label.FontSize = 14;
                label.HorizontalAlignment = HorizontalAlignment.Left;
                label.VerticalAlignment = VerticalAlignment.Center;

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, i);
                mainGrid.Children.Add(label);

                // ComboBox alignée avec les TextBox de GoogleMaps
                ComboBox comboBox = new ComboBox();
                comboBox.Width = double.NaN; // Auto width comme les TextBox
                comboBox.Margin = new Thickness(0, 10, 0, 0);
                comboBox.MaxDropDownHeight = 150;
                comboBox.SelectionChanged += ComboBox_OnSelectionChanged;
                comboBox.Tag = $"{i}";
                comboBoxes.Add(comboBox);

                Grid.SetColumn(comboBox, 1);
                Grid.SetRow(comboBox, i);
                mainGrid.Children.Add(comboBox);

                // Bouton dans la dernière colonne si nécessaire
                if (showButton && i + 1 == Program.programNumber)
                {
                    Button button = new Button();
                    button.Content = buttonContent;
                    button.Margin = new Thickness(0, 10, 0, 0);
                    button.FontWeight = FontWeights.Normal;
                    button.FontSize = 12;
                    button.FontFamily = new FontFamily("Segoe UI");
                    button.HorizontalAlignment = HorizontalAlignment.Center;
                    button.HorizontalContentAlignment = HorizontalAlignment.Center;
                    button.VerticalAlignment = VerticalAlignment.Center;
                    button.Width = 100;
                    button.Height = double.NaN; // Auto height
                    button.Background = new SolidColorBrush(Colors.White);
                    button.Padding = new Thickness(5);
                    button.BorderThickness = new Thickness(1);
                    button.Click += Button_OnClick;

                    Grid.SetColumn(button, 2);
                    Grid.SetRow(button, i);
                    mainGrid.Children.Add(button);
                }
            }

            ComboBoxPanel.Children.Add(mainGrid);
        }

        private async Task LoadBoxContent(int index)
        {
            if (index >= comboBoxes.Count)
            {
                return;
            }

            // Si le cache contient déjà les résultats pour cet index, on ne le recharge pas :
            // On s'assure juste qu'ItemsSource est assigné si nécessaire, puis on retourne.
            if (_cachedResults[index] != null)
            {
                if (comboBoxes[index].ItemsSource == null)
                {
                    comboBoxes[index].ItemsSource = _cachedResults[index];
                    if (comboBoxes[index].SelectedIndex < 0)
                    {
                        comboBoxes[index].SelectedIndex = 0;
                    }
                    programResults[index] = comboBoxes[index].SelectedItem as string;
                }
                // IMPORTANT : on ne recharge pas les indexes suivants pour ne pas écraser leurs sélections.
                return;
            }

            // Sinon, récupération depuis la source (premier chargement)
            string query = ModifiedQuery(Program.programs[index]);
            string[] result = await _main.CallSQLRequest(query);

            // Mettre en cache les résultats (même si vaz vide)
            _cachedResults[index] = result;

            // Assigner ItemsSource (référence stable, on ne remplacera plus la référence ensuite)
            comboBoxes[index].ItemsSource = result;
            if (comboBoxes[index].SelectedIndex < 0)
            {
                comboBoxes[index].SelectedIndex = 0;
            }
            programResults[index] = comboBoxes[index].SelectedItem as string;

            // Continuer le chargement séquentiel (remplissage initial)
            await LoadBoxContent(index + 1);
        }

        private string ModifiedQuery(string query)
        {
            string result = query;
            for (int i = 0; i < comboBoxes.Count; i++)
            {
                if (result.Contains($"${i}"))
                {
                    result = result.Replace($"${i}", $"'{programResults[i - 1]}'");
                }
            }
            if (result.Contains("USERID"))
            {
                result = result.Replace("USERID", $@"'{_main.login.GetIdUser()}'");
            }
            return result;
        }

        private void ComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null && initialized)
            {
                int index = int.Parse(comboBox.Tag as string);
                if (index >= programResults.Count)
                    return;

                // Met à jour la valeur choisie
                programResults[index] = comboBox.SelectedItem as string;

                // On ne relance le chargement des combos suivants QUE si l'index suivant n'a PAS été chargé auparavant.
                // Ainsi, si les résultats sont communs (TypeRisk identiques pour tous TypeResource), ils ne seront pas rechargés.
                int nextIndex = index + 1;
                if (nextIndex < comboBoxes.Count && _cachedResults[nextIndex] == null)
                {
                    // Si c'est la première fois pour le suivant, on le charge (chaînage normal)
                    _ = LoadBoxContent(nextIndex);
                }
                // sinon : on garde ce qui est dans le cache (pas d'écrasement)
            }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Action();
        }

        public List<string> GetSelectedItems()
        {
            return programResults;
        }
    }
}