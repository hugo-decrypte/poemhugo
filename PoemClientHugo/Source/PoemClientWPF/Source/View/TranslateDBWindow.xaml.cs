using PoemClient.Source.Tools;
using PoemClient.Source.Tools.IA;
using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace PoemClient.View
{
    public partial class TranslateDBWindow : Window
    {
        private readonly Config config;
        private readonly MainWindow _main;
        private static TranslateDBWindow instance = null;
        private string ActualLang;
        public bool canceled = false;
        public TranslateDBWindow(PoemClientWPF.MainWindow main)
        {
            InitializeComponent();
            this.config = Config.GetInstance("Config\\poemclient.config");
            if (config == null)
                return;
            InitializeDataContext();
            this.Loaded += Dropdown;
            _main = main;
            _main.InitClientConfig();
            this.ResizeMode = ResizeMode.NoResize;
        }

        private void InitializeDataContext()
        {
            DataContext = new
            {
                Ok_btn = config.GetKeyValue("OK"),
                Cancel_btn = config.GetKeyValue("Cancel"),
                From = config.GetKeyValue("From"),
                To = config.GetKeyValue("To"),
                Text_File = config.GetKeyValue("File"),
                Languages = GetLangDBList(),
            };
        }

        private void Dropdown(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.TextBoxSQLContents))
                {
                    string[] TextBoxSQLContents = Properties.Settings.Default.TextBoxSQLContents.Split(';');

                    if (TextBoxSQLContents.Length >= 4)
                    {
                        path_from.Text = TextBoxSQLContents[0];
                        path_to.Text = TextBoxSQLContents[1];
                        if (TextBoxSQLContents[2] != null && TextBoxSQLContents[2] != "")
                        {
                            FromList.SelectedIndex = int.Parse(TextBoxSQLContents[2]);
                        }

                        if (TextBoxSQLContents[3] != null && TextBoxSQLContents[3] != "")
                        {
                            ToList.SelectedIndex = int.Parse(TextBoxSQLContents[3]);
                        }
                    }
                }
                ActualLang = GetActualDBLang();
                FromList.SelectedItem = ActualLang;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string GetActualDBLang()
        {
            string ActualLanguage = config.GetKeyValue("DatabaseList").Split(',')[0];
            if (ActualLanguage == null)
            {
                return null;
            }
            return ActualLanguage;
        }

        private string[] GetLangDBList()
        {
            string[] langlist = config.GetKeyValue("DatabaseList").Split(',');
            if (langlist == null)
            {
                return null;
            }
            return langlist;
        }
        public static TranslateDBWindow GetInstance(PoemClientWPF.MainWindow main)
        {
            if (instance == null)
            {
                instance = new TranslateDBWindow(main);
            }
            return instance;
        }
        private void TranslateDBClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            instance = null;
            return;
        }

        private void Cancel_button(object sender, RoutedEventArgs e)
        {
            instance = null;
            this.Close();
            canceled = true;
            return;
        }

        private void updateText(string text)
        {
            Task.Run(() =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    this.Loading.Text = text;
                });
            });
        }

        private async void Ok_button(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
                return;
            string selectedFromLanguage = FromList.SelectedItem as string;
            string selectedToLanguage = ToList.SelectedItem as string;

            updateText(config.GetKeyValue("Running"));  // JZ 0610

            if (!string.IsNullOrEmpty(selectedFromLanguage) || !string.IsNullOrEmpty(selectedToLanguage))
            {
                if (String.Compare(selectedToLanguage, selectedFromLanguage) == 0 || String.Compare(selectedToLanguage, ActualLang) == 0)
                {
                    MessageBox.Show(config.GetKeyValue("LanguagesError"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    updateText(config.GetKeyValue("Failure"));
                    return;
                }
            }
            
            if (string.IsNullOrEmpty(path_from.Text))
            {
                MessageBox.Show("'" + config.GetKeyValue("From") + "'" + config.GetKeyValue("FileNotFound"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                updateText(config.GetKeyValue("Failure"));
                return;
            }
            else if (string.IsNullOrEmpty(path_to.Text))
            {
                MessageBox.Show("'" + config.GetKeyValue("To") + "'" + config.GetKeyValue("FileNotFound"), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                updateText(config.GetKeyValue("Failure"));
                return;
            }

            if (!Utils.IsInternetAvailable())
            {
                MessageBox.Show(config.GetKeyValue("NetworkError"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                updateText(config.GetKeyValue("Failure"));
                return;
            }

            config.SetKeyValueKeepLineBreak("DatabaseList", GetNewList(selectedToLanguage, FromList));
            updateText(config.GetKeyValue("Running"));


            string urlAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTUrl");
            string modelAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTModel");
            string keyAPI = MainWindow.GetLineBelowParameter(_main.clientConfig, "ChatGPTKey");
            ModeleIA gpt1Client = new ChatGPTClient(urlAPI, modelAPI, keyAPI);

            string sourceFilePath = path_from.Text;
            string targetFilePath = path_to.Text;
            string targetLanguage = selectedToLanguage;
            TypeRequete translationService = new TraductionService(sourceFilePath, gpt1Client, targetLanguage,this.config);
            string translatedContent = await translationService.executerRequete();
            try
            {
                if (translatedContent.StartsWith("Erreur"))
                {
                    MessageBox.Show(translatedContent.Substring("Erreur".Length).TrimStart(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    updateText(config.GetKeyValue("Failure"));
                }
                else
                {
                    File.WriteAllText(targetFilePath, translatedContent);
                    Console.WriteLine("Le fichier traduit a été créé avec succès : " + targetFilePath);
                    updateText(config.GetKeyValue("Success"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la création du fichier traduit : " + ex.Message);
                updateText(config.GetKeyValue("Failure"));
            }
        }

        private void ActualLang_changed(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox comboBox = (System.Windows.Controls.ComboBox)sender;
            object selectedItem = comboBox.SelectedItem;

            Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
            Properties.Settings.Default.Save();

            if (selectedItem != null)
            {
                ActualLang = selectedItem.ToString();
            }
        }

        private string GetNewList(string first_lang, System.Windows.Controls.ComboBox list)
        {
            string new_list = first_lang;
            int count = list.Items.Count;

            for (int i = 0; i < count; i++)
            {
                if (list.Items[i].ToString() != first_lang)
                {
                    new_list += ",";
                    new_list += list.Items[i].ToString();
                }
            }
            return new_list;
        }

        private void OnClickSelectFileFrom(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {

                DialogResult result = dialog.ShowDialog();
                dialog.Multiselect = false;
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    path_from.Text = dialog.FileName;
                    Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void OnClickSelectFileTo(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {

                DialogResult result = dialog.ShowDialog();
                dialog.Multiselect = false;
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    path_to.Text = dialog.FileName;
                    Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
                    Properties.Settings.Default.Save();
                }
            }
        }
        private void path_from_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
            Properties.Settings.Default.Save();
        }

        private void path_to_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
            Properties.Settings.Default.Save();
        }

        private void ToList_TextChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.TextBoxSQLContents = string.Join(";", new[] { path_from.Text, path_to.Text, FromList.SelectedIndex.ToString(), ToList.SelectedIndex.ToString() });
            Properties.Settings.Default.Save();
        }
    }
}
