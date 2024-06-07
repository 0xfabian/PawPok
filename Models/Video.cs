using System.Numerics;

namespace PawPok.Models;

public class Video
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public DateTime UploadDate { get; set; }
    public string? VideoURL { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public ICollection<Comment> Comments { get; set; } = default!;
    public ICollection<Like> Likes { get; set; } = default!;
    public long shares { get; set; }
}
