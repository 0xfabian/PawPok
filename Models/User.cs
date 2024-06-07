namespace PawPok.Models;

public class User
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ProfilePictureURL { get; set; } = "/images/default-pfp.jpg";
    public string? Name { get; set; }
    public string? Bio { get; set; } = string.Empty;
    public ICollection<Follow> Following { get; set; } = default!;
    public ICollection<Follow> Followers { get; set; } = default!;
    public ICollection<Video> Videos { get; set; } = default!;

    public User() { }

    public User(string username, string password)
    {
        Username = username;
        Password = password;
        Name = username;
    }
}
