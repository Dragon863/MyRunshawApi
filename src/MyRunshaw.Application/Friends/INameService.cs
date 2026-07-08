namespace MyRunshaw.Application.Friends;

public interface INameService
{
    Task<string> GetNameAsync(string studentId);
    Task<Dictionary<string, string>> BatchGetNamesAsync(List<string> studentIds);
}