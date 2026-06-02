using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using PoemClientWPF.Tools;

namespace PoemClientWPF.View
{
    public partial class About : Window
    {
        private readonly Config config;

        public About(Window main)
        {   
            if ((this.config = Config.GetInstance("Config\\poemclient.config")) == null)
            {
                return;
            }

            InitializeComponent();

            double windowWidth = StringSizeScaller(this.Copyright, config.GetKeyValue("AboutCopyright")).Width;

            this.Title = config.GetKeyValue("About");
            this.Version.Text = config.GetKeyValue("AboutVersion");
            this.Warning.Text = config.GetKeyValue("AboutText");
            this.Copyright.Text = config.GetKeyValue("AboutCopyright");
            this.TitleText.Text = config.GetKeyValue("Title");

            this.Window.Width = windowWidth + this.CloseButton.Width + 100;
            this.Window.MinWidth = this.Window.Width;
            this.Copyright.Width = windowWidth;
            this.Owner = main;

            ShowDialog();
        }

        private Size StringSizeScaller(TextBlock textBlock, string value)
        {
            FormattedText text = new FormattedText(
                value,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip
            );

            return new Size(text.Width, text.Height);
        }

        private void CloseAbout(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void Click_URI(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }
    }
}
