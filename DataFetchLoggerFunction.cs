using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace DataFetchLogger
{
    public class DataFetchLoggerFunction
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public DataFetchLoggerFunction(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;    
        }

        [FunctionName("DataFetchLoggerFunction")]
        public async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, 
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            string url = _configuration["Values:URL"];

            if (string.IsNullOrEmpty(url))
            {
                log.LogError("URL is not configured.");
                return;
            }

            string payload = null;
            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                var response = await httpClient.GetAsync(url);
                payload = await response.Content.ReadAsStringAsync();
                await LogAttemptAsync(true, "Success", payload);             
            }
            catch (HttpRequestException httpEx)
            {
                log.LogError($"HTTP request error: {httpEx.Message}");
                await LogAttemptAsync(false, httpEx.Message, payload);
            }
            catch (Exception ex)
            {
                log.LogError($"An error occurred: {ex.Message}");
                await LogAttemptAsync(false, ex.Message, payload);
            }
        }

        private async Task LogAttemptAsync(bool isSuccess, string message, string payload)
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("AzureConnectionString");
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                throw new Exception("Storage connection string is null or empty.");
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            
            CloudTable table = tableClient.GetTableReference("logs");
            await table.CreateIfNotExistsAsync();

            Log log = new Log(isSuccess, message);
            TableOperation insertOperation = TableOperation.Insert(log);
            await table.ExecuteAsync(insertOperation);

            if (isSuccess && !string.IsNullOrEmpty(payload))
            {
                CloudBlobContainer container = blobClient.GetContainerReference("payloads");
                await container.CreateIfNotExistsAsync();

                string blobName = $"{log.RowKey}.json";
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
                await blockBlob.UploadTextAsync(payload);
            }
        }
    }
}
