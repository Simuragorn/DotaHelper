using System.Text.Json;

namespace DotaHelper.Services;

public class JsonStorageService<T> : IStorageService<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;

    public JsonStorageService(string fileName)
    {
        string projectPath = Directory.GetCurrentDirectory();
        string fetchedDataPath = Path.Combine(projectPath, "FetchedData");
        _filePath = Path.Combine(fetchedDataPath, fileName);

        _options = new JsonSerializerOptions { WriteIndented = true };
    }

    public T? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
            return null;
        }
    }

    public void Save(T data)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(data, _options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving data: {ex.Message}");
        }
    }

    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting data: {ex.Message}");
        }
    }
}
