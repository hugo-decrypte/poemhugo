using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PoemClient.Source.Tools;
using PoemClient.Source.View;
using PoemClient.Tools;
using PoemClient.View;
using PoemClientWPF.Tools;
using PoemClientWPF.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using Path = System.IO.Path;

namespace PoemClientWPF
{
    public partial class MainWindow : MetroWindow
    {
        public static MainWindow main { get; set; }
        public static Config config;
        public string clientConfig;

        private WindowState previousWindowState;
        //private WindowStyle previousWindowStyle;
        private ResizeMode previousResizeMode;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private Thread PoemDataThread;
        private bool cancelSemaphore = false;
        public bool runActionFailed = false;
        private System.Windows.Point initialPosition;
        private bool isDragging = false;
        public readonly LogFile log;
        public bool isOpenTree;
        //private readonly bool isWindows11;
        private UserControlView leftView1;
        private UserControlView leftView2;
        public UserControlView rightView1 {  get; set; }
        public UserControlView rightView2 { get; set; }
        public UserControlView rightView3 { get; set; }
        public UserControlView rightView4 { get; set; }
        public readonly List<UserControlView> currentViews;
        private Button btn_retract;
        private Border firstColumnContainer;
        //Regex pour détécter les différents patterns dans folder.config
        private const string pattern1 = @"^HH\d+:\d+$";//HHn:n
        private const string pattern2 = @"^HHH\d+:\d+:\d+$";//HHHn:n:n
        private const string pattern3 = @"^HHHH\d+:\d+:\d+:\d+$";//HHHHn:n:n:n
        private const string pattern4 = @"^VV\d+:\d+$";//VVn:n
        private const string pattern5 = @"^VVV\d+:\d+:\d+$";//VVVn:n:n
        private const string pattern6 = @"^VVVV\d+:\d+:\d+:\d+$";//VVVVn:n:n:n
        private const string pattern7 = @"^V\d+:\d+HH\d+:\d+$";//Vn:nHHn:n
        private const string pattern8 = @"^HH\d+:\d+V\d+:\d+$";//HHn:nVn:n
        private const string pattern9 = @"^VV\d+:\d+H\d+:\d+$";//VVn:nHn:n
        private const string pattern10 = @"^H\d+:\d+VV\d+:\d+$";//Hn:nVVn:n
        private const string pattern11 = @"^HH\d+:\d+HH\d+:\d+VV\d+:\d+$";//HHn:nHHn:nVVn:n
        private const string pattern12 = @"^VV\d+:\d+VV\d+:\d+HH\d+:\d+$";//VVn:nVVn:nHHn:n

        /// Gestion fonction toolbar
        private int ScriptCounter = 0;
        private Dictionary <string, ContextMenu> contextMenuDic = new Dictionary<string, ContextMenu>();
        private readonly List<Button> allButtons = new List<Button>();
        private bool contextListing = false;
        private string serverPath = "";
        private string viewDirectory = "";
        private int currentZoom = 0;
        public bool isMousemoveSelected = true;
        private UserControlView scriptView;
        private int NbView = 0;
        private bool isFullScreen = false;
        private bool isRetracted = false;
        public string currentFolderConfig;//view du folder.config actuellement affichée
        public string[] dll_list = new string[12]; // lists all the DLL's
        private bool _isCustomMaximized = false;
        private Rect _restoreBounds;
        private double menuFontSize;
        private string menuFontName;
        private double subMenuFontSize;
        private string subMenuFontName;
        private bool isMenuClosed = false;

        //private bool cocanScroll = false;
        private readonly DispatcherTimer fixTimer;

        //Google Maps
        private GoogleMaps googleMaps;
        public GooglePaths googlePaths;
        public GoogleMatrix googleMatrix;
        public WebView2 webView2;
        public string[,] Tab2 = new string[3, 1];
        private readonly List<string> tabCoord = new List<string>();
        private Image backgroundImage;
        public string[] id_sites;//conserve l'id de chaque site de la bdd afin de pouvoir exporter apres l'import
        bool calculatingCoordinates = false;
        //bool calculatingDistances = false;
        public bool canDownload = true;
        public bool skipDownload = false;
        public string[] translateDownload;
        public bool WarningAIGemini = true;
        public bool WarningAIChatGPT = true;

        //Here
        public HerePaths herePaths = null;
        public HereMatrix hereMatrix = null;

        //Getters-setters
        public PoemClientWPF.Tools.Config GetConfig() { return config; }
        public WebView2 GetBrowser() { return webView2; }
        public String GetCurrentFolderConfig() { return currentFolderConfig; }
        public List<String> GetTabCoord() { return tabCoord; }
        public LogFile GetLogFile() { return log; }
        public string GetUserId()
        {
            return login.GetIdUser().ToString();
        }
        public string logo { get; set; }
        public string closeIcon { get; set; }
        public string minimizeIcon { get; set; }
        public string maximizeIcon { get; set; }
        public int toolBarHeight { get; set; }
        public Brush titleBarColor { get; set; }
        public Brush titleForegroundColor { get; set; }
        public string windowTitle { get; set; }
        public string windowTitleFont { get; set; }
        public string windowTitleSize { get; set; }
        //MAPS
        public string googleApiKey { get; set; }
        public string googlePassword { get; set; }

        //GPT
        public string gptApiKey { get; set; }
        public string gptPassword { get; set; }

        //HERE
        public string hereApiKey { get; set; }


        public MainWindow()
        {
            MainWindow.main = this;

            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, $"{e}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); // JZ 0403
            }

            MainWindow.config = PoemClientWPF.Tools.Config.GetInstance("Config\\poemclient.config");  // JZ ??????
            login.LoadLogin();

            ReadConfig();
            InitClientConfig();


            /** TEST KEYINPUT
            var popup = new KeyInputWindow(this);
            bool? result = popup.ShowDialog();

            if (result == true)
            {
                string k1 = popup.Key1;
                string k2 = popup.Key2;
                string k3 = popup.Key3;
                string k4 = popup.Key4;
                MessageBox.Show("k1:" + k1 + " k2:" + k2 + " k3:" + k3 + " k4:" + k4);
            }
            **/

            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            windowTitle = config.GetKeyValue("Title");
            windowTitleSize = config.GetKeyValue("TitleFontSize");
            windowTitleFont = config.GetKeyValue("TitleFont");
            titleForegroundColor = ParseColor("TitleFontColor");
            this.Title = windowTitle;

            this.log = LogFile.GetInstance(config);

            log.WriteTrace("Start log file");      // JZ

            this.currentViews = new List<UserControlView>();

            string[] titleColorSplit = config.GetKeyValue("TitleBackground").Replace("(", "").Replace(")", "").Split(',');
            titleBarColor = new SolidColorBrush(Color.FromRgb(byte.Parse(titleColorSplit[0]), byte.Parse(titleColorSplit[1]), byte.Parse(titleColorSplit[2])));
            Application.Current.Resources["WindowTitleBrush"] = titleBarColor;


            SetWindowIcon();

            closeIcon = BuildResourcePath(config.GetKeyValue("CloseSystemIcon"));
            minimizeIcon = BuildResourcePath(config.GetKeyValue("MinimizeIcon"));
            maximizeIcon = BuildResourcePath(config.GetKeyValue("MaximizeIcon"));

            toolBarHeight = int.Parse(config.GetKeyValue("MenuHeight"));

            //DataContext = this;
            //byte[] rgbBytes = GetRgbBytes(config.GetKeyValue("BottomFontColor"));
            //lbl_copyright.Content = config.GetKeyValue("AboutCopyright");
            //lbl_copyright.Foreground = (rgbBytes != null) ? new SolidColorBrush(Color.FromRgb(rgbBytes[0], rgbBytes[1], rgbBytes[2])) : lbl_copyright.Foreground;

            //this.isWindows11 = IsWindows11OrAbove();
            mainGrid.ColumnDefinitions.Clear();
            double width = Convert.ToDouble(config.GetKeyValue("FolderWidth"), CultureInfo.InvariantCulture);
            if (width <= 20)
                width = 21;
            width *= 10;
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            Grid.SetColumn(gridTree, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(gridView, 2);

            IntializeRetractButton();
            InitializeRetract();

            isRetracted = Convert.ToBoolean(config.GetKeyValue("OpenFolder"));

            InitializeTreeView();
            InitializeLeftViews();
            ExpandOrCollapseTreeView();
            //

            SetBackgroundColors();
            ReadFolderConfig(config.GetKeyValue("StartView"));      //JZ 1203 : if canceled ????????

            //

            DesignScroll();

            if (isRetracted == false)
            {
                OnClickRetract(null, null);
            }
            else
            {
                isRetracted = false;
            }


            fixTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            fixTimer.Tick += Fix_Timer;
            fixTimer.Start();
            this.SizeChanged += Window_Size;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Width = SystemParameters.WorkArea.Width * 0.66;
            Height = SystemParameters.WorkArea.Height * 0.66;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
            MaxWidth = SystemParameters.WorkArea.Width + 50;
            MaxHeight = SystemParameters.WorkArea.Height + 50;
        }

        private void SetWindowIcon()
        {
            string logo = BuildResourcePath(config.GetKeyValue("Logo")); // app logo

            var template = new DataTemplate();


            var factory = new FrameworkElementFactory(typeof(Image));
            try
            {
                factory.SetValue(Image.SourceProperty, new BitmapImage(new Uri(logo)));
            } catch
            {
                MessageBox.Show(this, $"Logo not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            factory.SetValue(Image.WidthProperty, 16.0);
            factory.SetValue(Image.HeightProperty, 16.0);
            factory.SetValue(Image.StretchProperty, System.Windows.Media.Stretch.Uniform);
            factory.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 0, 0));
            factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            template.VisualTree = factory;

            this.IconTemplate = template;
        }

        private Brush ParseColor(string color)
        {
            string[] colorSplit = config.GetKeyValue(color).Replace("(", "").Replace(")", "").Split(',');
            return new SolidColorBrush(Color.FromRgb(byte.Parse(colorSplit[0]), byte.Parse(colorSplit[1]), byte.Parse(colorSplit[2])));
        }
        private Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            // Path where your DLLs are located
            string folderPath = GetLibPath();

            // Assembly name being requested
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";
            var assemblyPath = Path.Combine(folderPath, assemblyName);

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
        private async Task ExecuteItemWithSemaphoreAsync(MenuItem item)
        {

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => ContextMenu_Click(item.Tag.ToString())).Task.Unwrap();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
            }
        }

        public void InitClientConfig()
        {
            if (clientConfig == "") return;
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(int.Parse(config.GetKeyValue("TimeOutNclRequest")) + 10) })
            {
                try
                {
                    string apiurl = $"{login.GetHttpServer()}:{login.GetServerPort()}/NCL:RUN?{login.GetClientConfig()}&Buffer={login.GetServerPort()}%20{GetUserId()}";
                    HttpResponseMessage response = client.GetAsync(apiurl).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        clientConfig = result;
                        Console.WriteLine(clientConfig);
                    }
                    else
                    {
                        Console.WriteLine($"Erreur de requête : {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
                    if (!login.shutdown)
                        throw new TimeoutException($"NCL: {new TimeoutException().Message}");
                }
            }
        }

        /// <summary>
        /// Returns the line under the param
        /// </summary>
        /// <param name="input"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string GetLineBelowParameter(string input, string parameter)
        {
            string pattern = $@"{parameter}\n(.*)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(input);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        /// Returns the line under the param at line = lineNumber
        /// </summary>
        /// <param name="input"></param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public static string GetLineBelowParameter(string input, int lineNumber)
        {
            string[] lines = input.Split('\n');
            if (lineNumber >= 0 && lineNumber < lines.Length - 1)
            {
                string line = lines[lineNumber + 1];
                return line;
            }
            else
            {
                return null;
            }
        }

        public string GetLineBelowParameter(string input, string query, int queryNumber)
        {
            string[] lines = input.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if ($"\"{query}\"" == lines[i])
                {
                    if (i + queryNumber < lines.Length)
                    {
                        return lines[i + queryNumber];
                    }
                }
            }
            return null;
        }


        public async Task<string[]> CallSQLRequest(string query)
        {
            string apiUrl = $"{login.GetHttpServer()}:{login.GetServerPort()}/";
            string UID = GetUserId();
            string PWD = login.GetDPWD();
            string sqlquery = $"SQLQUERY:PoemClient?UID={UID}&PWD={PWD}&TOKEN={PWD}&QUERY={query}";

            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(int.Parse(config.GetKeyValue("TimeOutNclRequest"))) })
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl + sqlquery);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
                        string result = Encoding.GetEncoding(1252).GetString(responseBytes);

                        result = result.Replace("\r", "");
                        string [] splitResult = result.Split('\n');
                        int index = 0;
                        while (index < splitResult.Length && splitResult[index] != "") index++;
                        if (index == 7)
                        {
                            index = 8;
                        } else
                        {
                            index = 7;
                        }
                        return splitResult.Take(splitResult.Length - 7).Skip(index).ToArray();
                    }
                    else
                    {
                        Console.WriteLine($"Erreur de requête : {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
                    throw new TimeoutException($"NCL: {new TimeoutException().Message}");
                }

            }

            return null;
        }

        public string EncodeForNCL(string text)
        {
            byte[] latin1Bytes = Encoding.GetEncoding(1252).GetBytes(text);
            StringBuilder result = new StringBuilder();
            foreach (byte b in latin1Bytes)
            {
                if ((b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || (b >= '0' && b <= '9') ||
                    b == '-' || b == '_' || b == '.' || b == '~')
                {
                    result.Append((char)b);
                }
                else
                {
                    result.Append($"%{b:X2}");
                }
            }
            return result.ToString();
        }


        public string[] GetQueries(string nclScript, string buffer)
        {
            string apiUrl = $"{login.GetHttpServer()}:{login.GetServerPort()}/NCL:RUN?{nclScript}&BUFFER={buffer}";
            Console.WriteLine("url: " + apiUrl);

            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(int.Parse(config.GetKeyValue("TimeOutNclRequest"))) })
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(apiUrl).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] rawBytes = response.Content.ReadAsByteArrayAsync().Result;
                        string result = Encoding.GetEncoding(1252).GetString(rawBytes);
                        Console.WriteLine(result);
                        return result.Split('\n');
                    }
                    else
                    {
                        Console.WriteLine($"Erreur de requête : {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
                    throw new TimeoutException($"NCL: {new TimeoutException().Message}");
                }
            }
            return null;
        }

        public bool SetQueries(string ncl, string buffer)
        {
            string[] resultArray = null;
            Console.WriteLine(buffer);
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(int.Parse(config.GetKeyValue("TimeOutNclRequest")));
                try
                {
                    string apiUrl = login.GetHttpServer() + ":" + login.GetServerPort() + "/NCL:RUN?" + ncl + "&SPACE=%20&BUFFER=" + buffer;
                    HttpResponseMessage response = client.GetAsync(apiUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string result = response.Content.ReadAsStringAsync().Result;
                        resultArray = result.Split('\n');
                    }
                    else
                    {
                        Console.WriteLine($"Erreur de requête : {response.StatusCode} - {response.ReasonPhrase}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de la requête : {ex.Message}");
                    return false;
                }
            }
            foreach (string str in resultArray)
            {
                if (str.Contains("failure"))
                {
                    return false;
                }
            }
            return true;
            }

        private void Fix_Timer(object sender, EventArgs e)
        {
            InitializeToolBar();
            if (isRetracted == true)
            {
                gridTree.Visibility = Visibility.Hidden;
            }
            fixTimer.Stop();
        }

        //private bool IsWindows11OrAbove()
        //{
        //    return new ComputerInfo().OSFullName.Contains("Windows 11");
        //}


        // redisign la scrollbar du TreeView si on est sur Windows 11
        private void DesignScroll()
        {
            scrollTree.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollTree.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            if (Resources.Contains("ScrollViewerCustom"))
            {
                scrollTree.Resources.Add(typeof(ScrollViewer), Resources["ScrollViewerCustom"]);
            }

            var scrollBar = FindScrollBar(scrollTree);
            if (scrollBar != null)
            {
                scrollBar.Width = 6;
                scrollBar.MinWidth = 6;
                scrollBar.Background = new SolidColorBrush(Colors.Transparent);
            }

            var thumb = FindThumb(scrollTree);
            if (thumb != null)
            {
                thumb.Width = 6;
                thumb.MinWidth = 6;
                thumb.Background = new SolidColorBrush(Colors.Gray);

                thumb.PreviewMouseDown += (s, e) =>
                {
                    thumb.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                };

                thumb.PreviewMouseUp += (s, e) =>
                {
                    thumb.Background = new SolidColorBrush(Colors.Gray);
                };
            }
        }


        private ScrollBar FindScrollBar(ScrollViewer scrollViewer)
        {
            var scrollBar = scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) as ScrollBar;
            if (scrollBar == null)
                scrollBar = scrollViewer.Template.FindName("PART_HorizontalScrollBar", scrollViewer) as ScrollBar;

            return scrollBar;
        }

        private Thumb FindThumb(ScrollViewer scrollViewer)
        {
            var scrollBar = FindScrollBar(scrollViewer);
            if (scrollBar != null)
            {
                var thumb = scrollBar.Template.FindName("PART_Thumb", scrollBar) as Thumb;
                return thumb;
            }
            return null;
        }

        //Initialise le bouton pour masquer/afficher le treeView et les view de gauche
        private void IntializeRetractButton()
        {
            SolidColorBrush brush = (SolidColorBrush)new BrushConverter().ConvertFromString("#c8c8c8");

            this.btn_retract = new Button
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                BorderThickness = new Thickness(0),
                Background = brush,
                Width = 30,
                Height = 30,
                Margin = new Thickness(2, 0, 0, 0),
                FocusVisualStyle = null
            };

            this.btn_retract.Click += OnClickRetract;
            this.firstColumnContainer = new Border() { Background = brush };
            SetImageRetract(config.GetKeyValue("ExpandIcon"), this.btn_retract);
        }

        /*
         * Initialize la grille de gauche en fonction du fichier de config
         * le chargement des views est bloquant, on attend que toutes les vues aient chargées pour executer le reste du code
         */
        public void InitializeLeftViews()
        {
            ClearGridTree();
            gridTree.Visibility = Visibility.Hidden;
            BorderView1.Visibility = Visibility.Collapsed;
            BorderView2.Visibility = Visibility.Collapsed;
            //si 2 views
            if ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1"))) && (Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))))
            {
                leftView1 = new UserControlView(gridView, this);
                leftView1.InitializeView();
                leftView2 = new UserControlView(gridView, this);
                leftView2.InitializeView();
                string[] heights = config.GetKeyValue("FolderProportionViews").Split('-');
                ViewTree.Height = new GridLength(int.Parse(heights[0]), GridUnitType.Star);
                ViewRow1.Height = new GridLength(int.Parse(heights[1]), GridUnitType.Star);
                ViewRow2.Height = new GridLength(int.Parse(heights[2]), GridUnitType.Star);
                SplitRow1.Height = new GridLength(4, GridUnitType.Pixel);
                SplitRow2.Height = new GridLength(4, GridUnitType.Pixel);
                GridSplitter leftSplitter1 = GetGridSplitter(true, LeftSplitter_DragDelta, LeftSplitter_DragCompleted);
                GridSplitter leftSplitter2 = GetGridSplitter(true, LeftSplitter_DragDelta, LeftSplitter_DragCompleted);

                leftSplitter1.Height = 4;
                leftSplitter2.Height = 4;
                leftSplitter1.Background = new SolidColorBrush(Colors.White);
                leftSplitter2.Background = new SolidColorBrush(Colors.White);
                leftSplitter1.Margin = new Thickness(5, 0, 0, 0);
                leftSplitter2.Margin = new Thickness(5, 0, 0, 0);

                TreePadding.BorderThickness = new Thickness(1, 1, 0, 1);
                BorderView1.BorderThickness = new Thickness(1, 1, 0, 1);
                BorderView2.BorderThickness = new Thickness(1, 1, 0, 0);

                BorderView1.Visibility = Visibility.Visible;
                BorderView2.Visibility = Visibility.Visible;

                BorderView1.Child = leftView1;
                BorderView2.Child = leftView2;

                gridTree.Children.Add(leftSplitter1);
                gridTree.Children.Add(leftSplitter2);

                Grid.SetRow(leftSplitter1, 2);
                Grid.SetColumn(leftSplitter1, 0);
                Grid.SetColumnSpan(leftSplitter1, 3);
                Grid.SetRow(leftSplitter2, 4);
                Grid.SetColumn(leftSplitter2, 0);
                Grid.SetColumnSpan(leftSplitter2, 3);

                if ((config.GetKeyValue("FolderView1File") != null) && (config.GetKeyValue("FolderView2File") != null))
                {
                    _ = Task.WhenAll(leftView1.LoadView(config.GetKeyValue("FolderView1File")), leftView2.LoadView(config.GetKeyValue("FolderView2File")));
                    leftView1.ShowView();
                    leftView2.ShowView();
                }

            }//si que leftview 2
            else if ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))) && !((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))))
            {
                leftView2 = new UserControlView(gridView, this);
                GridSplitter leftSplitter = GetGridSplitter(true, LeftSplitter_DragDelta, LeftSplitter_DragCompleted);
                leftSplitter.Height = 4;
                leftSplitter.Background = new SolidColorBrush(Colors.White);
                leftSplitter.Margin = new Thickness(5, 0, 0, 0);

                string[] heights = config.GetKeyValue("FolderProportionView1").Split('-');
                ViewTree.Height = new GridLength(double.Parse(heights[0]), GridUnitType.Star);
                ViewRow1.Height = new GridLength(double.Parse(heights[1]), GridUnitType.Star);
                SplitRow1.Height = new GridLength(4, GridUnitType.Pixel);
                ViewRow2.Height = new GridLength();

                TreePadding.BorderThickness = new Thickness(1, 1, 0, 1);
                BorderView1.BorderThickness = new Thickness(1, 1, 0, 0);

                BorderView1.Visibility = Visibility.Visible;

                BorderView1.Child = leftView2;

                gridTree.Children.Add(leftSplitter);

                Grid.SetRow(leftSplitter, 2);
                Grid.SetColumn(leftSplitter, 0);
                Grid.SetColumnSpan(leftSplitter, 3);
                leftView2.InitializeView();
                if (config.GetKeyValue("FolderView2File") != null)
                {
                    _ = Task.WhenAll(leftView2.LoadView(config.GetKeyValue("FolderView2File")));
                    leftView2.ShowView();
                }
            }//si que leftview1
            else if (!(Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))) && ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))))
            {
                leftView1 = new UserControlView(gridView, this);
                GridSplitter leftSplitter = GetGridSplitter(true, LeftSplitter_DragDelta, LeftSplitter_DragCompleted);
                leftSplitter.Height = 4;
                leftSplitter.Background = new SolidColorBrush(Colors.White);
                leftSplitter.Margin = new Thickness(5, 0, 0, 0);

                string[] heights = config.GetKeyValue("FolderProportionView1").Split('-');
                ViewTree.Height = new GridLength(double.Parse(heights[0]), GridUnitType.Star);
                ViewRow1.Height = new GridLength(double.Parse(heights[1]), GridUnitType.Star);
                SplitRow1.Height = new GridLength(4, GridUnitType.Pixel);
                ViewRow2.Height = new GridLength();

                TreePadding.BorderThickness = new Thickness(1, 1, 0, 1);
                BorderView1.BorderThickness = new Thickness(1, 1, 0, 0);

                BorderView1.Visibility = Visibility.Visible;

                BorderView1.Child = leftView1;

                gridTree.Children.Add(leftSplitter);

                Grid.SetRow(leftSplitter, 2);
                Grid.SetColumn(leftSplitter, 0);
                Grid.SetColumnSpan(leftSplitter, 3);

                leftView1.InitializeView();
                if (config.GetKeyValue("FolderView1File") != null)
                {
                    _ = Task.WhenAll(leftView1.LoadView(config.GetKeyValue("FolderView1File")));
                    leftView1.ShowView();
                }

            }//si aucune view (juste treeView)
            else
            {
                ViewTree.Height = new GridLength(1, GridUnitType.Star);

                TreeWrapper.Margin = new Thickness(1, 1, 0, 0);
                TreePadding.BorderThickness = new Thickness(1, 1, 0, 0);
            }
            gridTree.Visibility = Visibility.Visible;
        }

        /*
         * Splitter handling right bellow
         */
        private GridSplitter GetGridSplitter(bool isSplitterHorizontal, DragDeltaEventHandler dragDeltaHandler, DragCompletedEventHandler dragCompletedHandler)
        {
            GridSplitter splitter = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = isSplitterHorizontal == true ? (this.ActualWidth == 0 ? Width : this.ActualWidth) : 5,
                Height = isSplitterHorizontal == true ? 5 : (this.ActualHeight == 0 ? Height : this.ActualHeight),
                Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            };

            splitter.DragStarted += SplitterDragStartedHandler;
            splitter.DragDelta += dragDeltaHandler;
            splitter.DragCompleted += dragCompletedHandler;

            return splitter;
        }

        private void SplitterDragStartedHandler(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            initialPosition = System.Windows.Input.Mouse.GetPosition(null);
            isDragging = false;
        }

        private void MainSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))
                        leftView1.GetView().Hide();
                    if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView2")))
                        leftView2.GetView().Hide();
                    HideRightViews();
                    isDragging = true;
                }
            }
        }

        private void MainSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReloadLeftView();
                if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))
                {
                    leftView1.GetView().Show();
                }
                if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView2")))
                {
                    leftView2.GetView().Show();
                }
                ReloadView();
                for (int i = 0; i < currentViews.Count; i++)
                {
                    currentViews[i].GetView().Show();
                }
            }
        }

        private void LeftSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))
                        leftView1.GetView().Hide();
                    if (Convert.ToBoolean(config.GetKeyValue("OpenFolderView2")))
                        leftView2.GetView().Hide();
                    isDragging = true;
                }
            }
        }

        private void RightSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    HideRightViews();
                    isDragging = true;
                }
            }
        }

        private void RightSplitter_DragDelta12(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    HideRightViews12();
                    isDragging = true;
                }
            }
        }

        private void RightSplitter_DragDelta23(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    HideRightViews23();
                    isDragging = true;
                }
            }
        }

        private void RightSplitter_DragDelta34(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (!isDragging)
            {
                System.Windows.Point currentPosition = System.Windows.Input.Mouse.GetPosition(null);
                double distance = Math.Abs(currentPosition.X - initialPosition.X) + Math.Abs(currentPosition.Y - initialPosition.Y);

                if (distance > 2)
                {
                    HideRightViews34();
                    isDragging = true;
                }
            }
        }

        private void LeftSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReloadLeftView();
                leftView1?.ShowView();
                leftView2?.ShowView();
            }
        }

        private void RightSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReadFolderConfigAsyncbis(currentFolderConfig);
                ShowAll();
            }
        }

        private void RightSplitter_DragCompleted12(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReadFolderConfigAsyncbis12(currentFolderConfig);
                rightView1?.ShowView();
                rightView2?.ShowView();
            }
        }

        private void RightSplitter_DragCompleted23(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReadFolderConfigAsyncbis23(currentFolderConfig);
                rightView2?.ShowView();
                rightView3?.ShowView();
            }
        }

        private void RightSplitter_DragCompleted34(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (isDragging)
            {
                ReadFolderConfigAsyncbis34(currentFolderConfig);
                rightView3?.ShowView();
                rightView4?.ShowView();
            }
        }

        //réinitialise la grid de gauche
        public void ClearGridTree()
        {
            for (int i = 0; i < gridTree.Children.Count; i++)
            {
                if ((gridTree.Children[i] is UserControlView) || (gridTree.Children[i] is GridSplitter)
                    || (gridTree.Children[i] is StackPanel panel && panel.Name == "TopBorder"))
                {
                    gridTree.Children.RemoveAt(i);
                    i--;
                }
            }
            ViewRow1.Height = new GridLength(0, GridUnitType.Pixel);
            ViewRow2.Height = new GridLength(0, GridUnitType.Pixel);
            SplitRow1.Height = new GridLength(0, GridUnitType.Pixel);
            SplitRow2.Height = new GridLength(0, GridUnitType.Pixel);
        }

        /*
         * permet de recup proprement le chemin des resources
         * resource : nom du fichier resource dans fichier de config
         */
        private string BuildResourcePath(string resource)
        {
            resource = resource.Replace("\\", "/"); // rempalce les \ par /
            resource = resource.Replace("//", "/"); // rempalce les // par /
            if (resource.StartsWith("/"))
            {
                resource = resource.Substring(1);
            }
            string[] splittedRes = resource.Split('/');
            string resourcePath = config.GetRessourcePath();
            resourcePath = Path.Combine(resourcePath, splittedRes[1]);
            return resourcePath;
        }

        /// <summary>
        /// Puts a separator in a ToolBar
        /// </summary>
        /// <param name="toolBar">WPF Toolbar where the separator will go</param>
        /// <param name="marginMultiplier">Double of the margin size multiplier</param>
        private void AddSeparatorToolBar(ToolBar toolBar, double marginMultiplier)
        {
            Separator separator = new Separator
            {
                Visibility = Visibility.Hidden,
                Margin = new Thickness(this.ActualWidth * marginMultiplier, 0, this.ActualWidth * marginMultiplier, 0)
            };
            toolBar.Items.Add(separator);
        }

        //construit la barre d'outils à partir de poemclient.config
        private void InitializeToolBar()
        {
            menuFontName = config.GetKeyValue("MenuHeaderFont");
            menuFontSize = double.Parse(config.GetKeyValue("MenuHeaderFontSize"), CultureInfo.InvariantCulture);

            subMenuFontName = config.GetKeyValue("MenuFont");
            subMenuFontSize = double.Parse(config.GetKeyValue("MenuFontSize"), CultureInfo.InvariantCulture);

            byte[] rgbBytes = GetRgbBytes(config.GetKeyValue("MenuFontColor"));


            for (int buttonId = 1; buttonId <= int.Parse(config.GetKeyValue("MenuNumber")); buttonId++)
            {
                string menuText = config.GetKeyValueMenu("Menu" + buttonId + "Text");
                Button button = new Button();
                Separator separator = new Separator
                {
                    Visibility = Visibility.Hidden,
                    Margin = new Thickness(this.ActualWidth * 0.009, 0, this.ActualWidth * 0.009, 0)
                };

                if (menuText == null)
                {
                    break;
                }
                try
                {
                    if (!string.IsNullOrEmpty(menuText))
                    {
                        button.Content = menuText;
                        button.FontFamily = new FontFamily(menuFontName);
                        button.FontSize = menuFontSize;
                        button.Foreground = (rgbBytes != null) ? new SolidColorBrush(Color.FromRgb(rgbBytes[0], rgbBytes[1], rgbBytes[2])) : button.Foreground;
                    }
                    else
                    {
                        button.Content = new Image
                        {
                            Source = new BitmapImage(new Uri(BuildResourcePath(config.GetKeyValueMenu("Menu" + buttonId + "Icon")))),
                            Width = 15,
                            Height = 15
                        };
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(this, $"Menu failed : {"Menu" + buttonId} : {exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    log.WriteTrace($"Menu failed : {"Menu" + buttonId} : {exception.Message}");
                    Process.GetCurrentProcess().Kill();
                }

                if (buttonId <= int.Parse(config.GetKeyValue("MenuSeparator")))
                {
                    toolBarText.Items.Add(separator);
                    toolBarText.Items.Add(button);
                }
                else
                {
                    toolBarIcon.Items.Add(separator);
                    toolBarIcon.Items.Add(button);
                }

                AddContextMenu(button, $"Menu{buttonId}");

                try
                {
                    if (button.ContextMenu.Items.Count > 1)
                    {
                        throw new Exception("Do not add tool tip if we have more than 1 button");
                    }

                    button.ToolTip = new ToolTip
                    {
                        Content = new TextBlock
                        {
                            Text = config.GetKeyValueMenu("Menu" + buttonId + "Caption"),
                            Foreground = Brushes.Black,
                        },
                        BorderThickness = new Thickness(0),
                        Background = config.GetKeyValueMenu("Menu" + buttonId + "Caption") == "" ? Brushes.Transparent : Brushes.White,
                    };
                }
                catch { }

                if (ScriptCounter == 1)
                {
                    button.Tag = $"{config.GetKeyValueMenu("Menu" + buttonId + "Executable1")}{config.GetKeyValueMenu("Menu" + buttonId + "Static1")}{config.GetKeyValueMenu("Menu" + buttonId + "Script1")}";
                    button.Click += Toolbar_Click;
                }
                else
                {
                    button.Click += ToolBarButton_Click;
                    allButtons.Add(button);
                }

                ScriptCounter = 0;
            }

            // Handle little 'close' image closing current opened view
            try
            {
                Button close = new Button()
                {
                    Content = new Image
                    {
                        Source = new BitmapImage(new Uri(BuildResourcePath(config.GetKeyValue("CloseViewIcon")))),
                        Width = 10,
                        Height = 10
                    },
                    BorderThickness = new Thickness(0), // Removes the border
                    Background = Brushes.Transparent,   // Optional: remove background
                    Padding = new Thickness(0),
                };

                Separator separator = new Separator()
                {
                    Width = 10, // largeur du séparateur, tu peux ajuster
                    Margin = new Thickness(5, 0, 0, 0), // un peu d'espace autour
                    Background = Brushes.Gray, // couleur, ou tu peux définir un style
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                close.Click += OnClickCloseView;
                //AddSeparatorToolBar(toolBarClose, 0.0045);
                //toolBarClose.Items.Add(close);
                BarCloseView.Child = close;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show(this, $"Icon failed : {config.GetKeyValue("CloseViewIcon")} : {exception.Message}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                log.WriteTrace($"Icon failed : {config.GetKeyValue("CloseViewIcon")} : {exception.Message}");
                //Process.GetCurrentProcess().Kill();
            }

            AddSeparatorToolBar(toolBarText, 0.009);
            AddSeparatorToolBar(toolBarIcon, 0.009);
            gridtoolbar.Height = toolBarHeight;
        }


        private void Window_Size(object sender, SizeChangedEventArgs e)
        {
            log.WriteTrace("Window resized");

            foreach (UserControlView view in currentViews)
            {
                view.SetViewBestFit();
            }
        }

        private void AddContextMenu(Button button, string menuKey)
        {
            ContextMenu menu = AddMenuItemToContextMenu(new Tools.ContextMenuType[] { Tools.ContextMenuType.Static, Tools.ContextMenuType.Script, Tools.ContextMenuType.Executable }, menuKey, new ContextMenu { Name = menuKey });
            contextMenuDic.Add(menuKey, menu);

            menu.Placement = PlacementMode.Relative;
            menu.PlacementTarget = button;
            button.ContextMenu = menu;
            button.Loaded += (_sender1, _event1) =>
            {
                menu.Opened += (_sender2, _event2) =>
                {
                    isMenuClosed = false;
                    menu.HorizontalOffset = menu.ActualWidth;
                    menu.VerticalOffset = button.ActualHeight;

                    if (menu.PointToScreen(new Point(0, 0)).X - button.PointToScreen(new Point(0, 0)).X > 5)
                    {
                        menu.HorizontalOffset = 0;
                    }
                };

                menu.Unloaded += (_sender2, _event_2) =>
                {
                    Console.WriteLine("menu closed");
                    isMenuClosed = true;
                };
            };
        }

        private ContextMenu AddMenuItemToContextMenu(Tools.ContextMenuType[] types, string menuKey, ContextMenu contextMenu)
        {
            var combinedActions = new Dictionary<string, List<MenuItem>>();
            var menuEntries = new SortedDictionary<int, string>();

            // Fetch data from configuration file and store them in a sorted dictionnary
            foreach (var type in types)
            {
                int itemId = 1;

                for (string menuFullName = menuKey + type + itemId; config.GetKeyValueMenu(menuFullName + "Order") != null; menuFullName = menuKey + type + (++itemId))
                {
                    try
                    {
                        menuEntries.Add(int.Parse(config.GetKeyValueMenu(menuFullName + "Order")), menuFullName);
                        this.ScriptCounter++;
                    }
                    catch
                    {
                        MessageBoxError(config.GetKeyValue("MenuDuplicated") + menuFullName + "Order");
                        Process.GetCurrentProcess().Kill();
                    }
                }
            }

            // Create MenuItem objects for each fetched actions
            foreach (var kvp in menuEntries)
            {
                MenuItem menu = new MenuItem
                {
                    Header = $"{config.GetKeyValueMenu(kvp.Value + "Text")}",
                    Tag = $"{config.GetKeyValueMenu(kvp.Value)};{kvp.Value}",
                    FontSize = subMenuFontSize,
                    FontFamily = new FontFamily(subMenuFontName),
                    Name = contextMenu.Name
                };
                object tag = menu.Tag;
                if (menu.Header == null || menu.Tag.ToString().Split(';')[0] == null)
                {
                    MessageBox.Show(this, $"Invalid configuration file near XML key '{kvp.Value}'", "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
                    Process.GetCurrentProcess().Kill();
                }

                menu.Click += async (sender, e) =>
                {
                    while (!isMenuClosed)
                    {
                        await Task.Delay(100);
                    }
                    await Task.Delay(100);
                    Console.WriteLine("exec button");

                    await ContextMenu_Click(((MenuItem)sender).Tag.ToString());
                };

                if (combinedActions.ContainsKey(menu.Header.ToString()) == false)
                {
                    combinedActions.Add(menu.Header.ToString(), new List<MenuItem>());
                }

                combinedActions[menu.Header.ToString()].Add(menu);
            }

            // Link each MenuItem to the parent ContextMenu and link each MenuItem with the same name as a "combined action"
            foreach (var kvp in combinedActions)
            {

                if (kvp.Value.Count == 1)
                {
                    contextMenu.Items.Add(kvp.Value.First());
                }
                else
                {
                    MenuItem combinedItem = new MenuItem
                    {
                        Header = kvp.Key,
                        FontSize = subMenuFontSize,
                        FontFamily = new FontFamily(subMenuFontName),
                        Name = contextMenu.Name

                    };

                    combinedItem.Click += async (sender, e) =>
                    {
                        while (!isMenuClosed)
                        {
                            await Task.Delay(100);
                        }
                        await Task.Delay(100);
                        Console.WriteLine("exec button");

                        foreach (var item in kvp.Value)
                        {
                            if (runActionFailed)
                                break;
                            PoemDataThread?.Join();
                            await ExecuteItemWithSemaphoreAsync(item);

                            if (cancelSemaphore == true)
                            {
                                cancelSemaphore = false;
                                break;
                            }
                        }
                        runActionFailed = false;
                    };

                    contextMenu.Items.Add(combinedItem);
                }
            }

            return contextMenu;
        }

        //lit les valeurs dont on a besoin dans le fichier de config
        private void ReadConfig()
        {
            isOpenTree = Convert.ToBoolean(config.GetKeyValue("ExpandDirectory"));
            serverPath = login.GetHttpServer() + ":" + login.GetServerPort() + "/";
            viewDirectory = login.GetViewDirectory();
        }

        /*
         * load les views en fonction de la ligne correspondant lue dans folder.config
         * line : ligne correspondant dans folder.config ex : HH1:3,TableTypeVehicle_TableVehicle,0,TableVehicle,1
         */
        private void ReadFolderConfigAsyncbis12(string line)
        {
            string[] lineSplit = line.Split(';')[0].Split(',');
            List<string> viewFiles = GetViewFiles(lineSplit);
            _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]));
            currentViews.Add(rightView1);
            currentViews.Add(rightView2);
            currentFolderConfig = line;
        }

        private void ReadFolderConfigAsyncbis23(string line)
        {
            string[] lineSplit = line.Split(';')[0].Split(',');
            string disposistion = lineSplit[0];
            List<string> viewFiles = GetViewFiles(lineSplit);
            if (Regex.IsMatch(disposistion, pattern1) || Regex.IsMatch(disposistion, pattern4))
            {
                _ = Task.WhenAll(rightView2.LoadView(viewFiles[1]));
                currentViews.Add(rightView2);
            }
            else
            {
                _ = Task.WhenAll(rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                currentViews.Add(rightView2);
                currentViews.Add(rightView3);
            }
            currentFolderConfig = line;
        }

        private void ReadFolderConfigAsyncbis34(string line)
        {
            string[] lineSplit = line.Split(';')[0].Split(',');
            string disposistion = lineSplit[0];
            List<string> viewFiles = GetViewFiles(lineSplit);
            if (Regex.IsMatch(disposistion, pattern12) || Regex.IsMatch(disposistion, pattern11)
                || Regex.IsMatch(disposistion, pattern6) || Regex.IsMatch(disposistion, pattern3))
            {
                _ = Task.WhenAll(rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                currentViews.Add(rightView3);
                currentViews.Add(rightView4);
            }
            else
            {
                _ = Task.WhenAll(rightView3.LoadView(viewFiles[2]));
                currentViews.Add(rightView3);
            }
            currentFolderConfig = line;
        }

        public void ReadFolderConfigAsyncbis(string line)
        {
            string[] lineSplit = line.Split(';')[0].Split(',');
            string disposistion = lineSplit[0];
            List<string> viewFiles = GetViewFiles(lineSplit);
            if (Regex.IsMatch(disposistion, pattern12) || Regex.IsMatch(disposistion, pattern11)
                || Regex.IsMatch(disposistion, pattern6) || Regex.IsMatch(disposistion, pattern3))
            {
                _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                currentViews.Add(rightView1);
                currentViews.Add(rightView2);
                currentViews.Add(rightView3);
                currentViews.Add(rightView4);
            }
            else if (Regex.IsMatch(disposistion, pattern10) || Regex.IsMatch(disposistion, pattern9)
                || Regex.IsMatch(disposistion, pattern8) || Regex.IsMatch(disposistion, pattern7)
                || Regex.IsMatch(disposistion, pattern5) || Regex.IsMatch(disposistion, pattern2))
            {
                _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                currentViews.Add(rightView1);
                currentViews.Add(rightView2);
                currentViews.Add(rightView3);
            }
            else if (Regex.IsMatch(disposistion, pattern4) || Regex.IsMatch(disposistion, pattern1))
            {
                _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]));
                currentViews.Add(rightView1);
                currentViews.Add(rightView2);
            }
            else
            {
                if (line == "")//si line =="" alors il n'y a pas de view à afficher il faut donc affiche le background
                    DisplayBeautifulBackground();
                else
                {
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]));
                    currentViews.Add(rightView1);
                }
            }
            currentFolderConfig = line;
        }

        [DllImport("user32.dll")]
        static extern int MessageBoxTimeout(IntPtr handle, string text, string title, uint type, Int16 languageId, Int32 timeout);

        public void ReadFolderConfig(string line)
        {
            try
            {
                //si on ne Réinstancie pas les Views à chaque appel elles ne se redimmensionnent pas
                InitializeUserControlViews();
                scriptView = new UserControlView(gridView, this);
                scriptView.InitializeView();
                //reset grille
                ClearGridView();
                //des bons gros split
                string[] lineSplit = line.Split(';')[0].Split(',');
                string disposistion = lineSplit[0];
                List<string> viewFiles = GetViewFiles(lineSplit);
                if (Regex.IsMatch(disposistion, pattern12))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeffCol3 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeffCol4 = Convert.ToInt32(pattern.Split(',')[3]);
                    int coeffRow1 = Convert.ToInt32(pattern.Split(',')[4]);
                    int coeffRow2 = Convert.ToInt32(pattern.Split(',')[5]);
                    //gridView
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                    GridSplitter splitterHoriz = GetGridSplitter(true, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    //sous-grid 1
                    Grid gridViewRow1 = new Grid();
                    gridViewRow1.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                    gridViewRow1.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridViewRow1.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                    GridSplitter splitterVert1 = GetGridSplitter(false, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    gridViewRow1.Children.Add(rightView1);
                    gridViewRow1.Children.Add(splitterVert1);
                    gridViewRow1.Children.Add(rightView2);
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetColumn(splitterVert1, 1);
                    Grid.SetColumn(rightView2, 2);
                    //sous-grid 2
                    Grid gridViewRow2 = new Grid();
                    gridViewRow2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol3, GridUnitType.Star) });
                    gridViewRow2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridViewRow2.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol4, GridUnitType.Star) });
                    GridSplitter splitterVert2 = GetGridSplitter(false, RightSplitter_DragDelta34, RightSplitter_DragCompleted34);
                    gridViewRow2.Children.Add(rightView3);
                    gridViewRow2.Children.Add(splitterVert2);
                    gridViewRow2.Children.Add(rightView4);
                    Grid.SetColumn(rightView3, 0);
                    Grid.SetColumn(splitterVert2, 1);
                    Grid.SetColumn(rightView4, 2);
                    //ajout des sous-grid
                    gridView.Children.Add(gridViewRow1);
                    gridView.Children.Add(splitterHoriz);
                    gridView.Children.Add(gridViewRow2);
                    Grid.SetRow(gridViewRow1, 0);
                    Grid.SetRow(splitterHoriz, 1);
                    Grid.SetRow(gridViewRow2, 2);
                    NbView = 4;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                    currentViews.Add(rightView4);
                }
                else if (Regex.IsMatch(disposistion, pattern11))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeffRow3 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeffRow4 = Convert.ToInt32(pattern.Split(',')[3]);
                    int coeffCol1 = Convert.ToInt32(pattern.Split(',')[4]);
                    int coeffCol2 = Convert.ToInt32(pattern.Split(',')[5]);
                    //gridView sera une grid de 3 colonnes la premiere contiendra une grille de 3 row,
                    //la deuxieme le gridsplitter et la troisieme une autre grille de 3rows
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                    GridSplitter splitterVert = GetGridSplitter(false, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    //sous-grille 1
                    Grid gridViewCol1 = new Grid();
                    gridViewCol1.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                    gridViewCol1.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridViewCol1.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                    GridSplitter splitterHoriz1 = GetGridSplitter(true, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    gridViewCol1.Children.Add(rightView1);
                    gridViewCol1.Children.Add(splitterHoriz1);
                    gridViewCol1.Children.Add(rightView2);
                    Grid.SetRow(rightView1, 0);
                    Grid.SetRow(splitterHoriz1, 1);
                    Grid.SetRow(rightView2, 2);
                    //sous-grille 2
                    Grid gridViewCol2 = new Grid();
                    gridViewCol2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow3, GridUnitType.Star) });
                    gridViewCol2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridViewCol2.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow4, GridUnitType.Star) });
                    GridSplitter splitterHoriz2 = GetGridSplitter(true, RightSplitter_DragDelta34, RightSplitter_DragCompleted34);
                    gridViewCol2.Children.Add(rightView3);
                    gridViewCol2.Children.Add(splitterHoriz2);
                    gridViewCol2.Children.Add(rightView4);
                    Grid.SetRow(rightView3, 0);
                    Grid.SetRow(splitterHoriz2, 1);
                    Grid.SetRow(rightView4, 2);
                    //ajout des sous-grilles dans gridView
                    gridView.Children.Add(gridViewCol1);
                    gridView.Children.Add(splitterVert);
                    gridView.Children.Add(gridViewCol2);
                    Grid.SetColumn(gridViewCol1, 0);
                    Grid.SetColumn(splitterVert, 1);
                    Grid.SetColumn(gridViewCol2, 2);
                    NbView = 4;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                    currentViews.Add(rightView4);

                }
                else if (Regex.IsMatch(disposistion, pattern10))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeffCol1 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeffCol2 = Convert.ToInt32(pattern.Split(',')[3]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                    GridSplitter splitterHoriz = GetGridSplitter(true, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    GridSplitter splitterVert = GetGridSplitter(false, RightSplitter_DragDelta23, RightSplitter_DragCompleted23);
                    //ajout des view dans la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitterHoriz);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitterVert);
                    gridView.Children.Add(rightView3);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetColumnSpan(rightView1, gridView.ColumnDefinitions.Count);
                    //splitter
                    Grid.SetRow(splitterHoriz, 1);
                    Grid.SetColumn(splitterHoriz, 0);
                    Grid.SetColumnSpan(splitterHoriz, gridView.ColumnDefinitions.Count);
                    //placement view2
                    Grid.SetRow(rightView2, 2);
                    Grid.SetColumn(rightView2, 0);
                    //splitter
                    Grid.SetRow(splitterVert, 2);
                    Grid.SetColumn(splitterVert, 1);
                    //placement view3
                    Grid.SetRow(rightView3, 2);
                    Grid.SetColumn(rightView3, 2);
                    NbView = 3;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                }
                else if (Regex.IsMatch(disposistion, pattern9))
                {
                    try
                    {
                        string pattern = ExtractAndCheckNumbers(disposistion);
                        int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                        int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                        int coeffRow1 = Convert.ToInt32(pattern.Split(',')[2]);
                        int coeffRow2 = Convert.ToInt32(pattern.Split(',')[3]);
                        gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                        gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                        gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                        gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                        gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                        gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                        GridSplitter splitterVert = GetGridSplitter(false, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                        GridSplitter splitterHoriz = GetGridSplitter(true, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                        //ajout des view dans la grille
                        gridView.Children.Add(rightView1);
                        gridView.Children.Add(splitterHoriz);
                        gridView.Children.Add(rightView2);
                        gridView.Children.Add(splitterVert);
                        gridView.Children.Add(rightView3);
                        //placement view1
                        Grid.SetRow(rightView1, 0);
                        Grid.SetColumn(rightView1, 0);
                        //splitter
                        Grid.SetRow(splitterVert, 0);
                        Grid.SetColumn(splitterVert, 1);
                        //placement view2
                        Grid.SetRow(rightView2, 0);
                        Grid.SetColumn(rightView2, 2);
                        //splitter
                        Grid.SetRow(splitterHoriz, 1);
                        Grid.SetColumn(splitterHoriz, 0);
                        Grid.SetColumnSpan(splitterHoriz, gridView.ColumnDefinitions.Count);
                        //placement view3
                        Grid.SetRow(rightView3, 2);
                        Grid.SetColumn(rightView3, 0);
                        Grid.SetColumnSpan(rightView3, gridView.ColumnDefinitions.Count);
                        NbView = 3;
                        gridView.Visibility = Visibility.Hidden;
                        _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                        gridView.Visibility = Visibility.Visible;
                        currentViews.Add(rightView1);
                        currentViews.Add(rightView2);
                        currentViews.Add(rightView3);
                    }
                    catch
                    {
                        MessageBox.Show(this, config.GetKeyValue("FolderError"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);  // JZ 0403
                    }
                }
                else if (Regex.IsMatch(disposistion, pattern8))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeffCol1 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeffCol2 = Convert.ToInt32(pattern.Split(',')[3]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                    GridSplitter splitterHoriz = GetGridSplitter(true, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    GridSplitter splitterVert = GetGridSplitter(false, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    //ajout des view dans la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitterHoriz);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitterVert);
                    gridView.Children.Add(rightView3);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetColumn(rightView1, 0);
                    //splitter
                    Grid.SetRow(splitterHoriz, 1);
                    Grid.SetColumn(splitterHoriz, 0);
                    //placement view2
                    Grid.SetRow(rightView2, 2);
                    Grid.SetColumn(rightView2, 0);
                    //splitter
                    Grid.SetRow(splitterVert, 0);
                    Grid.SetRowSpan(splitterVert, gridView.RowDefinitions.Count);
                    Grid.SetColumn(splitterVert, 1);
                    //placement view3
                    Grid.SetRow(rightView3, 0);
                    Grid.SetColumn(rightView3, 2);
                    Grid.SetRowSpan(rightView3, gridView.RowDefinitions.Count);
                    NbView = 3;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                }
                else if (Regex.IsMatch(disposistion, pattern7))
                {
                    //Définition de la grille
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeffRow1 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeffRow2 = Convert.ToInt32(pattern.Split(',')[3]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeffRow2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeffCol2, GridUnitType.Star) });
                    GridSplitter splitterHoriz = GetGridSplitter(true, RightSplitter_DragDelta23, RightSplitter_DragCompleted23);
                    GridSplitter splitterVert = GetGridSplitter(false, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    // Ajout des views dans la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitterVert);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitterHoriz);
                    gridView.Children.Add(rightView3);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetRowSpan(rightView1, gridView.RowDefinitions.Count);
                    //splitter
                    Grid.SetColumn(splitterVert, 1);
                    Grid.SetRowSpan(splitterVert, gridView.RowDefinitions.Count);
                    // placement view2
                    Grid.SetRow(rightView2, 0);
                    Grid.SetColumn(rightView2, 2);
                    //splitter
                    Grid.SetRow(splitterHoriz, 1);
                    Grid.SetColumn(splitterHoriz, 2);
                    // placement view3
                    Grid.SetRow(rightView3, 2);
                    Grid.SetColumn(rightView3, 2);
                    //chargement des views
                    NbView = 3;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                }
                else if (Regex.IsMatch(disposistion, pattern6))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeff4 = Convert.ToInt32(pattern.Split(',')[3]);
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff3, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff4, GridUnitType.Star) });
                    GridSplitter splitter1 = GetGridSplitter(false, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    GridSplitter splitter2 = GetGridSplitter(false, RightSplitter_DragDelta23, RightSplitter_DragCompleted23);
                    GridSplitter splitter3 = GetGridSplitter(false, RightSplitter_DragDelta34, RightSplitter_DragCompleted34);
                    //ajout des views à la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter1);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitter2);
                    gridView.Children.Add(rightView3);
                    gridView.Children.Add(splitter3);
                    gridView.Children.Add(rightView4);
                    //placement view1
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetColumn(splitter1, 1);
                    //placement view2
                    Grid.SetColumn(rightView2, 2);
                    Grid.SetColumn(splitter2, 3);
                    //placement view3
                    Grid.SetColumn(rightView3, 4);
                    Grid.SetColumn(splitter3, 5);
                    //placement view4
                    Grid.SetColumn(rightView4, 6);
                    NbView = 4;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                    currentViews.Add(rightView4);
                }
                else if (Regex.IsMatch(disposistion, pattern5))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);//convert to int
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff3, GridUnitType.Star) });
                    GridSplitter splitter1 = GetGridSplitter(false, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    GridSplitter splitter2 = GetGridSplitter(false, RightSplitter_DragDelta, RightSplitter_DragCompleted);
                    //ajout des views à la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter1);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitter2);
                    gridView.Children.Add(rightView3);
                    //placement view1
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetColumn(splitter1, 1);
                    //placement view2
                    Grid.SetColumn(rightView2, 2);
                    Grid.SetColumn(splitter2, 3);
                    //placement view3
                    Grid.SetColumn(rightView3, 4);
                    NbView = 3;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                }
                else if (Regex.IsMatch(disposistion, pattern4))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);//convert to int
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(coeff2, GridUnitType.Star) });
                    GridSplitter splitter = GetGridSplitter(false, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    //ajout des views à la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter);
                    gridView.Children.Add(rightView2);
                    //placement view1
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetColumn(splitter, 1);
                    //placement view2
                    Grid.SetColumn(rightView2, 2);
                    NbView = 2;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                }
                else if (Regex.IsMatch(disposistion, pattern3))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);//convert to int
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                    int coeff4 = Convert.ToInt32(pattern.Split(',')[3]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff2, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff3, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff4, GridUnitType.Star) });
                    GridSplitter splitter1 = GetGridSplitter(true, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    GridSplitter splitter2 = GetGridSplitter(true, RightSplitter_DragDelta23, RightSplitter_DragCompleted23);
                    GridSplitter splitter3 = GetGridSplitter(true, RightSplitter_DragDelta34, RightSplitter_DragCompleted34);
                    //ajout des views a la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter1);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitter2);
                    gridView.Children.Add(rightView3);
                    gridView.Children.Add(splitter3);
                    gridView.Children.Add(rightView4);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetRow(splitter1, 1);
                    //placement view2
                    Grid.SetRow(rightView2, 2);
                    Grid.SetRow(splitter2, 3);
                    //placement view3
                    Grid.SetRow(rightView3, 4);
                    Grid.SetRow(splitter3, 5);
                    //placement view4
                    Grid.SetRow(rightView4, 6);
                    NbView = 4;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]), rightView4.LoadView(viewFiles[3]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                    currentViews.Add(rightView4);
                }
                else if (Regex.IsMatch(disposistion, pattern2))
                {
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);//convert to int
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff2, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff3, GridUnitType.Star) });
                    GridSplitter splitter1 = GetGridSplitter(true, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    GridSplitter splitter2 = GetGridSplitter(true, RightSplitter_DragDelta23, RightSplitter_DragCompleted23);
                    //ajout des views a la grille
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter1);
                    gridView.Children.Add(rightView2);
                    gridView.Children.Add(splitter2);
                    gridView.Children.Add(rightView3);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetRow(splitter1, 1);
                    //placement view2
                    Grid.SetRow(rightView2, 2);
                    Grid.SetRow(splitter2, 3);
                    //placement view3
                    Grid.SetRow(rightView3, 4);
                    NbView = 3;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]), rightView3.LoadView(viewFiles[2]));
                    gridView.Visibility = Visibility.Visible;
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    currentViews.Add(rightView3);
                }
                else if (Regex.IsMatch(disposistion, pattern1))
                {
                    //Définition de la grille
                    string pattern = ExtractAndCheckNumbers(disposistion);
                    int coeff1 = Convert.ToInt32(pattern.Split(',')[0]); //convert to int
                    int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff1, GridUnitType.Star) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
                    gridView.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(coeff2, GridUnitType.Star) });
                    gridView.ColumnDefinitions.Add(new ColumnDefinition());
                    GridSplitter splitter = GetGridSplitter(true, RightSplitter_DragDelta12, RightSplitter_DragCompleted12);
                    //Ajout des views
                    gridView.Children.Add(rightView1);
                    gridView.Children.Add(splitter);
                    gridView.Children.Add(rightView2);
                    //placement view1
                    Grid.SetRow(rightView1, 0);
                    Grid.SetColumn(rightView1, 0);
                    Grid.SetRow(splitter, 1);
                    //placement view2
                    Grid.SetRow(rightView2, 2);
                    Grid.SetColumn(rightView2, 0);
                    //chargement des views
                    NbView = 2;
                    gridView.Visibility = Visibility.Hidden;
                    _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]), rightView2.LoadView(viewFiles[1]));
                    currentViews.Add(rightView1);
                    currentViews.Add(rightView2);
                    gridView.Visibility = Visibility.Visible;
                }
                else
                {
                    if (line == "")//si line =="" alors il n'y a pas de view à afficher il faut donc affiche le background
                        DisplayBeautifulBackground();
                    else
                    {
                        gridView.RowDefinitions.Add(new RowDefinition());
                        gridView.ColumnDefinitions.Add(new ColumnDefinition());
                        gridView.Children.Add(rightView1);
                        Grid.SetRow(rightView1, 0);
                        Grid.SetColumn(rightView1, 0);
                        NbView = 1;
                        gridView.Visibility = Visibility.Hidden;
                        _ = Task.WhenAll(rightView1.LoadView(viewFiles[0]));
                        gridView.Visibility = Visibility.Visible;
                        currentViews.Add(rightView1);
                    }
                }

                FindAndMoveMsgBox(-10000, -10000, config.GetKeyValue("Loading"));
                MessageBoxTimeout((IntPtr)0, null, config.GetKeyValue("Loading"), 0, 0, 100);    // JZ 0723

                currentFolderConfig = line;
            }
            catch
            {
                MessageBox.Show(this, config.GetKeyValue("FolderError"), "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(IntPtr handle, string title);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr handle, out System.Drawing.Rectangle rectangle);

        [DllImport("user32.dll")]
        static extern void MoveWindow(IntPtr handle, int x, int y, int width, int height, bool repaint);

        void FindAndMoveMsgBox(int x, int y, string title)
        {
            Thread thread = new Thread(() =>
            {
                IntPtr messageBox = IntPtr.Zero;
                System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle();

                while ((messageBox = FindWindow(IntPtr.Zero, title)) == IntPtr.Zero) ;
                GetWindowRect(messageBox, out rectangle);
                MoveWindow(messageBox, x, y, rectangle.Width - rectangle.X, rectangle.Height - rectangle.Y, true);
            });

            thread.Start();
        }

        private void ClearGridView()
        {
            gridView.RowDefinitions.Clear();
            gridView.ColumnDefinitions.Clear();
            gridView.Children.Clear();
        }

        private void InitializeUserControlViews()
        {
            rightView1 = new UserControlView(gridView, this);
            rightView1.InitializeView();
            rightView2 = new UserControlView(gridView, this);
            rightView2.InitializeView();
            rightView3 = new UserControlView(gridView, this);
            rightView3.InitializeView();
            rightView4 = new UserControlView(gridView, this);
            rightView4.InitializeView();

            for (int i = 0; i < currentViews.Count(); i++)
            {
                currentViews[i].CloseView();
            }

            currentViews.Clear();
        }

        /*
         * modifie la ligne de folder.config (pas directement dans le fichier) quand on femre une view
         * closedViewFile : fichier view fermé
         * ex : currentFolderConfig = HH1:3,TableTypeVehicle_TableVehicle,0,TableVehicle,1 closedViewFile : TableVehicle.view
         * => return : TableTypeVehicle_TableVehicle,0
         */
        private void RemakeGridAfterClosingView(string closedViewFile)
        {
            string newFolderConfig = "";
            closedViewFile = closedViewFile.Split('/')[closedViewFile.Split('/').Length - 1];
            string[] lineSplit = currentFolderConfig.Split(';')[0].Split(',');
            string disposistion = lineSplit[0];
            List<string> viewFiles = GetViewFiles(lineSplit);
            if (Regex.IsMatch(disposistion, pattern12))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffCol3 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffCol4 = Convert.ToInt32(pattern.Split(',')[3]);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[4]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[5]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0 || deleted == 1)
                    newFolderConfig = "H" + coeffRow1 + ":" + coeffRow2 + "VV" + coeffCol3 + ":" + coeffCol4 + ",";
                else
                    newFolderConfig = "VV" + coeffCol1 + ":" + coeffCol2 + "H" + coeffRow1 + ":" + coeffRow2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern11))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffRow3 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffRow4 = Convert.ToInt32(pattern.Split(',')[3]);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[4]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[5]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0 || deleted == 1)
                    newFolderConfig = "V" + coeffCol1 + ":" + coeffCol2 + "HH" + coeffRow3 + ":" + coeffRow4 + ",";
                else
                    newFolderConfig = "HH" + coeffRow1 + ":" + coeffRow2 + "V" + coeffCol1 + ":" + coeffCol2 + ",";

            }
            else if (Regex.IsMatch(disposistion, pattern10))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "VV" + coeffCol1 + ":" + coeffCol2 + ",";
                else
                    newFolderConfig = "HH" + coeffRow1 + ":" + coeffRow2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern9))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 2)
                    newFolderConfig = "VV" + coeffCol1 + ":" + coeffCol2 + ",";
                else
                    newFolderConfig = "HH" + coeffRow1 + ":" + coeffRow2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern8))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 2)
                    newFolderConfig = "HH" + coeffRow1 + ":" + coeffRow2 + ",";
                else
                    newFolderConfig = "VV" + coeffCol1 + ":" + coeffCol2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern7))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeffCol1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeffCol2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeffRow1 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeffRow2 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "HH" + coeffRow1 + ":" + coeffRow2 + ",";
                else
                    newFolderConfig = "VV" + coeffCol1 + ":" + coeffCol2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern6))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeff4 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "VVV" + coeff2 + ":" + coeff3 + ":" + coeff4 + ",";
                else if (deleted == 1)
                    newFolderConfig = "VVV" + coeff1 + ":" + coeff3 + ":" + coeff4 + ",";
                else if (deleted == 2)
                    newFolderConfig = "VVV" + coeff1 + ":" + coeff2 + ":" + coeff4 + ",";
                else if (deleted == 3)
                    newFolderConfig = "VVV" + coeff1 + ":" + coeff2 + ":" + coeff3 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern5))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "VV" + coeff2 + ":" + coeff3 + ",";
                else if (deleted == 1)
                    newFolderConfig = "VV" + coeff1 + ":" + coeff3 + ",";
                else if (deleted == 2)
                    newFolderConfig = "VV" + coeff1 + ":" + coeff2 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern3))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                int coeff4 = Convert.ToInt32(pattern.Split(',')[3]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "HHH" + coeff2 + ":" + coeff3 + ":" + coeff4 + ",";
                else if (deleted == 1)
                    newFolderConfig = "HHH" + coeff1 + ":" + coeff3 + ":" + coeff4 + ",";
                else if (deleted == 2)
                    newFolderConfig = "HHH" + coeff1 + ":" + coeff2 + ":" + coeff4 + ",";
                else if (deleted == 3)
                    newFolderConfig = "HHH" + coeff1 + ":" + coeff2 + ":" + coeff3 + ",";
            }
            else if (Regex.IsMatch(disposistion, pattern2))
            {
                string pattern = ExtractAndCheckNumbers(disposistion);
                int coeff1 = Convert.ToInt32(pattern.Split(',')[0]);
                int coeff2 = Convert.ToInt32(pattern.Split(',')[1]);
                int coeff3 = Convert.ToInt32(pattern.Split(',')[2]);
                int deleted = IndexOfDeleted(viewFiles, closedViewFile);
                if (deleted == 0)
                    newFolderConfig = "HH" + coeff2 + ":" + coeff3 + ",";
                else if (deleted == 1)
                    newFolderConfig = "HH" + coeff1 + ":" + coeff3 + ",";
                else if (deleted == 2)
                    newFolderConfig = "HH" + coeff1 + ":" + coeff2 + ",";
            }

            newFolderConfig += GetFolderConfigViewFiles(viewFiles, closedViewFile);
            ReadFolderConfig(newFolderConfig);
        }


        static string ExtractAndCheckNumbers(string pattern)
        {
            MatchCollection matches = Regex.Matches(pattern, @"\d+");

            int[] numbers = matches.Cast<Match>().Select(match => int.Parse(match.Value)).ToArray();

            if (numbers.Any(num => num > 99))
            {
                string modifiedNumbers = string.Join(",", numbers.Select(num => (num > 99) ? "99" : num.ToString()));

                string modifiedPattern = Regex.Replace(pattern, @"\d+", m =>
                {
                    int num = int.Parse(m.Value);
                    return (num > 99) ? "99" : m.Value;
                });
                MessageBox.Show("\"" + pattern + "\" numbers cannot exceed 99 : value set to => " + modifiedPattern, "Folder Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return modifiedNumbers;
            }
            else
            {
                return string.Join(",", numbers.Select(num => num.ToString()));
            }
        }

        // affiche le background quand plus aucune view n'est affichée
        private void DisplayBeautifulBackground()
        {
            ClearGridView();

            try
            {
                if (backgroundImage == null)
                {
                    backgroundImage = new Image();

                    try
                    {
                        backgroundImage.Source = new BitmapImage(new Uri(BuildResourcePath(config.GetKeyValue("BackgroundImage"))));
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        log.WriteTrace($"Error : Background Image not found: {config.GetKeyValue("BackgroundImage")}. Closing PoemClient");
                        ResourceNotFoundMessage(config.GetKeyValue("BackgroundImage"));
                        Application.Current.Shutdown();
                        Process.GetCurrentProcess().Kill();
                    }

                    backgroundImage.Stretch = Stretch.UniformToFill;
                    backgroundImage.HorizontalAlignment = HorizontalAlignment.Stretch;
                    backgroundImage.VerticalAlignment = VerticalAlignment.Stretch;
                }

                gridView.RowDefinitions.Add(new RowDefinition());
                gridView.Children.Add(backgroundImage);
                Grid.SetRow(backgroundImage, 0);
            }
            catch (Exception ex)
            {
                // Log or handle the exception appropriately
                log.WriteTrace($"Background Exception : {ex}");
            }

            currentFolderConfig = "";
        }

        /*
         * retourne l'index du fichier fermé
         * files : liste contenant les noms des fichiers views à affciher
         * closedView : fichier view fermé
         */
        private int IndexOfDeleted(List<string> files, string closedView)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i] == closedView)
                {
                    return i;
                }
            }
            return -1;
        }

        /*
         * retourne le nom des fichiers comme dans folder.config ex : TableTypeVehicle_TableVehicle,0,TableVehicle,1
         * files : liste contenant les noms des fichiers views à affciher
         * closedView : fichier view fermé
         */
        private string GetFolderConfigViewFiles(List<string> files, string closedView)
        {
            string str = "";
            string patternNotFixe = @"\d{3}$";
            string[] splitted;
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i] != closedView)
                {
                    splitted = files[i].Split('.');
                    if (Regex.IsMatch(splitted[0], patternNotFixe))
                        str += new string(splitted[0].Where(c => !char.IsDigit(c)).ToArray()) + ",1";//enleve les nombres de la chaine
                    else
                        str += splitted[0] + ",0";
                    if (i < files.Count - 1)
                        str += ",";
                }
            }
            return str;
        }


        /*
         * récupère le nom des fichiers .view
         * linesplit : ligne correspondante du folder.config qui a été split
         * return : liste de chaque fichier .view
         */
        private List<string> GetViewFiles(string[] lineSplit)
        {
            List<string> viewFiles = new List<string>();
            if (lineSplit[0].Contains(":"))//si il y a plus d'une view à afficher
            {
                for (int i = 1; i < lineSplit.Length; i++)
                {
                    Console.WriteLine("TRUC NUMERO" + i + " " +lineSplit[i]);
                    if (!int.TryParse(lineSplit[i], out _))
                    {
                        viewFiles.Add(IsViewFixeNameFile(lineSplit, i));
                    }
                }
            }
            else//si il n'y a que une view à afficher
            {
                viewFiles.Add(IsViewFixeNameFile(lineSplit, 0));
            }
            return viewFiles;
        }

        /*
         * retourne le nom du fichier view en fonction de si il est "fixe" ou non
         * si le nom du fichier dans folder.config est suivi de ",1" alors le fichier n'est pas "fixe" et
         * on doit ajouter l'id de l'utilisateur au nom du fichier ex : TableVehicule000.view
         */
        private string IsViewFixeNameFile(string[] lineSplit, int i)
        {
            int tryparse = -1;
            bool isFixe = false;
            if (i < lineSplit.Length - 1)
            {
                isFixe = int.TryParse(lineSplit[i + 1].Trim(' '), out tryparse);
            }
            //si il y a un 1 après le nom de la view dans folder.config on ajoute USERID
            // il faut trim les espaces car desfois il y en a
            if ((isFixe) && (tryparse == 1))
            {
                return lineSplit[i].Trim(' ') + login.GetIdUser() + ".view";
            }
            else
                return lineSplit[i].Trim(' ') + ".view";
        }

        private void InitializeRetract()
        {
            this.btn_retract_tree.Click += OnClickRetract;
            SetImageRetract(config.GetKeyValue("CollapseIcon"), btn_retract_tree);
        }

        public void AccessDeniedMessage()
        {
            MessageBox.Show(
                  this,
                  config.GetKeyValue("AccessError"),
                  "Warning",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning
            );
        }

        public void ResourceNotFoundMessage(string resource)
        {
            MessageBox.Show(
                  this,
                  "Resource not found : " + resource,
                  "Error",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error
            );
        }

        public void SetBackgroundColors()
        {
            byte[] backgroundTitleRgb = GetRgbBytes(config.GetKeyValue("TitleBackground"));
            byte[] titleRgb = GetRgbBytes(config.GetKeyValue("TitleFontColor"));

            byte[] menuRgb = GetRgbBytes(config.GetKeyValue("MenuBackground"));

            byte[] folderRgb = GetRgbBytes(config.GetKeyValue("FolderBackground"));
            byte[] highlightRgb = GetRgbBytes(config.GetKeyValue("FolderHighlight"));

            byte[] frameRgb = GetRgbBytes(config.GetKeyValue("FrameColor"));

            if (menuRgb != null)
            {
                var brush = new SolidColorBrush(Color.FromRgb(menuRgb[0], menuRgb[1], menuRgb[2]));
                toolBarText.Background = brush;
                toolBarIcon.Background = brush;
                BarCloseView.Background = brush;
                menuSeparator1.Background = brush;
                menuSeparator2.Background = brush;
                menuMiddle.Background = brush;
            }

            gridTree.Background = (folderRgb != null) ? new SolidColorBrush(Color.FromRgb(folderRgb[0], folderRgb[1], folderRgb[2])) : treeView.Background;
            treeView.Background = gridTree.Background;


            Application.Current.Resources["BackgroundTitleBrush"] = getBrushColor(backgroundTitleRgb);
            Application.Current.Resources["WindowTitleColorBrush"] = getBrushColor(titleRgb);
            Application.Current.Resources["FrameColorBrush"] = getBrushColor(frameRgb);

            this.SetResourceReference(MetroWindow.TitleForegroundProperty, "WindowTitleColorBrush");
            this.SetResourceReference(MetroWindow.WindowTitleBrushProperty, "BackgroundTitleBrush");
            this.SetResourceReference(MetroWindow.NonActiveWindowTitleBrushProperty, "BackgroundTitleBrush");

            try
            {
                SetHighlightColor(treeView.Items, new object[] { SystemColors.HighlightBrushKey, SystemColors.HighlightTextBrushKey, SystemColors.InactiveSelectionHighlightBrushKey }, highlightRgb);
            }
            catch
            {
                log.WriteTrace($"Folder Highlight {highlightRgb} setting failed");
            }
        }

        private Brush getBrushColor(byte[] bytes)
        {
            return new SolidColorBrush(Color.FromRgb(bytes[0], bytes[1], bytes[2]));
        }
        private void SetHighlightColor(ItemCollection node, object[] settings, byte[] colors)
        {
            if (node == null)
            {
                return;
            }

            foreach (TreeViewItem item in node)
            {
                foreach (object setting in settings)
                {
                    item.Resources.Remove(setting);
                    item.Resources.Add(setting, new SolidColorBrush(Color.FromRgb(colors[0], colors[1], colors[2])));
                }

                SetHighlightColor(item.Items, settings, colors);
            }
        }

        public byte[] GetRgbBytes(string rgbValue)
        {
            string[] rgbString = rgbValue.Replace("(", "").Replace(")", "").Split(',');
            byte[] rgbBytes = new byte[3];

            if (rgbString.Length != 3 || String.IsNullOrEmpty(rgbValue))
                return null;
            //throw new ArgumentException("Input must contain 3 int values and cannot be null or empty");

            try
            {
                for (int i = 0; i < rgbString.Length; i++)
                    rgbBytes[i] = byte.Parse(rgbString[i].Trim());
            }
            catch (FormatException)
            {
                return null;
                //throw new FormatException("Error Parsing rgbValue");
            }
            catch (OverflowException)
            {
                return null;
                //throw new OverflowException("Rgb Value has to be between 0 and 255");
            }
            return rgbBytes;
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Prevent the automatic scrolling behavior
            e.Handled = true;
        }

        public void InitializeTreeView()
        {
            string rgbValue = config.GetKeyValue("FolderFontColor");
            byte[] rgbBytes = GetRgbBytes(rgbValue);
            bool shlag = false;
            string path = config.GetIniValue("DIRECTORY");

            if (path == null)
            {
                log.WriteTrace("Error : Failed to Load 'ClientDirectory' value : " + config.GetIniValue("DIRECTORY") + " : Closing PoemClient");
                MessageBox.Show(this, "Initialization Error. Please Try Again.", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                Environment.Exit(1);
                return;
            }

            if (!path.EndsWith("\\") && !path.EndsWith("/"))
                path += "/";

            path += config.GetKeyValue("FolderConfig");

            var lines = new List<string>();
            try
            {
                using (var sr = new StreamReader(path, Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Replace("€", "");
                        if (line.Contains("//"))
                            line = line.Substring(0, line.IndexOf("//")).Trim();
                        if (line.Contains("%"))
                            line = line.Substring(0, line.IndexOf("%")).Trim();

                        if (!string.IsNullOrWhiteSpace(line))
                            lines.Add(line);
                    }
                }

                var treeNode = new List<TreeViewItem>();
                treeView.Items.Clear();

                for (int i = 0; i < lines.Count; i++)
                {
                    string[] arr = lines[i].Split(',');

                    if (arr.Length > 3)
                    {
                        try
                        {
                            for (int j = 3; j < arr.Length; j++)
                            {
                                if (arr[j].Trim() == "1")
                                    arr[2] = arr[2].Trim() + login.GetIdUser();
                                else if (arr[j].Trim() == "0")
                                    arr[2] = arr[2].Trim();
                                else if (j == 7 || j == 8)
                                    arr[2] = arr[2].Trim() + " ," + arr[j].Trim();
                                else
                                    arr[2] = arr[2].Trim() + "," + arr[j].Trim();
                            }
                        }
                        catch (OverflowException)
                        {
                            MessageBox.Show(this, config.GetKeyValue("InputError"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(arr[0]))
                        continue;

                    int lTree = Convert.ToInt32(arr[0]);

                    while (treeNode.Count <= lTree)
                        treeNode.Add(new TreeViewItem());

                    TreeViewItem item = new TreeViewItem();
                    treeNode[lTree] = item;
                    item.Tag = arr[2];
                    item.FontFamily = new System.Windows.Media.FontFamily(config.GetKeyValue("FolderFont"));
                    item.Foreground = (rgbBytes != null) ? new SolidColorBrush(Color.FromRgb(rgbBytes[0], rgbBytes[1], rgbBytes[2])) : item.Foreground;

                    if (lTree == 0)
                    {
                        item.FontSize = Convert.ToDouble(config.GetKeyValue("FolderHeaderFontSize"), CultureInfo.InvariantCulture);
                        item.Margin = new Thickness(0);
                        shlag = true;
                    }
                    else
                    {
                        if (shlag)
                        {
                            item.Margin = new Thickness(0);
                            shlag = false;
                        }
                        item.Margin = new Thickness(0, Convert.ToDouble(config.GetKeyValue("DirectoryTextSpacing"), CultureInfo.InvariantCulture), 0, 0);
                        item.FontSize = Convert.ToDouble(config.GetKeyValue("FolderFontSize"), CultureInfo.InvariantCulture);
                    }

                    AddContextMenuTreeViewItem(item, lTree);

                    bool isFolder = false;
                    if (i + 1 < lines.Count)
                    {
                        string[] nextArr = lines[i + 1].Split(',');
                        if (nextArr.Length > 0 && int.TryParse(nextArr[0].Trim(), out int nextLevel))
                        {
                            isFolder = nextLevel > lTree;
                        }
                    }

                    // getting filepaths
                    string rootClosed = config.GetKeyValue("RootIcon");
                    string rootOpen = config.GetKeyValue("RootOpenIcon");
                    string folderClosed = config.GetKeyValue("FolderIcon");
                    string folderOpen = config.GetKeyValue("FolderOpenIcon");
                    string fileIcon = config.GetKeyValue("FileIcon");

                    //// local function to check if a filepah exists
                    //void CheckFileExists(string fpath, string keyName)
                    //{
                    //    if (!File.Exists(fpath))
                    //    {
                    //        string errorMessage = config.GetKeyValue("FileNotFound") + $" ({keyName}) " + fpath;
                    //        throw new Exception(errorMessage);
                    //    }
                    //}

                    //// Checking each filepath
                    //CheckFileExists(rootClosed, "RootIcon");
                    //CheckFileExists(rootOpen, "RootOpenIcon");
                    //CheckFileExists(folderClosed, "FolderIcon");
                    //CheckFileExists(folderOpen, "FolderOpenIcon");
                    //CheckFileExists(fileIcon, "FileIcon");

                    bool isRootFolder = (lTree == 0) && isFolder;
                    string iconPathClosed = isRootFolder ? rootClosed : (isFolder ? folderClosed : fileIcon);
                    string iconPathOpen = isRootFolder ? rootOpen : folderOpen;

                    var iconImage = new Image()
                    {
                        Source = new BitmapImage(new Uri(BuildResourcePath(iconPathClosed))),
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(0, 0, 5, 0)
                    };

                    var text = new TextBlock()
                    {
                        Text = arr[1],
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var stackPanel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children = { iconImage, text }
                    };

                    item.Header = stackPanel;

                    if (isFolder)
                    {
                        item.Expanded += (s, e) =>
                        {
                            iconImage.Source = new BitmapImage(new Uri(BuildResourcePath(iconPathOpen)));
                            e.Handled = true;
                        };

                        item.Collapsed += (s, e) =>
                        {
                            iconImage.Source = new BitmapImage(new Uri(BuildResourcePath(iconPathClosed)));
                            e.Handled = true;
                        };
                    }

                    if (lTree == 0)
                    {
                        treeView.Items.Add(item);
                    }
                    else if (lTree - 1 >= 0 && lTree - 1 < treeNode.Count)
                    {
                        treeNode[lTree - 1].Items.Add(item);
                    }
                }

                treeView.Margin = new Thickness(
                    0,
                    0,
                    0,
                    Convert.ToDouble(config.GetKeyValue("DirectoryBottomMargin")));
                TreePadding.Padding = new Thickness(
                    Convert.ToDouble(config.GetKeyValue("DirectoryLeftMargin")),
                    Convert.ToDouble(config.GetKeyValue("DirectoryTopMargin")),
                    0,
                    0);
            }
            catch (Exception ex)
            {
                log.WriteTrace("Error : Configuration File Error : " + ex.Message + " : Closing PoemClient");
                MessageBox.Show(ex.Message, "Configuration File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                Environment.Exit(1);
            }
        }


        // Class used to stock metadatas associated with each treeviewitem
        public class TreeNodeMetaData
        {
            public string Path { get; set; }
            public bool IsFolder { get; set; }
            public bool IsRoot { get; set; }
            public Image ImageControl { get; set; }
        }



        private void SetImageRetract(string imageName, Button btn)
        {
            string iconRetractPath = BuildResourcePath(imageName);

            if (!File.Exists(iconRetractPath))
            {
                System.Windows.MessageBox.Show(
                      this,
                      config.GetKeyValue("FileNotFound") + iconRetractPath,
                      "Error",
                      MessageBoxButton.OK,
                      MessageBoxImage.Error
                 );
                throw new Exception(config.GetKeyValue("FileNotFound") + iconRetractPath);
            }

            // Réglage conditionnel de la taille
            double imgSize = (btn.Name == "btn_retract_tree") ? 12 : 8;

            btn.Content = new Image
            {
                Source = new BitmapImage(new Uri(iconRetractPath)),
                Width = imgSize,
                Height = imgSize,
            };
        }


        /*
         * Ajoute (ou non) un ContextMenu au TreeViewItem passé en paramètre
         * lTree : indice dans l'arborescence du TreeView (si lTree=0 c'est une racine)
         */
        private void AddContextMenuTreeViewItem(TreeViewItem item, int lTree)
        {
            ContextMenu contextMenu = new ContextMenu();
            string[] splitTag = item.Tag.ToString().Split(';');
            int items = splitTag.Length;
            bool index = false;
            if ((lTree != 0))
            {
                AddMenuItem(contextMenu, config.GetKeyValue("DirectoryOpenView"), TreeViewItem_OpenView_Click);
                item.ContextMenu = contextMenu;
            }
            for (int i = 1; i < items; i++)
            {
                if ((splitTag.Length > 1) && splitTag[i].Contains("PoemData"))//si edit DataExplorer
                {
                    string[] splitDataExplorer = splitTag[i].Split(',');
                    if (splitDataExplorer.Length > 1)
                    {
                        AddMenuItem(contextMenu, splitDataExplorer[1], TreeViewItem_DataExplorer_Click);
                    }
                }
                if ((splitTag.Length > 1) && (splitTag[i].Contains(".cfg") || splitTag[i].Contains(".config")))//context menu pour ouvrir fichiers de config TMS
                {
                    //AddMenuItem(contextMenu, splitTag[i].Split(',')[1], TreeViewItem_OpenConfig_Click);
                }
                item.ContextMenu = contextMenu;
                index = true;
            }
            if ((item.Tag.ToString().Contains(".cfg") || item.Tag.ToString().Contains(".config")) && (lTree == 0) && index == false)//context menu pour ouvrir fichiers de config
            {
                AddMenuItemLoop(splitTag, contextMenu, TreeViewItem_OpenConfig_Click);
                item.ContextMenu = contextMenu;
            }
            else if (((item.Tag.ToString().Contains(".config") == false) && (item.Tag.ToString().Contains(".cfg") == false)) && (lTree == 0) && index == false)
            {
                AddMenuItem(contextMenu, config.GetKeyValue("DirectoryExpandAll"), ExpandAllTreeViewClick);
                AddMenuItem(contextMenu, config.GetKeyValue("DirectoryCollapseAll"), CollapseAllTreeViewClick);
                item.ContextMenu = contextMenu;
            }
            //MessageBox.Show(item.Tag.ToString());
        }

        /*
         * ajoute un MenuItem au ContextMenu passé en paramètre
         * onClickEvent : fonction appelée lorsqu'on clic sur le MenuItem
         */
        private void AddMenuItem(ContextMenu contextMenu, string header, RoutedEventHandler onClickEvent)
        {
            MenuItem menuItem = new MenuItem
            {
                Header = header
            };
            menuItem.Click += onClickEvent;
            contextMenu.Items.Add(menuItem);
        }

        private void AddMenuItemLoop(string[] splitTag, ContextMenu contextMenu, RoutedEventHandler onClickEvent)
        {
            for (int i = 1; i < splitTag.Length; i++)
            {
                AddMenuItem(contextMenu, splitTag[i].Split(',')[1], onClickEvent);
            }
        }

        public void ExpandOrCollapseTreeView()
        {
            if (isOpenTree)
                ExpandAll();
            else
                CollapseAll();
        }

        /*
         * Les fonctions CollapseAll et ExpandAll n'existent pas sur le TreeView WPF il a fallu les écrire
         * CollapseAll : retracte tous les noeuds du TreeView
         * ExpandAll : deploie tous les noeuds du TreeView
         */
        private void CollapseAll()
        {
            log.WriteTrace("Folder Directory collapsed");
            for (int i = 0; i < treeView.Items.Count; i++)
            {
                CollapseAllTreeItem((TreeViewItem)treeView.Items.GetItemAt(i));
            }
        }

        private void CollapseAllTreeItem(TreeViewItem item)
        {
            item.IsExpanded = false;
            foreach (object childItem in item.Items)
            {
                if (item.ItemContainerGenerator.ContainerFromItem(childItem) is TreeViewItem childTreeViewItem)
                {
                    CollapseAllTreeItem(childTreeViewItem);
                }
            }
        }

        private void ExpandAll()
        {
            log.WriteTrace("Folder Directory expanded");
            treeView.BeginInit();
            try
            {
                foreach (TreeViewItem item in treeView.Items)
                {
                    ExpandAllTreeItems(item);
                }
            }
            finally
            {
                treeView.EndInit();
            }
        }

        private void ExpandAllTreeItems(TreeViewItem item)
        {
            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            stack.Push(item);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                current.ExpandSubtree();

                foreach (object child in current.Items)
                {
                    if (current.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem childItem)
                    {
                        stack.Push(childItem);
                    }
                }
            }
        }


        private string AddDBInfoArgs(string arg, int permission) // ajoute les logins de la bdd dans les arguments
        {
            string nameDB, ownerDB, uidDB, pwdDB, serverIP, serverPort;
            string strDsnProfileServer, strPoemData;

            nameDB = login.GetDNAME();
            ownerDB = login.GetDOWNER();
            uidDB = login.GetDUID();
            pwdDB = login.GetDPWD();

            serverIP = login.GetServerIP();
            serverPort = login.GetServerPort();

            if (nameDB != null && ownerDB != null && uidDB != null && pwdDB != null && serverIP != null && serverPort != null && permission != -1)
            {
                strDsnProfileServer = "[PROFILE=(UID=" + login.GetIdUser() + ",PWD=" + login.GetPSWD() + ",ACCESS=" + permission + ");DSN=(DSN=" + nameDB + ",OWNER=" + ownerDB + ",UID=" + uidDB + ",PWD=" + pwdDB + ");SERVER=(SERVER=" + serverIP + ",PORT=" + serverPort + ",TIMEOUT=100000,UID=,PWD=,PROXYNAME=,PROXYBYPASS=)]";
            }
            else
            {
                strDsnProfileServer = null;
            }

            if (arg.Contains(":")) //absolute path
            {
                if (strDsnProfileServer != null && permission != -1)
                {
                    if (arg.Contains("?"))
                    {
                        string[] splitted = arg.Split('?');
                        strPoemData = splitted[0] + "?" + strDsnProfileServer + "?" + splitted[1];
                    }
                    else
                    {
                        strPoemData = arg + "?" + strDsnProfileServer;
                    }


                    return strPoemData;
                }
                else
                    return arg;
            }
            else // relative path
            {
                string path;
                string root = config.GetIniValue("DIRECTORY");

                if (arg.Contains("\""))
                    path = "\"" + root + "/" + arg.Split('\"')[1] + "\"";
                else
                    path = root + "/" + arg;

                if (strDsnProfileServer != null && permission != -1)
                {
                    if (path.Contains("?"))
                    {
                        string[] splitted = path.Split('?');
                        strPoemData = splitted[0] + "?" + strDsnProfileServer + "?" + splitted[1];
                    }
                    else
                    {
                        strPoemData = path + "?" + strDsnProfileServer; //PROFILE=(UID=000,PWD=toto,ACCESS=1)]]
                    }

                    return strPoemData;
                }
                else
                    return path;
            }
        }

        /*
         * retourne l'argument d'authorisation d'accès pour PoemData
         * auth : permissionLogin (Write,Read,...)
         * return permissionLogin mais en argument compatible pour PoemData (Write = 1, Read = 0,...)
         */
        private int GetPermissionInt(string permission)
        {
            if (permission == null)
            {
                return 0;
            }

            permission = permission.ToLower().Trim(' ');

            switch (permission)
            {
                case "0":
                case "read":
                    return 0;

                case "1":
                case "write":
                    return 1;

                case "2":
                case "admin":
                    return 2;
            }

            return -1;
        }
        private TreeViewItem GetTreeViewItemByMenuItem(object sender)
        {
            MenuItem clickedItem = (MenuItem)sender;
            ContextMenu contextClicked = (ContextMenu)clickedItem.Parent;
            return (TreeViewItem)contextClicked.PlacementTarget;
        }


        private void Toolbar_Click(object sender, RoutedEventArgs e)
        {
            if (PoemDataThread != null)
            {
                return;
            }
            Task task = ContextMenu_Click(((Button)sender).Tag.ToString());
        }

        public async Task ContextMenu_Click(string command)
        {

            string[] args = command.Split(';')[0].Split(',');

            isMenuClosed = false;

            int permission;

            contextListing = false;

            if (args[1].Contains("Logout"))
            {
                log.WriteTrace("Logout clicked");
                var cancelArgs = new CancelEventArgs();
                Window_Closing(null, cancelArgs);
                return;
            }

            permission = GetPermissionInt(login.GetLoginPermission());

            if (permission < GetPermissionInt(args[0]))
            {
                log.WriteTrace("Access denied: " + args[1]);
                AccessDeniedMessage();
                cancelSemaphore = true;
                return;
            }

            args = args.Skip(1).ToArray();

            // PoemData
            if (args[0].Contains("PoemData.exe"))
            {
                log.WriteTrace("Running PoemData : " + args[1]);
                PoemData(args[0] + "," + args[1]);
                return;
            }
            // ne sert a rien ???
            if (args[0].Contains("ButtonGenerateView.view"))
            {
                // Refresh que si generateview run avec success
                if (scriptView.Run_script(args[0], args[1], serverPath + viewDirectory) == 1)
                {
                    /*ReloadView();*/   // JIANYANG
                }
                return;
            }
            // Script
            if (args[0].Contains(".view"))
            {
                log.WriteTrace("Running Script : " + args[1]);

                int result = -1;
                Thread workerThread = new Thread(() =>
                {
                    result = scriptView.Run_script(args[0], args[1], serverPath + viewDirectory);
                });

                workerThread.SetApartmentState(ApartmentState.STA);
                workerThread.Start();
                workerThread.Join();

                if (result == 0)
                {
                    log.WriteTrace($"Script {args[1]} canceled");
                    cancelSemaphore = true;
                    return;
                }

                return;
            }
            // Executable
            if (args[0].Contains("."))
            {
                string CurEnv = config.GetIniValue("DIRECTORY");

                if (!CurEnv.EndsWith("\\"))
                {
                    CurEnv += "\\";
                }
                if (args[0].Contains(":"))
                {
                    if (!File.Exists(args[0]))
                    {
                        MessageBox.Show(this, config.GetKeyValue("FileNotFound") + args[0], config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        log.WriteTrace("Running Executable (absolute path) : " + args[0]);
                        Process.Start(args[0]);
                    }
                }
                else
                {
                    if (!File.Exists(CurEnv + args[0]))
                    {
                        MessageBox.Show(this, config.GetKeyValue("FileNotFound") + CurEnv + args[0], config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        log.WriteTrace("Running Executable (relative path) : " + args[0]);
                        Process.Start(CurEnv + args[0]);
                    }
                }
                return;
            }
            // Static
            // await RunAction(args);
            await StaticFunctions.RunAction(args);
        }

        public async Task ExecuteExcelToDatabase()
        {
            // Order 1 - Static : Write,Excel Import,Excel/ImportData.xls,Upload/Client/ImportData.txt
            string static1 = config.GetKeyValueMenu("Menu2Static1");
            await ContextMenu_Click(static1);
            if (cancelSemaphore) return;

            // Order 2 - Static : Write,Upload,Upload/Client/ImportData.txt
            string static2 = config.GetKeyValueMenu("Menu2Static2");
            await ContextMenu_Click(static2);
            if (cancelSemaphore) return;

            // Order 3 - Script : Write,ButtonImportData.view,ImportData
            string script1 = config.GetKeyValueMenu("Menu2Script1");
            await ContextMenu_Click(script1);
        }

        public async Task ExecuteDatabaseToExcel()
        {
            // Order 4 - Script : Write,ButtonExportData.view,ExportData
            string script2 = config.GetKeyValueMenu("Menu2Script2");
            await ContextMenu_Click(script2);
            if (cancelSemaphore) return;

            // Order 5 - Static : Write,Download,ExportData.txt,Download/Client
            string static3 = config.GetKeyValueMenu("Menu2Static3");
            await ContextMenu_Click(static3);
            if (cancelSemaphore) return;

            // Order 6 - Static : Write,Excel Export,Download/Client/ExportData.txt,Excel/ExportData.xls
            string static4 = config.GetKeyValueMenu("Menu2Static4");
            await ContextMenu_Click(static4);
        }

        public void ReloadLeftView()
        {
            if ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1"))) && (Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))))
            {
                if ((config.GetKeyValue("FolderView1File") != null) && (config.GetKeyValue("FolderView2File") != null))
                {
                    Task.WhenAll(leftView1.LoadView(config.GetKeyValue("FolderView1File")), leftView2.LoadView(config.GetKeyValue("FolderView2File")));
                }
            }
            else if (!(Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))) && ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))))
            {
                if (config.GetKeyValue("FolderView1File") != null)
                {
                    Task.WhenAll(leftView1.LoadView(config.GetKeyValue("FolderView1File")));
                }
            }
            else if ((Convert.ToBoolean(config.GetKeyValue("OpenFolderView2"))) && !((Convert.ToBoolean(config.GetKeyValue("OpenFolderView1")))))
            {
                if (config.GetKeyValue("FolderView2File") != null)
                {
                    Task.WhenAll(leftView2.LoadView(config.GetKeyValue("FolderView2File")));
                }
            }
        }

        // JZ 20260528
        public void ReloadAllViews()
        {
            log.WriteTrace("Views reloaded");
            if (leftView1.IsVisible)
                leftView1.ReloadView();
            if (leftView2.IsVisible)
                leftView2.ReloadView();
            if (rightView1.IsVisible)
                rightView1.ReloadView();
            if (rightView2.IsVisible)
                rightView2.ReloadView();
            if (rightView3.IsVisible)
                rightView3.ReloadView();
            if (rightView4.IsVisible)
                rightView4.ReloadView();
        }

        // JZ 20260528
        public void ReloadView()
        {
            log.WriteTrace("Views reloaded");
            ReadFolderConfigAsyncbis(currentFolderConfig);
            ReloadLeftView();
        }

        // TODO voir si AllowUIToUpdate est vraiment nécessaire
        public async Task ReloadSystem()
        {
            Mouse.SetCursor(Cursors.Wait);
            try
            {
                var newConfig = await Task.Run(() => config.ReloadInstance("Config\\poemclient.config"));

                var title = "   " + newConfig.GetKeyValue("Title");
                var windowTitleSize = newConfig.GetKeyValue("TitleFontSize");
                var windowTitleFont = newConfig.GetKeyValue("TitleFont");
                var width = Math.Max(21, Convert.ToDouble(newConfig.GetKeyValue("FolderWidth"), CultureInfo.InvariantCulture)) * 10;
                var openFolder = Convert.ToBoolean(newConfig.GetKeyValue("OpenFolder"));

                MainWindow.config = newConfig;

                contextMenuDic.Clear();
                toolBarText.Items.Clear();
                toolBarIcon.Items.Clear();
                contextListing = false;
                // AllowUIToUpdate();   // call to an update ui function to prevent the client from freezing and scaling

                this.Title = title;
                SetWindowIcon();
                // AllowUIToUpdate();

                ReadConfig();
                InitClientConfig();
                 
                // AllowUIToUpdate();

                mainGrid.ColumnDefinitions.Clear();
                mainGrid.Children.Remove(firstColumnContainer);
                mainGrid.Children.Remove(btn_retract);
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                Grid.SetColumn(gridView, 2);
                // AllowUIToUpdate();

                gridTree.Visibility = Visibility.Hidden;
                InitializeTreeView();
                // AllowUIToUpdate();

                IntializeRetractButton();
                // AllowUIToUpdate();
                ExpandOrCollapseTreeView();
                // AllowUIToUpdate();
                SetBackgroundColors();
                // AllowUIToUpdate();
                InitializeLeftViews();
                // AllowUIToUpdate();

                fixTimer.Start();
                ReadFolderConfigAsyncbis(currentFolderConfig);
                // AllowUIToUpdate();

                isRetracted = openFolder;
                if (!isRetracted)
                {
                    OnClickRetract(null, null);
                }
                else
                {
                    isRetracted = false;
                }

                gridTree.Visibility = Visibility.Visible;
                // AllowUIToUpdate();

                Upload.ResetSavedPath();
                Download.ResetSavedPaths();
                ExcelImport.ResetSavedPaths();
                ExcelExport.ResetSavedPaths();

                this.Resources["WindowTitleFont"] = new FontFamily(windowTitleFont);
                this.Resources["WindowTitleSize"] = double.Parse(windowTitleSize, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"{config.GetKeyValue("SystemError")}:\n{ex.Message}", config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error); // JZ 0408
                //MessageBox.Show(this, $"An error occured : {ex.Message}", "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
            Mouse.SetCursor(null);
        }

        public void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(_ =>
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
        }

        public void PoemData(string args)
        {
            int permission = GetPermissionInt(login.GetLoginPermission());              // JZ

            string exePoemData = args.Split(',')[0].Contains(":") ? args.Split(',')[0] : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + args.Split(',')[0];
            string argPoemData = AddDBInfoArgs(args.Split(',')[1], permission);   // JZ

            try
            {
                PoemDataThread = new Thread(() =>
                {
                    Process process = Process.Start(exePoemData, "\"" + argPoemData + "\"");                     // JZ

                    process.WaitForExit();
                    log.WriteTrace("PoemData exit code: " + process.ExitCode);
                    Console.WriteLine($"PoemData exit code {process.ExitCode}");
                    PoemDataThread = null;
                });

                PoemDataThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, config.GetKeyValue("FileNotFound") + exePoemData, config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                log.WriteTrace("Exception : " + ex.Message);
            }
        }

        public void ApplyScaling(bool bestFitMode)
        {
            if (!rightView2.IsVisible)
            {
                if (bestFitMode)
                    rightView1.ViewNaturalBestFit(1, 0);
                else
                    rightView1.ViewNaturalBestFit(0, 1);
            }
            if (rightView1.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    rightView1.ViewNaturalBestFit(1, 0);
                else
                    rightView1.ViewNaturalBestFit(0, 1);
            }
            if (rightView2.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    rightView2.ViewNaturalBestFit(1, 0);
                else
                    rightView2.ViewNaturalBestFit(0, 1);
            }
            if (rightView3.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    rightView3.ViewNaturalBestFit(1, 0);
                else
                    rightView3.ViewNaturalBestFit(0, 1);
            }
            if (rightView4.IsKeyboardFocusWithin)
            {
                if (bestFitMode)
                    rightView4.ViewNaturalBestFit(1, 0);
                else
                    rightView4.ViewNaturalBestFit(0, 1);
            }
        }

        public void SetMouseMode(bool changeValue = true)
        {
            int mouseMode = isMousemoveSelected ? 1 : 0;

            for (int i = 1; i <= NbView; i++)
            {
                switch (i)
                {
                    case 1:
                        rightView1.SetMouseMode(mouseMode);
                        break;
                    case 2:
                        rightView2.SetMouseMode(mouseMode);
                        break;
                    case 3:
                        rightView3.SetMouseMode(mouseMode);
                        break;
                    case 4:
                        rightView4.SetMouseMode(mouseMode);
                        break;
                }
            }
            if (changeValue)
            {
                isMousemoveSelected = !isMousemoveSelected;
            }
        }

        public void FuncZoom(int zoomValue)
        {
            if (zoomValue == currentZoom)
            {
                zoomValue = 0;
            }
            currentZoom = zoomValue;
            for (int i = 1; i <= NbView; i++)
            {
                switch (i)
                {
                    case 1:
                        rightView1.SetZoomCursor(zoomValue);
                        break;
                    case 2:
                        rightView2.SetZoomCursor(zoomValue);
                        break;
                    case 3:
                        rightView3.SetZoomCursor(zoomValue);
                        break;
                    case 4:
                        rightView4.SetZoomCursor(zoomValue);
                        break;
                }
            }
        }

        public async void InitializeBrowser()
        {
            string customFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");

            Directory.CreateDirectory(customFolderPath);

            string userDataFolder = Path.Combine(customFolderPath, "Project.exe.WebView2");

            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(
                null,
                userDataFolder,
                new CoreWebView2EnvironmentOptions
                {
                });

            webView2 = new WebView2()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            ClearGridView();
            gridView.RowDefinitions.Add(new RowDefinition());
            gridView.ColumnDefinitions.Add(new ColumnDefinition());
            gridView.Children.Add(webView2);
            Grid.SetRow(webView2, 0);
            Grid.SetColumn(webView2, 0);

            await webView2.EnsureCoreWebView2Async(environment);

            webView2.Source = new Uri("https://www.google.com/maps/");
        }


        //met toutes les vues de droites en Best Fit
        private void SetRightViewsBestFit()
        {
            for (int i = 0; i < currentViews.Count; i++)
            {
                currentViews[i].SetViewBestFit();
            }
        }
        private void HideRightViews()
        {
            for (int i = 0; i < currentViews.Count; i++)
            {
                currentViews[i].GetView().Hide();
            }
        }

        private void HideRightViews12()
        {
            currentViews[0].GetView().Hide();
            currentViews[1].GetView().Hide();
        }

        private void HideRightViews23()
        {
            currentViews[1].GetView().Hide();
            currentViews[2].GetView().Hide();
        }

        private void HideRightViews34()
        {
            currentViews[2].GetView().Hide();
            currentViews[3].GetView().Hide();
        }


        //-------------------------------------------- EVENTS -----------------------------------------------

        //Fermeture de PoemClient
        private bool isLoggingOut = false;
        private readonly object logoutLock = new object();

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            lock (logoutLock)
            {
                // Si déjà en train de se déconnecter, annuler cet événement
                if (isLoggingOut)
                {
                    if (e != null)
                        e.Cancel = true;
                    return;
                }

                // Application.Current.Shutdown(); déclenche l'event Window_Closing, 
                // il ne faut pas appeler logout deux fois
                if (!login.IsClosingLogout())
                {
                    isLoggingOut = true;
                    log.WriteTrace("Logout");
                    Activate();
                    try
                    {
                        login.LoadLogout(e);
                    }
                    catch (Exception ex)
                    {
                        log.WriteTrace("Error : " + ex.Message);
                    }
                    finally
                    {
                        // Réinitialiser si le logout a été annulé OU si e est null (bouton logout)
                        if (e != null && e.Cancel)
                        {
                            isLoggingOut = false;
                        }
                        else if (e == null)
                        {
                            // Pour le bouton logout, vérifier si on n'a pas fait Application.Current.Shutdown
                            if (!login.IsClosingLogout())
                            {
                                isLoggingOut = false;
                            }
                        }
                    }
                }
            }
        }

        private void ToolBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (PoemDataThread != null)
            {
                return;
            }

            contextListing = true;

            Task contextListingTask = Task.Run(() =>
            {
                while (contextListing)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        foreach (var button in allButtons)
                        {
                            var mousePosition = System.Windows.Input.Mouse.GetPosition(button);
                            bool mouseOverButton = mousePosition.X >= 0 && mousePosition.Y >= 0
                                                    && mousePosition.X <= button.ActualWidth
                                                    && mousePosition.Y <= button.ActualHeight;
                            var contextMenu = button.ContextMenu;
                            bool mouseOverContextMenu = false;

                            if (contextMenu != null && contextMenu.IsOpen)
                            {
                                var mousePositionContextMenu = System.Windows.Input.Mouse.GetPosition(contextMenu);
                                mouseOverContextMenu = mousePositionContextMenu.X >= 0 && mousePositionContextMenu.Y >= 0
                                                    && mousePositionContextMenu.X <= contextMenu.ActualWidth
                                                    && mousePositionContextMenu.Y <= contextMenu.ActualHeight;
                            }

                            if (mouseOverButton || mouseOverContextMenu)
                            {
                                if (!contextMenu.IsOpen)
                                {
                                    contextMenu.IsOpen = true;
                                }
                            }
                            else
                            {
                                if (contextMenu.IsOpen)
                                {
                                    contextMenu.IsOpen = false;
                                }
                            }
                        }
                    });
                    Thread.Sleep(100);
                }
            });
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(treeView);
            double hOffset = scrollViewer?.HorizontalOffset ?? 0;

            // Vérifie si un nœud est sélectionné
            if (treeView.SelectedItem is TreeViewItem selectedItem)
            {
                if (selectedItem.Tag != null)
                {
                    if (selectedItem.Tag.ToString() != "0")
                    {
                        ReadFolderConfig(selectedItem.Tag.ToString());
                    }
                }
            }

            // Restaure la position de scroll horizontal
            scrollViewer?.ScrollToHorizontalOffset(hOffset);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        //clic droit sur treeViewItem puis clic sur "Open Views"
        private void TreeViewItem_OpenView_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = GetTreeViewItemByMenuItem(sender);
            string[] viewsFiles = item.Tag.ToString().Split(';')[0].Split(',');
            List<string> views = GetViewFiles(viewsFiles);
            for (int i = 0; i < views.Count; i++)
            {
                OpenView openView = new OpenView(views[i], this);
                openView.Show();
            }
        }

        //clic droit sur treeViewItem puis clic sur "Edit ..."
        private void TreeViewItem_DataExplorer_Click(object sender, RoutedEventArgs e)
        {
            //récupère le tag du treeViewItem lié
            TreeViewItem item = GetTreeViewItemByMenuItem(sender);
            MenuItem clicked = (MenuItem)sender;
            string[] splitedDataExplorer = item.Tag.ToString().Split(';');
            //l'authorization du login doit etre >= authorization du folder.config

            int permission = GetPermissionInt(login.GetLoginPermission()); // JZ

            for (int i = 1; i < splitedDataExplorer.Length; i++)
            {
                if (clicked.Header.ToString() == splitedDataExplorer[i].Split(',')[1])
                {
                    try
                    {
                        int permissionFolder = GetPermissionInt(splitedDataExplorer[i].Split(',')[0]); // JZ

                        if (permission >= permissionFolder)
                        {
                            string poemData = splitedDataExplorer[i].Split(',')[2].Split(' ')[0]; //possibilité d'amélioration et de gestions d'erreurs

                            string exePoemData = $"{Environment.CurrentDirectory}/{poemData}";
                            log.WriteTrace("Running PoemData  : " + exePoemData);

                            string argPoemData = AddDBInfoArgs(splitedDataExplorer[i].Split(',')[2].Split('"')[1], permission);   // JZ

                            if (argPoemData == null)
                            {
                                log.WriteTrace("PoemData Argument Error : " + "\"" + argPoemData + "\"");
                                return;
                            }

                            Process.Start(exePoemData, "\"" + argPoemData + "\"");
                            log.WriteTrace("PoemData succeeded : " + exePoemData + " \"" + argPoemData + "\"");
                        }
                        else
                        {
                            log.WriteTrace("PoemData access denied");
                            AccessDeniedMessage();
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(this, config.GetKeyValue("FileNotFound") + splitedDataExplorer[i].Split(',')[2].Split(' ')[0], config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        //clic droit sur treeViewItem pour ouvrir un fichier .config
        private void TreeViewItem_OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = GetTreeViewItemByMenuItem(sender);

            try
            {
                for (int i = 1; i < item.Tag.ToString().Split(';').Length; i++)
                {
                    string[] configParts = item.Tag.ToString().Split(';')[i].Split(',');

                    if (configParts.Length >= 3 && ((MenuItem)sender).Header.ToString() == configParts[1])
                    {
                        try
                        {
                            Process.Start(configParts[2].Contains(":") == true ? configParts[2] : config.GetIniValue("DIRECTORY") + "\\" + configParts[2]);
                        }
                        catch
                        {
                            MessageBox.Show(this, config.GetKeyValue("FileNotFound") + configParts[2], config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch { }
        }

        private void OnClickRetract(object sender, RoutedEventArgs e)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                gridView.Visibility = Visibility.Hidden;
                gridTree.Visibility = Visibility.Hidden;

                mainGrid.ColumnDefinitions.Clear();
                mainGrid.Children.Remove(btn_retract);
                mainGrid.Children.Remove(firstColumnContainer);

                if (isRetracted == false)
                {
                    mainGrid.Children.Add(firstColumnContainer);
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(15, GridUnitType.Pixel) });
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    mainGrid.Children.Add(this.btn_retract);

                    Grid.SetColumn(firstColumnContainer, 0);
                    Grid.SetColumn(this.btn_retract, 0);
                    Grid.SetColumn(gridView, 1);

                    ReadFolderConfigAsyncbis(currentFolderConfig);
                }
                else
                {
                    double width = Convert.ToDouble(config.GetKeyValue("FolderWidth"), CultureInfo.InvariantCulture);

                    width = (width <= 20 ? 21 : width) * 10;

                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width, GridUnitType.Pixel) });
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });

                    Grid.SetColumn(gridView, 2);

                    leftView1?.ShowView();
                    leftView2?.ShowView();

                    gridTree.Visibility = Visibility.Visible;
                    ReadFolderConfigAsyncbis(currentFolderConfig);
                }

                gridView.Visibility = Visibility.Visible;
                SetRightViewsBestFit();

                // reintialize view to fit the screen
                //InitializeUserControlViews();
                //scriptView = new UserControlView(gridView, this);
                //scriptView.InitializeView();
                //ClearGridView();
            });

            isRetracted = !isRetracted;
        }

        private void OnClickCloseView(object sender, RoutedEventArgs e)
        {
            if (currentFolderConfig != "")
            {
                RemakeGridAfterClosingView(currentViews[IsAViewFocused() == -1 ? 0 : IsAViewFocused()].GetView().GetFileName());
            }
        }

        /*
         * indique si une view est focus
         * return -1 si pas de view focus
         * sinon return indice de la view focus dans currentViews
         */
        private int IsAViewFocused()
        {
            for (int i = 0; i < currentViews.Count; i++)
            {
                if (currentViews[i].IsKeyboardFocusWithin)
                {
                    return i;
                }
            }
            return -1;
        }

        public async Task<(double, double)> AskLocation(string apiKey, string address, bool useGoogle)
        {
            using (var client = new HttpClient())
            {
                string requestUrl = useGoogle
                    ? $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}"
                    : $"https://geocode.search.hereapi.com/v1/geocode?q={Uri.EscapeDataString(address)}&apiKey={apiKey}";

                try
                {
                    var response = await client.GetAsync(requestUrl);

                    if ((int)response.StatusCode == 429)
                    {
                        MessageBox.Show(this, config.GetKeyValue("TooManyRequests"), config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);  // JZ 0409
                        return (0,0);
                    }

                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;

                        if (useGoogle)
                        {
                            var status = root.GetProperty("status").GetString();

                            if (status == "OK" && root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                            {
                                var location = results[0].GetProperty("geometry").GetProperty("location");
                                double latitude = location.GetProperty("lat").GetDouble();
                                double longitude = location.GetProperty("lng").GetDouble();
                                return (latitude, longitude);
                            }
                            else if (json.Contains("\"status\" : \"REQUEST_DENIED\""))
                            {
                                MessageBoxError("Google Maps: " + config.GetKeyValue("ApiKeyError"));
                                return (-1, -1);
                            }
                            else
                            {
                                Console.WriteLine("Google Maps geocoding failed.");
                                return (0, 0);
                            }
                        }
                        else
                        {
                            if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                            {
                                var position = items[0].GetProperty("position");
                                double latitude = position.GetProperty("lat").GetDouble();
                                double longitude = position.GetProperty("lng").GetDouble();
                                return (latitude, longitude);
                            }
                            else
                            {
                                Console.WriteLine("HERE geocoding failed or returned no result.");
                                return (0, 0);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Geocoding exception: {ex.Message}");
                    return (0, 0);
                }
            }
        }

        //clic sur bouton expand (afficher le treeView)
        private void ExpandAllTreeViewClick(object sender, RoutedEventArgs e)
        {
            ExpandAll();
        }

        //clic sur bouton collapse (masquer le treeView)
        private void CollapseAllTreeViewClick(object sender, RoutedEventArgs e)
        {
            CollapseAll();
        }

        private void ShowAll()
        {
            rightView1?.ShowView();
            rightView2?.ShowView();
            rightView3?.ShowView();
            rightView4?.ShowView();
            mainGrid.Visibility = Visibility.Visible;
            mainDock.Visibility = Visibility.Visible;
        }

        private void TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            MouseWheelEventArgs eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };

            (((Control)sender).Parent as UIElement).RaiseEvent(eventArg);
        }

        public static List<string> GetViews()
        {
            string[] fichiers = Directory.GetFiles("..\\Logistics\\View\\Admin", "*.view");
            return fichiers.Select(f => Path.GetFileName(f)).ToList();
        }

        // ----------------GoogleMaps import matrix distance and import coordinates---------------------------------------------

        public void MessageBoxError(string message, bool dispatcherCheck = false)
        {
            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (dispatcherCheck)
            {
                // Utilisation du Dispatcher pour exécuter le code sur le thread UI.
                Dispatcher.Invoke(() => MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));   // JZ 0403
            }
        }

        public static string FormatDuration(int totalSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            string result = "";

            if (hours > 0)
                result += $"{hours}h ";
            if (minutes > 0 || hours > 0)
                result += $"{minutes}m ";

            result += $"{seconds}s";
            return result.Trim();
        }

        public string GetGoogleMapsKeyFromConfig(string input)
        {
            string[] lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("------Parameter"))
                {
                    if (i + 1 < lines.Length)
                        return lines[i + 1].Trim(); // Clé Google Maps
                }
            }

            return null;
        }

        public string GetGoogleMapsPasswordFromConfig(string input)
        {
            string[] lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("------Parameter"))
                {
                    if (i + 2 < lines.Length)
                        return lines[i + 2].Trim(); // Mot de passe Google Maps
                }
            }

            return null;
        }

        public List<AddressData> GetSiteMatrix(string[] list)
        {
            var listAddress = new List<AddressData>();

            int i = 0;
            while (!String.IsNullOrEmpty(list[i]))
            {
                var splitData = list[i].Replace("'", "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                i++;
                if (splitData.Length != 3) { continue; }
                listAddress.Add(new AddressData(splitData[0], splitData[1], splitData[2]));
            }
            return listAddress;
        }

        public List<AddressData> GetSiteSelected(string[] list)
        {
            var listAddress = new List<AddressData>();
            int i = 0;

            while (!String.IsNullOrEmpty(list[i]))
            {
                i++;
            }
            while (i < list.Length)
            {
                var splitData = list[i].Replace("'", "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                i++;
                if (splitData.Length != 3) { continue; }
                listAddress.Add(new AddressData(splitData[0], splitData[1], splitData[2]));
            }
            return listAddress;
        }

        public class AddressData
        {
            public AddressData(string id, string lat, string lng)
            {
                _id = id;
                _lat = lat;
                _lng = lng;
            }
            public string _id { get; set; }
            public string _lat { get; set; }
            public string _lng { get; set; }
        }

        public class DistanceData
        {
            public DistanceData(string lat, string lng, string time, string distance)
            {
                _lat = lat;
                _lng = lng;
                _time = time;
                _distance = distance;
            }

            public string _lat { get; set; }
            public string _lng { get; set; }
            public string _time { get; set; }
            public string _distance { get; set; }
        }

        public bool SendData(List<DistanceData> dataList, int iteration, int siteNumber, string typeResource, string typeRisk)
        {
            string query = login.GetServerPort() + "%20" + GetUserId() + "%20" + $@"""{typeResource}""" + "%20" + $@"""{typeRisk}""" + "%20" + siteNumber + "%20" + iteration;
            int i = 0;
            while (i < dataList.Count)
            {
                query += "%20" + dataList[i]._lat + "%20" + dataList[i]._lng + "%20" + dataList[i]._distance + "%20" + dataList[i]._time;
                Console.WriteLine(dataList[i]._lat + "%20" + dataList[i]._lng + "%20" + dataList[i]._distance + "%20" + dataList[i]._time);
                i++;
            }
            if (SetQueries(GetLineBelowParameter(clientConfig, "ClientSetSiteSite"), query) == false)
            {
                return false;
            }
            return true;
        }

        static string GetData(ref List<string> data)
        {
            string firstElement = data[0];
            data.RemoveAt(0);
            return firstElement;
        }

        static string GetDistanceFromOSRM(string apiUrl)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string response = client.DownloadString(apiUrl);
                    Console.WriteLine(response);
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var routes = doc.RootElement.GetProperty("routes")[0];
                        var distance = routes.GetProperty("distance");
                        var distanceValue = (int)distance.GetDouble();
                        var durationValue = (int)doc.RootElement
                                                .GetProperty("routes")[0]
                                                .GetProperty("duration")
                                                .GetDouble();
                        return $"{distanceValue} {durationValue}";
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"Erreur de connexion : {ex.Message}");
                }
                catch (System.FormatException ex)
                {
                    Console.WriteLine("$Error de parsing json: " + ex.Message);
                }
            }
            return $"{0} {0}";
        }

        public string GetDistanceFromAPI(string url)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string response = client.DownloadString(url);
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var distanceValue = doc.RootElement
                                               .GetProperty("rows")[0]
                                               .GetProperty("elements")[0]
                                               .GetProperty("distance")
                                               .GetProperty("value")
                                               .GetInt32();

                        var durationValue = doc.RootElement
                                               .GetProperty("rows")[0]
                                               .GetProperty("elements")[0]
                                               .GetProperty("duration")
                                               .GetProperty("value")
                                               .GetInt32();

                        return $"{distanceValue} {durationValue}";
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"Erreur de connexion : {ex.Message}");
                    return "Error";
                }
                // catch System.IndexOutOfRangeException = quota dépassé
            }
        }

        private string GetLibPath()
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            return currentPath + "/Libraries";
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCustomMaximized)
            {
                this.WindowState = WindowState.Normal;
                this.Left = _restoreBounds.Left;
                this.Top = _restoreBounds.Top;
                this.Width = _restoreBounds.Width;
                this.Height = _restoreBounds.Height;
                this.MaxWidth = _restoreBounds.Width;
                this.MaxHeight = _restoreBounds.Height;

                _isCustomMaximized = false;
            }
            else
            {
                _restoreBounds = new Rect(this.Left, this.Top, this.Width, this.Height);

                var workArea = SystemParameters.WorkArea;
                this.WindowState = WindowState.Normal;
                this.Left = workArea.Left;
                this.Top = workArea.Top;
                this.Width = workArea.Width;
                this.Height = workArea.Height;
                this.MaxWidth = workArea.Width + 50;
                this.MaxHeight = workArea.Height + 50;

                _isCustomMaximized = true;
            }
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { this.DragMove(); }
                catch {}
            }
        }
    }

    public class ProgramParser
    {
        public ProgramParser(string config, string programTitle)
        {
            string[] lines = config.Split('\n');
            int i = 0;
            while (i < lines.Length && lines[i] != programTitle)
            {
                i++;
            }
            i++;
            nclProgram = lines[i];
            i++;
            int argNumber = int.Parse(lines[i]);
            i++;
            programs = new List<string>();
            for (int j = 0; j < argNumber; j++)
            {
                programTitles.Add(lines[i].Substring(1, lines[i].Length - 2));
                i++;
                programs.Add(lines[i]);
                i++;
            }
            programNumber = argNumber;
        }

        public string nclProgram;
        public List<String> programTitles = new List<string>();
        public List<String> programs = new List<string>();
        public int programNumber;
    }
}