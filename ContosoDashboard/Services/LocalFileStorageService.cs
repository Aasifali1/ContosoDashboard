namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    public string GetRootPath()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "uploads");
        Directory.CreateDirectory(root);
        return root;
    }

    public async Task<string> UploadAsync(Stream fileStream, string targetPath, string contentType)
    {
        var absolutePath = Path.Combine(GetRootPath(), targetPath.TrimStart('/', '\\'));
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var outputStream = File.Create(absolutePath);
        await fileStream.CopyToAsync(outputStream);

        return absolutePath;
    }

    public Task DeleteAsync(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public async Task<Stream> DownloadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Document file was not found.");
        }

        return await Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration)
    {
        return Task.FromResult(filePath);
    }
}
