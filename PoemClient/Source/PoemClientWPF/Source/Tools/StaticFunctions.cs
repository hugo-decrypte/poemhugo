using PoemClient.Source.View;
using PoemClient.View;
using PoemClientWPF;
using PoemClientWPF.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PoemClient.Source.Tools
{
    /// <summary>
    /// Classe utilisée pour exécuter des fonction sur le main
    /// sans connaitre l'objet main
    /// </summary>
    internal static class StaticFunctions
    {
        public enum FUNCTIONS
        {
            ZoomIn,
            ZoomOut,
            FullScreen,
            BestFit,
            NaturalScale,
            Settings,
            ReloadView,
            About,
            ReloadSystem,
            MouseMove,
            OpenFile,
            Upload,
            Download,
            ExcelImport,
            ExcelExport,
            GoogleSite,
            GoogleTour,
            OpenGoogleTour,
            GoogleCoordinates,
            GoogleDistanceMatrix,
            HERESite,
            HERETour,
            OpenHERETour,
            HERECoordinates,
            HEREDistanceMatrix,
            Translation,
            DocumentInput,
            SupplyPrediction,
            DemandPrediction,
            AIAgent
        }

        private static MainWindow main = MainWindow.main;

        private static bool isFullScreen = false;

        private static WindowState previousWindowState;
        private static ResizeMode previousResizeMode;

        private static GoogleMaps googleMaps;
        private static GooglePaths googlePaths;
        private static GoogleMatrix googleMatrix;
        private static bool calculatingCoordinates = false;

        private static HerePaths herePaths = null;
        private static HereMatrix hereMatrix = null;

        private static TranslateWindow translateWindow;

        private static DocumentInputWindow docInputWindow;

        private static SupplyPredictWindow supplyPredictWindow;
        private static DemandPredictWindow demandPredictWindow;

        private static PromptWindow promptWindow;

        public static List<string> GetFunctionsString()
        {
            List<string> res = new List<string>();

            // Transforme l'enum en liste de string
            res = Enum.GetNames(typeof(FUNCTIONS)).ToList();
            return res;
        }

        public static async Task RunAction(string[] args)
        {
            Console.WriteLine(args[0]);
            string function = args[0].Replace(" ", "");
            Console.WriteLine("[ARG] " + function);
            main.log.WriteTrace($"{function} action");
            bool cancelSemaphore = false;
            switch (function)
            {
                case nameof(FUNCTIONS.ZoomIn):
                    main.FuncZoom(1);
                    break;
                case nameof(FUNCTIONS.ZoomOut):
                    main.FuncZoom(-1);
                    break;
                case nameof(FUNCTIONS.FullScreen):
                    OnFullScreen();
                    break;
                case nameof(FUNCTIONS.BestFit):
                    ApplyScaling(true);
                    break;
                case nameof(FUNCTIONS.NaturalScale):
                    ApplyScaling(false);
                    break;
                case nameof(FUNCTIONS.Settings):
                    new SystemSettings(main);
                    break;
                case nameof(FUNCTIONS.ReloadView):
                    // JZ 20260528 main.ReloadView();
                    main.ReloadAllViews();
                    break;
                case nameof(FUNCTIONS.About):
                    new About(main);
                    break;
                case nameof(FUNCTIONS.ReloadSystem):
                    await main.ReloadSystem();
                    break;
                case nameof(FUNCTIONS.MouseMove):
                    main.SetMouseMode();
                    break;
                case nameof(FUNCTIONS.OpenFile):
                    if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
                    {
                        OpenFile(args[1]);
                    }
                    else
                    {
                        OpenFile("");
                    }
                    break;

                case nameof(FUNCTIONS.Upload):
                    cancelSemaphore = Upload(args);
                    if (cancelSemaphore) main.log.WriteTrace($"Upload canceled");
                    break;
                case nameof(FUNCTIONS.Download):
                    cancelSemaphore = Download(args);
                    if (cancelSemaphore) main.log.WriteTrace($"Download canceled");
                    break;

                case nameof(FUNCTIONS.ExcelImport):
                    cancelSemaphore = await LoadExcelImport(args);
                    if (cancelSemaphore) main.log.WriteTrace($"Excel Import canceled");
                    break;
                case nameof(FUNCTIONS.ExcelExport):
                    cancelSemaphore = await LoadExcelExport(args);
                    if (cancelSemaphore) main.log.WriteTrace($"Excel Export canceled");
                    break;

                case nameof(FUNCTIONS.GoogleSite):
                    main.isMousemoveSelected = false;
                    OpenGoogleMaps(true);
                    break;

                case nameof(FUNCTIONS.GoogleTour):
                    main.isMousemoveSelected = false;
                    main.InitializeBrowser();
                    if (googlePaths == null)
                    {
                        googlePaths = new GooglePaths();
                    }
                    googlePaths.Show();
                    break;

                case nameof(FUNCTIONS.OpenGoogleTour):
                    main.isMousemoveSelected = false;
                    if (googlePaths == null)
                    {
                        googlePaths = new GooglePaths(true);
                    }
                    googlePaths.Show();
                    break;

                case nameof(FUNCTIONS.GoogleCoordinates):
                    main.isMousemoveSelected = false;
                    if (calculatingCoordinates) break;
                    if (googleMaps == null)
                        googleMaps = GoogleMaps.GetInstance(true);

                    calculatingCoordinates = true;
                    await googleMaps.CalculateCoordinates();
                    calculatingCoordinates = false;
                    break;
                case nameof(FUNCTIONS.GoogleDistanceMatrix):
                    main.isMousemoveSelected = false;
                    if (googleMatrix == null)
                    {
                        googleMatrix = new GoogleMatrix(main);
                    }
                    googleMatrix.Show();
                    break;

                case nameof(FUNCTIONS.HERESite):
                    main.isMousemoveSelected = false;
                    main.InitializeBrowser();
                    OpenGoogleMaps(false);
                    break;

                case nameof(FUNCTIONS.HERETour):
                    main.isMousemoveSelected = false;
                    main.InitializeBrowser();
                    if (herePaths == null)
                    {
                        herePaths = new HerePaths(main);
                    }
                    herePaths.Show();
                    break;

                case nameof(FUNCTIONS.OpenHERETour):
                    main.isMousemoveSelected = false;
                    if (herePaths == null)
                    {
                        herePaths = new HerePaths(main, true);
                    }
                    herePaths.Show();
                    break;

                case nameof(FUNCTIONS.HERECoordinates):
                    main.isMousemoveSelected = false;
                    if (calculatingCoordinates) break;
                    if (googleMaps == null)
                        googleMaps = GoogleMaps.GetInstance(false);

                    calculatingCoordinates = true;
                    await googleMaps.CalculateCoordinates();
                    calculatingCoordinates = false;
                    break;

                case nameof(FUNCTIONS.HEREDistanceMatrix):
                    main.isMousemoveSelected = false;
                    if (hereMatrix == null)
                    {
                        hereMatrix = new HereMatrix(main);
                    }
                    hereMatrix.Show();
                    break;


                case nameof(FUNCTIONS.Translation):
                    main.canDownload = false;
                    cancelSemaphore = Translation();
                    if (cancelSemaphore) main.log.WriteTrace($"Translation canceled");
                    main.canDownload = false;
                    while (!main.canDownload)
                    {
                        await Task.Delay(100);
                    }
                    if (main.translateDownload != null)
                    {
                        if (!main.skipDownload) cancelSemaphore = Download(main.translateDownload);
                        main.skipDownload = true;
                        if (cancelSemaphore) main.log.WriteTrace($"Download canceled");
                    }
                    break;

                case nameof(FUNCTIONS.DocumentInput):
                    main.canDownload = false;
                    cancelSemaphore = DocInput(args);
                    if (cancelSemaphore) main.log.WriteTrace($"Translation canceled");
                    main.canDownload = false;
                    while (!main.canDownload)
                    {
                        await Task.Delay(100);
                    }
                    if (main.translateDownload != null)
                    {
                        if (!main.skipDownload) cancelSemaphore = Download(main.translateDownload);
                        main.skipDownload = true;
                        if (cancelSemaphore) main.log.WriteTrace($"Download canceled");
                    }
                    break;

                case nameof(FUNCTIONS.SupplyPrediction):           
                    cancelSemaphore = SupplyPrediction();
                    if (cancelSemaphore) main.log.WriteTrace($"Supply Prediction canceled");
                    break;

                case nameof(FUNCTIONS.DemandPrediction):
                    cancelSemaphore = DemandPrediction();
                    if (cancelSemaphore) main.log.WriteTrace($"Demand Prediction canceled");
                    break;

                case nameof(FUNCTIONS.AIAgent):
                    cancelSemaphore = OpenAgent();
                    if (cancelSemaphore) main.log.WriteTrace("Agent canceled");
                    break;

            }
        } 
        public static void OnFullScreen()
        {
            if (!isFullScreen)
            {
                previousWindowState = main.WindowState;
                previousResizeMode = main.ResizeMode;
                main.ShowTitleBar = false;
                main.TitleBarHeight = 0;

                main.WindowState = WindowState.Normal;
                main.ResizeMode = ResizeMode.NoResize;

                main.Top = 0;
                main.Left = 0;
                main.Width = SystemParameters.PrimaryScreenWidth;
                main.Height = SystemParameters.PrimaryScreenHeight;
                main.MaxWidth = SystemParameters.PrimaryScreenWidth;
                main.MaxHeight = SystemParameters.PrimaryScreenHeight;

                main.Topmost = true;
            }
            else
            {
                main.TitleBarHeight = 25;
                main.ShowTitleBar = true;
                main.WindowState = previousWindowState;
                main.ResizeMode = previousResizeMode;
                main.Top = 0;
                main.Left = 0;
                main.Width = SystemParameters.WorkArea.Width;
                main.Height = SystemParameters.WorkArea.Height;
                main.MaxWidth = SystemParameters.WorkArea.Width + 50;
                main.MaxHeight = SystemParameters.WorkArea.Height + 50;
                main.Topmost = false;
            }

            isFullScreen = !isFullScreen;

            main.rightView1?.SetViewBestFit();
            main.rightView2?.SetViewBestFit();
            main.rightView3?.SetViewBestFit();
            main.rightView4?.SetViewBestFit();
        }

        public static void ApplyScaling(bool bestFitMode)
        {
            if (!main.rightView2.IsVisible)
            {
                if (bestFitMode)
                    main.rightView1.ViewNaturalBestFit(1, 0);
                else
                    main.rightView1.ViewNaturalBestFit(0, 1);
            }
            if (main.rightView1.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    main.rightView1.ViewNaturalBestFit(1, 0);
                else
                    main.rightView1.ViewNaturalBestFit(0, 1);
            }
            if (main.rightView2.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    main.rightView2.ViewNaturalBestFit(1, 0);
                else
                    main.rightView2.ViewNaturalBestFit(0, 1);
            }
            if (main.rightView3.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    main.rightView3.ViewNaturalBestFit(1, 0);
                else
                    main.rightView3.ViewNaturalBestFit(0, 1);
            }
            if (main.rightView4.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    main.rightView4.ViewNaturalBestFit(1, 0);
                else
                    main.rightView4.ViewNaturalBestFit(0, 1);
            }
        }

        private static void OpenFile(string file)
        {
            string directory = MainWindow.config.GetIniValue("DIRECTORY").Replace('/', '\\');
            file = file.Replace('/', '\\');

            string path = Path.Combine(directory, file);

            if (!string.IsNullOrEmpty(file) && File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                string folder = Path.GetDirectoryName(path);
                Process.Start("explorer.exe", Directory.Exists(folder) ? folder : Environment.CurrentDirectory);
            }
        }

        /// <summary>
        /// Creates a new Upload instance (upload a file to the server)
        /// </summary>
        /// <param name="argv">Arguments parsed from PoemClient's configuration file</param>
        public static bool Upload(string[] argv)
        {
            Upload upload = new Upload(main.login.GetUploadDirectory(), main.login.GetHttpServer(), main.login.GetServerPort());

            try
            {
                if (upload.SetFile(argv[1]) == true)
                {
                    upload.disableSuccessMessage = true;
                    upload.StartUpload(null, null);
                }
                else
                {
                    throw new Exception("Invalid configuration arguments");
                }
            }
            catch
            {
                upload.Show();
            }

            return upload.canceled;
        }

        /// <summary>
        /// Creates a new Download instance (download a file from the server)
        /// </summary>
        /// <param name="argv">Arguments parsed from PoemClient's configuration file</param>
        public static bool Download(string[] argv)
        {
            Download download = new Download(main.login.GetDownloadDirectory(), main.login.GetHttpServer(), main.login.GetServerPort());

            try
            {
                bool[] setters = {
                    download.SetFileName(argv[1]),
                    download.SetFolder(argv[2]),
                };

                if (setters.All(isFileSet => isFileSet.Equals(true)) == true)
                {
                    //download.disableSuccessMessage = true;
                    download.StartDownload(null, null);
                }
                else
                {
                    throw new Exception("Invalid configuration arguments");
                }
            }
            catch
            {
                download.Show();
            }

            //if (! download.canceled)
            //{
            //    MessageBox.Show(config.GetKeyValue("Success"), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            //}

            return download.canceled;
        }

        /// <summary>
        /// Creates a new ExcelImport instance (convert an Excel file to a text - CSV style - file)
        /// </summary>
        /// <param name="argv">Arguments parsed from PoemClient's configuration file</param>
        private static async Task<bool> LoadExcelImport(string[] argv)
        {
            ExcelImport excelImport = new ExcelImport();

            try
            {
                bool[] setters = {
                    excelImport.SetFile(excelImport.Input, argv[1]),
                    excelImport.SetFile(excelImport.Output, argv[2], false)
                };

                if (setters.All(isFileSet => isFileSet.Equals(true)) == true)
                {
                    await excelImport.StartImport();
                }
                else
                {
                    throw new Exception("Invalid configuration argument");
                }
            }
            catch
            {
                excelImport.Show();
            }

            return excelImport.canceled;
        }

        /// <summary>
        /// Creates a new ExcelExport instance (convert a text - CSV style - file to an Excel file)
        /// </summary>
        /// <param name="argv">Arguments parsed from PoemClient's configuration file</param>
        private static async Task<bool> LoadExcelExport(string[] argv)
        {
            ExcelExport excelExport = new ExcelExport();

            try
            {
                bool[] setters = {
                    excelExport.SetFile(excelExport.Input, argv[1]),
                    excelExport.SetFile(excelExport.Output, argv[2], false)
                };

                if (setters.All(isFileSet => isFileSet.Equals(true)) == true)
                {
                    await excelExport.StartImport();
                }
                else
                {
                    throw new Exception("Invalid configuration arguments");
                }
            }
            catch
            {
                excelExport.Show();
            }

            return excelExport.canceled;
        }

        //clic sur bouton Google maps de la toolBar, ouvre la fenêtre Google Maps
        public static void OpenGoogleMaps(bool useGoogle)
        {
            try
            {
                //login.SetDllsMoved(true);
                if (googleMaps == null || googleMaps.closed)
                    googleMaps = GoogleMaps.GetInstance(useGoogle);
                main.InitializeBrowser();
                if (!useGoogle)
                {
                    googleMaps.Title = MainWindow.config.GetKeyValue("HereMapsSites");
                    // On check les coordonnées par défaut
                    googleMaps.radio_addr_iti1.IsChecked = false;
                    googleMaps.radio_coord_iti1.IsChecked = true;
                    googleMaps.radio_addr_iti2.IsChecked = false;
                    googleMaps.radio_coord_iti2.IsChecked = true;
                }
                googleMaps.Show();
                main.log.WriteTrace("Google Maps opened");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool Translation()
        {
            translateWindow = TranslateWindow.GetInstance();
            translateWindow.Show();
            return translateWindow.canceled;
        }

        private static bool DocInput(string[] args)
        {
            docInputWindow = DocumentInputWindow.GetInstance(args);
            docInputWindow.Show();
            return docInputWindow.canceled;
        }

        private static bool SupplyPrediction()
        {
            // On instancie ou on récupère l'instance unique de la fenêtre
            supplyPredictWindow = SupplyPredictWindow.GetInstance();

            // On affiche la fenêtre
            supplyPredictWindow.Show();

            return supplyPredictWindow.canceled;
        }
        private static bool DemandPrediction() { 
            // On instancie ou on récupère l'instance unique de la fenêtre
            demandPredictWindow = DemandPredictWindow.GetInstance();

            // On affiche la fenêtre
            demandPredictWindow.Show();
            return demandPredictWindow.canceled;
        }
        private static bool OpenAgent()
        {
            promptWindow = PromptWindow.getInstance();
            // this.promptWindow.Owner = this;
            promptWindow.Show();
            return promptWindow.canceled;
        }
    }
}
