namespace BaseLibrary.DTOs
{
    public class ManageUser
    {
        public ManageUser() { }

        public ManageUser(ManageUser user)
        {
            Name = user.Name;
            Email = user.Email;
            UserId = user.UserId;
            Role = user.Role;
        }

        public string? Name { get; set; }
        public string? Email { get; set; }
        public int UserId { get; set; }
        public string? Role { get; set; }
    }
}
