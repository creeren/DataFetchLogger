using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;

namespace DataFetchLogger
{
    public static class PayloadGetFunction
    {
        [FunctionName("PayloadGetFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payload/{logId}")] HttpRequest req,
            string logId,
            ILogger log)
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("AzureConnectionString");
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("payloads");
            var blobClient = containerClient.GetBlobClient($"{logId}.json");

            try
            {
                if (!await blobClient.ExistsAsync())
                {
                    return new NotFoundResult();
                }

                var downloadInfo = await blobClient.DownloadAsync();

                using (var streamReader = new StreamReader(downloadInfo.Value.Content))
                {
                    string jsonContent = await streamReader.ReadToEndAsync();
                    return new OkObjectResult(jsonContent);
                }
            }
            catch (StorageException ex)
            {
                log.LogError($"Error fetching blob {logId}.json: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
