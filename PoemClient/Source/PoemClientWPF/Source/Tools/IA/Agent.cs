using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PoemClient.Source.Tools.IA
{
    public class Agent : TypeRequete
    {
        private class Button
        {
            public int Order {  get; }
            public string Name {  get; }
            public string Task {  get; }
            public Button(int order, string name, string task)
            {
                Order = order;
                Name = name;
                Task = task;
            }
        }

        Config config;

        // Accessible depuis PromptWindow pour résoudre nom → tasks sans les inclure dans le prompt
        public Dictionary<string, List<string>> MenuTasks { get; } = new Dictionary<string, List<string>>();
        public Dictionary<string, string> ScriptTasks { get; } = new Dictionary<string, string>();

        public Agent(Config config, string clientConfig, ModeleIA modeleIA, string donnees = null) : base(modeleIA, donnees)
        {
            this.config = config;
            string promptKey = modeleIA is Ollama ? "PromptAgent" : "PromptAgentGPT";
            this.prompt = config.GetKeyValue(promptKey);

            // Fonctions statiques — liste compacte pipe-séparée
            this.prompt = this.prompt.Replace("{STATIC}",
                string.Join("|", StaticFunctions.GetFunctionsString()));

            // Mapping ViewName → premier alias FR depuis AIKeyword_/AIAlias_
            var viewHints = new Dictionary<string, string>();
            foreach (string key in config.GetAllKeys().Where(k => k.Trim().StartsWith("AIKeyword_")))
            {
                string val = config.GetKeyValue(key.Trim());
                if (val != null && val.Trim().StartsWith("view:"))
                {
                    string viewName = Path.GetFileNameWithoutExtension(val.Trim().Substring(5));
                    if (!viewHints.ContainsKey(viewName))
                    {
                        string aliases = config.GetKeyValue("AIAlias_" + key.Trim().Substring(10));
                        if (!string.IsNullOrEmpty(aliases))
                            viewHints[viewName] = aliases.Split(',')[0].Trim();
                    }
                }
            }

            // Vues principales (sans sous-vues TITLEOBJ) — format nom=description pipe-séparé
            var views = MainWindow.GetViews()
                .Where(v => {
                    try {
                        string firstLine = File.ReadLines(Path.Combine("..", "Logistics", "View", "Admin", v)).FirstOrDefault() ?? "";
                        return !firstLine.Contains("TITLEOBJ");
                    } catch { return true; }
                });
            this.prompt = this.prompt.Replace("{VIEWS}",
                string.Join("|", views.Select(v => {
                    string name = Path.GetFileNameWithoutExtension(v);
                    return viewHints.TryGetValue(name, out string hint) ? name + "=" + hint : name;
                })));

            // Scripts — descriptions uniquement dans le prompt, tasks stockées dans ScriptTasks
            string[] lines = clientConfig.Split('\n');
            int currentLine = Array.FindIndex(lines, l => l.Contains("------Agent")) + 3;
            int numberOfScripts = int.Parse(MainWindow.GetLineBelowParameter(clientConfig, "------Agent"));
            var scriptNames = new List<string>();
            for (int i = 0; i < numberOfScripts; i++)
            {
                string desc = lines[currentLine].Trim();
                string task = lines[currentLine + 1].Trim();
                ScriptTasks[desc] = task;
                scriptNames.Add(desc);
                currentLine += 4 + int.Parse(lines[currentLine + 2]) * 2;
            }
            this.prompt = this.prompt.Replace("{SCRIPTS}", string.Join("|", scriptNames));

            // Menus — noms uniquement dans le prompt, tasks stockées dans MenuTasks
            Dictionary<string, List<Button>> mergedButtons = new Dictionary<string, List<Button>>();
            for (int menuId = 1; menuId <= int.Parse(config.GetKeyValue("MenuNumber")); menuId++)
            {
                for (int buttonId = 1; buttonId <= int.Parse(config.GetKeyValue($"Menu{menuId}FunctionNumber")); buttonId++)
                {
                    foreach (string type in new[] { "Executable", "Static", "Script" })
                    {
                        Button b = getButton(menuId, buttonId, type);
                        if (b != null && b.Name != null)
                        {
                            if (!mergedButtons.ContainsKey(b.Name))
                                mergedButtons[b.Name] = new List<Button>();
                            mergedButtons[b.Name].Add(b);
                        }
                    }
                }
            }
            foreach (var kvp in mergedButtons)
            {
                MenuTasks[kvp.Key] = kvp.Value
                    .OrderBy(b => b.Order)
                    .Select(b => b.Task)
                    .ToList();
            }
            this.prompt = this.prompt.Replace("{MENUS}", string.Join("|", MenuTasks.Keys));

            this.prompt = this.prompt.Replace('\'', '"');
            this.temperature = 0.0;
        }

        private Button getButton(int menuId, int buttonId, string type)
        {
            Button res = null;
            string buttonValue = $"{config.GetKeyValue($"Menu{menuId}{type}{buttonId}")}";

            if (buttonValue != null && buttonValue != "")
            {
                int buttonOrder = int.Parse(config.GetKeyValue($"Menu{menuId}{type}{buttonId}Order"));
                string buttonName = $"{config.GetKeyValue($"Menu{menuId}{type}{buttonId}Text")}";
                string buttonDesc = $"{config.GetKeyValue($"Menu{menuId}{type}{buttonId}Description")}";
                // res = $"name : '{buttonName}', description : '{buttonDesc}', task : '{buttonValue}' - ";
                res = new Button(buttonOrder, buttonName, buttonValue);
            }
            return res;
        }
    }
}
