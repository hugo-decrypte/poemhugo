using System;
using System.IO;
using System.Windows;

namespace PoemClientWPF.Tools
{
    public class LogFile
    {
        private static LogFile instance = null;
        private readonly string logFilename;
        private readonly string logDirectory;
        private readonly Config config;
        private bool log = true;
        private string logPath;

        private LogFile(Config conf)
        {
            this.config = conf;
            this.logPath = config.iniFile.FindKeyValue("LOG");
            if (this.logPath == null || this.logPath == "") {
                log = false;
                return;
            }
            this.logDirectory = Path.GetDirectoryName(this.logPath);
            this.logFilename = Path.GetFileName(this.logPath);
            InitializeLog();
        }

        public static LogFile GetInstance(Config conf)
        {
            if (instance == null)
                instance = new LogFile(conf);
            return instance;
        }

        public static void DeleteInstance()
        {
            instance = null;
        }

        private void InitializeLog()
        {
            if (logFilename == null || logFilename == "")
            {
                log = false;
                return;
            }
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                CreateLogFile(logDirectory);
            }
            catch(Exception)
            {
                MessageBox.Show("Can't find a part of the Log directory path : "+ logDirectory + ". Please Check ClientLogDirectory in the configuration file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateLogFile(string directory)
        {
            try
            {
                if (!File.Exists(Path.Combine(directory,logFilename)))
                {
                    using (File.Create(Path.Combine(directory, logFilename))) {}
                }
                File.WriteAllText(Path.Combine(directory, logFilename), string.Empty);
            }
            catch(Exception)
            {
                MessageBox.Show("Can't find a part of the Log file path : " + Path.Combine(directory, logFilename) + ". Please Check ClientLogDirectory in the configuration file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public void WriteTrace(string trace)
        {
            if (log == false)
            {
                return;
            }
            try
            {
                using (StreamWriter sw = new StreamWriter(Path.Combine(logDirectory, logFilename), true) { AutoFlush = true })
                {
                    sw.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - " + trace);
                }
            }
            catch { }
        }
    }
}
