namespace Locus.Models
{
    public class Asset
    {
        public string Serial { get; set; }
        public string Tag { get; set; }
        public Model Model { get; set; }
        public Group Group { get; set; }

    }
}