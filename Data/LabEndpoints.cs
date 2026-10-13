namespace CTFGame.Data;

/// <summary>
/// Намеренно «уязвимые» эндпоинты для веб-заданий. Флаги здесь лежат открытым текстом —
/// это часть заданий, а не ошибка.
/// </summary>
public static class LabEndpoints
{
    public const string HeadersFlag = "CTF{headers_carry_secrets}";
    public const string MethodFlag = "CTF{post_it_like_its_hot}";
    public const string BotFlag = "CTF{user_agent_is_just_a_string}";
    public const string CookieFlag = "CTF{never_trust_the_cookie}";

    public static void MapLabEndpoints(this IEndpointRouteBuilder app)
    {
        var lab = app.MapGroup("/lab");

        lab.MapGet("/headers", (HttpResponse response) =>
        {
            response.Headers["X-Flag"] = HeadersFlag;
            return Results.Text("Здесь ничего нет. Проходите мимо.", "text/plain; charset=utf-8");
        });

        lab.MapMethods("/method", ["GET", "HEAD"], () =>
            Results.Text("405: такой метод мне не нравится. Попробуйте отправить мне что-нибудь.",
                "text/plain; charset=utf-8", statusCode: StatusCodes.Status405MethodNotAllowed));
        lab.MapPost("/method", () => Results.Text(MethodFlag, "text/plain; charset=utf-8"));

        lab.MapGet("/bot", (HttpRequest request) =>
            request.Headers.UserAgent.ToString().Contains("CTFBot", StringComparison.OrdinalIgnoreCase)
                ? Results.Text($"Добро пожаловать, робот! {BotFlag}", "text/plain; charset=utf-8")
                : Results.Text($"Доступ запрещён. Ваш User-Agent: {request.Headers.UserAgent}", "text/plain; charset=utf-8",
                    statusCode: StatusCodes.Status403Forbidden));

        lab.MapGet("/vip", (HttpContext http) =>
        {
            var role = http.Request.Cookies["role"];
            if (role is null)
            {
                role = "guest";
                http.Response.Cookies.Append("role", role, new CookieOptions { Path = "/lab" });
            }

            return role == "admin"
                ? Results.Text($"Добро пожаловать, администратор! {CookieFlag}", "text/plain; charset=utf-8")
                : Results.Text($"Привет, {role}. VIP-зона только для admin.", "text/plain; charset=utf-8",
                    statusCode: StatusCodes.Status403Forbidden);
        });
    }
}
