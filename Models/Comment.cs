namespace PawPok.Models;

public class Comment
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int VideoId { get; set; }
    public Video? Video { get; set; }
}
