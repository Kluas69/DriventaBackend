using Driventa.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Driventa.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadsFolder;

    public FileStorageService(ILogger<FileStorageService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
        if (!Directory.Exists(_uploadsFolder))
            Directory.CreateDirectory(_uploadsFolder);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var storedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_uploadsFolder, storedFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(stream);

        _logger.LogInformation("File uploaded: {FileName} -> {StoredFileName}", fileName, storedFileName);
        return storedFileName;
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        var storedFileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_uploadsFolder, storedFileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted: {StoredFileName}", storedFileName);
        }

        return Task.CompletedTask;
    }

    public string GetFileUrlAsync(string storedFileName)
    {
        return $"/uploads/{storedFileName}";
    }
}
