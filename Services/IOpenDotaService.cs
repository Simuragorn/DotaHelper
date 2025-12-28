namespace DotaHelper.Services;

public interface IOpenDotaService
{
    Task<string?> GetPlayerPersonaNameAsync(string dotaId);
}
