using PoemClientWPF.Tools.IA;
using System;
using System.Threading.Tasks;

namespace PoemClient.Source.Tools.IA
{

    /**
     * Requête utilisant IA
     */
    public abstract class TypeRequete
    {

        protected string prompt;
        protected double temperature;
        protected string donnees { get; set; }
        public string Prompt { get { return prompt; } set { prompt = value; } }
        public string Donnees { get { return donnees; } set { donnees = value; } }
        public double Temperature { get { return temperature; } set { temperature = value;} }
        protected ModeleIA modeleIA;

        public TypeRequete(ModeleIA modeleIA, string donnees = null)
        {
            this.donnees = donnees;
            this.modeleIA = modeleIA;
        }

        public virtual async Task<string> executerRequete()
        {
            try
            {
                return await this.modeleIA.contacterIA(this.prompt, this.donnees, this.temperature);
            }
            catch (Exception ex)
            {
                return "Erreur : " + ex.Message;
            }
        }

        

        
    }
}
