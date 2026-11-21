namespace Gasoft.Api.Endpoints;

internal static class ExpenseReceiptStorage
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, (string Extension, byte[] Signature)> ReceiptTypes =
        new Dictionary<string, (string Extension, byte[] Signature)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", [0xff, 0xd8, 0xff]),
            ["image/png"] = (".png", [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
            ["image/webp"] = (".webp", []),
            ["application/pdf"] = (".pdf", "%PDF-"u8.ToArray())
        };

    public static async Task<(string? FileName, string? Error)> SaveAsync(
        IFormFile receipt,
        IWebHostEnvironment environment,
        long tenantId,
        long companyId,
        long reportId,
        long lineId,
        CancellationToken cancellationToken)
    {
        if (receipt.Length is <= 0 or > MaxFileSize)
            return (null, "Choose an image or PDF no larger than 5 MB.");
        if (!ReceiptTypes.TryGetValue(receipt.ContentType, out var type))
            return (null, "Choose a JPEG, PNG, WebP, or PDF receipt.");

        var header = new byte[12];
        await using var input = receipt.OpenReadStream();
        var headerLength = await input.ReadAtLeastAsync(
            header, type.Signature.Length == 0 ? header.Length : type.Signature.Length, false, cancellationToken);
        if (!MatchesType(receipt.ContentType, header, headerLength, type.Signature))
            return (null, "The receipt content does not match its file type.");
        input.Position = 0;

        var fileName = $"{Guid.NewGuid():N}{type.Extension}";
        var directory = GetDirectory(environment, tenantId, companyId, reportId, lineId);
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
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            throw;
        }

        return (fileName, null);
    }

    public static void Delete(
        IWebHostEnvironment environment,
        long tenantId,
        long companyId,
        long reportId,
        long lineId,
        string fileName)
    {
        if (!TryResolvePath(environment, tenantId, companyId, reportId, lineId, fileName, out var path))
            return;
        if (File.Exists(path))
            File.Delete(path);
    }

    public static (FileStream? Stream, string? ContentType) OpenRead(
        IWebHostEnvironment environment,
        long tenantId,
        long companyId,
        long reportId,
        long lineId,
        string fileName)
    {
        if (!TryResolvePath(environment, tenantId, companyId, reportId, lineId, fileName, out var path) ||
            !File.Exists(path))
        {
            return (null, null);
        }

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => null
        };
        return contentType is null
            ? (null, null)
            : (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
    }

    private static bool TryResolvePath(
        IWebHostEnvironment environment,
        long tenantId,
        long companyId,
        long reportId,
        long lineId,
        string fileName,
        out string path)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var validExtension = ReceiptTypes.Values.Any(type => type.Extension == extension);
        var validName = Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _);
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
        path = Path.GetFullPath(Path.Combine(
            root,
            "expense-receipts",
            tenantId.ToString(),
            companyId.ToString(),
            reportId.ToString(),
            lineId.ToString(),
            fileName));
        return validName &&
            validExtension &&
            Path.GetFileName(fileName) == fileName &&
            path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string GetDirectory(
        IWebHostEnvironment environment,
        long tenantId,
        long companyId,
        long reportId,
        long lineId) =>
        Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "uploads",
            "expense-receipts",
            tenantId.ToString(),
            companyId.ToString(),
            reportId.ToString(),
            lineId.ToString());

    private static bool MatchesType(string contentType, byte[] header, int headerLength, byte[] signature)
    {
        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return headerLength >= 12 &&
                header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return headerLength >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
    }
}
