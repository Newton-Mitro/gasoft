namespace Gasoft.Api.Endpoints;

internal static class ImageUpload
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, (string Extension, byte[] Signature)> ImageTypes =
        new Dictionary<string, (string Extension, byte[] Signature)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", [0xff, 0xd8, 0xff]),
            ["image/png"] = (".png", [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
            ["image/webp"] = (".webp", [])
        };

    public static async Task<(string? Url, string? Error)> SaveAsync(
        IFormFile image,
        IWebHostEnvironment environment,
        string category,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (image.Length is <= 0 or > MaxFileSize)
            return (null, "Choose an image no larger than 5 MB.");
        if (!ImageTypes.TryGetValue(image.ContentType, out var type))
            return (null, "Choose a JPEG, PNG, or WebP image.");

        var header = new byte[12];
        await using var input = image.OpenReadStream();
        var headerLength = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        if (!MatchesImageType(image.ContentType, header, headerLength, type.Signature))
            return (null, "The image content does not match its file type.");
        input.Position = 0;

        var fileName = $"{Guid.NewGuid():N}{type.Extension}";
        var relativeUrl = category switch
        {
            "users" => $"/api/v1/account/profile/image/{ownerId}/{fileName}",
            "companies" => $"/api/v1/companies/{ownerId}/logo/{fileName}",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
        var directory = Path.Combine(GetStorageRoot(environment), category, ownerId);
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, fileName);
        try
        {
            await using var output = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await input.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
            throw;
        }

        return (relativeUrl, null);
    }

    public static void Delete(IWebHostEnvironment environment, string? url)
    {
        if (!TryResolveStoredPath(environment, url, out var fullPath))
            return;
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public static (FileStream? Stream, string? ContentType) OpenRead(
        IWebHostEnvironment environment,
        string category,
        string ownerId,
        string fileName)
    {
        if (!TryResolveStoredPath(environment, category, ownerId, fileName, out var fullPath))
            return (null, null);
        if (!File.Exists(fullPath))
            return (null, null);

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        return contentType is null
            ? (null, null)
            : (new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
    }

    public static string ProfileImageUrl(long userId, string fileName) =>
        $"/api/v1/account/profile/image/{userId}/{fileName}";

    public static string CompanyLogoUrl(long companyId, string fileName) =>
        $"/api/v1/companies/{companyId}/logo/{fileName}";

    private static bool TryResolveStoredPath(
        IWebHostEnvironment environment,
        string? url,
        out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (url.StartsWith("/api/v1/account/profile/image/", StringComparison.Ordinal))
        {
            var segments = url["/api/v1/account/profile/image/".Length..].Split('/');
            if (segments.Length != 2 || !long.TryParse(segments[0], out _)) return false;
            return TryResolveStoredPath(environment, "users", segments[0], segments[1], out fullPath);
        }
        if (url.StartsWith("/api/v1/companies/", StringComparison.Ordinal))
        {
            var segments = url["/api/v1/companies/".Length..].Split('/');
            if (segments.Length != 3 || !long.TryParse(segments[0], out _) || segments[1] != "logo") return false;
            return TryResolveStoredPath(environment, "companies", segments[0], segments[2], out fullPath);
        }
        return false;
    }

    private static bool TryResolveStoredPath(
        IWebHostEnvironment environment,
        string category,
        string ownerId,
        string fileName,
        out string fullPath)
    {
        var root = Path.GetFullPath(GetStorageRoot(environment));
        fullPath = Path.GetFullPath(Path.Combine(root, category, ownerId, fileName));
        return Path.GetFileName(fileName) == fileName &&
            fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string GetStorageRoot(IWebHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "uploads");

    private static bool MatchesImageType(string contentType, byte[] header, int headerLength, byte[] signature)
    {
        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return headerLength >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return headerLength >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
    }
}
