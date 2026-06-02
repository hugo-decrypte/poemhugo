using PoemClientWPF;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PoemClient.Source.View
{
    public partial class SystemSettings : Window
    {
        private readonly MainWindow main;

        public SystemSettings(MainWindow _main)
        {
            main = _main;

            this.Owner = main;

            DataContext = new
            {
                Title = main.GetConfig().GetKeyValue("SystemSettings"),     // JZ

                FolderView = main.GetConfig().GetKeyValue("FolderViewTab"),
                TreeView = main.GetConfig().GetKeyValue("FolderDirectoryTab"),
                Colors = main.GetConfig().GetKeyValue("ColorsTab"),

                OptionalWindow1Label = main.GetConfig().GetKeyValue("FolderView1Setting"),
                OptionalWindow2Label = main.GetConfig().GetKeyValue("FolderView2Setting"),

                TopMargin = main.GetConfig().GetKeyValue("DirectoryTopMarginSetting"),
                LeftMargin = main.GetConfig().GetKeyValue("DirectoryLeftMarginSetting"),
                BottomMargin = main.GetConfig().GetKeyValue("DirectoryBottomMarginSetting"),
                Spacing = main.GetConfig().GetKeyValue("DirectoryTextSpacingSetting"),
                OpenTreeViewLabel = main.GetConfig().GetKeyValue("DirectoryExpandSetting"),


                TitleBackground = main.GetConfig().GetKeyValue("TitleBackgroundSetting"),
                TitleFontColor = main.GetConfig().GetKeyValue("TitleFontColorSetting"),

                MenuBackground = main.GetConfig().GetKeyValue("MenuBackgroundSetting"),
                MenuFontColor = main.GetConfig().GetKeyValue("MenuFontColorSetting"),

                FolderBackground = main.GetConfig().GetKeyValue("FolderBackgroundSetting"),
                FolderFontColor = main.GetConfig().GetKeyValue("FolderFontColorSetting"),
                FolderHighlight = main.GetConfig().GetKeyValue("FolderHighlightSetting"),

                FrameColor = main.GetConfig().GetKeyValue("FrameColorSetting"),

                Cancel = main.GetConfig().GetKeyValue("Cancel"),
            };

            InitializeComponent();

            OptionalWindow1.IsChecked = Convert.ToBoolean(main.GetConfig().GetKeyValue("OpenFolderView1"));
            OptionalWindow2.IsChecked = Convert.ToBoolean(main.GetConfig().GetKeyValue("OpenFolderView2"));

            OpenTreeView.IsChecked = Convert.ToBoolean(main.GetConfig().GetKeyValue("ExpandDirectory"));

            TreeViewTopMargin.Text = main.GetConfig().GetKeyValue("DirectoryTopMargin");
            TreeViewLeftMargin.Text = main.GetConfig().GetKeyValue("DirectoryLeftMargin");
            TreeViewBottomMargin.Text = main.GetConfig().GetKeyValue("DirectoryBottomMargin");
            TreeViewSpacing.Text = main.GetConfig().GetKeyValue("DirectoryTextSpacing");

            setColorRGBPanel();

            ShowDialog();
        }

        public void setColorRGBPanel()
        {
            string colorRGBList = MainWindow.config.GetKeyValue("ColorSettingList");
            string[] splitRGBList = colorRGBList.Split(',');

            foreach (string colorKey in splitRGBList)
            {
                string trimmedKey = colorKey.Trim();
                string colorValue = MainWindow.config.GetKeyValue(trimmedKey);
                string labelKey = trimmedKey + "Setting";

                // On essaie d’obtenir un libellé personnalisé, sinon on affiche le nom de la clé
                string displayLabel = MainWindow.config.GetKeyValue(labelKey);
                if (string.IsNullOrWhiteSpace(displayLabel))
                {
                    displayLabel = trimmedKey;
                }

                RGBStackPanel.Children.Add(new RGBPanel(colorValue, trimmedKey, displayLabel));
            }
        }
        public void CloseSystemSettings(object _sender, RoutedEventArgs _e)
        {
            Close();
        }

        private void SetTreeViewMargin(ItemCollection treeView)
        {
            if (treeView == null)
            {
                return;
            }
            foreach (TreeViewItem item in treeView)
            {
                // Do not add margin to the tree view's root node
                if (item != (TreeViewItem) main.treeView.Items.GetItemAt(0))
                {
                    item.Margin = new Thickness(0, Convert.ToDouble(MainWindow.config.GetKeyValue("DirectoryTextSpacing"), CultureInfo.InvariantCulture), 0, 0);
                }
                SetTreeViewMargin(item.Items);
            }
        }

        public void SaveSettings(object sender, RoutedEventArgs _e)
        {
            if (sender == SaveOptionalWindows)
            {
                bool optionalWindow1Update = UpdateConfig("OpenFolderView1", OptionalWindow1);
                bool optionalWindow2Update = UpdateConfig("OpenFolderView2", OptionalWindow2);

                if (optionalWindow1Update == true || optionalWindow2Update == true)
                {
                    main.InitializeLeftViews();
                }
                return;
            }
            if (sender == SaveTreeView)
            {
                bool topMarginUpdate = string.IsNullOrEmpty(TreeViewTopMargin.Text) == false && UpdateConfig("DirectoryTopMargin", TreeViewTopMargin);
                bool leftMarginUpdate = string.IsNullOrEmpty(TreeViewLeftMargin.Text) == false && UpdateConfig("DirectoryLeftMargin", TreeViewLeftMargin);
                bool bottomMarginUpdate = string.IsNullOrEmpty(TreeViewBottomMargin.Text) == false && UpdateConfig("DirectoryBottomMargin", TreeViewBottomMargin);

                if (UpdateConfig("ExpandDirectory", OpenTreeView) == true)
                {
                    main.isOpenTree = Convert.ToBoolean(OpenTreeView.IsChecked);
                    main.ExpandOrCollapseTreeView();
                }
                if ((string.IsNullOrEmpty(TreeViewSpacing.Text) == false && UpdateConfig("DirectoryTextSpacing", TreeViewSpacing)) == true)
                {
                    SetTreeViewMargin(main.treeView.Items);
                }
                if (topMarginUpdate == true || leftMarginUpdate == true || bottomMarginUpdate == true)
                {
                    main.treeView.Margin = new Thickness(
                        0,
                        0,
                        0,
                        Convert.ToDouble(MainWindow.config.GetKeyValue("DirectoryBottomMargin")));
                    main.TreePadding.Padding = new Thickness(
                        Convert.ToDouble(MainWindow.config.GetKeyValue("DirectoryLeftMargin")),
                        Convert.ToDouble(MainWindow.config.GetKeyValue("DirectoryTopMargin")),
                        0,
                        0);
                }
                return;
            }
            bool updatedColor = false;
            if (sender == SaveColors)
            {
                for (int i = 0; i < RGBStackPanel.Children.Count; i++)
                {
                    var item = RGBStackPanel.Children[i];
                    if (item is RGBPanel)
                    {
                        RGBPanel stackItem = (RGBPanel)item;
                        string[] stackColor = stackItem.getColor();
                        if (int.Parse(stackColor[0]) > 255 || int.Parse(stackColor[1]) > 255 || int.Parse(stackColor[2]) > 255)
                        {
                            continue;
                        }
                        bool update = UpdateConfig(stackItem.panelName.Trim(), $"{stackColor[0]},{stackColor[1]},{stackColor[2]}");
                        if (update) { updatedColor = true; }
                    }
                }
                if (updatedColor)
                {
                    main.SetBackgroundColors();
                }
                return;
            }
        }

        private void DisablePaste(object _sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void DisablePastePopUp(object _sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void NumberValidationTextBox(object _sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private bool UpdateConfig(string key, CheckBox cb)
        {
            if (Convert.ToBoolean(main.GetConfig().GetKeyValue(key)) != cb.IsChecked)
            {
                main.GetConfig().SetKeyValueKeepLineBreak(key, cb.IsChecked.ToString());
                return true;
            }

            return false;
        }

        private bool UpdateConfig(string key, TextBox tb)
        {
            if (main.GetConfig().GetKeyValue(key) != tb.Text)
            {
                main.GetConfig().SetKeyValueKeepLineBreak(key, tb.Text);
                return true;
            }

            return false;
        }

        private bool UpdateConfig(string key, string value)
        {
            if (main.GetConfig().GetKeyValue(key) != value)
            {
                main.GetConfig().SetKeyValueKeepLineBreak(key, value);
                return true;
            }

            return false;
        }

        private void GotFocusHandler(object sender, RoutedEventArgs _e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
    }

    public class RGBPanel : StackPanel
    {
        public string panelName;
        public RGBPanel(string color, string keyName, string labelName)
        {
            this.VerticalAlignment = VerticalAlignment.Center;
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.Orientation = Orientation.Horizontal;
            this.Height = 50;
            this.panelName = keyName;

            string[] colors = color.Replace("(", "").Replace(")", "").Trim().Split(',');

            this.Children.Add(new RGBTextBlockName(labelName));
            this.Children.Add(new RGBTextBlock(Brushes.Red, new Thickness(35, 0, 0, 0)));
            this.Children.Add(new RGBTextBox(colors[0], $"{keyName}R"));
            this.Children.Add(new RGBTextBlock(Brushes.Green, new Thickness(10, 0, 0, 0)));
            this.Children.Add(new RGBTextBox(colors[1], $"{keyName}G"));
            this.Children.Add(new RGBTextBlock(Brushes.Blue, new Thickness(10, 0, 0, 0)));
            this.Children.Add(new RGBTextBox(colors[2], $"{keyName}B"));
        }

        public string[] getColor()
        {
            string name = this.Name;
            string[] res = new string[3];
            int count = 0;

            for (int i = 0; i < this.Children.Count; i++)
            {
                var item = this.Children[i];
                if (item is RGBTextBox)
                {
                    res[count++] = ((RGBTextBox)item).Text;
                }
            }
            return res;

        }
    }

    public class RGBTextBlock : TextBlock
    {
        public RGBTextBlock(Brush color, Thickness margin)
        {
            this.Background = color;
            this.Text = "";
            this.VerticalAlignment = VerticalAlignment.Bottom;
            this.HorizontalAlignment = HorizontalAlignment.Right;
            this.Margin = margin;
            this.Width = 3;
        }
    }

    public class RGBTextBox : TextBox
    {

        public string textName;
        public RGBTextBox(string value, string name)
        {
            this.MouseRightButtonUp += DisablePastePopUp;
            this.GotFocus += GotFocusHandler;
            this.BorderThickness = new Thickness(0, 0, 0, 1);
            this.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 198, 219, 219));
            this.Width = 40;
            this.Margin = new Thickness(5, 0, 5, 0);
            this.VerticalAlignment = VerticalAlignment.Bottom;
            this.TextAlignment = TextAlignment.Center;
            this.PreviewTextInput += NumberValidationTextBox;
            this.Text = value;
            this.textName = name;
        }
        private void DisablePastePopUp(object _sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void GotFocusHandler(object sender, RoutedEventArgs _e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        private void NumberValidationTextBox(object _sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }
    }

    public class RGBTextBlockName : TextBlock
    {
       public RGBTextBlockName(string name)
       {
            this.Text = name;
            this.Padding = new Thickness(10, 0, 0, 0);
            this.FontSize = 12;
            this.VerticalAlignment = VerticalAlignment.Bottom;
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.Width = 150;
            this.MaxWidth = 150;
       }
    }
}