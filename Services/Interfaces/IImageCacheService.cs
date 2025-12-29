namespace todo_app.Services.Interfaces
{
    public interface IImageCacheService
    {
        Task<string> GetCurrentImagePathAsync();
    }
}
