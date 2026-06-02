using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace PoemClient.Source.Tools
{
    /// <summary>
    /// A class that allows to run a python script and listen to its output
    /// Python app MUST have a venv folder in order to be executed
    /// </summary>
    public class PythonScript
    {
        public event DataReceivedEventHandler DataReceived;
        protected Process process;
        private string path;
        private string exec;
        private string args;
        public PythonScript(string path, string exec, string args)
        {
            this.path = path;
            this.exec = exec;
            this.args = args;
        }

        protected virtual void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("[PYTHON DEBUG] : " + e.Data);
            DataReceived.Invoke(this, e);
        }

        private void createProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    path,
                    "venv",
                    "Scripts",
                    "python.exe"
                ),
                Arguments = $"{exec} {args}",
                UseShellExecute = false,
                WorkingDirectory = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    path
                ),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            Console.WriteLine(psi.FileName);
            Console.WriteLine(psi.WorkingDirectory);
            Console.WriteLine(psi.Arguments);

            process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = psi
            };

            process.OutputDataReceived += OnOutputDataReceived;
            this.process.Exited += (s,e) => kill();
        }

        public void launch() 
        {
            createProcess();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public virtual void kill()
        {
            if (process != null)
            {
                process.OutputDataReceived -= OnOutputDataReceived;
                process.ErrorDataReceived -= OnOutputDataReceived;

                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }
    }
}
