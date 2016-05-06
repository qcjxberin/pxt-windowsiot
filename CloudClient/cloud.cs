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

    class CloudStream
    {
        public string Name;
        public string Id;
        public string Key;
        public List<string> Buffer = new List<string>();
    }

    public sealed partial class MainPage : Page
    {
        HttpClient client = new HttpClient();

        Dictionary<string, CloudStream> streams = new Dictionary<string, CloudStream>();

        string baseUri = "https://www.pxt.io";

        async Task<CloudStream> CreateStream(string streamName)
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
                        return new CloudStream { Name = streamName, Id = responseObj.id, Key = responseObj.privatekey };
                    }
                }
            }
            throw new Exception("Cannot create stream");
        }

        long ToMsSinceEpoch(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        const int batch = 16;

        int totalMessagesSent = 0;
        async Task SendDataToCloud(JObject jsonObject)
        {
            string objectStreamName = jsonObject.Value<string>("stream");

            if (objectStreamName == null) return; // bad data

            CloudStream stream;

            // Have we already created this stream?
            if(!streams.TryGetValue(objectStreamName, out stream))
            {
                // No, create one
                stream = await CreateStream(objectStreamName);
                streams.Add(objectStreamName, stream);
            }

            this.state.lastStreamName.Update(stream.Name);

            List<KeyValuePair<string, string>> valuesSansStream = new List<KeyValuePair<string, string>>();
            foreach (var kv in jsonObject)
            {
                if (kv.Key != "stream")
                    valuesSansStream.Add(new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()));
            }

            // Create a value record for this object
            StringBuilder valueRecord = new StringBuilder();
            valueRecord.AppendFormat("[{0},", ToMsSinceEpoch(DateTime.Now).ToString());
            valueRecord.AppendFormat("{0}]", string.Join(",", valuesSansStream.Select(_ => _.Value)));

            stream.Buffer.Add(valueRecord.ToString());

            if (stream.Buffer.Count < batch)
            {
                // We've saved it. It will be posted later
                return;
            }

            var postToStreamUri = string.Format("{0}/api/{1}/data?privatekey={2}", baseUri, stream.Id, stream.Key);

            // TODO: what if the number of fields for this object is different? Should we error out or create a new stream?

            var postBody = new StringBuilder();
            postBody.AppendFormat( @"{{""fields"": [""timestamp"",{0}", string.Join(",", valuesSansStream.Select(_ => string.Format("\"{0}\"",_.Key))));
            postBody.AppendFormat(@"],""values"" : [{0}]}}", string.Join(",", stream.Buffer));

            stream.Buffer.Clear();

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

                        totalMessagesSent += batch;
                        RunOnGUI(() =>
                        {
                            textBlockMsgCount.Text = totalMessagesSent.ToString();
                        });
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