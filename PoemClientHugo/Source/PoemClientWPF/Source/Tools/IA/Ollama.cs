using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA
{
    //
    // LLM Local
    //
    public class Ollama : ModeleIA
    {
        private OllamaApiClient client;

        public Ollama(Config conf) : base(conf.GetKeyValue("OllamaIP"), conf.GetKeyValue("OllamaModel"), null)
        {
            var uri = new Uri(this.urlAPI);
            var customHttpClient = new HttpClient()
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromMinutes(15)
            };
            this.client = new OllamaApiClient(customHttpClient);
        }

        public override async Task<string> contacterIA(string prompt, string donnees, double temperature)
        {
            var request = new ChatRequest()
            {
                Model = this.model,
                Messages = new[]
                {
                    new Message(ChatRole.System, prompt),
                    new Message(ChatRole.User, donnees)
                },
                Stream = false,
                KeepAlive = "1h",
                Options = new RequestOptions
                {
                    Temperature = (float)temperature,
                    NumPredict = 200,
                }
            };

            var stream = client.ChatAsync(request);
            var enumerator = stream.GetAsyncEnumerator();
            var reponse = new StringBuilder();

            while (await enumerator.MoveNextAsync())
            {
                if (!string.IsNullOrWhiteSpace(enumerator.Current.Message.Content))
                {
                    reponse.Append(enumerator.Current.Message.Content);
                }
            }

            return reponse.ToString();
        }
    }
}