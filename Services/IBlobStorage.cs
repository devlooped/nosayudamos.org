using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace NosAyudamos
{
    public interface IBlobStorage
    {
        Task UploadAsync(byte[] bytes, string containerName, string blobName);
    }

    public class BlobStorage : IBlobStorage
    {
        private readonly IEnviroment enviroment;

        public BlobStorage(IEnviroment enviroment)
        {
            Contract.Assert(enviroment != null);

            this.enviroment = enviroment;
        }

        public async Task UploadAsync(byte[] bytes, string containerName, string blobName)
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
        }

        private BlobServiceClient CreateBlobServiceClient() => new BlobServiceClient(enviroment.GetVariable("StorageConnectionString"));
    }
}
