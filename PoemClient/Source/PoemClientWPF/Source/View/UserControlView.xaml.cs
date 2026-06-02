using PoemClientWPF.Tools;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;

namespace PoemClientWPF.View
{
    public partial class UserControlView : System.Windows.Controls.UserControl
    {

        /*
         * UserControl qui va contenir des Views
         * _host : les controles activeX doivent être host sous Windows Forms pour être intégré à une interface WPF
         * _vue : controle activeX
         * _config : fichier de configuration
         * _gridView : Grid qui contient les Views dans MainWindow (= null dans fenêtre OpenView)
         * isNotFoundError : indique si une erreur a eu lieu à cause d'un fichier .view introuvable
         */
        private readonly WindowsFormsHost _host;
        private AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl _view;
        private readonly LogFile log;
        private readonly Config _config;
        private readonly Grid _gridView;
        private bool isNotFoundError = false;
        private readonly string viewFolder;
        private readonly MainWindow _main;

        public UserControlView(Grid gridView, MainWindow main)
        {
            InitializeComponent();
            this._config = Config.GetInstance("Config\\poemclient.config");
            if (_config == null)
            {
                return;
            }
            this.log = LogFile.GetInstance(this._config);
            this._host = new WindowsFormsHost();
            this._view = new AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl();
            _host.Child = _view;
            Content = _host;
            this._gridView = gridView;
            _main = main;
            viewFolder = _main.login.GetViewDirectory();
        }

        public AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl GetView()
        {
            return _view;
        }

        public void InitializeView()
        {
            ((System.ComponentModel.ISupportInitialize)(this._view)).BeginInit();
            this._view.Dock = System.Windows.Forms.DockStyle.Fill;
            this._view.Enabled = true;
            this._view.Location = new System.Drawing.Point(0, 0);
            this._view.Name = "view";
            this._view.TabIndex = 0;
            _view.Visible = false;
            _view.ObjectEvent += Test_object;
            ((System.ComponentModel.ISupportInitialize)(this._view)).EndInit();
            _view.Initialize("");
        }

        public void SetViewBestFit()
        {
            _view.SetBestFit(1);
            _host.Child = _view;
        }

        public void ShowView()
        {
            _view.Show();
            _host.Child = _view;
        }


        // JZ 20260528
        public void ReloadView()
        {
            _view.ReloadObject("");
        }

        public Task LoadView(string viewFile)
        {
            Console.WriteLine($"{_config.GetKeyValue("Loading")} view {viewFile}");
            return Task.Run(() =>
            {
                if (viewFile.Contains("http://") == false)
                {
                    viewFile = $"{_main.login.GetHttpServer()}:{_main.login.GetServerPort()}/{viewFolder + viewFile}";
                }

                if (isNotFoundError)
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Content = _host;
                    });
                    this.isNotFoundError = false;
                }

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    _host.Visibility = Visibility.Hidden;
                });

                switch (_view.LoadFile(viewFile))
                {
                    case 1:
                        log.WriteTrace($"View succeeded : {viewFile}");    // JZ
                        break;
                    default:
                        log.WriteTrace($"View failed : {viewFile}");
                        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                        {
                            _view.Dispose();
                            _view = new AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl();
                            _view.BeginInit();
                            _view.CreateControl();
                            _view.EndInit();
                            ViewNotFound();
                        });
                        break;
                }
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    _host.Visibility = Visibility.Visible;
                });
            });
        }

        private string BuildResourcePath(string resource)
        {
            string[] splittedRes = resource.Split('/');
            string resourcePath = _config.GetRessourcePath();
            Console.WriteLine(resourcePath);
            resourcePath = Path.Combine(resourcePath, splittedRes[1]);
            return resourcePath;
        }

        private void ViewNotFound()
        {
            Grid gridError = new Grid
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Image background = new Image();
            try
            {
                background.Source = new BitmapImage(new Uri(BuildResourcePath(_config.GetKeyValue("BackgroundImage"))));
            }
            catch
            {
                log.WriteTrace("Error : Background Image not found : " + _config.GetKeyValue("BackgroundImage"));
                System.Windows.Controls.Label lbl = new System.Windows.Controls.Label
                {
                    FontSize = 25,
                    Content = _config.GetKeyValue("FileNotFound")
                };
                gridError.Children.Add(lbl);
                Content = gridError;
                this.isNotFoundError = true;
            }
            background.Stretch = Stretch.UniformToFill;
            background.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            background.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            gridError.Children.Add(background);
            Content = gridError;
            this.isNotFoundError = true;
        }

        public bool DoesFileExistOnServer(string fileUrl)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                request.Method = "HEAD";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        public int Run_script(string file, string script, string path)
        {
            string buffer = new string('\0', 1000);

            if (DoesFileExistOnServer(path + file) == false)
            {
                System.Windows.MessageBox.Show(_config.GetKeyValue("NetworkError"));   // JZ 0506
                //MessageBox.Show($"{file} doesn't exist on the server");
                return 0;
            }

            int result = 0;

            using (var host = new Form())
            {
                host.ShowInTaskbar = false;
                host.WindowState = FormWindowState.Minimized;
                host.Opacity = 0;

                var view = new AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl();
                view.BeginInit();
                host.Controls.Add(view);
                view.EndInit();

                host.Show();

                view.LoadFile(path + file);
                result = view.RunScript(script, ref buffer, 1000);

                host.Close();
            }

            return result;
        }

        public void SetMouseMode(int modeValue)
        {
            _view.SetMouseMode(modeValue);
        }

        public void SetZoomCursor(int zoomValue)
        {
            _view.SetZoomCursor(zoomValue);
        }

        public void ViewNaturalBestFit(int BestFit, int NaturalScale)
        {
            _view.SetBestFit(BestFit);
            _view.SetNaturalScale(NaturalScale);
        }

        public void HideView()
        {
            _view.Hide();
            _host.Child = _view;
        }

        public void CloseView()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_view != null)
                    {
                        _view.ObjectEvent -= Test_object;

                        _view.Hide();

                        _host.Child = null;

                        _view.Dispose();
                        Marshal.FinalReleaseComObject(_view);
                        _view = null;
                    }
                }
                catch (Exception ex)
                {
                    log.WriteTrace("Error closing view: " + ex.Message);
                }
            });
        }

        private void Test_object(object sender, AxCOMPOEMGUICTRLLib._DComPoemGuiCtrlEvents_ObjectEventEvent e)
        {
            if (_gridView != null)
            {
                AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl ctrl = (AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl)sender;
                if (e.strMsg == "OK")
                {
                    for (int i = 0; i < _gridView.Children.Count; i++)
                    {
                        if (_gridView.Children[i] is UserControlView view)
                        {
                            string patternNotFixe = @"\d{3}$";
                            bool isNotFixe = Regex.IsMatch(view.GetView().GetFileName().Split('/')[view.GetView().GetFileName().Split('/').Length - 1].Split('.')[0], patternNotFixe);
                            if ((ctrl.GetFileName() != view.GetView().GetFileName()) && isNotFixe)
                            {
                                string test = view.GetView().GetFileName();
                                view.GetView().LoadFile(test);
                            }
                        }
                    }
                }
            }
        }

    }
}
