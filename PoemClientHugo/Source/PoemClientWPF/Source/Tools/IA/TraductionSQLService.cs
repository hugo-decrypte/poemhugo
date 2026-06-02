using PoemClientWPF.Tools.IA;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA
{
    public class TraductionSQLService : TypeRequete
    {

        public TraductionSQLService(string donnees, ModeleIA modeleIA,string sourceDatabase, string targetDatabase) : base(modeleIA,donnees)
        {
            this.prompt = $"Tu fonctionnes comme un traducteur de requête SQL. " +
                          $"Voici ma requête qui vient de {sourceDatabase}. Traduis-la en {targetDatabase}. " +
                          $"Réponds-moi avec UNIQUEMENT la requête, sans texte additionnel ni balises Markdown :";
            this.temperature = 0.2;
        }
        public override async Task<string> executerRequete()
        {
            try
            {
                if (!File.Exists(this.donnees)) return "Le fichier spécifié n'existe pas.";
                string query = File.ReadAllText(this.donnees);
                return await this.modeleIA.contacterIA(this.prompt, this.donnees, this.temperature);
            }
            catch (Exception ex)
            {
                return "Erreur : " + ex.Message;
            }
        }
 
    }
}
