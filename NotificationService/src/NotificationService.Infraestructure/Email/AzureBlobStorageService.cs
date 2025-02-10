using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infraestructure.Email
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration,
                                       ILogger<AzureBlobStorageService> logger)
        {
            _logger = logger;
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
            {
                _logger.LogWarning("AzureBlobStorageService não configurado corretamente.");
                return;
            }

            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<Stream> GetFileStreamAsync(string blobPath)
        {
            if (_containerClient == null)
            {
                _logger.LogError("BlobContainerClient não está configurado.");
                return null;
            }

            try
            {
                string fileName = Path.GetFileName(new Uri(blobPath).AbsolutePath);
                var blobClient = _containerClient.GetBlobClient(fileName);

                var exists = await blobClient.ExistsAsync();
                if (!exists.Value)
                {
                    _logger.LogWarning("Blob não encontrado: {blobPath}", blobPath);
                    return null;
                }

                var downloadInfo = await blobClient.DownloadAsync();
                return downloadInfo.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter stream do blob: {blobPath}", blobPath);
                return null;
            }
        }
    }
}
