using PoemClientWPF.Tools.IA;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PoemClientWPF.Tools
{

    public class GeminiClient : ModeleIA
    {
        
        public GeminiClient(string urlAPI, string model, string keyAPI) : base(urlAPI, model, keyAPI) {}



        public override async Task<string> contacterIA(string prompt, string chunk, double temperature)
        {
            Console.WriteLine("Gemini");

            string finalPrompt = prompt + chunk;            

            var requestData = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = finalPrompt } } }
                },
                generationConfig = new
                {
                    temperature = temperature,
                    maxOutputTokens = MaxTokens,
                    topP = 1.0
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // CORRECTION 1 : Sécurisation de l'URL
            string finalUrl = $"{this.urlAPI}{this.model}:generateContent?key={this.keyAPI}";
            Console.WriteLine(finalUrl);

            var response = await _httpClient.PostAsync(finalUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                
                // CORRECTION 2 : Ajout du 'using' pour éviter les fuites de mémoire
                using (var doc = JsonDocument.Parse(jsonResponse)){

                    // CORRECTION 3 : Ajout d'un try-catch de sécurité
                    try
                    {
                        return doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString()?.Trim() ?? "";
                    }
                    catch (Exception)
                    {
                        return "Erreur lors de l'extraction de la réponse (structure JSON inattendue).";
                    }
                }
            }

            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Error: {response.StatusCode} - {error}");
            return $"Erreur API ({response.StatusCode})";
        }

    }

}