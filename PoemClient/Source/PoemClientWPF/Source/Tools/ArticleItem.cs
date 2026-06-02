namespace PoemClientWPF.Tools
{
    public class ArticleItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TechName { get; set; }

        // Surcharge pour que la ComboBox affiche directement le nom
        public override string ToString() => Name;
    }
}