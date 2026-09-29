namespace API.Models
{
    public class UserDto
    {
        public required string Username  { get; set; } = string.Empty;
        public required string PasswordHash { get; set; } = string.Empty;
    }
}
