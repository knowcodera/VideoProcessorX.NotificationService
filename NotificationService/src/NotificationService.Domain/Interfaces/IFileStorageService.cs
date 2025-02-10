namespace NotificationService.Domain.Interfaces
{
    public interface IFileStorageService
    {
        Task<Stream> GetFileStreamAsync(string blobPath);
    }
}
