namespace PoemClientWPF.Database
{
    class DatabaseConnectionAccess : DatabaseConnectionCOR
    {
        public DatabaseConnectionAccess(DatabaseConnectionCOR suiv) : base(suiv) { }

        public override string connexion2(string sgbd, string duid, string dpwd, string serverAddress, string dsn)
        {
            if (sgbd.ToLower() == "access")
                return "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\\" + dsn + "\\Data\\" + dsn + ".mdb";
            else
                return "";
        }
    }
}
