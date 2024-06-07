using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PawPok.Data;
using PawPok.Models;
using System.Security.Claims;

namespace PawPok.Controllers;

public class AuthenticationController : Controller
{
    private readonly DataContext _context;

    public AuthenticationController(DataContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Register(Register model)
    {
        if(ModelState.IsValid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Username))
                    ModelState.AddModelError(string.Empty, "Username is empty");
                else if (string.IsNullOrWhiteSpace(model.Password))
                    ModelState.AddModelError(string.Empty, "Password is empty");
                else if (string.IsNullOrWhiteSpace(model.PasswordConfirm))
                    ModelState.AddModelError(string.Empty, "You have to confirm your password");
                else if (model.Password != model.PasswordConfirm)
                    ModelState.AddModelError(string.Empty, "Passwords do not match");
                else if (_context.Users.Where(u => u.Username == model.Username).Any())
                    ModelState.AddModelError(string.Empty, "Username is already taken");
                else
                {
                    try
                    {
                        User user = new User(model.Username, model.Password);

                        _context.Add(user);
                        _context.SaveChanges();

                        List<Claim> claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username!),
                            new Claim("pfp", user.ProfilePictureURL!)
                        };

                        var claimIdentity = new ClaimsIdentity(claims, "AuthenticationCookie");

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity));

                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Error creating account: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(User user)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                ModelState.AddModelError(string.Empty, "Username is empty");
            else if (string.IsNullOrWhiteSpace(user.Password))
                ModelState.AddModelError(string.Empty, "Password is empty");
            else if (!_context.Users.Where(u => u.Username == user.Username && u.Password == user.Password).Any())
                ModelState.AddModelError(string.Empty, "Invalid username or password");
            else
            {
                try
                {
                    string pfp = _context.Users.FirstOrDefault(u => u.Username == user.Username)!.ProfilePictureURL!;

                    List<Claim> claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim("pfp", pfp)
                        };

                    var claimIdentity = new ClaimsIdentity(claims, "AuthenticationCookie");

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimIdentity));

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error logging in: " + ex.Message);
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
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        if (User!.Identity!.IsAuthenticated)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login", "Authentication");
    }
}
