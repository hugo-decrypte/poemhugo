using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA.SpeechRecognition
{
    internal interface ISpeechRecognition
    {
        event Action<string> MessageReceived;

        /// <summary>
        /// Start listening in microphone
        /// </summary>
        void startListen();

        /// <summary>
        /// Stop listening in microphone
        /// </summary>
        void stopListen();

        /// <summary>
        /// Completely stops the SpeechRecognition tool
        /// </summary>
        void stop();
        void messageReceived(string msg);
    }
}
