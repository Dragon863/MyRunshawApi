namespace MyRunshaw.Application.Authentication;

public interface IAuthService
{
    // takes the token from Entra, validates it, creates/fetches the user, returns a custom JWT
    Task<string> LoginWithEntraAsync(string entraIdToken);
}