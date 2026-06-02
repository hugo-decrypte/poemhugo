using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA
{
    public class EmbeddingService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        private readonly string _ollamaUrl;
        private readonly string _model;
        private readonly List<EmbeddingItem> _items = new List<EmbeddingItem>();

        public bool IsReady { get; private set; } = false;

        public EmbeddingService(string ollamaUrl, string model = "nomic-embed-text")
        {
            _ollamaUrl = ollamaUrl.TrimEnd('/');
            _model = model;
        }

        public enum ItemCategory { Button, View, Script }

        public void Register(string description, string scriptValue, ItemCategory category = ItemCategory.Button)
        {
            if (!string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(scriptValue))
                _items.Add(new EmbeddingItem(description, scriptValue, category));
        }

        public void Clear()
        {
            _items.Clear();
            IsReady = false;
        }

        public IReadOnlyList<EmbeddingItem> GetItems() => _items.AsReadOnly();

        // Tente de charger les embeddings depuis le cache disque.
        // Retourne true si tous les items ont été restaurés depuis le cache.
        public bool TryLoadCache(string cachePath)
        {
            try
            {
                if (!File.Exists(cachePath)) return false;

                var cache = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(cachePath));
                if (cache?.Hash != ComputeHash()) return false;

                var lookup = cache.Items
                    .GroupBy(c => c.Description + "\x00" + c.ScriptValue)
                    .ToDictionary(g => g.Key, g => g.First());
                foreach (var item in _items)
                {
                    if (lookup.TryGetValue(item.Description + "\x00" + item.ScriptValue, out var hit))
                        item.Embedding = hit.Embedding;
                }

                IsReady = _items.All(i => i.Embedding != null);
                if (IsReady)
                    Console.WriteLine($"[Embedding] Cache chargé ({_items.Count} éléments) — démarrage instantané.");
                return IsReady;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Embedding] Cache illisible : {ex.Message}");
                return false;
            }
        }

        public void SaveCache(string cachePath)
        {
            try
            {
                var cache = new CacheFile
                {
                    Hash  = ComputeHash(),
                    Items = _items
                        .Where(i => i.Embedding != null)
                        .Select(i => new CacheItem { Description = i.Description, ScriptValue = i.ScriptValue, Embedding = i.Embedding })
                        .ToList()
                };
                File.WriteAllText(cachePath, JsonSerializer.Serialize(cache));
                Console.WriteLine($"[Embedding] Cache sauvegardé ({cache.Items.Count} éléments).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Embedding] Sauvegarde cache échouée : {ex.Message}");
            }
        }

        // Hash déterministe des items enregistrés — détecte tout changement de config
        private string ComputeHash()
        {
            long h = 0;
            foreach (var item in _items)
            {
                string key = item.Description + "\x01" + item.ScriptValue + "\x01" + (int)item.Category;
                foreach (char c in key) h = h * 31 + c;
            }
            return h.ToString("X16");
        }

        public async Task ComputeAllAsync()
        {
            var pending = _items.Where(i => i.Embedding == null).ToList();
            Console.WriteLine($"[Embedding] Calcul de {pending.Count} embeddings (séquentiel)...");

            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                item.Embedding = await FetchAsync(item.Description);
                if (item.Embedding == null)
                    Console.WriteLine($"[Embedding] Echec ({i + 1}/{pending.Count}) : {item.Description}");
                else if ((i + 1) % 20 == 0 || i + 1 == pending.Count)
                    Console.WriteLine($"[Embedding] {i + 1}/{pending.Count}...");
            }

            IsReady = true;
            Console.WriteLine($"[Embedding] {_items.Count(i => i.Embedding != null)}/{_items.Count} embeddings calculés.");
        }

        public async Task<List<EmbeddingItem>> FindTopKAsync(string query, int k = 4, float minScore = 0f)
        {
            float[] queryVec = await FetchAsync(query);
            if (queryVec == null)
                return new List<EmbeddingItem>();

            var ranked = _items
                .Where(i => i.Embedding != null)
                .Select(i => new { Item = i, Score = Cosine(queryVec, i.Embedding) })
                .OrderByDescending(x => x.Score)
                .ToList();

            Console.WriteLine($"[Embedding] Top résultats pour \"{query}\" :");
            foreach (var x in ranked.Take(k + 2))
                Console.WriteLine($"  {x.Score:F3}  [{x.Item.Category}]  {x.Item.Description}  →  {x.Item.ScriptValue}");

            float best = ranked.Count > 0 ? ranked[0].Score : 0f;
            if (best < minScore)
            {
                Console.WriteLine($"[Embedding] Score max {best:F3} < seuil {minScore:F3} → aucune correspondance");
                return new List<EmbeddingItem>();
            }

            return ranked.Take(k).Select(x => x.Item).ToList();
        }

        private async Task<float[]> FetchAsync(string text)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { model = _model, prompt = text });
                var response = await _http.PostAsync(
                    _ollamaUrl + "/api/embeddings",
                    new StringContent(body, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Embedding] HTTP {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EmbeddingResponse>(json)?.Embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Embedding] Erreur : {ex.Message}");
                return null;
            }
        }

        private static float Cosine(float[] a, float[] b)
        {
            float dot = 0, na = 0, nb = 0;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                dot += a[i] * b[i];
                na  += a[i] * a[i];
                nb  += b[i] * b[i];
            }
            float denom = (float)(Math.Sqrt(na) * Math.Sqrt(nb));
            return denom < 1e-10f ? 0f : dot / denom;
        }

        public class EmbeddingItem
        {
            public string Description { get; }
            public string ScriptValue { get; }
            public ItemCategory Category { get; }
            public float[] Embedding { get; set; }

            public EmbeddingItem(string description, string scriptValue, ItemCategory category = ItemCategory.Button)
            {
                Description = description;
                ScriptValue = scriptValue;
                Category    = category;
            }
        }

        private class EmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; }
        }

        private class CacheFile
        {
            [JsonPropertyName("hash")]  public string Hash { get; set; }
            [JsonPropertyName("items")] public List<CacheItem> Items { get; set; }
        }

        private class CacheItem
        {
            [JsonPropertyName("d")] public string Description { get; set; }
            [JsonPropertyName("v")] public string ScriptValue { get; set; }
            [JsonPropertyName("e")] public float[] Embedding  { get; set; }
        }
    }
}
