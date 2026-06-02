using Newtonsoft.Json.Linq;
using PoemClient.Source;
using PoemClient.Source.Tools;
using PoemClient.Source.View;
using PoemClientWPF.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoemClientWPF.View
{
    public partial class GoogleMaps : Window
    {
        public string apiKey;
        private static GoogleMaps instance = null;
        private readonly Config _config;
        private readonly MainWindow _main;
        private string requete = String.Empty;
        public string temps_estime;
        private readonly string DSN = null;
        public string[,] Tab1 = new string[3, 1];
        public string[] id_sites;
        private readonly List<string> data = new List<string>();
        public string[] Txt_save = new string[10];
        public string[] Site_save = new string[5];
        readonly bool FileExists = false;
        private readonly string configPath = "";
        private Configuration appconfig;
        public ExeConfigurationFileMap exeConfig;
        private GoogleMapsProgramBox ProgramBox;
        public bool closed = false;
        private string savedStartSite;
        private string savedStartAlias;
        private string savedStartStreet;
        private string savedStartCountry;
        private string savedStartLatitude;
        private string savedStartLongitude;
        private string savedStartPostCode;
        private string savedEndSite;
        private string savedEndAlias;
        private string savedEndStreet;
        private string savedEndCountry;
        private string savedEndLatitude;
        private string savedEndLongitude;
        private string savedEndPostCode;
        public bool _useGoogle;

        private GoogleMaps(bool useGoogle)
        {
            InitializeComponent();
            this._useGoogle = useGoogle;
            this._main = MainWindow.main;
            this._config = _main.GetConfig();
            this.DSN = this._main.login.GetDNAME();
            InitializeDataContext();
            configPath = Directory.GetCurrentDirectory();
            configPath += "\\Cache\\Gmap.config";
            FileExists = File.Exists(configPath);
            if (FileExists == false)
                Create_config_file();
            FileCheck();
            Get_Config(FileExists);
            radio_addr_iti1.Checked += Addr_checked;
            radio_addr_iti2.Checked += Addr_checked;
            FillComboBox();
            apiKey = MainWindow.GetLineBelowParameter(_main.clientConfig, "GoogleMapsKey");
            this.Closed += Window_Closed;
            this.ResizeMode = ResizeMode.NoResize;

            savedStartSite = GetSavedValue("StartSite");
            savedStartAlias = GetSavedValue("StartAlias");
            savedStartStreet = GetSavedValue("StartStreet");
            savedStartCountry = GetSavedValue("StartCountry");
            savedStartLatitude = GetSavedValue("StartLatitude");
            savedStartLongitude = GetSavedValue("StartLongitude");
            savedStartPostCode = GetSavedValue("StartPostCode");

            savedEndSite = GetSavedValue("EndSite");
            savedEndAlias = GetSavedValue("EndAlias");
            savedEndStreet = GetSavedValue("EndStreet");
            savedEndCountry = GetSavedValue("EndCountry");
            savedEndLatitude = GetSavedValue("EndLatitude");
            savedEndLongitude = GetSavedValue("EndLongitude");
            savedEndPostCode = GetSavedValue("EndPostCode");

            this.Topmost = false;

            ProgramBox = new GoogleMapsProgramBox(_main.clientConfig, "GetTypeResourceTypeRisk", showButton: false);
            ProgramBox.Margin = new Thickness(0, 0, 0, 0);
            PanelMaps.Children.Add(ProgramBox);

            if (useGoogle)
            {
                ProgramBox.Visibility = Visibility.Collapsed;
                this.Height = 520;
                this.Title = this._config.GetKeyValue("GoogleMapsSites");
            }
            else {
                Grid.SetRow(search_stackpanel, 3);
                Grid.SetRow(btn_iti_search, 3);
                this.Height = 620;
                this.Title = this._config.GetKeyValue("HereMapsSites");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.Activate(); // Remet la fenêtre MainWindow au premier plan
                this.Owner.Topmost = true;
                this.Owner.Topmost = false;
            }
        }

        private void FillComboBox()
        {
        }


        public static GoogleMaps GetInstance(bool useGoogle)
        {
            if (instance == null)
                instance = new GoogleMaps(useGoogle);
            return instance;
        }

        private void InitializeDataContext()
        {
            this.FontFamily = new FontFamily(_config.GetKeyValue("FolderFont"));
            this.FontSize = Convert.ToDouble(_config.GetKeyValue("FolderFontSize"), CultureInfo.InvariantCulture);
            DataContext = new
            {
                lbl_address = _config.GetKeyValue("Address"),
                lbl_coord = _config.GetKeyValue("Coordinates"),

                lbl_site = _config.GetKeyValue("SiteName"),
                lbl_alias = _config.GetKeyValue("SiteAlias"),
                lbl_street = _config.GetKeyValue("Street"),
                lbl_city = _config.GetKeyValue("City"),
                lbl_country = _config.GetKeyValue("Country"),
                lbl_postcode = _config.GetKeyValue("PostCode"),

                btn_search = _config.GetKeyValue("Search"),
                btn_itinerary = _config.GetKeyValue("Itinerary"),
            };
        }
        private void DisplayCoord(ref Label lbl1, ref TextBox txt1, ref Label lbl2, ref TextBox txt2, ref Label lbl3, ref TextBox txt3, ref Label lbl4, ref TextBox txt4, ref Label lbl5, ref TextBox txt5, string prefix = "Start")
        {
            lbl1.Content = _config.GetKeyValue("Latitude");
            txt1.Text = (prefix == "Start") ? savedStartLatitude : savedEndLatitude;
            lbl2.Content = _config.GetKeyValue("Longitude");
            txt2.Text = (prefix == "Start") ? savedStartLongitude : savedEndLongitude;
            lbl3.Visibility = Visibility.Hidden;
            txt3.Visibility = Visibility.Hidden;
            lbl4.Visibility = Visibility.Hidden;
            txt4.Visibility = Visibility.Hidden;
            lbl5.Visibility = Visibility.Hidden;
            txt5.Visibility = Visibility.Hidden;
        }

        private void ClearTxt(TextBox txt1, TextBox txt2, TextBox txt3, TextBox txt4, TextBox txt5)
        {
            txt1.Clear();
            txt2.Clear();
            txt3.Clear();
            txt4.Clear();
            txt5.Clear();
        }

        private void DisplayAddr(ref Label lbl1, ref TextBox txt1, ref Label lbl2, ref TextBox txt2, ref Label lbl3, ref TextBox txt3, ref Label lbl4, ref TextBox txt4, ref Label lbl5, ref TextBox txt5, string prefix = "Start")
        {
            if (DataContext != null)
            {
                lbl1.Content = ((dynamic)DataContext).lbl_site;
                lbl2.Content = ((dynamic)DataContext).lbl_alias;
                lbl3.Content = ((dynamic)DataContext).lbl_street;
                lbl4.Content = ((dynamic)DataContext).lbl_city;
                lbl5.Content = ((dynamic)DataContext).lbl_postcode;
            }
            txt1.Text = (prefix == "Start") ? savedStartSite : savedEndSite;
            txt2.Text = (prefix == "Start") ? savedStartAlias : savedEndAlias;
            lbl3.Visibility = Visibility.Visible;
            txt3.Visibility = Visibility.Visible;
            txt3.Text = (prefix == "Start") ? savedStartStreet : savedEndStreet;
            lbl4.Visibility = Visibility.Visible;
            txt4.Visibility = Visibility.Visible;
            txt4.Text = (prefix == "Start") ? savedStartCountry : savedEndCountry;
            lbl5.Visibility = Visibility.Visible;
            txt5.Visibility = Visibility.Visible;
            txt5.Text = (prefix == "Start") ? savedStartPostCode : savedEndPostCode;
        }

        public void UpdateGmapEntry()
        {
            File.SetAttributes(configPath, FileAttributes.Normal);
            try
            {
                Console.WriteLine("saving");
                if (radio_addr_iti1.IsChecked == true)
                {
                    appconfig.AppSettings.Settings["StartSite"].Value = txt_iti1.Text;
                    appconfig.AppSettings.Settings["StartAlias"].Value = txt_iti4.Text;
                    appconfig.AppSettings.Settings["StartCountry"].Value = txt_iti3.Text;
                    appconfig.AppSettings.Settings["StartStreet"].Value = txt_iti2.Text;
                    appconfig.AppSettings.Settings["StartPostCode"].Value = txt_iti_postcode1.Text;
                }
                else
                {
                    appconfig.AppSettings.Settings["StartLatitude"].Value = txt_iti1.Text;
                    appconfig.AppSettings.Settings["StartLongitude"].Value = txt_iti4.Text;
                }
                if (radio_addr_iti2.IsChecked == true)
                {
                    appconfig.AppSettings.Settings["EndSite"].Value = txt_iti5.Text;
                    appconfig.AppSettings.Settings["EndAlias"].Value = txt_iti8.Text;
                    appconfig.AppSettings.Settings["EndCountry"].Value = txt_iti7.Text;
                    appconfig.AppSettings.Settings["EndStreet"].Value = txt_iti6.Text;
                    appconfig.AppSettings.Settings["EndPostCode"].Value = txt_iti_postcode2.Text;
                }
                else
                {
                    appconfig.AppSettings.Settings["EndLatitude"].Value = txt_iti5.Text;
                    appconfig.AppSettings.Settings["EndLongitude"].Value = txt_iti8.Text;
                }

                appconfig.AppSettings.Settings["Width"].Value = this.Width.ToString();
                appconfig.AppSettings.Settings["Height"].Value = this.Height.ToString();
                appconfig.AppSettings.Settings["Left"].Value = this.Left.ToString();
                appconfig.AppSettings.Settings["Top"].Value = this.Top.ToString();
                appconfig.AppSettings.Settings["FullScreen"].Value = (this.WindowState == WindowState.Maximized).ToString();
                appconfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                Console.WriteLine("Failed to save");
            }
            File.SetAttributes(configPath, FileAttributes.Hidden);
        }

        private string GetSavedValue(string key)
        {

            if (appconfig.AppSettings.Settings.AllKeys.Contains(key))
            {
                return appconfig.AppSettings.Settings[key].Value;
            }
            else
            {
                return "";
            }
        }

        private void Get_Config(bool FileExists)
        {
            exeConfig = new ExeConfigurationFileMap
            {
                ExeConfigFilename = @configPath
            };
            appconfig = ConfigurationManager.OpenMappedExeConfiguration(exeConfig, ConfigurationUserLevel.None);

            if (FileExists)
            {
                try
                {
                    if (Convert.ToBoolean(GetSavedValue("FullScreen")))
                        this.WindowState = WindowState.Maximized;
                    else
                    {
                        this.Width = Convert.ToDouble(GetSavedValue("Width"));
                        this.Height = Convert.ToDouble(GetSavedValue("Height"));
                        this.Left = Convert.ToDouble(GetSavedValue("Left"));
                        this.Top = Convert.ToDouble(GetSavedValue("Top"));
                    }
                }
                catch (Exception)
                {
                    this.Width = 850;
                    this.Height = 500;
                }
                txt_iti1.Text = GetSavedValue("StartSite");
                txt_iti2.Text = GetSavedValue("StartStreet");
                txt_iti3.Text = GetSavedValue("StartCountry");
                txt_iti4.Text = GetSavedValue("StartAlias");
                txt_iti_postcode1.Text = GetSavedValue("StartPostCode");
                txt_iti5.Text = GetSavedValue("EndSite");
                txt_iti6.Text = GetSavedValue("EndStreet");
                txt_iti7.Text = GetSavedValue("EndCountry");
                txt_iti8.Text = GetSavedValue("EndAlias");
                txt_iti_postcode2.Text = GetSavedValue("EndPostCode");
            }
            else
            {
                for (int i = 0; i < Txt_save.Length; i++)
                    Txt_save[i] = "";
                for (int i = 0; i < Txt_save.Length; i++)
                    Txt_save[i] = "";
            }
        }

        private void Create_config_file()
        {
            TextWriter writer = new StreamWriter(configPath);
            writer.WriteLine("<?xml version=\"1.0\"?>\n<configuration>\n    <appSettings>\n    </appSettings>\n</configuration>");
            writer.Close();
            File.SetAttributes(configPath, FileAttributes.Normal);
            exeConfig = new ExeConfigurationFileMap
            {
                ExeConfigFilename = @configPath
            };
            appconfig = ConfigurationManager.OpenMappedExeConfiguration(exeConfig, ConfigurationUserLevel.None);
            appconfig.AppSettings.Settings.Add("StartStreet", "");
            appconfig.AppSettings.Settings.Add("EndStreet", "");
            appconfig.AppSettings.Settings.Add("StartCountry", "");
            appconfig.AppSettings.Settings.Add("EndCountry", "");
            appconfig.AppSettings.Settings.Add("StartPostCode", "");
            appconfig.AppSettings.Settings.Add("EndPostCode", "");
            appconfig.AppSettings.Settings.Add("StartSite", "");
            appconfig.AppSettings.Settings.Add("EndSite", "");
            appconfig.AppSettings.Settings.Add("StartAlias", "");
            appconfig.AppSettings.Settings.Add("EndAlias", "");
            appconfig.AppSettings.Settings.Add("StartLatitude", "");
            appconfig.AppSettings.Settings.Add("StartLongitude", "");
            appconfig.AppSettings.Settings.Add("EndLatitude", "");
            appconfig.AppSettings.Settings.Add("EndLongitude", "");
            appconfig.AppSettings.Settings.Add("Site", "");
            appconfig.AppSettings.Settings.Add("Street", "");
            appconfig.AppSettings.Settings.Add("City", "");
            appconfig.AppSettings.Settings.Add("Country", "");
            appconfig.AppSettings.Settings.Add("Latitude", "");
            appconfig.AppSettings.Settings.Add("Longitude", "");
            appconfig.AppSettings.Settings.Add("State", "");
            appconfig.AppSettings.Settings.Add("Value_State", "");
            appconfig.AppSettings.Settings.Add("TableMatrix", "");
            appconfig.AppSettings.Settings.Add("DivisionMatrix", "");
            appconfig.AppSettings.Settings.Add("ValueMatrix", "");
            appconfig.AppSettings.Settings.Add("Site1Matrix", "");
            appconfig.AppSettings.Settings.Add("Site2Matrix", "");
            appconfig.AppSettings.Settings.Add("DistanceMatrix", "");
            appconfig.AppSettings.Settings.Add("TimeMatrix", "");
            appconfig.AppSettings.Settings.Add("StateMatrix", "");
            appconfig.AppSettings.Settings.Add("ValueStateMatrix", "");
            appconfig.AppSettings.Settings.Add("Width", "");
            appconfig.AppSettings.Settings.Add("Height", "");
            appconfig.AppSettings.Settings.Add("Left", "");
            appconfig.AppSettings.Settings.Add("Top", "");
            appconfig.AppSettings.Settings.Add("FullScreen", "");
            appconfig.AppSettings.Settings.Add("ComboVehicle", "");
            appconfig.AppSettings.Settings.Add("ComboTour", "");
            appconfig.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            File.SetAttributes(configPath, FileAttributes.Hidden);
        }

        private void Recreate_file()
        {
            File.Delete(configPath);
            Create_config_file();
        }

        private void FileCheck()
        {
            string[] Key_Check = {"StartAlias", "EndAlias", "ComboVehicle", "ComboTour",
                                  "StartStreet", "EndStreet", "StartCountry","StartLatitude","StartLongitude",
                                  "EndCountry", "StartSite", "EndSite","EndLatitude","EndLongitude",
                                  "Site","City","Country","Latitude","Longitude","State","Street",
                                  "Width","Height","Left","Top","FullScreen","StartPostCode","EndPostCode"};
            bool check = false;
            using (StreamReader sr = new StreamReader(configPath))
            {
                string content = sr.ReadToEnd();
                foreach (string i in Key_Check)
                {
                    //Console.WriteLine(i);
                    if (!content.Contains(i))
                    {
                        check = true;
                        break;
                    }
                }
            }
            if (check == true)
                Recreate_file();
        }

        private bool VerifyInputAddress(string txt1, string txt2, string txt3, string txt4)
        {
            return (txt1.Trim() == "") && (txt2.Trim() == "") && (txt3.Trim() == "") && (txt4.Trim() == "");
        }

        private void InputError()
        {
            MessageBoxError(_config.GetKeyValue("InputError"));
        }

        private bool VerifyInputCoord(string txt1, string txt2)
        {
            string patternCoord = "^[0-9]+([.,][0-9]+)?$";
            txt1 = txt1.Trim();
            txt2 = txt2.Trim();
            Console.WriteLine($"txt1 : {txt1} et txt2 : {txt2}");
            return ((txt1 == "") || (txt2 == "")) || (!(Regex.IsMatch(txt1, patternCoord)) || (!Regex.IsMatch(txt2, patternCoord)));
        }

        private void InputCoordError()
        {
            MessageBoxError(_config.GetKeyValue("NumericExpected"));
        }

        private void MessageBoxError(string message)
        {
            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //-------------------------------------------- EVENTS -----------------------------------

        private void Window_closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateGmapEntry();
            _main.GetLogFile().WriteTrace("close GoogleMaps");
            instance = null;
            this.closed = true;
        }
        private void Coord_checked(object sender, RoutedEventArgs e)
        {
            if (sender == radio_coord_iti1)
            {
                savedStartSite = txt_iti1.Text;
                savedStartAlias = txt_iti4.Text;
                savedStartStreet = txt_iti2.Text;
                savedStartCountry = txt_iti3.Text;
                savedStartPostCode = txt_iti_postcode1.Text;
                ClearTxt(txt_iti1, txt_iti2, txt_iti3, txt_iti4, txt_iti_postcode1);
                DisplayCoord(ref lbl_iti1, ref txt_iti1, ref lbl_iti4, ref txt_iti4, ref lbl_iti2, ref txt_iti2, ref lbl_iti3, ref txt_iti3, ref lbl_iti_postcode1, ref txt_iti_postcode1);
            }
            else if (sender == radio_coord_iti2)
            {
                savedEndSite = txt_iti5.Text;
                savedEndAlias = txt_iti8.Text;
                savedEndStreet = txt_iti6.Text;
                savedEndCountry = txt_iti7.Text;
                savedEndPostCode = txt_iti_postcode2.Text;
                ClearTxt(txt_iti5, txt_iti6, txt_iti7, txt_iti8, txt_iti_postcode2);
                DisplayCoord(ref lbl_iti5, ref txt_iti5, ref lbl_iti8, ref txt_iti8, ref lbl_iti6, ref txt_iti6, ref lbl_iti7, ref txt_iti7, ref lbl_iti_postcode2, ref txt_iti_postcode2, "End");
            }
        }

        private void Addr_checked(object sender, RoutedEventArgs e)
        {
            if (sender == radio_addr_iti1)
            {
                savedStartLatitude = txt_iti1.Text;
                savedStartLongitude = txt_iti4.Text;
                ClearTxt(txt_iti1, txt_iti2, txt_iti3, txt_iti4, txt_iti_postcode1);
                DisplayAddr(ref lbl_iti1, ref txt_iti1, ref lbl_iti4, ref txt_iti4, ref lbl_iti2, ref txt_iti2, ref lbl_iti3, ref txt_iti3, ref lbl_iti_postcode1, ref txt_iti_postcode1);
            }
            else if (sender == radio_addr_iti2)
            {
                savedEndLatitude = txt_iti5.Text;
                savedEndLongitude = txt_iti8.Text;
                ClearTxt(txt_iti5, txt_iti6, txt_iti7, txt_iti8, txt_iti_postcode2);
                DisplayAddr(ref lbl_iti5, ref txt_iti5, ref lbl_iti8, ref txt_iti8, ref lbl_iti6, ref txt_iti6, ref lbl_iti7, ref txt_iti7, ref lbl_iti_postcode2, ref txt_iti_postcode2, "End");
            }
        }

        private async void OnClickSearch(object sender, RoutedEventArgs e)
        {
            List<LatLngZ> route = new List<LatLngZ>();
            if (!IsInternetConnected())
            {
                MessageBoxError(_config.GetKeyValue("NetworkError"));
                return;
            }

            if (sender.Equals(btn_search))
            {
                if (_useGoogle)
                {
                    if (VerifyInputAddress(txt_iti1.Text, txt_iti2.Text, txt_iti3.Text, txt_iti_postcode1.Text))
                    {
                        InputError();
                    }
                    else
                    {
                        if ((bool)radio_addr_iti1.IsChecked)
                        {
                            requete = "https://www.google.com/maps/search/?api=1&query=" +
                                      (txt_iti1.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                      (txt_iti2.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                      (txt_iti3.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                      (txt_iti_postcode1.Text.Replace(" ", "+")).Replace("'", "+");
                        }
                        else
                        {
                            if (VerifyInputCoord(txt_iti1.Text, txt_iti4.Text))
                                InputCoordError();
                            else
                                requete = "https://www.google.com/maps/search/?api=1&query=" +
                                          txt_iti1.Text.Replace(",", ".") + "%2C" +
                                          txt_iti4.Text.Replace(",", ".");
                        }
                    }
                }
                else // HERE
                {
                    LatLngZ point = null;

                    if ((bool)radio_addr_iti1.IsChecked)
                    {
                        // Adresse
                        if (VerifyInputAddress(txt_iti1.Text, txt_iti2.Text, txt_iti3.Text, txt_iti_postcode1.Text))
                        {
                            InputError();
                            return;
                        }

                        string fullAddress = $"{txt_iti1.Text} {txt_iti2.Text} {txt_iti3.Text} {txt_iti_postcode1.Text}".Trim();
                        point = await GetCoordinatesFromAddressAsync(fullAddress);

                        if (point == null)
                        {
                            MessageBox.Show("Impossible de géocoder l'adresse.");
                            return;
                        }
                    }
                    else
                    {
                        // Coordonnées
                        if (VerifyInputCoord(txt_iti1.Text, txt_iti4.Text))
                        {
                            InputCoordError();
                            return;
                        }

                        double lat = double.Parse(txt_iti1.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                        double lng = double.Parse(txt_iti4.Text.Replace(",", "."), CultureInfo.InvariantCulture);
                        point = new LatLngZ(lat, lng);
                    }
                    try
                    {
                        _main.GetBrowser().NavigateToString(HEREMaps.GenerateSinglePointMapScript(point));
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            if (sender.Equals(btn_iti_search))
            {
                if (VerifyInputAddress(txt_iti1.Text, txt_iti2.Text, txt_iti3.Text, txt_iti_postcode1.Text) ||
                    VerifyInputAddress(txt_iti5.Text, txt_iti6.Text, txt_iti7.Text, txt_iti_postcode2.Text))
                {
                    InputError();
                }
                else
                {
                    if ((bool)radio_addr_iti1.IsChecked && (bool)radio_addr_iti2.IsChecked)
                    {
                        Console.WriteLine("Adress + Adress iti");
                        requete = "https://www.google.com/maps/dir/?api=1&origin=" +
                                  (txt_iti1.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti2.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti3.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti_postcode1.Text.Replace(" ", "+")).Replace("'", "+");
                        requete += "&destination=" +
                                   (txt_iti5.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                   (txt_iti6.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                   (txt_iti7.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti_postcode2.Text.Replace(" ", "+")).Replace("'", "+");
                    }
                    else if ((bool)radio_coord_iti1.IsChecked && (bool)radio_coord_iti2.IsChecked)
                    {
                        if (VerifyInputCoord(txt_iti1.Text, txt_iti4.Text) || VerifyInputCoord(txt_iti5.Text, txt_iti8.Text))
                            InputCoordError();
                        else
                        {
                            Console.WriteLine("Coord + Coord iti");
                            requete = "https://www.google.com/maps/dir/?api=1&origin=" +
                                      txt_iti1.Text.Replace(",", ".") + "%2C" +
                                      txt_iti4.Text.Replace(",", ".");
                            requete += "&destination=" +
                                       txt_iti5.Text.Replace(",", ".") + "%2C" +
                                       txt_iti8.Text.Replace(",", ".");
                        }
                    }
                    else if ((bool)radio_addr_iti1.IsChecked && (bool)radio_coord_iti2.IsChecked)
                    {
                        if (VerifyInputCoord(txt_iti5.Text, txt_iti8.Text))
                            InputCoordError();
                        else
                        {
                            Console.WriteLine("Adress + Coord iti");
                            requete = "https://www.google.com/maps/dir/?api=1&origin=" +
                                      (txt_iti1.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                      (txt_iti2.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                      (txt_iti3.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti_postcode1.Text.Replace(" ", "+")).Replace("'", "+");
                            requete += "&destination=" +
                                       txt_iti5.Text.Replace(",", ".") + "%2C" +
                                       txt_iti8.Text.Replace(",", ".");
                        }
                    }
                    else if ((bool)radio_coord_iti1.IsChecked && (bool)radio_addr_iti2.IsChecked)
                    {
                        if (VerifyInputCoord(txt_iti1.Text, txt_iti4.Text))
                            InputCoordError();
                        else
                        {
                            Console.WriteLine("Coord + Adress iti");
                            requete = "https://www.google.com/maps/dir/?api=1&origin=" +
                                      txt_iti1.Text.Replace(",", ".") + "%2C" +
                                      txt_iti4.Text.Replace(",", ".");
                            requete += "&destination=" +
                                       (txt_iti5.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                       (txt_iti6.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                       (txt_iti7.Text.Replace(" ", "+")).Replace("'", "+") + "+" +
                                  (txt_iti_postcode2.Text.Replace(" ", "+")).Replace("'", "+");
                        }
                    }
                    requete += "&travelmode=driving";


                    // ===== Call HERE API for Itinerary =====
                    if (!_useGoogle)
                    {
                        try
                        {
                            List<String> selected = ProgramBox.GetSelectedItems();
                            string buffer = _main.login.GetServerPort() + "%20" + _main.GetUserId() + "%20";

                            string encodedSelected0 = _main.EncodeForNCL(selected[0]);
                            string encodedSelected1 = _main.EncodeForNCL(selected[1]);

                            buffer += $@"""{encodedSelected0}""%20""{encodedSelected1}"""; string[] resultResource = _main.GetQueries(ProgramBox.Program.nclProgram, buffer);

                            string[] splitResult = resultResource[1].Substring(16).Replace("'", "").Split();

                            string idTypeResource = splitResult[0];
                            string idTypeRisk = splitResult[1];
                            double length = double.Parse(splitResult[2], CultureInfo.InvariantCulture) * 100;
                            double width = double.Parse(splitResult[3], CultureInfo.InvariantCulture) * 100;
                            double height = double.Parse(splitResult[4], CultureInfo.InvariantCulture) * 100;
                            double maxWeight = double.Parse(splitResult[5], CultureInfo.InvariantCulture) * 100;
                            string categoryTunnel = splitResult[6];
                            int nbTrailer = int.Parse(splitResult[7]);

                            LatLngZ originCoord = null;
                            LatLngZ destinationCoord = null;

                            // Origine
                            if (radio_coord_iti1.IsChecked == true)
                            {
                                originCoord = new LatLngZ(
                                    double.Parse(txt_iti1.Text.Replace(",", "."), CultureInfo.InvariantCulture),
                                    double.Parse(txt_iti4.Text.Replace(",", "."), CultureInfo.InvariantCulture)
                                );
                            }
                            else
                            {
                                string originAddress = $"{txt_iti1.Text} {txt_iti2.Text} {txt_iti3.Text}".Replace("  ", " ").Trim();
                                originCoord = await GetCoordinatesFromAddressAsync(originAddress);
                                if (originCoord == null)
                                {
                                    MessageBox.Show("Impossible de géocoder l'adresse de départ.");
                                    return;
                                }
                            }

                            // Destination
                            if (radio_coord_iti2.IsChecked == true)
                            {
                                destinationCoord = new LatLngZ(
                                    double.Parse(txt_iti5.Text.Replace(",", "."), CultureInfo.InvariantCulture),
                                    double.Parse(txt_iti8.Text.Replace(",", "."), CultureInfo.InvariantCulture)
                                );
                            }
                            else
                            {
                                string destAddress = $"{txt_iti5.Text} {txt_iti6.Text} {txt_iti7.Text}".Replace("  ", " ").Trim();
                                destinationCoord = await GetCoordinatesFromAddressAsync(destAddress);
                                if (destinationCoord == null)
                                {
                                    MessageBox.Show("Impossible de géocoder l'adresse d'arrivée.");
                                    return;
                                }
                            }

                            // Vérification
                            if (originCoord == null || destinationCoord == null)
                            {
                                MessageBox.Show("Erreur : coordonnées manquantes.");
                                return;
                            }

                            string origin = $"{originCoord.Lat.ToString(CultureInfo.InvariantCulture)},{originCoord.Lng.ToString(CultureInfo.InvariantCulture)}";
                            string destination = $"{destinationCoord.Lat.ToString(CultureInfo.InvariantCulture)},{destinationCoord.Lng.ToString(CultureInfo.InvariantCulture)}";

                            var routePoints = await HEREMaps.GetTruckRouteFromHereApiAsync(
                                origin, destination, maxWeight, height, width, length, idTypeRisk, categoryTunnel, nbTrailer, _main);

                            route = routePoints;

                            if (routePoints == null || routePoints.Count == 0)
                            {
                                MessageBox.Show("Erreur : aucun point d’itinéraire reçu depuis HERE.");
                                return;
                            }

                            string routeStr = string.Join("\n", routePoints.Select(p => $"{p.Lat},{p.Lng}"));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Erreur lors du calcul de l’itinéraire camion : " + ex.Message);
                        }
                    }
                }
            }
            if (_useGoogle)
            {
                _main.GetBrowser().Source = new Uri(requete);
                UpdateGmapEntry();
            }
            else
            {
                try
                {
                    _main.GetBrowser().NavigateToString(HEREMaps.GenerateLeafletMapHtml(_main, route, _config));
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        public async Task<LatLngZ> GetCoordinatesFromAddressAsync(string address)
        {
            string apiKey = MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsKey");
            string url = $"https://geocode.search.hereapi.com/v1/geocode?q={Uri.EscapeDataString(address)}&apiKey={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    string content = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Erreur Geocoding ({response.StatusCode}) :\n{url}\n\n{content}");
                        return null;
                    }

                    var json = JObject.Parse(content);
                    var items = json["items"] as JArray;

                    if (items != null && items.Count > 0)
                    {
                        var item = items[0];
                        double lat = item["position"]["lat"].Value<double>();
                        double lng = item["position"]["lng"].Value<double>();
                        string title = item["title"]?.Value<string>();

                        return new LatLngZ(lat, lng, 0, title);
                    }
                    else
                    {
                        MessageBox.Show($"Aucun résultat pour l'adresse : {address}\n\nURL : {url}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception durant le géocodage : " + ex.Message + "\n\nURL : " + url);
                    return null;
                }
            }
        }

        private bool IsInternetConnected()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public string[] GetAddresTable()
        {
            try
            {
                string[] result = _main.GetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientGetAddress"), _main.login.GetServerPort() + "%20" + _main.GetUserId());
                result = result.Skip(2).Take(result.Length - 8).ToArray(); // enleve les infos inutiles
                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CalculateCoordinates()
        {
            if (!IsInternetConnected())
            {
                MessageBox.Show(this, _config.GetKeyValue("NetworkError"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (this._useGoogle)
            {
                var t = MainWindow.GetLineBelowParameter(_main.clientConfig, "GoogleMapsKey");
                bool ok = KeyInputWindow.EnsureValidKeys(
                KeyInputMode.GoogleMaps,
                _main,
                MainWindow.GetLineBelowParameter(_main.clientConfig, "GoogleMapsKey"),
                MainWindow.GetLineBelowParameter(_main.clientConfig, "GoogleMapsPassword"),
                KeyInputWindow.TestGoogleMapsCredentials,
                out string googleApiKey,
                out string googlePassword);

                if (!ok)
                {
                    return false;
                }
                // Les clés sont valides à ce stade :
                _main.googleApiKey = googleApiKey;
                _main.googlePassword = googlePassword;
            }
            else
            {
                bool ok = KeyInputWindow.EnsureValidKeys(
                KeyInputMode.HereMaps,
                _main,
                MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsKey"),
                MainWindow.GetLineBelowParameter(_main.clientConfig, "HereMapsPassword"),
                (theApiKey, _) => KeyInputWindow.TestHereMapsCredentials(theApiKey, _main),
                out string apiKey,
                out string _
                );

                if (!ok)
                {
                    return false;
                }
                _main.hereApiKey = apiKey; // (HERE ne necessite pas de mot de passe) - clé valide à ce stade
            }

            _main.GetTabCoord().Clear();
            var dynamicBox = new MessageForm(_config.GetKeyValue("GoogleMapsCoordinates"), _config.GetKeyValue("Loading"), _main);    // JZ 0723
            dynamicBox.okButton.Visibility = Visibility.Collapsed;
            dynamicBox.ShowMessage();

            try
            {
                string[] addresses = GetAddresTable();
                if (addresses == null)
                {
                    MessageBoxError(_config.GetKeyValue("ServerError"));
                    dynamicBox.Close();
                    return false;
                }
                string limitConfig = MainWindow.GetLineBelowParameter(_main.clientConfig, "maxSitePacketSiteTable");
                int limit = int.Parse(limitConfig);

                CancellationToken token = dynamicBox.Token;

                await Task.Run(async () =>
                {
                    string prebuffer = _main.login.GetServerPort() + "%20" + _main.GetUserId() + "%20" + MainWindow.GetLineBelowParameter(_main.clientConfig, "SiteTableState") + "%20";
                    string buffer = "";
                    int amount = 0;
                    int currentSite = 0;

                    foreach (string address in addresses)
                    {
                        token.ThrowIfCancellationRequested();

                        if (amount == limit)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                dynamicBox.FontSize = 12;
                                dynamicBox.FontFamily = new FontFamily(MainWindow.config.GetKeyValue("FontFamily"));
                                dynamicBox.SetMessage($"{MainWindow.config.GetKeyValue("Progress")} : {addresses.Length - currentSite * limit}");  // JZ 0723
                            });
                            currentSite++;

                            _main.SetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientSetAddress"), prebuffer + limit + buffer);
                            amount = 0;
                            buffer = "";
                        }

                        (string, string) addressInfo = ParseAddress(address);

                        token.ThrowIfCancellationRequested();

                        string apiKey = _useGoogle ? _main.googleApiKey : _main.hereApiKey;
                        (double, double) coord = await _main.AskLocation(apiKey, addressInfo.Item2, _useGoogle);

                        if (coord.Item1 == 0 || coord.Item1 == -1)
                            continue;

                        buffer += "%20" + addressInfo.Item1 + "%20" + coord.Item1 + "%20" + coord.Item2;
                        amount++;
                    }

                    if (amount != 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            dynamicBox.FontSize = 12;
                            dynamicBox.FontFamily = new FontFamily(MainWindow.config.GetKeyValue("FontFamily"));
                            dynamicBox.SetMessage($"{MainWindow.config.GetKeyValue("Progress")} : {addresses.Length - currentSite * limit}");  // JZ 0723
                        });
                        _main.SetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientSetAddress"), prebuffer + amount + buffer);
                    }

                }, token);
            }
            catch (OperationCanceledException)
            {
                //MessageBox.Show("Operation cancelled.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    dynamicBox.CloseMessage();
                });

                MessageBox.Show(_config.GetKeyValue(""), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //dynamicBox.CloseMessage();
            dynamicBox.SetMessage(_config.GetKeyValue("Terminated"));

            // Attendre la fermeture de la fenêtre
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            dynamicBox.Closed += (s, e) => tcs.SetResult(true);
            await tcs.Task;

            //MessageBox.Show(_config.GetKeyValue("Terminated"), "Calcul de coordonées", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        public (string, string) ParseAddress(string input)
        {
            string result = input.Replace("'", "");

            // Match the first number (if any)
            var match = Regex.Match(result, @"^[A-Za-z0-9]+");

            string code = "";
            if (match.Success)
            {
                code = match.Value;
                // Remove the matched number (and any following spaces)
                result = Regex.Replace(result, @"^[A-Za-z0-9]+\s*", "");
            }
            return (code, result);
        }
    }

    public class MessageForm : Window
    {
        private readonly TextBlock messageText;
        public Button okButton;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public MessageForm(string title, string message, Window owner)
        {
            this.Width = 350;
            this.Height = 120;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Owner = owner;
            this.Topmost = false;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Title = title;
            this.ResizeMode = ResizeMode.CanMinimize;

            messageText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = 16,
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(10)
            };

            okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 10, 10)
            };
            okButton.Click += OkButton_Click;

            var background = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                Child = CreateLayout()
            };

            this.Content = background;
            messageText.Text = message;

            this.Closed += (s, e) =>
            {
                if (!_cts.IsCancellationRequested)
                    _cts.Cancel();

                this.Owner?.Activate();
            };
        }

        private UIElement CreateLayout()
        {
            var grid = new Grid();
            grid.Children.Add(messageText);
            grid.Children.Add(okButton);
            return grid;
        }
        public void SetMessage(string message) => messageText.Text = message;
        public void ShowMessage() => this.Show();
        public void CloseMessage()
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
        }
            private void OkButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        }
}