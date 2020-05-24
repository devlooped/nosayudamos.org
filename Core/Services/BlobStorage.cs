using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace NosAyudamos
{
    class BlobStorage : IBlobStorage
    {
        static readonly Dictionary<string, string> EmptyMetadata = new Dictionary<string, string>();
        readonly IEnvironment enviroment;

        public BlobStorage(IEnvironment enviroment) => this.enviroment = enviroment;

        public async Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
                return EmptyMetadata;

            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return EmptyMetadata;

            return new Dictionary<string, string>(
                (await blobClient.GetPropertiesAsync()).Value.Metadata,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<Uri?> GetUriAsync(string containerName, string blobName)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
                return default;

            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return default;

            return blobClient.Uri;
        }

        public async Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> blobMetadata)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
                return;

            var blobClient = containerClient.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync())
                return;

            await blobClient.SetMetadataAsync(blobMetadata);
        }

        public async Task<Uri> UploadAsync(byte[] bytes, string containerName, string blobName, IDictionary<string, string>? blobMetadata = null)
        {
            var blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName).ConfigureAwait(false);
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(bytes);
            await blobClient.UploadAsync(stream, true).ConfigureAwait(false);

            if (blobMetadata != null)
                await blobClient.SetMetadataAsync(blobMetadata);

            return blobClient.Uri;
        }

        BlobServiceClient CreateBlobServiceClient() => new BlobServiceClient(enviroment.GetVariable("AzureWebJobsStorage"));
    }

    interface IBlobStorage
    {
        Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName);
        Task<Uri?> GetUriAsync(string containerName, string blobName);
        Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> blobMetadata);
        Task<Uri> UploadAsync(byte[] bytes, string containerName, string blobName, IDictionary<string, string>? blobMetadata = null);
    }
}
