using MahApps.Metro.Controls;
using PoemClientWPF;
using PoemClientWPF.View;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using static PoemClientWPF.MainWindow;

namespace PoemClient.Source.View
{
    public partial class HereMatrix : Window
    {
        private MainWindow _main;
        private ProgramBox ProgramBox;
        public HereMatrix (MainWindow main)
        {
            InitializeComponent();
            _main = main;
            this.Topmost = false;
            this.ResizeMode = ResizeMode.NoResize;
            ProgramBox = new ProgramBox(_main.clientConfig, "GetTypeResourceTypeRisk", _main, GetDistances , "Calcul");
            contentGrid.Children.Add(ProgramBox);
            this.Closing += Window_Closing;
            this.Title = MainWindow.config.GetKeyValue("HereMapsMatrix");
        }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
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

        private async void GetDistances()
        {
            if (!PoemClient.Source.Tools.Utils.IsInternetAvailable())
            {
                _main.MessageBoxError(MainWindow.config.GetKeyValue("NetworkError"));
                return;
            }
            this.Hide();
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
                return;

            try
            {
                List<String> selected = ProgramBox.GetSelectedItems();

                if (selected == null || selected.Count < 2 ||
                    string.IsNullOrWhiteSpace(selected[0]) || string.IsNullOrWhiteSpace(selected[1]))
                {
                    _main.MessageBoxError(MainWindow.config.GetKeyValue("InputError"), true);
                    return;
                }

                MessageBoxResult serviceResult = MessageBox.Show(this, MainWindow.config.GetKeyValue("ServiceNotFree"), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (serviceResult == MessageBoxResult.No)
                    return;

                string buffer = _main.login.GetServerPort() + "%20" + _main.GetUserId() + "%20";
                string s0 = EncodeForNCL(selected[0]);
                string s1 = EncodeForNCL(selected[1]);
                buffer += $@"""{s0}""%20""{s1}""";
                string[] resultResource = _main.GetQueries(ProgramBox.Program.nclProgram, buffer);

                string[] splitResult = resultResource[1].Substring(16).Replace("'", "").Split();

                string idTypeResource;
                string idTypeRisk;
                double length;
                double width;
                double height;
                double maxWeight;
                string categoryTunnel;
                int nbTrailer;

                try
                {
                    idTypeResource = splitResult[0];
                    idTypeRisk = splitResult[1];
                    length = double.Parse(splitResult[2], CultureInfo.InvariantCulture) * 100;
                    width = double.Parse(splitResult[3], CultureInfo.InvariantCulture) * 100;
                    height = double.Parse(splitResult[4], CultureInfo.InvariantCulture) * 100;
                    maxWeight = double.Parse(splitResult[5], CultureInfo.InvariantCulture) * 100;
                    categoryTunnel = splitResult[6];
                    nbTrailer = int.Parse(splitResult[7]);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(_main, MainWindow.config.GetKeyValue("DataError") + ": xxx", MainWindow.config.GetKeyValue("DataError"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dynamicBox = new MessageForm(MainWindow.config.GetKeyValue("HereMapsMatrix"), MainWindow.config.GetKeyValue("Loading"), _main);   // JZ 0723
                dynamicBox.okButton.Visibility = Visibility.Collapsed;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    dynamicBox.ShowMessage();
                });

                string hereTransport = "transportMode=truck";
                string hereTruckInfo = $"&return=summary&truck[length]={length}&truck[width]={width}&truck[height]={height}&truck[grossWeight]={maxWeight}";

                string[] hazard = { "combustible", "corrosive", "explosive", "flammable", "gas", "organic", "poison", "radioactive", "harmfulToWater", "other", "poisonousInhalation" };
                string hazardousGoods = !hazard.Contains(idTypeRisk) ? "" : idTypeRisk;

                if (categoryTunnel != "") hereTruckInfo += "&truck[tunnelCategory]=" + categoryTunnel;
                if (hazardousGoods != "") hereTruckInfo += "&truck[shippedHazardousGoods]=" + hazardousGoods;
                if (nbTrailer > 0) hereTruckInfo += "&truck[trailerCount]=" + nbTrailer;

                string hereApiInfo = $"&apiKey={apiKey}";

                string[] values = _main.GetQueries(MainWindow.GetLineBelowParameter(_main.clientConfig, "ClientGetSite"), _main.login.GetServerPort() + "%20" + _main.GetUserId());
                string input = values[1];
                int index = input.IndexOf("MESSAGE=");
                string messagePart = input.Substring(index + "MESSAGE=".Length);
                if (messagePart.Contains("failure"))
                {
                    _main.MessageBoxError(MainWindow.config.GetKeyValue("DatabaseError"), true);
                    return;
                }
                values = values.Skip(2).Take(values.Length - 7).ToArray();

                List<AddressData> siteMatrix = _main.GetSiteMatrix(values);
                List<AddressData> siteSelected = _main.GetSiteSelected(values);
                List<AddressData> siteAll = siteMatrix.Concat(siteSelected).ToList();
                List<DistanceData> listDistance = new List<DistanceData>();

                int limit = int.Parse(MainWindow.GetLineBelowParameter(_main.clientConfig, "maxSitePacketSiteSiteTable"));

                int nbSite = siteAll.Count;
                int limitSite = nbSite * limit;
                int siteNumber = 0;
                int totalRequests = siteSelected.Count + siteMatrix.Count;

                await Task.Run(async () =>
                {
                    DateTime lastRequestTime = DateTime.MinValue;
                    const int delayBetweenRequestsMs = 250;
                    int currentSite = 0;
                    int cpt = 0;

                    foreach (var siteS in siteSelected)
                    {
                        currentSite++;
                        siteNumber++;
                        cpt = 0;
                        foreach (var siteM in siteAll)
                        {
                            cpt++;
                            if (dynamicBox.Token.IsCancellationRequested) return;
                            TimeSpan timeSinceLastRequest = DateTime.Now - lastRequestTime;
                            if (timeSinceLastRequest.TotalMilliseconds < delayBetweenRequestsMs)
                            {
                                await Task.Delay(delayBetweenRequestsMs - (int)timeSinceLastRequest.TotalMilliseconds);
                            }

                            string coord1 = siteS._lat.Replace(",", ".") + "," + siteS._lng.Replace(",", ".");
                            string coord2 = siteM._lat.Replace(",", ".") + "," + siteM._lng.Replace(",", ".");
                            string url = "https://router.hereapi.com/v8/routes?" + hereTransport + "&origin=" + coord1 + "&destination=" + coord2 + hereTruckInfo + hereApiInfo;

                            Console.WriteLine("url: " + url);

                            lastRequestTime = DateTime.Now; // Track this call time

                            string result = GetDistanceFromHere(url);

                            if (result.Contains("Error"))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _main.MessageBoxError(MainWindow.config.GetKeyValue("ApiError"), true);
                                    dynamicBox.CloseMessage();
                                });
                                return;
                            }

                            string[] split = result.Split(' ');
                            string distance = split[0];
                            string time = split[1];

                            listDistance.Add(new DistanceData(siteS._id, siteM._id, time, distance));

                            if (listDistance.Count >= limitSite)
                            {
                                bool success = _main.SendData(listDistance, nbSite, siteNumber, idTypeResource, idTypeRisk);
                                siteNumber = 0;
                                if (!success)
                                {
                                    _main.MessageBoxError(MainWindow.config.GetKeyValue("ServerError"), true);
                                    dynamicBox.CloseMessage();
                                    return;
                                }
                                listDistance.Clear();
                            }


                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                dynamicBox.SetMessage($"{MainWindow.config.GetKeyValue("Progress")} : {siteSelected.Count - currentSite + 1}x{siteAll.Count}");
                            });
                        }
                    }

                    if (listDistance.Count > 0)
                    {
                        bool success = _main.SendData(listDistance, nbSite, siteNumber, idTypeResource, idTypeRisk);
                        if (!success)
                        {
                            _main.MessageBoxError(MainWindow.config.GetKeyValue("ServerError"), true);
                        }
                        listDistance.Clear();
                    }

                    siteNumber = 0;
                    currentSite = 0;
                    limitSite = siteSelected.Count * limit;
                    nbSite = siteSelected.Count;

                    foreach (var siteS in siteMatrix)
                    {
                        currentSite++;
                        siteNumber++;
                        foreach (var siteM in siteSelected)
                        {
                            if (dynamicBox.Token.IsCancellationRequested) return;
                            TimeSpan timeSinceLastRequest = DateTime.Now - lastRequestTime;
                            if (timeSinceLastRequest.TotalMilliseconds < delayBetweenRequestsMs)
                            {
                                await Task.Delay(delayBetweenRequestsMs - (int)timeSinceLastRequest.TotalMilliseconds);
                            }

                            string coord1 = siteS._lat.Replace(",", ".") + "," + siteS._lng.Replace(",", ".");
                            string coord2 = siteM._lat.Replace(",", ".") + "," + siteM._lng.Replace(",", ".");
                            string url = "https://router.hereapi.com/v8/routes?" + hereTransport + "&origin=" + coord1 + "&destination=" + coord2 + hereTruckInfo + hereApiInfo;

                            Console.WriteLine("url: " + url);

                            lastRequestTime = DateTime.Now; // Track this call time

                            string result = GetDistanceFromHere(url);

                            if (result.Contains("Error"))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _main.MessageBoxError(MainWindow.config.GetKeyValue("ApiError"), true);
                                    dynamicBox.CloseMessage();
                                });
                                return;
                            }

                            string[] split = result.Split(' ');
                            string distance = split[0];
                            string time = split[1];

                            listDistance.Add(new DistanceData(siteS._id, siteM._id, time, distance));

                            if (listDistance.Count >= limitSite)
                            {
                                bool success = _main.SendData(listDistance, nbSite, siteNumber, idTypeResource, idTypeRisk);
                                siteNumber = 0;
                                if (!success)
                                {
                                    _main.MessageBoxError(MainWindow.config.GetKeyValue("ServerError"), true);
                                    dynamicBox.CloseMessage();
                                    return;
                                }
                                listDistance.Clear();
                            }


                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                dynamicBox.SetMessage($"{MainWindow.config.GetKeyValue("Progress")} : {siteMatrix.Count - currentSite + 1}x{siteSelected.Count}");
                            });
                        }
                    }

                    if (listDistance.Count > 0)
                    {
                        bool success = _main.SendData(listDistance, nbSite, siteNumber, idTypeResource, idTypeRisk);
                        if (!success)
                        {
                            _main.MessageBoxError(MainWindow.config.GetKeyValue("ServerError"), true);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        dynamicBox.SetMessage(MainWindow.config.GetKeyValue("Terminated"));
                        //MessageBox.Show(_main.config.GetKeyValue("Terminated"), _main.config.GetKeyValue("HereMapsMatrix"), MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                MessageBox.Show($"{MainWindow.config.GetKeyValue("DatabaseError")}:\n{ex.Message}", MainWindow.config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);  // JZ 0403
                //_main.MessageBoxError(_main.config.GetKeyValue("DatabaseError"), true);
            }
        }

    private string GetDistanceFromHere(string url)
    {
        using (WebClient client = new WebClient())
        {
            try
            {
                string response = client.DownloadString(url);
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var distanceValue = doc.RootElement
                                           .GetProperty("routes")[0]
                                           .GetProperty("sections")[0]
                                           .GetProperty("summary")
                                           .GetProperty("length")
                                           .GetInt32();

                    var durationValue = doc.RootElement
                                           .GetProperty("routes")[0]
                                           .GetProperty("sections")[0]
                                           .GetProperty("summary")
                                           .GetProperty("duration")
                                           .GetInt32();

                    return $"{distanceValue} {durationValue}";
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"Erreur de connexion : {ex.Message}");
                return "Error";
            }
        }
    }
}
}
