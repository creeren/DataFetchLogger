using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DataFetchLogger
{
    public static class LogsGetFunction
    {
        [FunctionName("LogsGetFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "logs")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string fromDateQuery = req.Query["from"];
            string toDateQuery = req.Query["to"];

            if (!DateTime.TryParse(fromDateQuery, out DateTime fromDate) || !DateTime.TryParse(toDateQuery, out DateTime toDate))
            {
                return new BadRequestObjectResult("Invalid date range");
            }

            string storageConnectionString = Environment.GetEnvironmentVariable("AzureConnectionString");
            if (string.IsNullOrEmpty(storageConnectionString))
            {
                throw new Exception("Storage connection string is null or empty.");
            }

            var tableClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudTableClient();
            var table = tableClient.GetTableReference("logs");

            try
            {
                var query = new TableQuery<Log>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, fromDate),
                    TableOperators.And,
                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThanOrEqual, toDate)));

                 var logs = await table.ExecuteQuerySegmentedAsync(query, null);

                 return new OkObjectResult(logs.Results);
            }
            catch (StorageException ex)
            {
                log.LogError($"StorageException: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                log.LogError($"Exception: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
