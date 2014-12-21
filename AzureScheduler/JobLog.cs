using Microsoft.WindowsAzure.Storage.Table;

namespace AzureScheduler
{
    public class JobLog : TableEntity
    {
        //PartitionKey: time
        //RowKey: (none)

        public long RunTime { get; set; }
        public string Result { get; set; }
        public string JobGroup { get; set; }
        public string JobName { get; set; }
        public int StatusCode { get; set; }
    }
}
