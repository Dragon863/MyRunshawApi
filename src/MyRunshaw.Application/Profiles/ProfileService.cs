using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Storage;
using SkiaSharp;

namespace MyRunshaw.Application.Users;

public class ProfileService : IProfileService
{
    private readonly IStorageService _storageService;
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache _cache;

    private const int MaxFileSizeInBytes = 10 * 1024 * 1024;
    private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public ProfileService(IStorageService storageService, IUserRepository userRepository, IDistributedCache cache)
    {
        _storageService = storageService;
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<string> UploadProfilePictureAsync(string studentId, IFormFile file)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File is empty.");
        if (file.Length > MaxFileSizeInBytes) throw new ArgumentException("File exceeds the 10MB size limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"Invalid file type {extension}. Only JPG, PNG, and WebP are allowed.");
        }

        using var inputStream = file.OpenReadStream();
        using var originalBitmap = SKBitmap.Decode(inputStream);

        if (originalBitmap == null)
        {
            throw new ArgumentException("The uploaded file is not a valid image.");
        }

        // calculate a perfect center square crop. this is mostly copied from docs
        int minDimension = Math.Min(originalBitmap.Width, originalBitmap.Height);
        int cropX = (originalBitmap.Width - minDimension) / 2;
        int cropY = (originalBitmap.Height - minDimension) / 2;

        var sourceRect = new SKRect(cropX, cropY, cropX + minDimension, cropY + minDimension);
        var destRect = new SKRect(0, 0, 512, 512);

        var info = new SKImageInfo(512, 512);
        using var surface = SKSurface.Create(info);
        using var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { IsAntialias = true };
        var sampling = new SKSamplingOptions(SKCubicResampler.CatmullRom);
        canvas.DrawImage(SKImage.FromBitmap(originalBitmap), sourceRect, destRect, sampling, paint);


        // encode to webp format (85 is the quality out of 100)
        using var finalImage = surface.Snapshot();
        using var webpData = finalImage.Encode(SKEncodedImageFormat.Webp, 85);

        using var outStream = new MemoryStream();
        webpData.SaveTo(outStream);
        outStream.Position = 0;

        // upload to S3 as a guaranteed .webp file (nice!)
        var fileName = $"{studentId}.webp";
        var url = await _storageService.UploadPublicFileAsync(outStream, fileName, "image/webp");

        // increment version in db
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (user != null)
        {
            user.ProfilePicVersion += 1;
            await _userRepository.UpdateAsync(user);
        }

        return $"{url}?v={user?.ProfilePicVersion ?? 1}";
    }

    public async Task UpdateNameAsync(string studentId, string newName)
    {
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (user == null) throw new KeyNotFoundException("User not found.");

        user.Name = newName;
        await _userRepository.UpdateAsync(user);

        // invalidate cache to be refreshed next time
        await _cache.RemoveAsync($"user_name:{studentId}");
    }

    public async Task DeleteProfilePictureAsync(string studentId)
    {
        var user = await _userRepository.GetByStudentIdAsync(studentId);
        if (user == null) throw new KeyNotFoundException("User not found.");

        var fileName = $"{studentId}.webp";
        await _storageService.DeleteFileAsync(fileName);

        // increment version in db
        user.ProfilePicVersion += 1;
        await _userRepository.UpdateAsync(user);
    }
}