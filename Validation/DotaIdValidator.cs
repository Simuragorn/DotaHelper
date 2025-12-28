using System.Text.RegularExpressions;

namespace DotaHelper.Validation;

public class DotaIdValidator : IValidator<string>
{
    private static readonly Regex DotaIdPattern = new(@"^\d{9}$", RegexOptions.Compiled);
    private const string ErrorMessage = "Dota ID must be exactly 9 digits";

    public bool IsValid(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && DotaIdPattern.IsMatch(value);
    }

    public string GetErrorMessage()
    {
        return ErrorMessage;
    }
}
