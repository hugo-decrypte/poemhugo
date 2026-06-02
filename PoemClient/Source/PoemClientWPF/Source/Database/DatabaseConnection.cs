namespace PoemClientWPF.Database
{
    interface DatabaseConnection
    {
        string connexion(string sgbd, string duid, string dpwd, string serverAddress, string dsn);
    }
}
