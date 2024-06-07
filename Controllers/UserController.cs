using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawPok.Data;
using PawPok.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Routing.Constraints;

namespace PawPok.Controllers;

public class UserController : Controller
{
    private readonly DataContext _context;

    public UserController(DataContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        if (User!.Identity!.IsAuthenticated)
            return RedirectToAction("Profile", new { username = User.Identity.Name });

        return RedirectToAction("Login", "Authentication");
    }

    public IActionResult Profile(string username)
    {
        User? user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
            return NotFound();

        ViewBag.following = _context.Follows.Where(f => f.FollowerId == user.Id).Count();
        ViewBag.followers = _context.Follows.Where(f => f.FollowingId == user.Id).Count();

        user.Videos = _context.Videos.Where(v => v.UserId == user.Id).Include(v => v.Likes).OrderByDescending(v => v.UploadDate).ToList();

        ViewBag.likes = user.Videos.Sum(v => v.Likes.Count());

        if (User!.Identity!.IsAuthenticated)
        {
            int myId = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!.Id;

            ViewBag.isFollowed = _context.Follows.Any(f => f.FollowerId == myId && f.FollowingId == user.Id);
        }
        else
            ViewBag.isFollowed = false;

        return View(user);
    }

    public IActionResult Follow(int userId)
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        User? other = _context.Users.FirstOrDefault(u => u.Id == userId);

        if (other == null)
            return NoContent();

        User me = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

        if (me.Id == other.Id)
            return NoContent();

        Follow? follow = _context.Follows.FirstOrDefault(f => f.FollowerId == me.Id && f.FollowingId == other.Id);

        if (follow != null)
            return NoContent();

        follow = new Follow()
        {
            FollowerId = me.Id,
            FollowingId = other.Id
        };

        try
        {
            _context.Follows.Add(follow);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to follow: " + ex.Message);
        }

        return NoContent();
    }

    public IActionResult Unfollow(int userId)
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        User? other = _context.Users.FirstOrDefault(u => u.Id == userId);

        if (other == null)
            return NoContent();

        User me = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

        if (me.Id == other.Id)
            return NoContent();

        Follow? follow = _context.Follows.FirstOrDefault(f => f.FollowerId == me.Id && f.FollowingId == other.Id);

        if (follow == null)
            return NoContent();

        try
        {
            _context.Follows.Remove(follow);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to unfollow: " + ex.Message);
        }

        return NoContent();
    }


    [HttpPost]
    public async Task<IActionResult> Edit(User user, IFormFile file)
    {
        try
        {
            if (user.Bio == null)
                user.Bio = string.Empty;

            if (string.IsNullOrWhiteSpace(user.Username))
                ModelState.AddModelError(string.Empty, "Username is empty");
            else if (string.IsNullOrWhiteSpace(user.Name))
                ModelState.AddModelError(string.Empty, "Name is empty");
            else if (_context.Users.Any(u => u.Username == user.Username && user.Username != User!.Identity!.Name))
                ModelState.AddModelError(string.Empty, "Username is already taken");
            else if (user.Bio!.Count() > 80)
                return View(user);
            else
            {
                try
                {
                    User me = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;
                    me.Username = user.Username;
                    me.Name = user.Name;
                    me.Bio = user.Bio;

                    if (file != null && file.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(me.ProfilePictureURL) && me.ProfilePictureURL != "/images/default-pfp.jpg")
                        {
                            string old_path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", me.ProfilePictureURL.TrimStart('/'));

                            if (System.IO.File.Exists(old_path))
                                System.IO.File.Delete(old_path);
                        }

                        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        string filename = $"{me.Id}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", filename);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        me.ProfilePictureURL = "/images/" + filename;
                    }

                    _context.SaveChanges();

                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    List<Claim> claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, me.Username),
                            new Claim("pfp", me.ProfilePictureURL!)
                        };

                    var claimIdentity = new ClaimsIdentity(claims, "AuthenticationCookie");

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity));

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error saving changes: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(user);
    }

    [HttpGet]
    public IActionResult Edit()
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        User user = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(Video video, IFormFile file)
    {
        if (video.Description == null)
            video.Description = string.Empty;

        if (file == null || file.Length == 0)
            ModelState.AddModelError(string.Empty, "No video uploaded");
        else if (video.Description.Count() > 2000)
            ModelState.AddModelError(string.Empty, "Description is too long");
        else
        {
            try
            {
                video.UploadDate = DateTime.Now;
                video.UserId = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!.Id;

                _context.Videos.Add(video);
                _context.SaveChanges();

                string filename = $"{video.Id}.mp4";
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos", filename);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                video.VideoURL = "/videos/" + filename;

                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(video);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        Video video = new Video();

        return View(video);
    }

    public IActionResult Like(int videoId)
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        Video? other = _context.Videos.FirstOrDefault(u => u.Id == videoId);

        if (other == null)
            return NoContent();

        User me = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

        if (me.Id == other.Id)
            return NoContent();

        Like? like = _context.Likes.FirstOrDefault(f => f.UserId == me.Id && f.VideoId == other.Id);

        if (like != null)
            return NoContent();

        like = new Like()
        {
            UserId = me.Id,
            VideoId = other.Id
        };

        try
        {
            _context.Likes.Add(like);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to like: " + ex.Message);
        }

        return NoContent();
    }

    public IActionResult Unlike(int videoId)
    {
        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        Video? other = _context.Videos.FirstOrDefault(u => u.Id == videoId);

        if (other == null)
            return NoContent();

        User me = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name)!;

        if (me.Id == other.Id)
            return NoContent();

        Like? like = _context.Likes.FirstOrDefault(f => f.UserId == me.Id && f.VideoId == other.Id);

        if (like == null)
            return NoContent();

        try
        {
            _context.Likes.Remove(like);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to unlike: " + ex.Message);
        }

        return NoContent();
    }

    [HttpPost]
    public IActionResult Comment(Comment comm)
    {
        if (string.IsNullOrWhiteSpace(comm.Content))
            return NoContent();

        if (User!.Identity!.IsAuthenticated == false)
            return RedirectToAction("Login", "Authentication");

        try
        {
            comm.CreatedDate = DateTime.Now;
            _context.Comments.Add(comm);
            _context.SaveChanges();
        }
        catch(Exception)
        {

        }

        return RedirectToAction("Index", "Video", new {videoId = comm.VideoId});
    }

    public async Task<IActionResult> Remove()
    {
        if (User!.Identity!.IsAuthenticated)
        {
            User? user = _context.Users.FirstOrDefault(u => u.Username == User!.Identity!.Name);

            if (user != null)
            {
                try
                {
                    List<string?> urls = new List<string?>();

                    if (user.ProfilePictureURL != "/images/default-pfp.jpg")
                        urls.Add(user.ProfilePictureURL);

                    _context.Follows.RemoveRange(_context.Follows.Where(f => f.FollowerId == user.Id || f.FollowingId == user.Id));
                    _context.Likes.RemoveRange(_context.Likes.Where(l => l.UserId == user.Id));
                    _context.Comments.RemoveRange(_context.Comments.Where(c => c.UserId == user.Id));

                    _context.SaveChanges();

                    List<Video> videos = _context.Videos.Where(v => v.UserId == user.Id).ToList();

                    foreach(var video in videos)
                    {
                        urls.Add(video.VideoURL);

                        _context.Likes.RemoveRange(_context.Likes.Where(l => l.VideoId == video.Id));
                        _context.Comments.RemoveRange(_context.Comments.Where(c => c.VideoId == video.Id));
                    }

                    _context.Videos.RemoveRange(videos);
                    _context.Users.Remove(user);

                    _context.SaveChanges();

                    foreach(var url in urls)
                    {
                        if (url != null)
                        {
                            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/'));

                            if (System.IO.File.Exists(path))
                                System.IO.File.Delete(path);
                        }
                    }

                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    return RedirectToAction("Login", "Authentication");
                }
                catch (Exception)
                {

                }
            }
        }
            
        return NoContent();
    }

    public IActionResult Share(int videoId)
    {
        Video? video = _context.Videos.FirstOrDefault(u => u.Id == videoId);

        if (video != null)
        {
            try
            {
                video.shares++;
                _context.SaveChanges();
            }
            catch
            {


            }
        }

        return NoContent();
    }
}
