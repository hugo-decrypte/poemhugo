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
    public partial class ProgramBoxUpdate : UserControl
    {
        public ProgramParser Program;
        private List<String> programResults;
        private List<ComboBox> comboBoxes = new List<ComboBox>();
        private MainWindow _main;
        private Action Action;
        private bool initialized = false;
        public ProgramBoxUpdate(string config, string programTitle, MainWindow main, Action action = null, string buttonContent = "", bool showButton = true)
        {
            InitializeComponent();
            Program = new ProgramParser(config, programTitle);
            _main = main;
            this.Loaded += WindowLoaded;
            InitalizeComboBox(buttonContent, showButton);
            programResults = Enumerable.Repeat(string.Empty, comboBoxes.Count).ToList();
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

        private async Task LoadBoxContent(int index)
        {
            if (index >= comboBoxes.Count)
            {
                return;
            }
            string query = ModifiedQuery(Program.programs[index]);
            string[] result = await _main.CallSQLRequest(query);
            comboBoxes[index].ItemsSource = result;
            comboBoxes[index].SelectedIndex = 0;
            programResults[index] = comboBoxes[index].SelectedItem as string;
            LoadBoxContent(index + 1);
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
                programResults[index] = comboBox.SelectedItem as string;
                LoadBoxContent(index + 1);
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
