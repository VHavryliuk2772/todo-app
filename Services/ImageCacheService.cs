using System.Text.Json;
using todo_app.Services.Interfaces;

namespace todo_app.Services
{
    public class ImageCacheService : IImageCacheService
    {
        private readonly string _imageDir;
        private readonly string _imagePath;
        private readonly string _metaPath;
        private readonly HttpClient _httpClient;
        private readonly object _lock = new();

        private class ImageMeta
        {
            public DateTime LastUpdatedUtc { get; set; }
            public int ServedAfterExpiryCount { get; set; }
        }

        public ImageCacheService(IConfiguration configuration, HttpClient httpClient)
        {
            _imageDir = configuration["HOME_IMAGE_DIR"] ?? "/app/images";
            _imagePath = Path.Combine(_imageDir, "home-image.jpg");
            _metaPath = Path.Combine(_imageDir, "home-image-meta.json");
            _httpClient = httpClient;
        }

        public async Task<string> GetCurrentImagePathAsync()
        {
            Directory.CreateDirectory(_imageDir);

            lock (_lock)
            {
                // блокування тільки на читання метаданих, щоб не ловити гонки
            }

            ImageMeta meta = null;

            if (File.Exists(_metaPath))
            {
                var json = File.ReadAllText(_metaPath);
                try
                {
                    meta = JsonSerializer.Deserialize<ImageMeta>(json);
                }
                catch
                {
                    meta = null;
                }
            }

            var now = DateTime.UtcNow;
            var needNew = false;

            if (meta == null || !File.Exists(_imagePath))
            {
                needNew = true;
            }
            else
            {
                var age = now - meta.LastUpdatedUtc;
                if (age < TimeSpan.FromMinutes(10))
                {
                    // ще свіженьке – просто повертаємо існуюче зображення
                    return _imagePath;
                }

                // старше 10 хв
                if (meta.ServedAfterExpiryCount == 0)
                {
                    // дозволяємо ще ОДИН раз віддати стару картинку
                    meta.ServedAfterExpiryCount = 1;
                    SaveMeta(meta);
                    return _imagePath;
                }

                // було вже віддано один раз після 10 хв → качаємо нову
                needNew = true;
            }

            if (needNew)
            {
                // завантажуємо нову картинку
                var bytes = await _httpClient.GetByteArrayAsync("https://picsum.photos/1200");
                Directory.CreateDirectory(_imageDir);
                await File.WriteAllBytesAsync(_imagePath, bytes);

                meta = new ImageMeta
                {
                    LastUpdatedUtc = now,
                    ServedAfterExpiryCount = 0
                };
                SaveMeta(meta);
            }

            return _imagePath;
        }

        private void SaveMeta(ImageMeta meta)
        {
            var json = JsonSerializer.Serialize(meta);
            File.WriteAllText(_metaPath, json);
        }
    }
}
