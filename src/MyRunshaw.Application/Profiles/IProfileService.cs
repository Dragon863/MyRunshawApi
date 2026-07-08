using Microsoft.AspNetCore.Http;

namespace MyRunshaw.Application.Users;

public interface IProfileService
{
    Task<string> UploadProfilePictureAsync(string studentId, IFormFile file);
    Task UpdateNameAsync(string studentId, string newName);
    Task DeleteProfilePictureAsync(string studentId);
}