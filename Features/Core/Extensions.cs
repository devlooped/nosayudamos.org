using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace NosAyudamos
{
    static class Extensions
    {
        public static string ToSingleLine(this string value)
            => string.Join(' ', value
                .Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()));

        public static async Task ClearStorageAsync(this CloudStorageAccount storageAccount)
        {
            await CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient()
                .GetTableReference("Person")
                .DeleteIfExistsAsync();

            await CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient()
                .GetTableReference("Entity")
                .DeleteIfExistsAsync();

            await CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient()
                .GetTableReference("Event")
                .DeleteIfExistsAsync();
        }
    }
}
