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

        // Демо-задания дозаливаются по названию: новые появятся и в уже существующей базе.
        // Чтобы удалённые через админку задания не возвращались, выключите Seed:DemoChallenges.
        if (config.GetValue("Seed:DemoChallenges", true))
        {
            var existing = (await db.Challenges.Select(c => c.Title).ToListAsync()).ToHashSet();
            var missing = SampleChallenges().Where(c => !existing.Contains(c.Title)).ToList();
            db.Challenges.AddRange(missing);
            if (missing.Count > 0)
                logger.LogInformation("Добавлено демо-заданий: {Count}", missing.Count);
        }

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

        const string reverse = "CTF{read_it_backwards}";
        yield return Make("Зеркало", "Misc", 50, reverse,
            $"<p>Кто-то написал флаг, глядя в зеркало.</p><pre>{new string(reverse.Reverse().ToArray())}</pre>");

        const string binary = "CTF{ones_and_zeros}";
        yield return Make("Нули и единицы", "Forensics", 100, binary,
            $"<p>Перехваченная передача:</p><pre>{string.Join(' ', Encoding.ASCII.GetBytes(binary).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}</pre>");

        yield return Make("Точка-тире", "Crypto", 100, "CTF{morse_is_fun}",
            $"<p>Сигнал с маяка. Ответ запишите строчными буквами и оберните в <code>CTF{{...}}</code>.</p><pre>{Morse("MORSE_IS_FUN")}</pre>");

        const string xor = "CTF{xor_with_a_single_byte}";
        const byte xorKey = 0x5A;
        yield return Make("Один байт", "Crypto", 200, xor,
            $"<p>Каждый байт флага сложили по XOR с одним и тем же ключом. Ключ — один байт.</p><pre>{Convert.ToHexString(Encoding.ASCII.GetBytes(xor).Select(b => (byte)(b ^ xorKey)).ToArray()).ToLowerInvariant()}</pre>");

        const string matryoshka = "CTF{layers_upon_layers}";
        var layered = Convert.ToBase64String(Encoding.ASCII.GetBytes(
            Convert.ToHexString(Encoding.ASCII.GetBytes(Rot13(matryoshka))).ToLowerInvariant()));
        yield return Make("Матрёшка", "Crypto", 250, matryoshka,
            $"<p>Внутри одной кодировки другая, а внутри неё — третья.</p><pre>{layered}</pre>");

        const string jsLock = "CTF{client_side_is_not_security}";
        yield return Make("Кодовый замок", "Web", 150, jsLock,
            $$"""
            <p>Введите код, чтобы открыть замок. Проверка выполняется прямо в браузере — надёжно же?</p>
            <div class="flag-form"><input id="lock-code" placeholder="код" autocomplete="off" /><button type="button" class="btn" onclick="checkLock()">Открыть</button></div>
            <p id="lock-result"></p>
            <script>
            function checkLock() {
                const secret = [{{string.Join(',', jsLock.Select(c => c ^ 7))}}].map(c => String.fromCharCode(c ^ 7)).join('');
                const ok = document.getElementById('lock-code').value === secret;
                document.getElementById('lock-result').textContent = ok ? 'Открыто! Сдайте этот код как флаг.' : 'Неверный код.';
            }
            </script>
            """);

        yield return Make("Что в заголовке?", "Web", 100, LabEndpoints.HeadersFlag,
            "<p>На этой <a href=\"/lab/headers\" target=\"_blank\">странице</a> ничего нет. Совсем ничего. По крайней мере, в теле ответа.</p>");

        yield return Make("Не тот метод", "Web", 150, LabEndpoints.MethodFlag,
            "<p><a href=\"/lab/method\" target=\"_blank\">Этот адрес</a> не любит, когда его просто открывают в браузере.</p>");

        yield return Make("Только для своих", "Web", 150, LabEndpoints.BotFlag,
            "<p><a href=\"/lab/bot\" target=\"_blank\">Сюда</a> пускают только нашего робота <code>CTFBot</code>. Браузер выдаёт себя с первого же запроса.</p>");

        yield return Make("Печенька", "Web", 200, LabEndpoints.CookieFlag,
            "<p>В <a href=\"/lab/vip\" target=\"_blank\">VIP-зону</a> пускают только администратора. Но как сайт узнаёт, кто вы?</p>");
    }

    private static string Morse(string text)
    {
        var table = new Dictionary<char, string>
        {
            ['A'] = ".-", ['B'] = "-...", ['C'] = "-.-.", ['D'] = "-..", ['E'] = ".", ['F'] = "..-.", ['G'] = "--.",
            ['H'] = "....", ['I'] = "..", ['J'] = ".---", ['K'] = "-.-", ['L'] = ".-..", ['M'] = "--", ['N'] = "-.",
            ['O'] = "---", ['P'] = ".--.", ['Q'] = "--.-", ['R'] = ".-.", ['S'] = "...", ['T'] = "-", ['U'] = "..-",
            ['V'] = "...-", ['W'] = ".--", ['X'] = "-..-", ['Y'] = "-.--", ['Z'] = "--..", ['_'] = "..--.-",
        };
        return string.Join(' ', text.Select(c => table[c]));
    }

    private static string Rot13(string s) => new(s.Select(c => c switch
    {
        >= 'a' and <= 'z' => (char)('a' + (c - 'a' + 13) % 26),
        >= 'A' and <= 'Z' => (char)('A' + (c - 'A' + 13) % 26),
        _ => c,
    }).ToArray());
}
