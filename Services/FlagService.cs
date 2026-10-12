using System.Security.Cryptography;
using System.Text;
using CTFGame.Data;
using CTFGame.Models;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Services;

public enum SubmitResult
{
    Correct,
    Incorrect,
    AlreadySolved,
    RateLimited,
    NotFound,
}

public class FlagService(AppDbContext db)
{
    public const int MaxAttemptsPerMinute = 10;

    public static string Hash(string flag) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(flag.Trim())));

    public static bool Verify(string flag, string expectedHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(flag)),
            Encoding.ASCII.GetBytes(expectedHash));

    public async Task<SubmitResult> SubmitAsync(int userId, int challengeId, string flag)
    {
        var challenge = await db.Challenges.FindAsync(challengeId);
        if (challenge is null || !challenge.IsVisible)
            return SubmitResult.NotFound;

        if (await db.Solves.AnyAsync(s => s.UserId == userId && s.ChallengeId == challengeId))
            return SubmitResult.AlreadySolved;

        var since = DateTime.UtcNow.AddMinutes(-1);
        var recent = await db.Submissions.CountAsync(s => s.UserId == userId && s.SubmittedAt > since);
        if (recent >= MaxAttemptsPerMinute)
            return SubmitResult.RateLimited;

        var correct = Verify(flag, challenge.FlagHash);
        db.Submissions.Add(new Submission { UserId = userId, ChallengeId = challengeId, IsCorrect = correct });
        if (correct)
            db.Solves.Add(new Solve { UserId = userId, ChallengeId = challengeId });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException) when (correct)
        {
            // Параллельная отправка того же флага: уникальный индекс (UserId, ChallengeId) уже сработал.
            return SubmitResult.AlreadySolved;
        }

        return correct ? SubmitResult.Correct : SubmitResult.Incorrect;
    }
}
