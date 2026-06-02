using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA.SpeechRecognition
{
    internal class WhisperLive : PythonScript, ISpeechRecognition
    {
        public event Action<string> MessageReceived;

        public WhisperLive(string lang) : base("Tools\\SpeechRecognition\\WhisperLive", "run_client.py", $"-p 9090 --server localhost --lang {lang}")
        {
            this.DataReceived += (s, e) => messageReceived(e.Data);
        }

        public virtual void messageReceived(string msg)
        {
            
            Console.WriteLine($"[DEBUG WHISPER] : {msg}");
            MessageReceived?.Invoke(msg);
        }

        public void startListen()
        {
            this.launch();
        }

        public void stopListen()
        {
            this.kill();
            MessageReceived.Invoke("STOPPED");
        }
        public void stop()
        {
            this.kill();
        }
    }
}