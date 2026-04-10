namespace CoffeeShopAPI.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration     _config;
    private readonly IHttpContextAccessor _accessor;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public FileService(IWebHostEnvironment env, IConfiguration config, IHttpContextAccessor accessor)
    {
        _env      = env;
        _config   = config;
        _accessor = accessor;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        // Validate extension
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use: jpg, jpeg, png, webp");

        // Validate size
        var maxMb = _config.GetValue<int>("FileStorage:MaxFileSizeMB", 5);
        if (file.Length > maxMb * 1024 * 1024)
            throw new InvalidOperationException($"File exceeds maximum size of {maxMb}MB.");

        // Build save path
        var uploadPath = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        var fileName  = $"{Guid.NewGuid()}{ext}";
        var filePath  = Path.Combine(uploadPath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return a public URL
        var request  = _accessor.HttpContext!.Request;
        var baseUrl  = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/uploads/{folder}/{fileName}";
    }

    public bool DeleteImage(string imageUrl)
    {
        try
        {
            // Extract relative path from URL
            var uri      = new Uri(imageUrl);
            var relative = uri.AbsolutePath.TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, relative.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath)) return false;
            File.Delete(fullPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
