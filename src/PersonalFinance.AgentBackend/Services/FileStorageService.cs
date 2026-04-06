namespace PersonalFinance.AgentBackend.Services;

public interface IFileStorageService
{
    Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<byte[]> GetAsync(string attachmentId, CancellationToken ct = default);
    Task DeleteAsync(string attachmentId, CancellationToken ct = default);
}

/// <summary>
/// Local-disk file storage for development. 
/// Swap with an Azure Blob Storage implementation for production.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "PersonalFinance", "attachments");
        Directory.CreateDirectory(_rootPath);
        _logger = logger;
        _logger.LogInformation("File storage path: {Path}", _rootPath);
    }

    public async Task<string> StoreAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var id = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_rootPath, id);

        await using var fs = File.Create(filePath);
        await content.CopyToAsync(fs, ct);

        _logger.LogInformation("Stored attachment {Id} ({Bytes} bytes)", id, fs.Length);
        return id;
    }

    public async Task<byte[]> GetAsync(string attachmentId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_rootPath, Path.GetFileName(attachmentId));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Attachment not found: {attachmentId}");

        return await File.ReadAllBytesAsync(filePath, ct);
    }

    public Task DeleteAsync(string attachmentId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_rootPath, Path.GetFileName(attachmentId));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted attachment {Id}", attachmentId);
        }
        return Task.CompletedTask;
    }
}
