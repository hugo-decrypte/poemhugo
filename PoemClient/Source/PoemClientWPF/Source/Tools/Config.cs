using PoemClient.Tools;
using PoemClient.View;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;

namespace PoemClientWPF.Tools
{
    public class Config
    {
        private readonly string configPath;
        private Configuration configuration;
        private static Config instance = null;
        public readonly IniFile iniFile = null;
        public bool cancel = false;
        private static bool isConfigChosen = false;

        private Config(string path)
        {
            this.iniFile = IniFile.GetInstance();
            this.configPath = path;
            var savedConfig = iniFile.GetConfigValue("save");
            try
            {
                if (iniFile.GetConfigValue("save") != "")
                {
                    iniFile.SetCurrent(savedConfig);
                    InitializeConfigFile(savedConfig);
                    this.configPath = savedConfig;
                    return;
                }
                if (isConfigChosen == false)
                {
                    if (new SelectConfig(iniFile).ShowDialog() == true)
                    {
                        string selectedFilePath = iniFile.GetCurrentConfig();

                        try
                        {
                            Console.WriteLine(selectedFilePath);
                            using (XmlReader reader = XmlReader.Create(selectedFilePath))
                            {
                                while (reader.Read()) { }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Invalid config file : " + ex.Message, this.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                            Process.GetCurrentProcess().Kill();
                        }
                        this.configPath = selectedFilePath;
                        this.cancel = false;
                        InitializeConfigFile(selectedFilePath);
                        isConfigChosen = true;
                    }
                    else
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                }
                else
                {
                    this.cancel = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void InitializeConfigFile(string pathDir)
        {
            if (File.Exists(pathDir) == false)
            {
                try
                {
                    if (File.Exists(pathDir) == false) {
                        throw new FileNotFoundException();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"{exception.Message} ({pathDir})", "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
                    Process.GetCurrentProcess().Kill();
                }
            }

            try
            {
                this.configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = pathDir }, ConfigurationUserLevel.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Config ReloadInstance(string path)
        {
            InitializeConfigFile(this.configPath);
            return GetInstance(path);
        }

        public static Config GetInstance(string path)
        {
            if (instance == null)
            {
                instance = new Config(path);
            }
            return instance;
        }

        public static string ExtractExceptionDetail(string exceptionMessage)
        {
            int startIndex = exceptionMessage.IndexOf('(');
            int endIndex = exceptionMessage.LastIndexOf(')');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                string detail = exceptionMessage.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                return detail;
            }

            return string.Empty;
        }
        public string GetKeyValue(string key)
        {
            try
            {
                string parsedvalue = null;
                if (configuration.AppSettings.Settings.AllKeys.Contains(key))
                {
                    parsedvalue = configuration.AppSettings.Settings[key].Value.Replace("€", "");
                    return parsedvalue;
                }
                else
                {
                    /*
                    IniFile ini = IniFile.GetInstance();
                    string tmp = ini.GetCurrentConfig();
                    MessageBox.Show(iniFile.GetConfigValue("TAG_KeyError") + " \"" + key + "\"\n(" + tmp + ")", iniFile.GetConfigValue("TAG_CONFIGERROR"), MessageBoxButton.OK, MessageBoxImage.Error);

                    Application.Current.Shutdown();
                    Process.GetCurrentProcess().Kill();
                    */
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(iniFile.GetConfigValue("TAG_BOOTERROR") + " : " + configPath.ToString().Split('\\')[configPath.ToString().Split('\\').Length - 1] + "\n" + "Line " + ExtractExceptionDetail(ex.Message).ToString().Split(' ')[2], iniFile.GetConfigValue("TAG_CONFIGERROR"), MessageBoxButton.OK, MessageBoxImage.Error);

                Application.Current.Shutdown();
                Process.GetCurrentProcess().Kill();
                return null;
            }
        }

        public string[] GetAllKeys()
        {
            return configuration.AppSettings.Settings.AllKeys;
        }

        public string GetKeyValueMenu(string key)
        {
            try
            {
                string parsedvalue = null;
                if (configuration.AppSettings.Settings.AllKeys.Contains(key)) {
                    parsedvalue = configuration.AppSettings.Settings[key].Value.Replace("€", "");
                    return parsedvalue;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception catch : " + ExtractExceptionDetail(ex.Message), "Config File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                Process processes = Process.GetCurrentProcess();
                processes.Kill();
                return null;
            }
        }

        public void SaveConfig()
        {
            configuration.Save();
        }

        public void SetKeyValueKeepLineBreak(string key, string value)
        {
            configuration.Save();
            string configFileContent = File.ReadAllText(configPath);
            string line = GetLineConfig(configFileContent, key);
            configFileContent = configFileContent.Replace(line, line.Replace("value=\"" + GetKeyValue(key) + "\"", "value=\"" + value + "\""));
            File.WriteAllText(configPath, configFileContent);
            InitializeConfigFile(configPath);
        }

        private string GetLineConfig(string content, string key)
        {
            string[] splitted = content.Split('\n');
            for (int i = 0; i < splitted.Length; i++)
            {
                if (splitted[i].Contains(key) && splitted[i].Contains(GetKeyValue(key)))
                    return splitted[i];
            }
            return null;
        }

        public string GetRessourcePath()
        {
            string initialPath = iniFile.GetConfigValue("RESOURCE");
            string path = "";
            if (Directory.Exists(initialPath))
            {
                return initialPath;
            }
            path = AppDomain.CurrentDomain.BaseDirectory + iniFile.GetResourceDirname();
            if (!Directory.Exists(path))
            {
                MessageBox.Show($"{this.GetKeyValue("PathError")}:\n{initialPath}", this.GetKeyValue("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
            return path;
        }

        public string GetIniValue(string key)
        {
            return iniFile.FindKeyValue(key);
        }

        // Returns the code corresponding to the language (ex : French => fr)
        public string GetLanguageCode()
        {
            string[] language_list = GetKeyValue("LanguageList").Split(',');
            int language_index = Array.IndexOf(language_list, GetKeyValue("CurrentLanguage"));
            return GetKeyValue("LanguageCodes").Split(',')[language_index];
        }
    }
}
