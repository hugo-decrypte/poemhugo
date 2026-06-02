using PoemClientWPF;
using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;
using System;
using System.Collections.Generic;
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
        public Agent(Config config, string clientConfig, ModeleIA modeleIA, string donnees = null) : base(modeleIA, donnees)
        {
            this.config = config;
            this.prompt = config.GetKeyValue("PromptAgent");

            // Récupère les fonctions statiques
            List<string> staticFunctions = StaticFunctions.GetFunctionsString();
            string functions = "";
            foreach (string f in staticFunctions)
            {
                functions += $"'{f}', ";
            }
            this.prompt = this.prompt.Replace("{STATIC}", functions);

            // Récupère les vues
            List<string> views = MainWindow.GetViews();
            string viewsAPI = "";
            foreach (string view in views)
            {
                viewsAPI += $"'{view}', ";
            }
            viewsAPI = viewsAPI.Remove(viewsAPI.Length - 1);
            this.prompt = this.prompt.Replace("{VIEWS}", viewsAPI);

            // Récupère les scripts exécutables
            string scripts = "";
            string[] lines = clientConfig.Split('\n');
            int currentLine = Array.FindIndex(lines, l => l.Contains("------Agent")) + 3;

            int numberOfScripts = int.Parse(MainWindow.GetLineBelowParameter(clientConfig, "------Agent"));
            for (int i = 0; i < numberOfScripts; i++)
            {
                scripts += $"description : {lines[currentLine]}, task : {lines[currentLine + 1]} - ";
                currentLine += 4 + int.Parse(lines[currentLine + 2]) * 2;
            }
            this.prompt = this.prompt.Replace("{SCRIPTS}", scripts);

            // Get all menu buttons
            // string buttons = "";
            Dictionary<string, List<Button>> mergedButtons = new Dictionary<string, List<Button>>();
            for (int menuId = 1; menuId <= int.Parse(config.GetKeyValue("MenuNumber")); menuId++)
            {
                for (int buttonId = 1; buttonId <= int.Parse(config.GetKeyValue($"Menu{menuId}FunctionNumber")); buttonId++)
                {
                    /*
                    buttons += getButton(menuId, buttonId, "Executable");
                    buttons += getButton(menuId, buttonId, "Static");
                    buttons += getButton(menuId, buttonId, "Script");
                    */
                    string[] types = { "Executable", "Static", "Script" };

                    foreach (string type in types)
                    {
                        Button b = getButton(menuId, buttonId, type);

                        if (b != null && b.Name != null)
                        {
                            if (!mergedButtons.ContainsKey(b.Name))
                            {
                                mergedButtons[b.Name] = new List<Button>();
                            }

                            mergedButtons[b.Name].Add(b);
                        }
                    }
                }

            }

            StringBuilder buttonsPrompt = new StringBuilder();

            foreach (KeyValuePair<string, List<Button>> kvp in mergedButtons)
            {
                string name = kvp.Key;
                List<Button> list = kvp.Value;

                list.Sort((a, b) => a.Order.CompareTo(b.Order));

                buttonsPrompt.Append($"name : {name}, tasks : [");

                buttonsPrompt.Append(
                    string.Join(",", list.Select(b => $"\"{b.Task}\""))
                );

                buttonsPrompt.AppendLine("] -");
            }

            this.prompt = this.prompt.Replace("{MENUS}", buttonsPrompt.ToString());

            this.prompt = this.prompt.Replace('\'', '"');
            Console.WriteLine(this.prompt);
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
