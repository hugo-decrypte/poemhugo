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
    public partial class ProgramBox : UserControl
    {
        public ProgramParser Program;
        private List<String> programResults;
        private List<ComboBox> comboBoxes = new List<ComboBox>();
        private MainWindow _main;
        private Action Action;
        private bool initialized = false;

        // --- Nouveau : cache des résultats par index ---
        // Chaque entrée contient le tableau de strings renvoyé par la requête SQL pour cet index.
        private string[][] _cachedResults;

        public ProgramBox(string config, string programTitle, MainWindow main, Action action = null, string buttonContent = "", bool showButton = true)
        {
            InitializeComponent();
            Program = new ProgramParser(config, programTitle);
            _main = main;
            this.Loaded += WindowLoaded;
            InitalizeComboBox(buttonContent, showButton);
            programResults = Enumerable.Repeat(string.Empty, comboBoxes.Count).ToList();
            // initialisation du cache avec la taille correcte
            _cachedResults = new string[Program.programNumber][];
            Action = action;
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await LoadBoxContent(0);
            initialized = true;
        }

        private void InitalizeComboBox(string buttonContent, bool showButton)
        {
            for (int i = 0; i < Program.programNumber; i++)
            {
                // create comboBox
                Grid grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Label label = new Label();
                label.Content = Program.programTitles[i];
                label.Margin = new Thickness(0, 10, 0, 0);
                label.FontSize = 14;
                grid.Children.Add(label);
                Grid.SetColumn(label, 1);

                ComboBox comboBox = new ComboBox();
                comboBox.Margin = new Thickness(0, 10, 0, 0);
                comboBox.MaxDropDownHeight = 150;
                comboBox.SelectionChanged += ComboBox_OnSelectionChanged;
                comboBox.Tag = $"{i}";
                comboBoxes.Add(comboBox);

                grid.Children.Add(comboBox);
                Grid.SetColumn(comboBox, 2);

                ComboBoxPanel.Children.Add(grid);

                if (showButton && i + 1 == Program.programNumber)
                {
                    Button button = new Button();
                    button.Content = buttonContent;
                    button.Margin = new Thickness(0, 10, 0, 0);
                    button.FontWeight = FontWeights.Normal;
                    button.FontSize = 12;
                    button.FontFamily = new FontFamily("Segoe UI");
                    button.HorizontalAlignment = HorizontalAlignment.Right;
                    button.HorizontalContentAlignment = HorizontalAlignment.Center;
                    button.VerticalAlignment = VerticalAlignment.Bottom;
                    button.Width = 100;
                    button.BorderThickness = new Thickness(1);
                    button.Click += Button_OnClick;

                    grid.Children.Add(button);
                    Grid.SetColumn(button, 3);
                }
            }
        }

        /// <summary>
        /// Charge le contenu pour le combo d'index 'index'.
        /// -> Stocke le résultat dans _cachedResults[index] à la première récupération.
        /// -> Après la première récupération, le cache est utilisé et on n'écrase plus ItemsSource.
        /// </summary>
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
