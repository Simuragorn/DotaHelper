using DotaHelper.Models;
using DotaHelper.Validation;

namespace DotaHelper.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IStorageService<UserProfile> _storageService;
    private readonly IValidator<string> _validator;

    public UserProfileService(IStorageService<UserProfile> storageService, IValidator<string> validator)
    {
        _storageService = storageService;
        _validator = validator;
    }

    public UserProfile? GetProfile()
    {
        return _storageService.Load();
    }

    public void SaveProfile(string dotaId)
    {
        if (!_validator.IsValid(dotaId))
        {
            throw new ArgumentException(_validator.GetErrorMessage());
        }

        var profile = new UserProfile
        {
            DotaId = dotaId,
            LastModified = DateTime.UtcNow
        };

        _storageService.Save(profile);
    }

    public bool HasProfile()
    {
        return _storageService.Exists();
    }
}
