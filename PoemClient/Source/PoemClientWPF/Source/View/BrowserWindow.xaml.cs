using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;

namespace PoemClientWPF
{
    public partial class BrowserWindow : Window
    {
        private string _pendingUrl = null;
        private string _pendingHtml = null;
        private bool _isInitialized = false;

        public BrowserWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                string userDataFolder = Path.Combine(Path.GetTempPath(), "BrowserWindow_" + Guid.NewGuid().ToString());
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                _isInitialized = true;

                // Joue le contenu en attente si Navigate/NavigateToString a été appelé avant l'init
                if (_pendingUrl != null)
                {
                    webView.Source = new Uri(_pendingUrl);
                    _pendingUrl = null;
                }
                else if (_pendingHtml != null)
                {
                    webView.NavigateToString(_pendingHtml);
                    _pendingHtml = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation WebView2 :\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Navigate(string url)
        {
            if (_isInitialized)
                webView.Source = new Uri(url);
            else
                _pendingUrl = url;
        }

        public void NavigateToString(string html)
        {
            if (_isInitialized)
                webView.NavigateToString(html);
            else
                _pendingHtml = html;
        }
    }
}