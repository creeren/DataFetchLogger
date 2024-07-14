using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace DataFetchLogger
{
    public class Log: TableEntity
    {
        public Log() { }

        public Log(bool isSuccess, string message)
        {
            PartitionKey = isSuccess ? "Success" : "Failure";
            RowKey = Guid.NewGuid().ToString();
            Message = message;
            Timestamp = DateTime.UtcNow;
        }

        public string Status { get; set; }
        public string Message { get; set; }
        public string PayloadUri { get; set; }
    }
}
