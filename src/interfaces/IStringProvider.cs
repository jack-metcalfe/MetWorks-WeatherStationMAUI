namespace Interfaces;
public interface IStringProvider
{
    Task<string?> GetAsync(string key);
}

