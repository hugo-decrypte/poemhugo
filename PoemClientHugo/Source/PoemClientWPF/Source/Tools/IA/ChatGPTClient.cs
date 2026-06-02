using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PoemClientWPF.Tools.IA
{
    public class ChatGPTClient : ModeleIA
    { 
        public ChatGPTClient(string urlAPI, string model, string keyAPI) : base(urlAPI, model, keyAPI)
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + this.keyAPI);
            }
            Console.WriteLine("GPT");
        }

        public override async Task<string> contacterIA(string prompt, string chunk, double temperature)
        {

            var requestData = new
                {
                    model = this.model,
                    messages = new[]
                    {
                        new { role = "system", content = prompt},
                        new { role = "user", content = chunk}
                    },
                    temperature = temperature,
                    max_tokens = MaxTokens,
                    top_p = 1,
                    frequency_penalty = 0,
                    presence_penalty = 0
                };

                var jsonRequest = JsonSerializer.Serialize(requestData);
                var requestContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(this.urlAPI, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse);
                    return responseData?.Choices?[0]?.Message?.Content?.Trim() ?? "";
                }
                string errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error: {response.StatusCode} - {errorResponse}");
                return "";
        }

        public class OpenAIResponse
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }
        }

        public class Choice
        {
            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; }

            [JsonPropertyName("message")]
            public Message Message { get; set; }
        }

        public class Message
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        public class OpenAIErrorResponse
        {
            [JsonPropertyName("error")]
            public OpenAIError Error { get; set; }
        }

        public class OpenAIError
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }
        }
    }
}