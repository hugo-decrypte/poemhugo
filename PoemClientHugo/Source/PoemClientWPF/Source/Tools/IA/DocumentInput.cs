using PoemClientWPF.Tools;
using PoemClientWPF.Tools.IA;


namespace PoemClient.Source.Tools.IA
{
    public class DocumentInput : TypeRequete
    {
        public DocumentInput(Config config,ModeleIA modeleIA, string donnees = null) : base(modeleIA, donnees)
        {

            this.temperature = 0.9;
            this.prompt = config.GetKeyValue("PromptDocumentInput");    // JZ 0319
        }
    }
}