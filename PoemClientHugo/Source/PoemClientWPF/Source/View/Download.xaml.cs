using AxCOMPOEMGUICTRLLib;
using PoemClientWPF;
using PoemClientWPF.Tools;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms.Integration;

namespace PoemClient.Source.View
{
    public partial class Download : Window
    {
        private readonly WindowsFormsHost host = new WindowsFormsHost();
        private readonly AxComPoemGuiCtrl gui = new AxComPoemGuiCtrl();
        private readonly Config config;

        private readonly string path = null;

        private readonly string directory = null;
        private readonly string server = null;
        private readonly string port = null;

        private const string TIMEOUT = "3600";

        private static readonly string[] savedPaths = { null, null };

        public bool disableSuccessMessage = false;
        public bool canceled = false;

        /// <summary>
        /// Class constructor
        /// </summary>
        public Download(string downloadDirectory, string serverUri, string serverPort)
        {
            if ((config = MainWindow.config) == null)
            {
                return;
            }

            {
                host.Child = gui;
                ((System.ComponentModel.ISupportInitialize)(gui)).BeginInit();
                ((System.ComponentModel.ISupportInitialize)(gui)).EndInit();
                Content = host;
            }

            InitializeComponent();

            this.Owner = MainWindow.main;
            DataContext = new
            {
                windowTitle = config.GetKeyValue("Download"),               
                windowFileLabel = config.GetKeyValue("File"),
                windowLocalDirectoryLabel = config.GetKeyValue("DownloadedTo"),
            };

            directory = downloadDirectory;
            server = serverUri;
            port = serverPort;

            path = $"{config.GetIniValue("DIRECTORY")}".Replace('/', '\\');
            path += path.EndsWith("\\") ? "" : "\\";

            SetFolder(savedPaths[0] ?? config.GetKeyValue("DownloadClientDirectory"));
            SetFileName(savedPaths[1] ?? config.GetKeyValue("DownloadFile"));
        }

        /// <summary>
        /// Opens a new BrowseFolder to select a folder
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        private void BrowseFolder(object _sender, RoutedEventArgs _e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                SelectedPath = (Path.GetDirectoryName(savedPaths[0]) ?? (Directory.Exists(path) == false ? Environment.CurrentDirectory : path)),
            };

            folderDialog.ShowDialog();

            if (string.IsNullOrEmpty(folderDialog.SelectedPath) == false)
            {
                SetFolder(folderDialog.SelectedPath);
            }
        }

        /// <summary>
        /// Sets button content and save file path internally
        /// </summary>
        /// <param name="folder">Folder's path</param>
        /// <param name="configKey">Config key to check relative path from</param>
        /// <returns>true if folder exists, false otherwise</returns>
        public bool SetFolder(string folder, string configKey = "DIRECTORY")
        {
            if (string.IsNullOrEmpty(folder) == true)
            {
                return savedPaths[0] != null;
            }

            if (Directory.Exists(Path.GetPathRoot(folder)) == false)
            {
                folder = config.GetIniValue(configKey) + "/" + folder;
            }

            if (Directory.Exists(Path.GetPathRoot(folder)) == false)
            {
                folder = AppDomain.CurrentDomain.BaseDirectory + folder;
            }
            
            try
            {
                if (Directory.Exists(folder) == false)
                {
                    throw new DirectoryNotFoundException();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show((exception.Message + $" ({folder})"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            localDirectory.Content = (savedPaths[0] = folder);
            return true;
        }

        /// <summary>
        /// Sets file name
        /// </summary>
        /// <param name="file">Name of the file to download</param>
        /// <returns>true</returns>
        public bool SetFileName(string file)
        {
            filename.Text = (savedPaths[1] = file);
            return true;
        }

        /// <summary>
        /// Sets cursor position at the end of the buffer
        /// </summary>
        /// <param name="_sender"></param>
        /// <param name="_e"></param>
        private void SetFileNameOnFocus(object _sender, RoutedEventArgs _e)
        {
            filename.SelectionStart = filename.Text.Length;
        }

        /// <summary>
        /// Unsets saved paths
        /// </summary>
        public static void ResetSavedPaths()
        {
            for (int path = 0; path < savedPaths.Length; path++)
            {
                savedPaths[path] = null;
            }
        }

        /// <summary>
        /// Closes Download window
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void CancelDownload(object _sender, RoutedEventArgs _e)
        {
            canceled = true;
            Close();
        }

        /// <summary>
        /// Downloads a file from server, using AxComPoemGuiCtrl library
        /// </summary>
        /// <param name="_sender">Object which has raised the event</param>
        /// <param name="_e">Additional information about the event</param>
        public void StartDownload(object _sender, RoutedEventArgs _e)
        {
            if (localDirectory.Content == null)
            {
                MessageBox.Show(config.GetKeyValue("InputError"), DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);

                if (IsActive == false)
                {
                    ShowDialog();
                }

                return;
            }

            if (string.IsNullOrEmpty(filename.Text) == true)
            {
                if (IsActive == false)
                {
                    ShowDialog();
                }

                return;
            }

            savedPaths[0] = localDirectory.Content.ToString();
            savedPaths[1] = filename.Text;

            try
            {
                if (File.Exists($"{savedPaths[0]}/{savedPaths[1]}".Replace('\\', '/')) == true)
                {
                    File.Delete($"{savedPaths[0]}/${savedPaths[1]}".Replace('\\', '/'));
                    File.Move($"{savedPaths[0]}/{savedPaths[1]}".Replace('\\', '/'), $"{savedPaths[0]}/${savedPaths[1]}".Replace('\\', '/'));
                }

                string poemGuiConfig = $"[SERVER={server.Replace("http://", "")};PORT={port};UID=,PWD=;TIMEOUT={TIMEOUT}]";
                string error = new string('\0', 1000);

                switch (gui.DownloadFile($"/{directory}/{savedPaths[1]}".Replace('\\', '/'), $"{savedPaths[0]}/{savedPaths[1]}".Replace('\\', '/'), poemGuiConfig, ref error, error.Length))
                {
                    case 1:
                        if (File.Exists($"{savedPaths[0]}/{savedPaths[1]}".Replace('\\', '/')) == false)
                        {
                            throw new FileNotFoundException($"{new FileNotFoundException().Message} ({server}:{port}/{directory.TrimEnd('/')}/{savedPaths[1]})".Replace('\\', '/'));
                        }

                        Close();

                        if (disableSuccessMessage == false)
                        {
                            string msgDownloaded = config.GetKeyValue("DownloadedTo") + $"{savedPaths[0].Replace('\\', '/').TrimEnd('/')}/{savedPaths[1]}";    // JZ 0723
                            MessageBox.Show(msgDownloaded, DataContext.ToString().Split('=')[1].Split(',')[0], MessageBoxButton.OK, MessageBoxImage.Information);
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
