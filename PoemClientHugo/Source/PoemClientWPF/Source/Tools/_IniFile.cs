using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PoemClient.Tools
{
    public class IniFile
    {
        private static IniFile instance = null;
        private const string iniFileName = "poemclient.ini";
        private readonly Dictionary<string,string> configs;
        private string currentEnv;
        private string currentConfig;

        private IniFile()
        {
            this.configs = new Dictionary<string, string>();
            ReadIniFile();
        }

        public static IniFile GetInstance()
        {
            if (instance == null)
                instance = new IniFile();
            return instance;
        }

        public Dictionary<string, string> GetConfigs() { return configs; }

        public string GetCurrentEnv() { return currentEnv; }
        public string GetCurrentConfig() { return currentConfig; }
        public void SetCurrent(string conf) {
            this.currentConfig = conf;
            this.currentEnv = GetPathUntilDirectory(conf,"Config");
        }

        public void SetLock(bool isLocked)
        {
            UpdateValue("Save", isLocked.ToString());
        }

        public void UpdateCurrent()
        {
            string oldCurrent = configs["Current"];
            string changedKey = FindValue(this.currentConfig);
            if (changedKey != null)
            {
                UpdateValue("Current", this.currentConfig);
                UpdateValue(changedKey, oldCurrent);
            }
        }

        private string GetLastEltKey()
        {
            return configs.Keys.LastOrDefault();
        }

        private string FindValue(string value)
        {
            foreach (KeyValuePair<string, string> entry in configs)
            {
                if (entry.Key != "Save")
                {
                    if (entry.Value == value)
                        return entry.Key;
                }
            }
            return null;
        }

        private void ReadIniFile()
        {
            string currentSection = null;
            int num = 1;
            string config_n = "config_" + num.ToString();
            string key;
            string value;
            try
            {
                foreach (var line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory + "/" + iniFileName))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                    }
                    else if (currentSection.ToLower() == "config")
                    {
                        int equalSignIndex = line.IndexOf('=');
                        if (equalSignIndex > 0)
                        {
                            key = line.Substring(0, equalSignIndex).Trim();
                            value = line.Substring(equalSignIndex + 1).Trim();
                            this.configs.Add(key, value);
                        }
                    }
                    else if (currentSection.ToLower() == "files")
                    {
                        if (num == 1)
                        {
                            key = "Current";
                        }
                        else
                            key = config_n;
                        value = line.Trim();
                        this.configs.Add(key, value);
                        if (key.ToLower() == "current")
                        {
                            this.currentConfig = value;
                            try
                            {
                                this.currentEnv = GetPathUntilDirectory(currentConfig, "Config");
                            }
                            catch (ArgumentException ae)
                            {
                                Console.WriteLine(ae);
                                MessageBox.Show("It looks like you don't have Config directory in your environment");
                            }
                        }
                        num++;
                        config_n = "config_" + num.ToString();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "PoemClient", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
        }

        private string GetPathUntilDirectory(string fullPath, string targetDirectory)
        {
            string[] splittedFull = fullPath.Split('\\');
            string pathUntilDirectory = string.Empty;
            bool found = false;
            for(int i = 0; i < splittedFull.Length; i++)
            {
                if (splittedFull[i] == targetDirectory)
                {
                    found = true;
                    break;
                }
                else
                {
                    pathUntilDirectory+= splittedFull[i] + "\\";
                }
            }
            pathUntilDirectory = pathUntilDirectory.Substring(0, pathUntilDirectory.Length - 1);//enleve le dernier '\'

            if (found)
            {
                return pathUntilDirectory;
            }
            else
            {
                throw new ArgumentException($"Le répertoire spécifié '{targetDirectory}' n'a pas été trouvé dans le chemin donné.");
            }
        }

        public void UpdateValue(string key, string value)
        {
            try
            {
                var lines = File.ReadAllLines(iniFileName);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                    {
                        int equalSignIndex = line.IndexOf('=');
                        if (equalSignIndex > 0)
                        {
                            string existingKey = line.Substring(0, equalSignIndex).Trim();
                            if (existingKey == key)
                            {
                                lines[i] = $"{key}={value}";
                                configs[key] = value;
                                break;
                            }
                        }
                    }
                }
                File.WriteAllLines(iniFileName, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur s'est produite : " + ex.Message);
            }
        }

        public void AddLine(string value)
        {
            if (!configs.ContainsValue(value))
            {
                try
                {
                    string key = string.Empty;
                    if (configs.Count == 1)
                        key = "Current";
                    else
                    {
                        int newId = 2;
                        if (configs.Count > 2)
                            newId = Convert.ToInt32(GetLastEltKey().Split('_')[1]) + 1;
                        key = "Config_" + newId;
                    }
                    if (!configs.ContainsKey(key))
                    {
                        string FileContent = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + iniFileName);
                        if (FileContent[FileContent.Length - 1] != '\n')
                        {
                            using (StreamWriter writer = new StreamWriter(iniFileName, true))
                            {
                                writer.WriteLine("\n" + value);
                            }
                            this.configs.Add(key, value);
                        }
                        else
                        {
                            using (StreamWriter writer = new StreamWriter(iniFileName, true))
                            {
                                writer.WriteLine(value);
                            }
                            this.configs.Add(key, value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Une erreur s'est produite : " + ex.Message);
                }
            }
        }
    }
}
