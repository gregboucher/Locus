namespace Locus.Models
{
    public class User
    {
        public int Id { get; set; }
        public Role RoleId { get; set; }
        public string Name { get; set; }
        public string Absentee { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}