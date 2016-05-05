using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

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

    struct PostRecord
    {
        public long timestamp;
        public int data;
    }

    public sealed partial class MainPage : Page
    {
        HttpClient client = new HttpClient();
        string baseUri = "https://www.pxt.io";

        string streamId;
        string streamPrivateKey;

        async Task CreateStream(string streamName)
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
                        streamId = responseObj.id;
                        streamPrivateKey = responseObj.privatekey;
                    }
                }
            }
        }

        long ToMsSinceEpoch(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        List<PostRecord> records = new List<PostRecord>();
        int batch = 16;

        async Task SendDataToCloud(DataRecord record, bool resetStream)
        {
            var fieldName = "light";

            if (resetStream)
            {
                await CreateStream(this.streamName);
            }

            Debug.Assert(streamId != null);
            Debug.Assert(streamPrivateKey != null);

            if (records.Count < batch)
            {
                records.Add(new PostRecord { timestamp = ToMsSinceEpoch(DateTime.Now), data = record.Light });
                return;
            }

            var postToStreamUri = baseUri + "/api/" + streamId + "/data?privatekey=" + streamPrivateKey;

            var postBody = new StringBuilder();

            postBody.AppendFormat( @"{{""fields"": [""timestamp"", ""{0}""],", fieldName);
            postBody.AppendFormat(@"""values"" : [");

            for (int i = 0; i < records.Count; ++i)
            {
                if (i > 0) postBody.AppendFormat(",");
                postBody.AppendFormat("[{0}, {1}]", records[i].timestamp, records[i].data);
            }

            records.Clear();

            postBody.Append(@"]}");

            var postContent = new StringContent(postBody.ToString(), Encoding.UTF8, "application/json");

            using (var response = await client.PostAsync(postToStreamUri, postContent))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (var responseContent = response.Content)
                    {
                        string result = await responseContent.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<PostToStreamResponse>(result);
                        this.state.cloudWire.Update(DataFlow.Active);
                        this.state.cloudWire.Update(WireState.Solid);
                    }
                }
                else if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Debug.WriteLine("Too many requests!");
                    string result = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(result);
                }
                else
                {
                    this.state.cloudWire.Update(DataFlow.Stopped);
                    this.state.cloudWire.Update(WireState.Cut);
                }
            }

        }
    }
}