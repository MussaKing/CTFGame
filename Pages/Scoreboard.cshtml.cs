using CTFGame.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages;

public class ScoreboardModel(AppDbContext db) : PageModel
{
    public record Row(string UserName, int Score, int Solved, DateTime? LastSolve);

    public List<Row> Rows { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var rows = await db.Users
            .Where(u => !u.IsAdmin)
            .Select(u => new Row(
                u.UserName,
                u.Solves.Sum(s => s.Challenge.Points),
                u.Solves.Count,
                u.Solves.Max(s => (DateTime?)s.SolvedAt)))
            .ToListAsync();

        Rows = rows
            .Where(r => r.Solved > 0)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.LastSolve)
            .ToList();
    }
}
