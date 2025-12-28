namespace DotaHelper.Services;

public interface IStorageService<T>
{
    T? Load();
    void Save(T data);
    bool Exists();
}
