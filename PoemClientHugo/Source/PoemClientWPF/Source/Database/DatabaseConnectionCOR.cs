namespace PoemClientWPF.Database
{
    abstract class DatabaseConnectionCOR : DatabaseConnection
    {
        protected DatabaseConnectionCOR suivant;

        public DatabaseConnectionCOR(DatabaseConnectionCOR suiv)
        {
            this.suivant = suiv;
        }
        public string connexion(string sgbd, string duid, string dpwd, string serverAddress, string dsn)
        {
            string connexionString;
            connexionString = connexion2(sgbd, duid, dpwd, serverAddress, dsn);
            if (connexionString != "")
                return connexionString;
            else
            {
                if (this.suivant != null)
                {
                    return this.suivant.connexion(sgbd, duid, dpwd, serverAddress, dsn);
                }
                else
                {
                    return "";
                }
            }
        }
        public abstract string connexion2(string sgbd, string duid, string dpwd, string serverAddress, string dsn);
    }
}
