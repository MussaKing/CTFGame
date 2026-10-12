using CTFGame.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public int ChallengeCount { get; private set; }
    public int PlayerCount { get; private set; }
    public int SolveCount { get; private set; }

    public async Task OnGetAsync()
    {
        ChallengeCount = await db.Challenges.CountAsync(c => c.IsVisible);
        PlayerCount = await db.Users.CountAsync(u => !u.IsAdmin);
        SolveCount = await db.Solves.CountAsync(s => !s.User.IsAdmin);
    }
}
