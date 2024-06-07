using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawPok.Data;
using PawPok.Models;

namespace PawPok.Controllers;

public class VideoController : Controller
{
    private readonly DataContext _context;

    public VideoController(DataContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index(int videoId)
    {
        Video? video = _context.Videos.Include(v => v.Comments).Include(v => v.Likes).Include(v => v.User).FirstOrDefault(v => v.Id == videoId);

        if(video == null)
            return RedirectToAction("Index", "Home");

        video.Comments = video.Comments.OrderByDescending(c => c.CreatedDate).ToList();

        ViewBag.Video = video;

        foreach (var com in video.Comments)
            com.User = _context.Users.FirstOrDefault(u => u.Id == com.UserId);

        Comment comm = new Comment();

        if (User!.Identity!.IsAuthenticated)
        {
            User user = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

            ViewBag.isFollowed = _context.Follows.Any(f => f.FollowerId == user.Id && f.FollowingId == video.UserId);
            ViewBag.isLiked = _context.Likes.Any(l => l.UserId == user.Id && l.VideoId == video.Id);

            comm.UserId = user.Id;
            comm.VideoId = video!.Id;
        }
        else
        {
            ViewBag.isFollowed = false;
            ViewBag.isLiked = false;
        }

        return View(comm);
    }

    public IActionResult Remove(int videoId)
    {
        if (User!.Identity!.IsAuthenticated)
        {
            Video? video = _context.Videos.Include(v => v.Likes).Include(v => v.Comments).Include(v => v.User).FirstOrDefault(v => v.Id == videoId);

            if (video != null && video.User?.Username == User!.Identity!.Name)
            {
                try
                {
                    string? url = video.VideoURL;

                    _context.Likes.RemoveRange(video.Likes);
                    _context.Comments.RemoveRange(video.Comments);
                    _context.Videos.Remove(video);
                    _context.SaveChanges();

                    if (url != null)
                    {
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/'));

                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }

                    return RedirectToAction("Profile", "User", new { username = User!.Identity!.Name });
                }
                catch (Exception)
                {

                }
            }
        }

        return NoContent();
    }

    public IActionResult RemoveComment(int commId)
    {
        if (User!.Identity!.IsAuthenticated)
        {
            int myId = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!.Id;

            Comment? comm = _context.Comments.Include(c => c.Video).FirstOrDefault(c => c.Id == commId);

            if (comm != null && (comm.UserId == myId || comm.Video?.UserId == myId))
            {
                try
                {
                    int videoId = comm.VideoId;

                    _context.Remove(comm);
                    _context.SaveChanges();

                    return RedirectToAction("Index", new { videoId = videoId });
                }
                catch (Exception)
                {

                }
            }
        }

        return NoContent();
    }
}
