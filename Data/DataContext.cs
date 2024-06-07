using PawPok.Models;
using Microsoft.EntityFrameworkCore;

namespace PawPok.Data;

public class DataContext : DbContext
{
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Video> Videos { get; set; } = default!;
    public DbSet<Comment> Comments { get; set; } = default!;
    public DbSet<Like> Likes { get; set; } = default!;
    public DbSet<Follow> Follows { get; set; } = default!;

    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Follow>()
            .HasKey(f => new { f.FollowerId, f.FollowingId });

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Video)
            .WithMany(v => v.Comments)
            .HasForeignKey(c => c.VideoId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Like>()
            .HasOne(c => c.Video)
            .WithMany(v => v.Likes)
            .HasForeignKey(c => c.VideoId)
            .OnDelete(DeleteBehavior.NoAction);

        base.OnModelCreating(modelBuilder);
    }
}
