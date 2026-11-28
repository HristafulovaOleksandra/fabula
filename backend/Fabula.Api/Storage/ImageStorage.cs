namespace Fabula.Api.Storage;

public class ImageStorage(string rootPath)
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string PublicPrefix = "/uploads";

    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    public string RootPath { get; } = Directory.CreateDirectory(rootPath).FullName;

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            throw new ImageRejectedException($"Файл має бути від 1 байта до {MaxBytes / 1024 / 1024} МБ.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Signatures.TryGetValue(extension, out var signatures))
        {
            throw new ImageRejectedException("Дозволені формати: png, jpg, jpeg, webp.");
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!signatures.Any(signature => bytes.AsSpan().StartsWith(signature)))
        {
            throw new ImageRejectedException("Вміст файлу не відповідає його розширенню.");
        }

        var directory = Directory.CreateDirectory(Path.Combine(RootPath, folder));
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory.FullName, fileName), bytes, cancellationToken);
        return $"{PublicPrefix}/{folder}/{fileName}";
    }
}

public static class ImageStorageExtensions
{
    public static string? LocalPath(this ImageStorage storage, string? publicPath)
    {
        if (publicPath is null || !publicPath.StartsWith(ImageStorage.PublicPrefix + "/"))
        {
            return null;
        }

        var relative = publicPath[(ImageStorage.PublicPrefix.Length + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(storage.RootPath, relative));
        return fullPath.StartsWith(storage.RootPath + Path.DirectorySeparatorChar) && File.Exists(fullPath) ? fullPath : null;
    }

    public static void DeleteIfOwned(this ImageStorage storage, string? publicPath)
    {
        if (publicPath is null || !publicPath.StartsWith(ImageStorage.PublicPrefix + "/"))
        {
            return;
        }

        var relative = publicPath[(ImageStorage.PublicPrefix.Length + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(storage.RootPath, relative));
        if (fullPath.StartsWith(storage.RootPath + Path.DirectorySeparatorChar) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}

public class ImageRejectedException(string message) : Exception(message);
