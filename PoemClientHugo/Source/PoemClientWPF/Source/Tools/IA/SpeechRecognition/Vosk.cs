using System;

namespace PoemClient.Source.Tools.IA.SpeechRecognition
{
    internal class Vosk : PythonScript, ISpeechRecognition
    {
        public event Action<string> MessageReceived;
        public Vosk(string lang) : base("Tools\\SpeechRecognition\\Vosk", "main.py", $"--language {lang.Split('-')[0]}")
        {
            this.DataReceived += (s, e) => messageReceived(e.Data);
        }

        public virtual void messageReceived(string msg)
        {
            MessageReceived.Invoke(msg);
            Console.WriteLine($"[DEBUG VOSK] : {msg}");
        }

        public void startListen()
        {
            this.launch();
        }

        public void stopListen()
        {
            this.kill();
        }
        override
        public void kill()
        {
            base.kill();
            MessageReceived.Invoke("STOPPED");
        }
        public void stop()
        {
            this.kill();
        }
    }
}
