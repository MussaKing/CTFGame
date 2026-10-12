using CTFGame.Data;
using CTFGame.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Challenges;

public class IndexModel(AppDbContext db) : PageModel
{
    public record Item(int Id, string Title, string Category, int Points, int Solves, bool Solved);

    public List<IGrouping<string, Item>> Categories { get; private set; } = new();
    public int Score { get; private set; }
    public int SolvedCount { get; private set; }
    public int TotalCount { get; private set; }

    public async Task OnGetAsync()
    {
        var userId = User.GetUserId();
        var solvedIds = (await db.Solves
            .Where(s => s.UserId == userId)
            .Select(s => s.ChallengeId)
            .ToListAsync()).ToHashSet();

        var challenges = await db.Challenges
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Category).ThenBy(c => c.Points).ThenBy(c => c.Title)
            .Select(c => new { c.Id, c.Title, c.Category, c.Points, Solves = c.Solves.Count(s => !s.User.IsAdmin) })
            .ToListAsync();

        var items = challenges
            .Select(c => new Item(c.Id, c.Title, c.Category, c.Points, c.Solves, solvedIds.Contains(c.Id)))
            .ToList();

        Categories = items.GroupBy(i => i.Category).ToList();
        TotalCount = items.Count;
        SolvedCount = items.Count(i => i.Solved);
        Score = items.Where(i => i.Solved).Sum(i => i.Points);
    }
}
