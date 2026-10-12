using System.ComponentModel.DataAnnotations;
using CTFGame.Data;
using CTFGame.Infrastructure;
using CTFGame.Models;
using CTFGame.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Challenges;

public class DetailsModel(AppDbContext db, FlagService flags) : PageModel
{
    public record Solver(string UserName, DateTime SolvedAt);

    public Challenge Item { get; private set; } = null!;
    public bool Solved { get; private set; }
    public int SolveCount { get; private set; }
    public List<Solver> Solvers { get; private set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Введите флаг")]
    [StringLength(200)]
    public string Flag { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int id) =>
        await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
            return await LoadAsync(id) ? Page() : NotFound();

        var result = await flags.SubmitAsync(User.GetUserId(), id, Flag);
        switch (result)
        {
            case SubmitResult.NotFound:
                return NotFound();
            case SubmitResult.Correct:
                TempData["Success"] = "Флаг верный! Очки начислены.";
                break;
            case SubmitResult.AlreadySolved:
                TempData["Success"] = "Это задание уже решено.";
                break;
            case SubmitResult.RateLimited:
                TempData["Error"] = $"Слишком много попыток. Не больше {FlagService.MaxAttemptsPerMinute} в минуту.";
                break;
            default:
                TempData["Error"] = "Неверный флаг.";
                break;
        }

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var challenge = await db.Challenges.FindAsync(id);
        if (challenge is null || (!challenge.IsVisible && !User.IsInRole("Admin")))
            return false;

        Item = challenge;
        var userId = User.GetUserId();
        Solved = await db.Solves.AnyAsync(s => s.ChallengeId == id && s.UserId == userId);

        var solves = db.Solves.Where(s => s.ChallengeId == id && !s.User.IsAdmin);
        SolveCount = await solves.CountAsync();
        Solvers = await solves
            .OrderBy(s => s.SolvedAt)
            .Take(10)
            .Select(s => new Solver(s.User.UserName, s.SolvedAt))
            .ToListAsync();
        return true;
    }
}
