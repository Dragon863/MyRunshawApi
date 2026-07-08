namespace MyRunshaw.Application.Storage;

public interface IStorageService
{
    Task<string> UploadPublicFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string fileName);
}