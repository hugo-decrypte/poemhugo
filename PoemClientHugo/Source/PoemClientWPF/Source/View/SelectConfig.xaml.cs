using Microsoft.Win32;
using PoemClient.Tools;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PoemClient.View
{
    public partial class SelectConfig : Window
    {
        private readonly IniFile iniFile;
        private readonly bool locked;
        private readonly List<string> configFiles = new List<string>();

        public SelectConfig(IniFile ini)
        {
            InitializeComponent();
            this.iniFile = ini;
            this.locked = iniFile.GetConfigValue("SAVE") == "" ? true : false;
            chk_ask.IsChecked = false;
            InitializeDataContext();
            InitializeListConfigs();
        }

        private void InitializeDataContext()
        {
            DataContext = new
            {
                btn_Ok = iniFile.GetConfigValue("TAG_OK"),
                btn_Cancel = iniFile.GetConfigValue("TAG_CANCEL"),
                Check_Save = iniFile.GetConfigValue("TAG_SAVE"),
                Title = iniFile.GetConfigValue("TAG_BOOT"),
            };
        }

        private void InitializeListConfigs()
        {
            bool findCur = false;
            if (listConfigs.Items.Count > 0)
                listConfigs.Items.Clear();

            foreach (KeyValuePair<string, string> entry in iniFile.GetConfigs())
            {
                if (entry.Key.ToUpper() != "SAVE")
                {
                    if (File.Exists(entry.Value.Contains("=") ? entry.Value.Split('=')[1] : entry.Value) && (entry.Key == "Current" || entry.Key.StartsWith("config")))
                    {
                        listConfigs.Items.Add(entry.Value.Contains("=") ? entry.Value.Split('=')[0] : entry.Value);
                        configFiles.Add(entry.Value.Contains("=") ? entry.Value.Split('=')[1] : entry.Value);
                        if (File.Exists(iniFile.GetCurrentConfig()) && (entry.Value.Contains("=") ? entry.Value.Split('=')[1].ToString() : entry.Value) == iniFile.GetCurrentConfig())
                        {
                            listConfigs.SelectedItem = entry.Value.Contains("=") ? entry.Value.Split('=')[1].ToString() : entry.Value;
                            findCur = true;
                        }
                    }
                }
            }
            if (!findCur)
            {
                if (listConfigs.Items.Count == 0)
                {
                    MessageBox.Show("No configuration file given", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    Process.GetCurrentProcess().Kill();
                    return;
                }
                listConfigs.SelectedItem = listConfigs.Items.GetItemAt(0).ToString().Contains("=") ? listConfigs.Items.GetItemAt(0).ToString().Split('=')[1].ToString() : listConfigs.Items.GetItemAt(0);
                return;
            }
        }

        //------------------------------- EVENTS ------------------------------

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            iniFile.SetCurrent(configFiles[listConfigs.SelectedIndex]);
            iniFile.UpdateCurrent();
            if (locked == chk_ask.IsChecked)
            {
                iniFile.UpdateValue("Save", configFiles[listConfigs.SelectedIndex]);
            }
            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers de configuration (*.config)|*.config|Fichiers de configuration (*.cfg)|*.cfg|Tous les fichiers (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                iniFile.AddLine(selectedFilePath);
                iniFile.SetCurrent(selectedFilePath);
                InitializeListConfigs();
            }
        }
    }
}
