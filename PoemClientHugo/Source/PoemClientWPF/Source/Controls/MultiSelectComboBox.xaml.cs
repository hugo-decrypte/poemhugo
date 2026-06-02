using PoemClientWPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoemClient.Source.Controls
{
    public partial class MultiSelectComboBox : UserControl
    {
        private List<string> _items = new List<string>();
        private List<string> _selected = new List<string>();
        private bool _suppressEvents = false;

        public event Action SelectionChanged;
        public event Action PopupClosed; // Nouvel événement

        public MultiSelectComboBox()
        {
            InitializeComponent();

            // S'abonner à l'événement de fermeture du popup
            PART_Popup.Closed += (s, e) => PopupClosed?.Invoke();
        }

        public IEnumerable<string> ItemsSource
        {
            get => _items;
            set
            {
                _items = value?.ToList() ?? new List<string>();
                BuildItems();
            }
        }

        public IEnumerable<string> SelectedItems => _selected.ToList();

        private void BuildItems()
        {
            ItemsPanel.Children.Clear();
            _selected.Clear();

            _suppressEvents = true;
            CbAll.IsChecked = false;
            _suppressEvents = false;

            foreach (var it in _items)
            {
                var cb = new CheckBox() { Content = it, Tag = it, Margin = new Thickness(0, 2, 0, 2) };
                cb.Checked += Item_Checked;
                cb.Unchecked += Item_Unchecked;
                ItemsPanel.Children.Add(cb);
            }

            RefreshDisplayText();
        }

        private void Item_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;

            var cb = sender as CheckBox;
            if (cb?.Tag is string v)
            {
                _selected.Add(v);

                // si tout sélectionné -> cocher Tous (protégé)
                if (_selected.Count == _items.Count)
                {
                    _suppressEvents = true;
                    CbAll.IsChecked = true;
                    _suppressEvents = false;
                }
            }

            RefreshDisplayText();
            SelectionChanged?.Invoke();
        }

        private void Item_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;

            var cb = sender as CheckBox;
            if (cb?.Tag is string v)
            {
                _selected.Remove(v);

                if (CbAll.IsChecked == true)
                {
                    _suppressEvents = true;
                    CbAll.IsChecked = false;
                    _suppressEvents = false;
                }
            }

            RefreshDisplayText();
            SelectionChanged?.Invoke();
        }

        private void CbAll_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;

            _suppressEvents = true;
            _selected.Clear();
            foreach (var child in ItemsPanel.Children.OfType<CheckBox>())
            {
                child.IsChecked = true;
                _selected.Add(child.Tag as string);
            }
            _suppressEvents = false;

            RefreshDisplayText();
            SelectionChanged?.Invoke();
        }

        private void CbAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressEvents) return;

            _suppressEvents = true;
            foreach (var child in ItemsPanel.Children.OfType<CheckBox>())
            {
                child.IsChecked = false;
            }
            _selected.Clear();
            _suppressEvents = false;

            RefreshDisplayText();
            SelectionChanged?.Invoke();
        }

        private void RefreshDisplayText()
        {
            if (_selected.Count == 0)
                PART_DisplayText.Text = "";                                           // JZ 2025
            else if (_selected.Count == _items.Count)
                PART_DisplayText.Text = "Tous";     //  config.GetKeyValue("All");    // JZ 2025
            else
            {
                var s = string.Join(", ", _selected.Take(5));
                if (_selected.Count > 5) s += $" (+{_selected.Count - 5})";
                PART_DisplayText.Text = s;
            }
        }

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            PART_Popup.IsOpen = !PART_Popup.IsOpen;
        }

        public void SetSelectedItems(IEnumerable<string> items)
        {
            if (items == null) return;
            var toSelect = new HashSet<string>(items);
            _suppressEvents = true;
            _selected.Clear();
            foreach (var cb in ItemsPanel.Children.OfType<CheckBox>())
            {
                var tag = cb.Tag as string;
                bool should = toSelect.Contains(tag);
                cb.IsChecked = should;
                if (should) _selected.Add(tag);
            }
            _suppressEvents = false;
            RefreshDisplayText();
            SelectionChanged?.Invoke();
        }
    }
}