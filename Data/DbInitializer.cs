using System.Security.Cryptography;
using System.Text;
using CTFGame.Models;
using CTFGame.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync(u => u.IsAdmin))
        {
            var userName = config["Admin:UserName"] is { Length: > 0 } n ? n : "admin";
            var password = config["Admin:Password"];
            var generated = string.IsNullOrWhiteSpace(password);
            if (generated)
                password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));

            var admin = new User { UserName = userName, IsAdmin = true };
            admin.PasswordHash = hasher.HashPassword(admin, password!);
            db.Users.Add(admin);

            if (generated)
                logger.LogWarning("Создан администратор '{UserName}' с паролем: {Password} — смените его или задайте Admin:Password",
                    userName, password);
        }

        if (!await db.Challenges.AnyAsync())
            db.Challenges.AddRange(SampleChallenges());

        await db.SaveChangesAsync();
    }

    private static IEnumerable<Challenge> SampleChallenges()
    {
        static Challenge Make(string title, string category, int points, string flag, string description) => new()
        {
            Title = title,
            Category = category,
            Points = points,
            FlagHash = FlagService.Hash(flag),
            Description = description,
        };

        const string welcome = "CTF{welcome_to_the_game}";
        const string base64 = "CTF{base64_is_not_encryption}";
        const string rot13 = "CTF{caesar_would_be_proud}";
        const string hex = "CTF{hex_marks_the_spot}";
        const string source = "CTF{view_source_is_your_friend}";

        yield return Make("Добро пожаловать", "Misc", 50, welcome,
            $"<p>Флаги имеют формат <code>CTF{{...}}</code>. Вот ваш первый: <code>{welcome}</code></p>");

        yield return Make("Base64", "Crypto", 100, base64,
            $"<p>Кажется, это зашифровано. Или нет?</p><pre>{Convert.ToBase64String(Encoding.UTF8.GetBytes(base64))}</pre>");

        yield return Make("Салат Цезаря", "Crypto", 150, rot13,
            $"<p>Юлий любил сдвигать буквы. На 13 позиций.</p><pre>{Rot13(rot13)}</pre>");

        yield return Make("Шестнадцать", "Forensics", 100, hex,
            $"<p>В дампе памяти нашлась странная строка:</p><pre>{Convert.ToHexString(Encoding.UTF8.GetBytes(hex)).ToLowerInvariant()}</pre>");

        yield return Make("Смотри глубже", "Web", 100, source,
            $"<p>Флаг где-то на этой странице. Глазами вы его не увидите.</p><!-- {source} -->");

        yield return Make("Роботам вход воспрещён", "Web", 150, "CTF{robots_txt_tells_everything}",
            "<p>Поисковым роботам на этом сайте кое-что запрещено индексировать. Интересно, что именно?</p>");
    }

    private static string Rot13(string s) => new(s.Select(c => c switch
    {
        >= 'a' and <= 'z' => (char)('a' + (c - 'a' + 13) % 26),
        >= 'A' and <= 'Z' => (char)('A' + (c - 'A' + 13) % 26),
        _ => c,
    }).ToArray());
}
