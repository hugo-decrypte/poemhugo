//ClasseTraducteur
//

using PoemClient.Source.Tools.IA;
using System;
using System.Collections.Generic;


namespace PoemClientWPF.Tools.IA
{
    public class TraductionService : TypeRequete
    {

        public TraductionService(string donnees, ModeleIA modeleIA, string targetLanguage, Config config) : base(modeleIA,donnees) {
            this.prompt = config.GetKeyValue("PromptTranslation").Replace("{targetLanguage}",targetLanguage);
            this.temperature = 0.9;
        }

    }
}