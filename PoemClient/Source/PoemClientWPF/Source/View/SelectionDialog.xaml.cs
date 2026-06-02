using System;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NPOI.POIFS.Properties;
using PoemClient.View;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.View;

namespace PoemClient.Source.View
{

    public partial class SelectionDialog : Window
    {

        private readonly Config _config;

        public string SelectedOption;

        private static int _lastSelectedIndex = 0;

        public SelectionDialog(Config config)
        {
            InitializeComponent();

            this._config = config;

            DataContext = new
            {
                //SelectionDialogTitle = _config.GetKeyValue("AnalysisType"),
                okButton = _config.GetKeyValue("OK")
            };

            ComboSelection.SelectedIndex = _lastSelectedIndex;
            // Écouter le changement pour mettre à jour la variable
            ComboSelection.SelectionChanged += (s, e) => {
                _lastSelectedIndex = ComboSelection.SelectedIndex;
            };

        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = (ComboSelection.SelectedItem as ComboBoxItem)?.Content.ToString();
            this.DialogResult = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

    }
}

