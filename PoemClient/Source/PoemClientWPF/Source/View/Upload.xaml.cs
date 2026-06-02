using AxCOMPOEMGUICTRLLib;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms.Integration;

namespace PoemClient.Source.View
{
    public partial class Upload : Window
    {
        private readonly WindowsFormsHost host = new WindowsFormsHost();
        private readonly AxComPoemGuiCtrl gui = new AxComPoemGuiCtrl();
        private readonly Config configuration;

        private readonly string path = null;

        private readonly string directory = null;
        private readonly string server = null;
        private readonly string port = null;

        private const string TIMEOUT = "3600";

        private static string savedPath = null;

        public bool disableSuccessMessage = false;
        public bool canceled = false;

        /// <summary>
        /// Class constructor
        /// </summary>
        public Upload(string uploadDirectory, string serverUri, string serverPort)
        {
            {
                host.Child = gui;
                ((System.ComponentModel.ISupportInitialize)(gui)).BeginInit();
                ((System.ComponentModel.ISupportInitialize)(gui)).EndInit();
                Content = host;
            }

            configuration = MainWindow.config;

            if (configuration == null)
            {
                return;
            }

            DataContext = new
            {
                windowTitle = configuration.GetKeyValue("Upload"),
                windowSelectLabel = configuration.GetKeyValue("File"),
            };

            directory = uploadDirectory;
            server = serverUri;
            port = serverPort;

            path = $"{configuration.GetIniValue("DIRECTORY")}".Replace('/', '\\');
            path += path.EndsWith("\\") ? "" : "\\";

            InitializeComponent();
            this.Owner = MainWindow.main;
            SetFile(savedPath);
        }

        /// <summary>
        /// Opens a new FileDialog to browse a file
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        private void BrowseFile(object _sender, RoutedEventArgs _e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "All files|*.*",
                CheckFileExists = true,
                InitialDirectory = (Path.GetDirectoryName(savedPath) ?? (Directory.Exists(path) == false ? Environment.CurrentDirectory : path)),
            };

            if (fileDialog.ShowDialog() == true && string.IsNullOrEmpty(fileDialog.FileName) == false)
            {
                SetFile(savedPath = fileDialog.FileName);
            }
        }

        /// <summary>
        /// Sets button content and save file path internally
        /// </summary>
        /// <param name="file">File's path</param>
        /// <returns>true if file isn't exists and isn't too big, false otherwise</returns>
        public bool SetFile(string file)
        {
            if (string.IsNullOrEmpty(file) == true)
            {
                return savedPath != null;
            }

            try
            {
                if (File.Exists(file) == false)
                {
                    file = path + file;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show((exception.Message + $" ({file})"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            savedPath = file;

            if (new FileInfo(savedPath).Length > Convert.ToInt64(configuration.GetKeyValueMenu("MaxUploadByteSize")))
            {
                MessageBox.Show($"{configuration.GetKeyValue("UploadSizeError")} ({file})", DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            SelectedFile.Content = (savedPath);
            return true;
        }

        /// <summary>
  
        /// Unsets saved file path
        /// </summary>
        public static void ResetSavedPath()
        {
            savedPath = null;
        }

        /// <summary>
        /// Closes Upload window
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void CancelUpload(object _sender, RoutedEventArgs _e)
        {
            canceled = true;
            Close();
        }

        /// <summary>
        /// Uploads a file to server, using AxComPoemGuiCtrl library
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void StartUpload(object _sender, RoutedEventArgs _e)
        {
            this.Hide();
            if (savedPath == null)
            {
                MessageBox.Show(configuration.GetKeyValue("InputError"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (IsActive == false)
                {
                    ShowDialog();
                }
                return;
            }

            try
            {
                string filename = Path.GetFileName(savedPath);

                string poemGuiConfig = $"[SERVER={server.Replace("http://", "")};PORT={port};UID=,PWD=;OVERWRITE=1;TIMEOUT={TIMEOUT}]";
                string error = new string('\0', 1000);

                switch (gui.UploadFile(savedPath, $"{directory}/{filename}".Replace('\\', '/'), poemGuiConfig, ref error, error.Length))
                {
                    case 1:
                        Close();

                        if (disableSuccessMessage == false)
                        {
                            string msgUploaded = configuration.GetKeyValue("UploadedTo") + $"{directory.Replace('\\', '/').TrimEnd('/')}/{filename}".Replace('\\', '/');     // JZ 0723
                            MessageBox.Show(msgUploaded, DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        return;

                    default:
                        throw new Exception($"An error occured : {error}");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);

                if (IsActive == false)
                {
                    ShowDialog();
                }
                return;
            }
        }
    }
}
