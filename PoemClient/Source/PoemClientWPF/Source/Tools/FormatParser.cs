using System.Collections.Generic;
using System.IO;

namespace PoemClientWPF.Tools
{
    // --- CLASSES DE MODÈLE ---

    // Modèle pour une colonne
    public class ColumnConfig
    {
        public string Label { get; set; }       // ex: "Début travail"
        public string InternalName { get; set; } // ex: "timeStart"
        public string DataType { get; set; }     // ex: "hh:mm"
    }

    // Modèle pour une feuille
    public class SheetConfig
    {
        public string SheetName { get; set; }
        public int ExpectedColumns { get; set; }
        public int HeaderRowIndex { get; set; }
        public List<ColumnConfig> Columns { get; set; } = new List<ColumnConfig>();
    }

    // Modèle global qui contient toutes les feuilles
    public class DataFormatConfig
    {
        public int TotalSheets { get; set; }
        public Dictionary<string, SheetConfig> Sheets { get; set; } = new Dictionary<string, SheetConfig>();
    }

    // --- LE PARSEUR ---

    public static class FormatParser
    {
        public static DataFormatConfig LoadConfig(string filePath)
        {
            var config = new DataFormatConfig();

            // Si le fichier n'existe pas, on renvoie une config vide pour ne pas faire planter l'application
            if (!File.Exists(filePath))
            {
                return config;
            }

            string[] lines = File.ReadAllLines(filePath);

            SheetConfig currentSheet = null;
            int lineState = 0; // 0: Attente de SHEET_NAME, 1: Labels, 2: InternalNames, 3: DataTypes

            string[] currentLabels = null;
            string[] currentNames = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 1. Lecture du nombre total de feuilles
                if (line.StartsWith("SHEET_NUMBER="))
                {
                    string numStr = line.Replace("SHEET_NUMBER=", "").TrimEnd(';');
                    if (int.TryParse(numStr, out int num))
                        config.TotalSheets = num;
                    continue;
                }

                // 2. Détection d'une nouvelle feuille
                if (line.StartsWith("SHEET_NAME="))
                {
                    string[] parts = line.Replace("SHEET_NAME=", "").Split(';');
                    if (parts.Length >= 3)
                    {
                        currentSheet = new SheetConfig
                        {
                            SheetName = parts[0],
                            ExpectedColumns = int.Parse(parts[1]),
                            HeaderRowIndex = int.Parse(parts[2])
                        };

                        // On ajoute la feuille au dictionnaire
                        if (!config.Sheets.ContainsKey(currentSheet.SheetName))
                        {
                            config.Sheets.Add(currentSheet.SheetName, currentSheet);
                        }

                        lineState = 1; // La prochaine ligne sera les labels
                    }
                    continue;
                }

                // 3. Remplissage des colonnes de la feuille courante
                if (currentSheet != null)
                {
                    string[] columnsData = line.Split(';');

                    if (lineState == 1) // Ligne des Labels (ID;nom;...)
                    {
                        currentLabels = columnsData;
                        lineState = 2;
                    }
                    else if (lineState == 2) // Ligne des Noms internes (id;name;...)
                    {
                        currentNames = columnsData;
                        lineState = 3;
                    }
                    else if (lineState == 3) // Ligne des Types (string key;string;...)
                    {
                        for (int i = 0; i < currentSheet.ExpectedColumns; i++)
                        {
                            // On gère les potentiels index hors limites si le fichier cfg est mal formaté
                            string label = (currentLabels != null && i < currentLabels.Length) ? currentLabels[i] : "";
                            string name = (currentNames != null && i < currentNames.Length) ? currentNames[i] : "";
                            string type = (columnsData != null && i < columnsData.Length) ? columnsData[i] : "";

                            currentSheet.Columns.Add(new ColumnConfig
                            {
                                Label = label,
                                InternalName = name,
                                DataType = type
                            });
                        }
                        lineState = 0; // Réinitialise pour la prochaine feuille
                        currentSheet = null;
                    }
                }
            }

            return config;
        }
    }
}