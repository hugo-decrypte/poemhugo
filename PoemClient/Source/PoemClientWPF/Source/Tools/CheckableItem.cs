namespace PoemClientWPF.Tools
{
    public class CheckableItem
    {
        public string Name { get; set; }
        public object Value { get; set; } // Stocke l'ArticleItem complet ou juste le string du pays
        public bool IsSelected { get; set; }
    }
}