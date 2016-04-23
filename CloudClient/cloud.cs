using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;


namespace CloudClient
{

    public class DataEntity : TableEntity
    {
        public DataEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public DataEntity() { }

        public int Light { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        int counter = 0;
        CloudTable table = null;

        async Task SendDataToCloud(DataRecord record)
        {
            if (counter == 0)
            {
                var connectionString = "DefaultEndpointsProtocol=https;AccountName=arturladu;AccountKey=8pbadC7VszHLRxnXHhZdJ44LxiSDjeVKGJX6VDh0lQIrZ/84WscsOPmpKs39PxzZIXmKJX0YmQsIM3Y+cWKxMg==;EndpointSuffix=core.windows.net;";

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                // Retrieve a reference to the table.
                table = tableClient.GetTableReference("light");

                // Create the table if it doesn't exist.
                await table.CreateIfNotExistsAsync();
            }

            ++counter;

            // Create a new customer entity.
            DataEntity weatherData = new DataEntity("part1", counter.ToString());
            weatherData.Light = record.Light;

            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.InsertOrReplace(weatherData);

            // Execute the insert operation.
            await table.ExecuteAsync(insertOperation);

            Debug.WriteLine("{0} : {1}", counter, weatherData.Light);
        }
    }
}