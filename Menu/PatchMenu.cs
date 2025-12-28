using DotaHelper.Models;
using DotaHelper.Services;

namespace DotaHelper.Menu;

public class PatchMenu : IMenu
{
    private readonly IStorageService<Patch> _patchStorageService;

    public PatchMenu(IStorageService<Patch> patchStorageService)
    {
        _patchStorageService = patchStorageService;
    }

    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Change Patch ===");
    }

    public async Task ExecuteAsync()
    {
        Display();

        var currentPatch = _patchStorageService.Load();

        if (currentPatch != null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nCurrent patch: {currentPatch.Version}");
            Console.ResetColor();
        }

        Console.WriteLine("\nEnter new patch version:");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nPatch version cannot be empty.");
            Console.ResetColor();
            Console.WriteLine("\nPress any key to return to main menu...");
            Console.ReadKey();
            return;
        }

        var patch = new Patch
        {
            Version = input.Trim(),
            LastModified = DateTime.UtcNow
        };

        _patchStorageService.Save(patch);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Patch updated to: {patch.Version}");
        Console.ResetColor();

        await Task.CompletedTask;
    }
}
