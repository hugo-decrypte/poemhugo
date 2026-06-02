using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA.SpeechRecognition
{
    internal class WindowsRecognitionOld : ISpeechRecognition
    {
        public event Action<string> MessageReceived;
        SpeechRecognitionEngine recognizer;
        string text = "";

        public WindowsRecognitionOld(string lang)
        {
            // Create an in-process speech recognizer for the fr-FR locale.
            recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo(lang));

            // Create and load a dictation grammar.
            recognizer.LoadGrammar(new DictationGrammar());

            // Configure input to the speech recognizer.
            recognizer.SetInputToDefaultAudioDevice();

            // Add a handler for the speech recognized event.
            recognizer.SpeechRecognized += (s, e) => messageReceived("RESULT:" + e.Result.Text);

        }

        public void messageReceived(string msg)
        {
            if (msg.Split(':')[0] == "RESULT")
            {
                text += msg.Split(':')[1];
                MessageReceived.Invoke("RESULT:" + msg);
            }else
            {
                MessageReceived.Invoke(msg);
            }
            Console.WriteLine($"[DEBUG WINDOWS RECOGNITION] : {msg}");
        }

        public void startListen()
        {
            // Start asynchronous, continuous speech recognition.
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
            messageReceived("LISTENING:");
        }

        public void stop()
        {
            recognizer.RecognizeAsyncStop();
        }

        public void stopListen()
        {
            recognizer.RecognizeAsyncStop();
        }
    }
}
