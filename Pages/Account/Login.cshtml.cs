using System.ComponentModel.DataAnnotations;
using CTFGame.Data;
using CTFGame.Infrastructure;
using CTFGame.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Account;

public class LoginModel(AppDbContext db, IPasswordHasher<User> hasher) : PageModel
{
    public class InputModel
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Введите пароль"), DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == Input.UserName.Trim());
        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            return Page();
        }

        await HttpContext.SignInUserAsync(user);

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Challenges/Index");
    }
}
