using PoemClientWPF.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoemClient.Source.Tools.IA
{
    public class KeywordMatcher
    {
        private readonly List<(string[] words, string target)> _entries;

        private static readonly HashSet<string> _stopWords = new HashSet<string>
        {
            "le","la","les","l","un","une","des","de","du","d",
            "je","tu","il","elle","on","nous","vous","ils","elles",
            "me","te","se","moi","toi","lui","leur","y","en",
            "que","qui","quoi","dont","et","ni","car","or","mais","donc",
            "ce","cet","cette","ces","mon","ma","mes","ton","ta","tes","son","sa","ses",
            "a","au","aux","est","sont","ete","etre","avoir","faire",
            "veux","veut","vouloir","pouvoir","peux","peut",
            "pour","sur","sous","dans","avec","vers",
            "chez","entre","depuis","pendant","apres","avant","lors",
            "si","tres","trop","peu","assez","beaucoup",
            "ici","quand","comment","pourquoi","quel","quelle","quels","quelles",
            "bien","tous","tout","toute","toutes","alors","aussi","encore","deja",
            "montre","affiche","ouvre","ouvrir","afficher","montrer","voir","aller",
            "besoin","envie","voudrais","aimerais","souhaite","merci","stp","svp","veux"
        };

        private static readonly HashSet<string> _negationWords = new HashSet<string>
        {
            "pas","ne","non","sans","aucun","aucune","jamais","plus","ni","ferme","fermer","enleve","retire","cache","ferm"
        };

        public KeywordMatcher(Config config)
        {
            _entries = new List<(string[], string)>();

            // AIKeyword_* : règles directes
            foreach (var key in config.GetAllKeys())
            {
                if (!key.StartsWith("AIKeyword_")) continue;

                string rawKeyword = key.Substring("AIKeyword_".Length).Replace("_", " ");
                string[] words = Normalize(rawKeyword)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => !_stopWords.Contains(w))
                    .Select(Stem)
                    .Where(w => w.Length > 0)
                    .ToArray();
                string target = config.GetKeyValue(key);
                if (words.Length > 0)
                    _entries.Add((words, target));
            }

            // AIAlias_* : synonymes/traductions pointant vers la même cible qu'un AIKeyword_
            foreach (var key in config.GetAllKeys())
            {
                if (!key.StartsWith("AIAlias_")) continue;

                string baseKey = "AIKeyword_" + key.Substring("AIAlias_".Length);
                string target = config.GetKeyValue(baseKey);
                if (string.IsNullOrWhiteSpace(target)) continue;

                foreach (string synonym in config.GetKeyValue(key).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] words = Normalize(synonym.Trim())
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !_stopWords.Contains(w))
                        .Select(Stem)
                        .Where(w => w.Length > 0)
                        .ToArray();
                    if (words.Length > 0)
                        _entries.Add((words, target));
                }
            }

            _entries.Sort((a, b) => b.words.Length.CompareTo(a.words.Length));
            Console.WriteLine("[Keyword] " + _entries.Count + " règles chargées.");
        }

        public string TryMatch(string query)
        {
            string[] rawWords = Normalize(query)
                .Split(new[] { ' ', '\'', '’' }, StringSplitOptions.RemoveEmptyEntries);

            if (rawWords.Any(w => _negationWords.Contains(w)))
            {
                Console.WriteLine("[Keyword] Negation dans : " + query);
                return null;
            }

            string[] queryWords = rawWords
                .Where(w => !_stopWords.Contains(w))
                .Select(Stem)
                .Where(w => w.Length > 0)
                .ToArray();

            if (queryWords.Length == 0) return null;

            bool queryHasScript = rawWords.Contains("script");

            foreach (var (words, target) in _entries)
            {
                if (words.All(w => queryWords.Any(qw =>
                        qw == w || (qw.Length >= 4 && w.StartsWith(qw) && (float)qw.Length / w.Length >= 0.75f))))
                {
                    if (queryHasScript && !target.StartsWith("script:"))
                        continue;

                    Console.WriteLine("[Keyword] \"" + string.Join(" ", words) + "\" -> " + target);
                    return BuildResponse(target);
                }
            }

            Console.WriteLine("[Keyword] Aucun match pour : \"" + query + "\" (mots : " + string.Join(", ", queryWords) + ")");
            return null;
        }

        private static string Stem(string w)
        {
            if (w.Length <= 3) return w;
            if (w.EndsWith("eaux") && w.Length > 5) return w.Substring(0, w.Length - 1);
            if (w.EndsWith("aux")  && w.Length > 4) return w.Substring(0, w.Length - 2) + "l";
            if (w.EndsWith("s") && w.Length > 4
                && !w.EndsWith("is") && !w.EndsWith("as") && !w.EndsWith("us") && !w.EndsWith("os"))
                return w.Substring(0, w.Length - 1);
            return w;
        }

        private static string BuildResponse(string target)
        {
            if (target.StartsWith("view:"))
                return "{\"tool\":\"view\",\"arguments\":[\"" + target.Substring(5) + "\"]}";
            if (target.StartsWith("static:"))
                return "{\"tool\":\"static\",\"arguments\":[\"" + EscapeJson(target.Substring(7)) + "\"]}";
            if (target.StartsWith("menu:"))
                return "{\"tool\":\"menu\",\"arguments\":[\"" + EscapeJson(target.Substring(5)) + "\"]}";
            return null;
        }

        private static string Normalize(string s)
        {
            s = s.ToLowerInvariant();
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case 'é': case 'è': case 'ê': case 'ë': sb.Append('e'); break;
                    case 'à': case 'â': case 'ä':                sb.Append('a'); break;
                    case 'ù': case 'û': case 'ü':                sb.Append('u'); break;
                    case 'î': case 'ï':                               sb.Append('i'); break;
                    case 'ô': case 'ö':                               sb.Append('o'); break;
                    case 'ç':                                              sb.Append('c'); break;
                    case 'ß':                                              sb.Append("ss"); break;
                    default:                                                    sb.Append(c);   break;
                }
            }
            return sb.ToString();
        }

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}