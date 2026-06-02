using NAudio.Wave;
using System;
using System.Collections.Generic;
using Windows.Media.SpeechRecognition;
using System.Net.NetworkInformation;

namespace PoemClient.Source.Tools.IA.SpeechRecognition
{
    // Uses the built-in Windows Speech Recognition
    // Can use only languages installed on the system
    // Listens to microphone continuously to look like it has no delay
    internal class WindowsRecognition : ISpeechRecognition
    {
        public event Action<string> MessageReceived;

        private string lang;

        private SpeechRecognizer recognizer;
        private SpeechContinuousRecognitionSession session;
        private string text = "";
        private bool listening = false;
        private bool micOn; // Microphone opened but not necessarly listening
        private bool waitNextResult = false;


        // Sound detection
        private WaveInEvent waveIn;
        private List<bool> soundFromMic = new List<bool>();
        System.Timers.Timer micTimer;

        public WindowsRecognition(string lang)
        {
            this.lang = lang;

            checkMicrophone();
            startRecognizer();
        }

        public void messageReceived(string msg)
        {
            Console.WriteLine($"[DEBUG WINDOWS RECOGNITION] MESSAGE : {msg}");
            switch (msg.Split(':')[0])
            {
                case "WARNING":
                    stopListen();
                    goto default;
                default:
                    MessageReceived?.Invoke(msg);
                    break;
            }
        }

        public void startListen()
        {
            text = "";
            if (Utils.IsInternetAvailable())
            {
                if (micOn)
                {
                    messageReceived("LISTENING:");
                    listening = true;
                }
                else
                {
                    messageReceived("WARNING:NoMicrophone");
                    listening = false;
                }
            }
            else
            {
                messageReceived("WARNING:NoInternet");
            }
        }

        public void stopListen()
        {
            listening = false;
        }

        public void stop()
        {
            micTimer.Stop();
            if(waveIn != null)
            {
                waveIn.DataAvailable -= WaveIn_DataAvailable;
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            stopListen();
            stopRecognizer();
        }

        // ------------  Recognizer methods ------------ //
        public void startRecognizer()
        {
            recognizer = new SpeechRecognizer(new Windows.Globalization.Language(lang));
            session = recognizer.ContinuousRecognitionSession;

            recognizer.StateChanged += onStateChange;
            recognizer.HypothesisGenerated += onPartialResult;
            session.ResultGenerated += onResult;

            // Compile constraints to gain access to microphone
            recognizer.CompileConstraintsAsync().AsTask().Wait();
            try
            {
                session.StartAsync().AsTask().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[DEBUG WINDOWS RECOGNITION] Erreur demarrage session : {e.Message}");
            }
        }

        public void stopRecognizer()
        {
            if (recognizer != null)
            {
                recognizer.StateChanged -= onStateChange;
                recognizer.HypothesisGenerated -= onPartialResult;
                session.ResultGenerated -= onResult;
                recognizer.Dispose();
                recognizer = null;
            }
            waitNextResult = true;
        }

        // ------------  Recognizer events ------------ //
        private void onPartialResult(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs e)
        {
            if(!listening)
            {
                waitNextResult = true;
            }

            if(!waitNextResult)
                messageReceived($"RESULT:{text}{e.Hypothesis.Text.ToLower()}");
        }
        private void onResult(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs e)
        {
            if(!listening)
            {
                waitNextResult = true;
            }

            if(!waitNextResult)
            {
                text += e.Result.Text.ToLower() + " ";
                messageReceived($"RESULT:{text}");
            }
            waitNextResult = false;
        }
        private void onStateChange(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            if (!listening && args.State == SpeechRecognizerState.SpeechDetected)
            {
                waitNextResult = true;
            }

            // Restarts recognizer when Idle state
            if (args.State == SpeechRecognizerState.Idle)
            {
                stopRecognizer();
                System.Threading.Thread.Sleep(1000);
                waitNextResult = false; // New recognizer so no need to wait for next result
                startRecognizer();
            }
        }

        // ------------  Detect sound if microphone has any sound ------------ //
        // Opens the microphone for x seconds to detect any noise
        private void checkMicrophone()
        {

            waveIn = new WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(44100, 1);
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();

            // Set the timer for the microphone detection
            micTimer = new System.Timers.Timer(1000);
            micTimer.Elapsed += (s, e) =>
            {
                if (waveIn != null)
                {
                    if (soundFromMic.Contains(true))
                    {
                        micOn = true;
                    }
                    else
                    {
                        micOn = false;
                        if (listening)
                            messageReceived("WARNING:NoMicrophone");
                    }
                    soundFromMic = new List<bool>();
                }
            };
            micTimer.AutoReset = true;
            micTimer.Start();
        }
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            int bytesRecorded = e.BytesRecorded;
            int sum = 0;
            for (int index = 0; index < bytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index]);
                sum += Math.Abs(sample);
            }
            float average = sum / (bytesRecorded / 2f);

            if (average > 0.5)
                soundFromMic.Add(true);
            else
                soundFromMic.Add(false);
        }
    }
}
