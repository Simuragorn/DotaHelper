using DotaHelper.Models;

namespace DotaHelper.Services;

public interface IUserProfileService
{
    UserProfile? GetProfile();
    void SaveProfile(string dotaId);
    bool HasProfile();
}
