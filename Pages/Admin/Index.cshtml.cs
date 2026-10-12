using CTFGame.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Admin;

public class IndexModel(AppDbContext db) : PageModel
{
    public record Row(int Id, string Title, string Category, int Points, int Solves, bool IsVisible);

    public List<Row> Challenges { get; private set; } = new();
    public int PlayerCount { get; private set; }
    public int SubmissionCount { get; private set; }
    public int CorrectCount { get; private set; }

    public async Task OnGetAsync()
    {
        Challenges = await db.Challenges
            .OrderBy(c => c.Category).ThenBy(c => c.Points)
            .Select(c => new Row(c.Id, c.Title, c.Category, c.Points, c.Solves.Count(s => !s.User.IsAdmin), c.IsVisible))
            .ToListAsync();
        PlayerCount = await db.Users.CountAsync(u => !u.IsAdmin);
        SubmissionCount = await db.Submissions.CountAsync();
        CorrectCount = await db.Submissions.CountAsync(s => s.IsCorrect);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var challenge = await db.Challenges.FindAsync(id);
        if (challenge is null)
            return NotFound();

        challenge.IsVisible = !challenge.IsVisible;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var challenge = await db.Challenges.FindAsync(id);
        if (challenge is null)
            return NotFound();

        db.Challenges.Remove(challenge);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Задание «{challenge.Title}» удалено.";
        return RedirectToPage();
    }
}
