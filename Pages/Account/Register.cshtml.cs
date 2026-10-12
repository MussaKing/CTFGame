using System.ComponentModel.DataAnnotations;
using CTFGame.Data;
using CTFGame.Infrastructure;
using CTFGame.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Account;

public class RegisterModel(AppDbContext db, IPasswordHasher<User> hasher) : PageModel
{
    public class InputModel
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        [RegularExpression(@"^[A-Za-z0-9_\-]{3,32}$",
            ErrorMessage = "3–32 символа: латиница, цифры, «_» и «-»")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Введите пароль"), DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Пароль должен быть не короче 8 символов")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = "";
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (await db.Users.AnyAsync(u => u.UserName == Input.UserName))
        {
            ModelState.AddModelError("Input.UserName", "Это имя уже занято");
            return Page();
        }

        var user = new User { UserName = Input.UserName };
        user.PasswordHash = hasher.HashPassword(user, Input.Password);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Input.UserName", "Это имя уже занято");
            return Page();
        }

        await HttpContext.SignInUserAsync(user);
        TempData["Success"] = $"Добро пожаловать, {user.UserName}!";
        return RedirectToPage("/Challenges/Index");
    }
}
