using PoemClient.Source.Tools.IA;
using System.Collections.Generic;


namespace PoemClientWPF.Tools.IA
{
    // --- CLASSES DE DONNÉES UNIFIÉES ---
    // 1. Classe pour l'interface graphique (Combobox/Listbox)
    public class ArticleItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TechName { get; set; }
        public override string ToString() => Name;
    }

    // 2. Classe principale qui réceptionne le JSON de l'IA
    public class MarketEntry
    {
        // --- DONNÉES COMMUNES (Prix & Demande) ---
        public string product_name { get; set; }
        public string country { get; set; }
        public string reliability_score { get; set; }
        public string comments { get; set; }
        public double probability { get; set; }

        // --- DONNÉES SPÉCIFIQUES AU PRIX (SupplyPredict) ---
        public SupplyData min_data { get; set; }
        public SupplyData max_data { get; set; }

        // --- DONNÉES SPÉCIFIQUES À LA DEMANDE (DemandPredict) ---
        public int min_quantity { get; set; }
        public int max_quantity { get; set; }
        public double min_weight { get; set; }
        public double max_weight { get; set; }
        public double min_volume { get; set; }
        public double max_volume { get; set; }
        public int? supply_score { get; set; }
        public int? demand_score { get; set; }
        public string market_status { get; set; }
        public string trend_prediction { get; set; }
    }

    // 3. Sous-classe requise par min_data et max_data
    public class SupplyData
    {
        public double price { get; set; }
        public string currency { get; set; }
        public List<string> suppliers { get; set; }
        public string suppliers_type { get; set; }

        // Données ajoutées pour la base de données
        public int quantity { get; set; }
        public double weight { get; set; }
        public double volume { get; set; }
    }

    public abstract class Prediction : TypeRequete
    {
        public Prediction(ModeleIA modeleIA, string product, List<string> countries, string datedebut, string datefin) : base(modeleIA)
        {
            this.donnees = $"Product : {product},  Countries : {string.Join(", ", countries)}, DateDébut : {datedebut}, DateFin : {datefin}";
            this.temperature = 0.2;
        }
        
    }

    public class SupplyPredict : Prediction      // JZ 0328 : price -> supply
    {
        public SupplyPredict(ModeleIA modeleIA, string product, List<string> countries, string datedebut, string datefin, Config config) : base(modeleIA, product, countries, datedebut, datefin)
        {
            this.prompt = config.GetKeyValue("PromptSupplyPrediction");     // JZ 0319

        }
    }
    public class DemandPredict : Prediction
    {
        public DemandPredict(ModeleIA modeleIA, string product, List<string> countries, string datedebut, string datefin, Config config) : base(modeleIA, product, countries, datedebut, datefin)
        {
            // On demande spécifiquement à l'IA d'estimer les quantités, poids, volumes et probabilités pour chaque produit.
            this.prompt = config.GetKeyValue("PromptDemandPrediction");

        }
    }
}