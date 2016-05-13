using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Linq;

namespace CloudClient
{
    class CreateStreamResponse
    {
        public string kind;
        public string id;
        public string time;
        public string name;
        public string target;
        public string privatekey;
    }

    class PostToStreamResponse
    {
        public long quotaUsedHere;
        public long quotaLeft;
    }

    public class CloudStream
    {
        public string Name;
        public string Id;
        public string Key;
        public List<string> Buffer = new List<string>();
    }

    public enum DataPushStatus
    {
        Buffered,
        Post_Succeeded,
        Post_Failed
    }

    public class CloudDataSender
    {
        readonly HttpClient client = new HttpClient();
        const string baseUri = "https://www.pxt.io";

        string streamName;
        CloudStream currentStream;

        public const int entriesPerBatch = 1;

        public CloudDataSender(string streamName)
        {
            this.streamName = streamName;
            this.currentStream = null;
        }

        public async Task<CloudStream> GetCurrentStream()
        {
            if (currentStream == null)
            {
                await CreateNewStream();
            }

            Debug.Assert(currentStream != null);
            return this.currentStream;
        }

        private async Task CreateNewStream()
        {
            var createStreamUri = baseUri + "/api/streams";
            var postBody = string.Format(@"{{""name"": ""{0}"", ""target"": ""microbit""}}", streamName);

            var postContent = new StringContent(postBody, Encoding.UTF8, "application/json");

            using (var response = await client.PostAsync(createStreamUri, postContent))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (var responseContent = response.Content)
                    {
                        string result = await responseContent.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<CreateStreamResponse>(result);
                        Debug.WriteLine("Created stream Id={0}, key={1}", responseObj.id, responseObj.privatekey);
                        this.currentStream = new CloudStream { Name = streamName, Id = responseObj.id, Key = responseObj.privatekey };
                        return;
                    }
                }
            }
            throw new Exception("Cannot create stream");
        }

        List<string> buffer = new List<string>();

        // For now, all values in batch must have the same field name. This will be expanded later to support multiplexing values
        string dataFieldName = null;

        public async Task<DataPushStatus> PushData(long timeStamp, int deviceID, int deviceTimeStamp, string fieldName, int value)
        {
            // The format is: timestamp, partition, value
            var entry = string.Format("[{0}, {1}, {2}]", timeStamp, deviceID, value);

            if (dataFieldName == null)
            {
                dataFieldName = fieldName;
            }
            else
            {
                Debug.Assert(dataFieldName == fieldName); // not yet supported
            }

            if (buffer.Count < entriesPerBatch)
            {
                // Add the entry to the buffer and get out; it will be pushed to server later
                buffer.Add(entry);
                return DataPushStatus.Buffered;
            }
            else
            {
                var postToStreamUri = string.Format("{0}/api/{1}/data?privatekey={2}", baseUri, currentStream.Id, currentStream.Key);
                var postBody = new StringBuilder();
                postBody.AppendFormat(@"{{""fields"": [""timestamp"", ""partition"", ""{0}""]", dataFieldName);
                postBody.AppendFormat(@",""values"" : [{0}]}}", string.Join(",", buffer));
                var postContent = new StringContent(postBody.ToString(), Encoding.UTF8, "application/json");

                using (var response = await client.PostAsync(postToStreamUri, postContent))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        using (var responseContent = response.Content)
                        {
                            string result = await responseContent.ReadAsStringAsync();
                            var responseObj = JsonConvert.DeserializeObject<PostToStreamResponse>(result);

                            buffer.Clear();

                            return DataPushStatus.Post_Succeeded;
                        }
                    }
                    else if (response.StatusCode == (System.Net.HttpStatusCode)429)
                    {
                        Debug.WriteLine("Too many requests!");
                        string result = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(result);

                        return DataPushStatus.Post_Failed;
                    }
                    else
                    {
                        return DataPushStatus.Post_Failed;
                    }
                }
            }
        }
    }

    public sealed partial class MainPage : Page
    {
        CloudDataSender dataSender = new CloudDataSender("Cloud Gateway Stream");

        static long ToMsSinceEpoch(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        int totalMessagesSent = 0;

        async Task SendDataToCloud(JObject jsonObject)
        {
            CloudStream stream;

            try
            {
                stream = await dataSender.GetCurrentStream();
                this.state.cloudWire.Update(WireState.Solid);
            }
            catch
            {
                this.state.cloudWire.Update(DataFlow.Stopped);
                this.state.cloudWire.Update(WireState.Cut);
                return;
            }

            this.state.streamName.Update(stream.Name);

            int deviceTimeStamp = jsonObject.Value<int>("t");
            if (deviceTimeStamp < 0)
            {
                // This is a command, TODO
            }
            long timeStamp = ToMsSinceEpoch(DateTime.Now);
            int deviceId = jsonObject.Value<int>("s");
            string fieldName = jsonObject.Value<string>("n");
            if (string.IsNullOrEmpty(fieldName))
            {
                fieldName = "value";
            }
            int value = jsonObject.Value<int>("v");

            var pushResult = await dataSender.PushData(timeStamp, deviceId, deviceTimeStamp, fieldName, value);

            switch (pushResult)
            {
                case DataPushStatus.Buffered:
                    break;

                case DataPushStatus.Post_Succeeded:

                    this.state.cloudWire.Update(DataFlow.Active);
                    this.state.cloudWire.Update(WireState.Solid);
                    this.state.streamName.Update(stream.Name);
                    this.state.streamId.Update(stream.Id);

                    totalMessagesSent += CloudDataSender.entriesPerBatch;
                    this.state.messagesSent.Update(totalMessagesSent.ToString());

                    break;

                case DataPushStatus.Post_Failed:
                    this.state.cloudWire.Update(DataFlow.Stopped);
                    this.state.cloudWire.Update(WireState.Cut);
                    break;
            }
        }
    }
}