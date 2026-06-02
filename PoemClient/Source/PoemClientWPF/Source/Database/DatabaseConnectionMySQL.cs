namespace PoemClientWPF.Database
{
    class DatabaseConnectionMySQL : DatabaseConnectionCOR
    {
        public DatabaseConnectionMySQL(DatabaseConnectionCOR suiv) : base(suiv) { }
        public override string connexion2(string sgbd, string duid, string dpwd, string serverAddress, string dsn)//retourne une chaine de connexion a MySQL
        {
            if (sgbd.ToLower() == "mysql")
                return "server=" + serverAddress + ";uid=" + duid + ";pwd=" + dpwd + ";database=" + dsn;
            else
                return "";
        }
    }
}
