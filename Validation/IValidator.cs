namespace DotaHelper.Validation;

public interface IValidator<T>
{
    bool IsValid(T value);
    string GetErrorMessage();
}
