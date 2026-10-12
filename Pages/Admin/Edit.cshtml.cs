using System.ComponentModel.DataAnnotations;
using CTFGame.Data;
using CTFGame.Models;
using CTFGame.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Pages.Admin;

public class EditModel(AppDbContext db) : PageModel
{
    public class InputModel
    {
        [Required(ErrorMessage = "Укажите название"), StringLength(100)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Укажите категорию"), StringLength(50)]
        public string Category { get; set; } = "";

        [Required(ErrorMessage = "Добавьте описание")]
        public string Description { get; set; } = "";

        [Range(1, 10000, ErrorMessage = "От 1 до 10000")]
        public int Points { get; set; } = 100;

        [StringLength(200)]
        public string? Flag { get; set; }

        public bool IsVisible { get; set; } = true;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public bool IsNew => Id is null;
    public List<string> KnownCategories { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsNew)
        {
            var challenge = await db.Challenges.FindAsync(Id);
            if (challenge is null)
                return NotFound();

            Input = new InputModel
            {
                Title = challenge.Title,
                Category = challenge.Category,
                Description = challenge.Description,
                Points = challenge.Points,
                IsVisible = challenge.IsVisible,
            };
        }

        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (IsNew && string.IsNullOrWhiteSpace(Input.Flag))
            ModelState.AddModelError("Input.Flag", "Для нового задания флаг обязателен");

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return Page();
        }

        Challenge? entity;
        if (IsNew)
        {
            entity = new Challenge();
            db.Challenges.Add(entity);
        }
        else
        {
            entity = await db.Challenges.FindAsync(Id);
            if (entity is null)
                return NotFound();
        }

        entity.Title = Input.Title.Trim();
        entity.Category = Input.Category.Trim();
        entity.Description = Input.Description;
        entity.Points = Input.Points;
        entity.IsVisible = Input.IsVisible;
        if (!string.IsNullOrWhiteSpace(Input.Flag))
            entity.FlagHash = FlagService.Hash(Input.Flag);

        await db.SaveChangesAsync();
        TempData["Success"] = $"Задание «{entity.Title}» сохранено.";
        return RedirectToPage("Index");
    }

    private async Task LoadCategoriesAsync() =>
        KnownCategories = await db.Challenges.Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync();
}
