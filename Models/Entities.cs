using System.ComponentModel.DataAnnotations;

namespace CTFGame.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string UserName { get; set; } = "";

    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Solve> Solves { get; set; } = new();
}

public class Challenge
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = "";

    [MaxLength(50)]
    public string Category { get; set; } = "";

    /// <summary>HTML-описание. Пишется только администратором и выводится как есть.</summary>
    public string Description { get; set; } = "";

    public int Points { get; set; }

    /// <summary>SHA-256 флага в hex. Сам флаг в БД не хранится.</summary>
    public string FlagHash { get; set; } = "";

    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Solve> Solves { get; set; } = new();
}

public class Solve
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public DateTime SolvedAt { get; set; } = DateTime.UtcNow;
}

public class Submission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
