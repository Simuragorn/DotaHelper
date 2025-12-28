namespace DotaHelper.Menu;

public class DraftMenu : IMenu
{
    public void Display()
    {
        Console.Clear();
        Console.WriteLine("=== Draft ===");
    }

    public Task ExecuteAsync()
    {
        Display();
        Console.WriteLine("Draft feature coming soon");
        Console.WriteLine("\nPress any key to return to main menu...");
        Console.ReadKey();
        return Task.CompletedTask;
    }
}
