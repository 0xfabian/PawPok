using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawPok.Data;
using PawPok.Models;
using System.Diagnostics;

namespace PawPok.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _context;

    public HomeController(ILogger<HomeController> logger, DataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(string search)
    {
        List<Video> videos = _context.Videos.Include(v => v.User).Include(v => v.Likes).Include(v => v.Comments).OrderByDescending(v => v.UploadDate).ToList();

        if(!string.IsNullOrEmpty(search))
            videos.RemoveAll(v => !v.User!.Username!.Contains(search) && !v.Description!.Contains(search));

        if (User!.Identity!.IsAuthenticated)
        {
            videos.RemoveAll(v => v.User!.Username == User!.Identity!.Name);

            int myId = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!.Id;
            ViewBag.isFollowed = new List<bool>();
            ViewBag.isLiked = new List<bool>();

            foreach (var video in videos)
            {
                ViewBag.isFollowed.Add(_context.Follows.Any(f => f.FollowerId == myId && f.FollowingId == video.UserId));
                ViewBag.isLiked.Add(_context.Likes.Any(f => f.UserId == myId && f.VideoId == video.Id));
            }
        }
        else
        {
            ViewBag.isFollowed = Enumerable.Repeat(false, videos.Count()).ToList();
            ViewBag.isLiked = Enumerable.Repeat(false, videos.Count()).ToList();
        }

        return View(videos);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
