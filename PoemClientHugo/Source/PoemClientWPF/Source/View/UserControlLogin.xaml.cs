using AxCOMPOEMGUICTRLLib;
using Microsoft.VisualBasic.FileIO;
using PoemClient.Source.Tools;
using PoemClient.Tools;
using PoemClientWPF.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace PoemClientWPF.View
{
    public partial class UserControlLogin : UserControl
    {
        private readonly WindowsFormsHost _host;
        public AxCOMPOEMGUICTRLLib.AxComPoemGuiCtrl _login;
        private readonly LogFile log;
        private readonly Config config;
        private readonly IniFile inifile;
        
        private string USERID;
        private string PASSWORD;

        private string DNAME;
        private string DOWNER;
        private string DUID;
        private string DPWD;

        private string httpServer;

        private string serverIP;
        private string serverPort;

        private string clubID;

        private string permissionLogin;
        private string passwardDB;
        private string serverDirectory;  // JZ 0711
        private string httpView;
        private string httpDeclaration;
        private string clientConfigProgram;
        private string uploadDirectory;
        private string downloadDirectory;

        private bool isClosing = false;
        public bool shutdown = false; // si shutdown un delay et lancer dans la main window pour assurer l'execution de Application.Shutdown

        //----------------------------------------Getters/Setters--------------------------------
        public string GetIdUser() { return USERID; }
        public string GetPSWD() { return PASSWORD; }
        public string GetLoginPermission() { return permissionLogin; }
        public string GetDNAME() { return DNAME; }
        public string GetDOWNER() { return DOWNER; }
        public string GetDUID() { return DUID; }
        public string GetDPWD() { return DPWD; }
        //public void SetDllsMoved(bool m) { this.dllsMoved = m; }
        public bool IsClosingLogout() { return isClosing; }


        public string GetHttpServer() { return httpServer; }
        public string GetServerIP() { return serverIP; }
        public string GetServerPort() { return serverPort; }
        public string GetServerDirectory() { return serverDirectory; }      // JZ 0711
        public string GetViewDirectory() { return httpView; }
        public string GetClientConfig() { return clientConfigProgram; }
        public string GetUploadDirectory() { return uploadDirectory; }
        public string GetDownloadDirectory() { return downloadDirectory; }

        public UserControlLogin()
        {
                InitializeComponent();
            try
            {
                // Initialisation des services internes
                config = Config.GetInstance("Config\\poemclient.config");   // JZ
                inifile = IniFile.GetInstance();
                log = LogFile.GetInstance(config);

                // Instanciation dynamique du contrôle COM
                _host = new WindowsFormsHost();
                _login = new AxComPoemGuiCtrl();
                _host.Child = _login;

                ((System.ComponentModel.ISupportInitialize)(_login)).BeginInit();
                _login.AllowDrop = true;
                _login.Anchor = ((System.Windows.Forms.AnchorStyles)
                    ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
                _login.Enabled = true;
                _login.Location = new System.Drawing.Point(65, 61);
                _login.Name = "axComPoemGuiCtrl_Login";
                _login.Size = new System.Drawing.Size(86, 74);
                _login.TabIndex = 12;
                _login.Visible = false;
                ((System.ComponentModel.ISupportInitialize)(_login)).EndInit();

                // Ajout dynamique au conteneur XAML
                MainContainer.Children.Add(_host);

            }
            catch (COMException)
            {
                MessageBox.Show("Erreur : Un composant requis n'est pas installé. Veuillez exécuter StartView.bat avant de relancer le client.", "Erreur COM", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur innatendue s'est produite : \n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadLogin()
        {
            string scriptName = config.GetKeyValue("LoginScript").Split(',')[2], scriptNameAdapted;
            string scriptUri, clientDirectory, poemClientDirectory, serverViewRoot;
            string configConsoleLoginView, configServerIP, configServerPort, configClubId, configUser, configPassword, configPermission, configServerDirectory, configClientDirectory, configPoemClientDirectory, configNameDB, configOwnerDB, configUidDB, configPwdDB;        // JZ
       
            MessageBoxResult loginScriptLoading;
            int loginState;
            


            _login.Initialize("");

            clientDirectory = inifile.GetConfigValue("DIRECTORY");
            poemClientDirectory = Environment.CurrentDirectory + "/";     //inifile.GetConfigValue("POEMCLIENT");

            serverViewRoot = config.GetKeyValue("ServerViewRoot");


        LOG_IN:
            serverIP = inifile.GetConfigValue("IP");        // JZ 0616
            serverPort = inifile.GetConfigValue("PORT");

            scriptUri = "http://" + serverIP + ":" + serverPort + "/" + serverViewRoot + config.GetKeyValue("LoginScript").Split(',')[1];  // JZ 0610


            log.WriteTrace("Login: " + scriptUri);

            // Login View file
            loginScriptLoading = (MessageBoxResult)_login.LoadFile(scriptUri);

            configUser = config.GetKeyValue("LoginUserId");
            configPassword = config.GetKeyValue("LoginPassword");

            configServerIP = config.GetKeyValue("ServerIP");
            configServerPort = config.GetKeyValue("ServerPort");

            configConsoleLoginView = config.GetKeyValue("ConsoleLoginView");

            if (Convert.ToBoolean(config.GetKeyValue("ConsoleLogin")) == true)
            {
               string consoleUri = $"http://{inifile.GetConfigValue("IP")}:{inifile.GetConfigValue("PORT")}/{config.GetKeyValue("ServerViewRoot") + configConsoleLoginView}";  // JZ 0610

                log.WriteTrace("Console login view: " + consoleUri);
                _login.LoadFile(consoleUri);

                try
                {
                    //string[] argv = Environment.GetCommandLineArgs(); // enveler le commentaire pour tester avec PoemClient.bat
                    string[] argv = new string[] { "PoemClient.exe", "PoemClient:\\USERID=000|PASSWORD=titi|PERMISSION=Admin" }; // test pour debug PoemClient.bat

                    string uid = argv[1].Split('\\')[argv[1].Split('\\').Length - 1].Split('|')[0].Split('=')[1];
                    string pwd = argv[1].Split('\\')[argv[1].Split('\\').Length - 1].Split('|')[1].Split('=')[1];

                    log.WriteTrace("Set console arguments: " + uid + " " + pwd);

                    _login.SetVariableValue(configUser, scriptName, uid);
                    _login.SetVariableValue(configPassword, scriptName, pwd);
                }
                catch { }
            }
            else
            {
                log.WriteTrace("User interface login");
            }


            USERID = _login.GetVariableValue(configUser, scriptName);
            PASSWORD = _login.GetVariableValue(configPassword, scriptName);


            log.WriteTrace("Login: " + USERID + " " + PASSWORD + " " + serverIP + " " + serverPort);


            string messageLogin = new string('\0', 5000);


            scriptNameAdapted = "[ID=" + scriptName + ";SERVERIP=" + serverIP + ";SERVERPORT=" + serverPort + "]";
            log.WriteTrace("Login script: " + scriptNameAdapted);


            _login.SetVariableValue(configServerIP, scriptName, serverIP);
            _login.SetVariableValue(configServerPort, scriptName, serverPort);


            // Run Login script
            loginState = _login.RunScript(scriptNameAdapted, ref messageLogin, 5000);


            serverIP = _login.GetVariableValue(configServerIP, scriptName);
            serverPort = _login.GetVariableValue(configServerPort, scriptName);


            log.WriteTrace("Login message: " + messageLogin);


            // Login View file
            switch (loginState)
            {
                case 1:     // success
                    log.WriteTrace("Login succeeded");

                    try
                    {
                        List<string> arguments = new List<string>();

                        using (TextFieldParser parser = new TextFieldParser(new StringReader(messageLogin.TrimEnd('\0'))) { TextFieldType = FieldType.Delimited, Delimiters = new string[] { "\t" } })
                        {
                            foreach (string field in parser.ReadFields())
                            {
                                arguments.Add(field.Remove(0, field.IndexOf('=') + 1).TrimEnd('\t'));
                            }
                        }


                        // GetLoginConfig
                        clubID              = arguments[0];

                        serverIP            = arguments[1];
                        serverPort          = arguments[2];
                        permissionLogin     = arguments[3];
                        passwardDB          = arguments[4];
                        serverDirectory     = arguments[5];     // JZ 0711

                        httpView            = arguments[6];
                        httpDeclaration     = arguments[7];

                        clientConfigProgram = arguments[8];
                        uploadDirectory     = arguments[9];
                        downloadDirectory   = arguments[10];     // removed '\n' and set it to '\t' in ERP\Model\Common\Interface\GetLoginConfig.n


                        httpServer = "http://" + serverIP;


                        log.WriteTrace("Load view declaration");


                        using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(int.Parse(config.GetKeyValue("TimeOutLogin"))) })
                        {
                            try
                            {
                                string httpviewdeclaration = $"{httpServer}:{serverPort}/{httpDeclaration}";

                                if (client.GetAsync(httpviewdeclaration).Result.IsSuccessStatusCode == false)
                                {
                                    throw new FileNotFoundException();
                                }
                            }
                            catch (FileNotFoundException exception)
                            {
                                throw new Exception($"{httpServer}:{serverPort} - {httpDeclaration} : {exception.Message}");
                            }
                            catch (Exception)
                            {
                                throw new Exception($"{httpServer.Replace("http://", "")}:{serverPort} - {new TimeoutException().Message}");
                            }
                        }


                        string viewDeclarationUri = $"{httpServer}:{serverPort}/{httpDeclaration}";

                        log.WriteTrace("View declaration: " + viewDeclarationUri);


                        _login.LoadFile(viewDeclarationUri);
                        Content = null;

                        configPermission = config.GetKeyValue("LoginPermission");

                        configNameDB = config.GetKeyValue("DatabaseName"); 
                        configOwnerDB = config.GetKeyValue("DatabaseOwner");
                        configUidDB = config.GetKeyValue("DatabaseUser");
                        configPwdDB = config.GetKeyValue("DatabasePassword");

                        configServerIP = config.GetKeyValue("ServerIP");
                        configServerPort = config.GetKeyValue("ServerPort");

                        configClubId = config.GetKeyValue("ClubId");

                        configServerDirectory = config.GetKeyValue("ServerDirectory");              // JZ 0711

                        configClientDirectory = config.GetKeyValue("ClientDirectory");
                        configPoemClientDirectory = config.GetKeyValue("PoemClientDirectory");


                        // Reconfigurate view variables
                        _login.SetVariableValue(configServerDirectory, scriptName, serverDirectory);    // JZ 0711

                        _login.SetVariableValue(configPwdDB, scriptName, passwardDB);
                        _login.SetVariableValue(configPermission, scriptName, permissionLogin);

                        _login.SetVariableValue(configClubId, scriptName, clubID);
                        _login.SetVariableValue(configServerIP, scriptName, serverIP);
                        _login.SetVariableValue(configServerPort, scriptName, serverPort);

                        _login.SetVariableValue(configClientDirectory, scriptName, clientDirectory);
                        _login.SetVariableValue(configPoemClientDirectory, scriptName, poemClientDirectory);


                        DNAME = _login.GetVariableValue(configNameDB, scriptName);  
                        DOWNER = _login.GetVariableValue(configOwnerDB, scriptName); 
                        DUID = _login.GetVariableValue(configUidDB, scriptName);
                        DPWD = _login.GetVariableValue(configPwdDB, scriptName);

                        return;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        log.WriteTrace($"Login exception : Could not retrieve data from server");
                        MessageBox.Show(config.GetKeyValue("PoemViewError"), config.GetKeyValue("SystemError"), MessageBoxButton.OK, MessageBoxImage.Error);

                        //Application.Current.Shutdown(0);    // JZ 0603
                    }
                    catch (Exception exception)
                    {
                        log.WriteTrace($"Login exception : {exception.Message}");
                        MessageBox.Show(exception.Message, config.GetKeyValue("PoemViewError"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;

                case 0:     // failure
                    log.WriteTrace($"Login failed : {messageLogin.Trim('\0')}");
                    //MessageBox.Show(messageLogin.Trim('\0'), config.GetKeyValue("PoemViewError"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;  // JZ

                case -4:     // cancelled
                    log.WriteTrace($"Login cancelled : {messageLogin.Trim('\0')}");
                    break;

                default:    // exception
                    log.WriteTrace($"Server error : {inifile.GetConfigValue("IP")}:{inifile.GetConfigValue("PORT")} - {config.GetKeyValue("ServerError")}");
                    MessageBox.Show($"{inifile.GetConfigs()["IP"]}:{inifile.GetConfigs()["PORT"]} - {config.GetKeyValue("ServerError")}", config.GetKeyValue("PoemViewError"), MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }

            messageLogin.Trim('\0');

            _login.GlobalCleanup();


            if (loginScriptLoading == MessageBoxResult.OK &&
                loginState != -4 && /* canceled */
                Convert.ToBoolean(config.GetKeyValue("ConsoleLogin")) != true)
            {
                goto LOG_IN;
            }
            // Though failed, continue to login


            log.WriteTrace("Login aborted, exiting...");
            shutdown = true;

            if (Application.Current != null)        // JZ 0603
            {
                Application.Current.Shutdown(0);
            }
        }


        // unused
        public void LoadLogout()
        {
            string[] logoutScript = config.GetKeyValue("LogoutScript").Split(',');
            string errorMessage = new string('\0', 1000);

            log.WriteTrace("LogoutScript '" + logoutScript + "' identified");

            try
            {
                // server open = 1, 0 otherwise
                if (_login.LoadFile($"{httpServer}:{serverPort}/{httpView}/{logoutScript[1]}") == 0)
                {
                    Application.Current.Shutdown(0);
                }

                switch (_login.RunScript(logoutScript[2], ref errorMessage, 1000))
                {
                    case 0:
                        Application.Current.Shutdown(0);
                        break;
                    default:
                        MessageBox.Show(errorMessage, config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                        Process.GetCurrentProcess().Kill();
                        break;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, config.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadLogout(CancelEventArgs e)
        {
            bool cancel = false;
            int isPoemServerOpen = 69, isLogout;

            string[] logoutScript = config.GetKeyValue("LogoutScript").Split(',');
            string messageLogout = config.GetKeyValue("ServerError");


            log.WriteTrace("LogoutScript '" + logoutScript + "' identified");

            try
            {
                isPoemServerOpen = _login.LoadFile(httpServer + ":" + serverPort + "/" + httpView + "/" + logoutScript[1]);
            }
            catch (Exception ex)
            {
                log.WriteTrace("Error : " + ex.Message);
                isClosing = true;
                Application.Current.Shutdown(0);
            }
            try
            {
                string msgRun = new string('*', 1001);

                isLogout = _login.RunScript(logoutScript[2], ref msgRun, 1000);
                _login.Hide();

                if (isLogout == -1 && isPoemServerOpen == 0)
                {
                    MessageBox.Show(messageLogout, config.GetKeyValue("SystemError"), MessageBoxButton.OK, MessageBoxImage.Error);  // JZ
                    
                    log.WriteTrace("Logout exception");
                    isClosing = true;
                    Application.Current.Shutdown(0);
                }

                else if (isLogout == 1 && isPoemServerOpen == 1 || isPoemServerOpen == 0)
                {
                    log.WriteTrace("Logout succeeded");
                    
                    isClosing = true;
                    Application.Current.Shutdown(0);
                }

                else if (isLogout == 0 && isPoemServerOpen == 1)
                {
                    cancel = true;
                    log.WriteTrace("Logout canceled");
                    if (e != null)
                        e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(messageLogout, config.GetKeyValue("SystemError"), MessageBoxButton.OK, MessageBoxImage.Error);

                log.WriteTrace("Logout exception : " + ex.Message);
                isClosing = true;
                Application.Current.Shutdown(0);
            }

            //Forced logout si serveur mort
            if (isPoemServerOpen != 0 && e != null && !isClosing && !cancel)
            {
                log.WriteTrace("Logout forced");
                e.Cancel = true;
                Application.Current.Shutdown(0);
            }
        }

        private string GetLocalIP()
        {
            string ip = string.Empty;

            foreach (IPAddress ipa in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ip = ipa.ToString();
                }
            }
            return ip;
        }
    }
}
