namespace DotaHelper.Helpers;

public static class ConsoleHelper
{
    public static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void WriteLineColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void WriteLabelValue(string label, string value, ConsoleColor valueColor)
    {
        Console.Write(label);
        Console.ForegroundColor = valueColor;
        Console.WriteLine(value);
        Console.ResetColor();
    }
}
