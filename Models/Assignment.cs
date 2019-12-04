namespace Locus.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public User UserId { get; set; }
        public Asset AssetSerial { get; set; }
    }
}