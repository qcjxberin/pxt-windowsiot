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
using System.IO;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CloudClient
{
    public sealed class HttpServer : IDisposable
    {
        private const uint BufferSize = 8192;

        private readonly StreamSocketListener listener;

        private const int port = 8002;

        public delegate void HttpRequestReceivedEvent(HTTPRequest request);

        CloudDataSender dataSender;

        public HttpServer(CloudDataSender dataSender)
        {
            this.listener = new StreamSocketListener();
            this.dataSender = dataSender;

            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        public void Dispose()
        {
            if (listener != null)
            {
                listener.Dispose();
            }
        }

        public async void Start()
        {
            await listener.BindServiceNameAsync(port.ToString());
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            HTTPRequest request;
            using (IInputStream stream = socket.InputStream)
            {
                HttpRequestParser parser = new HttpRequestParser();
                request = await parser.GetHttpRequestForStream(stream);
                // TODO: Handle request here
            }

            using (IOutputStream output = socket.OutputStream)
            {
                if (request.Method == "GET")
                {
                    await WriteResponseAync(request.URL, output);
                }
                else
                {
                    // just ignore it
                }
            }
        }

        private async Task WriteResponseAync(string request, IOutputStream output)
        {
            using (Stream resp = output.AsStreamForWrite())
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/main.html"));
                var fileStream = await file.OpenReadAsync();

                var buffer = new Windows.Storage.Streams.Buffer(BufferSize);
                var result = await fileStream.ReadAsync(buffer, BufferSize, Windows.Storage.Streams.InputStreamOptions.None);

                // When this fires, either make the buffer bigger or read in chunks
                Debug.Assert(result.Length < BufferSize);

                var str = Encoding.UTF8.GetString(result.ToArray());

                int streamId = 0;

                // Inject the stream ID into the string
                try
                {
                    streamId = dataSender.GetCurrentStream().Id;
                }
                catch
                {
                    // Could not connect. TODO: show error
                }
                str = string.Format(str, streamId);

                byte[] bodyArray = Encoding.UTF8.GetBytes(str);

                MemoryStream memoryStream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                "Content-Length: {0}\r\n" +
                                                "Connection: close\r\n\r\n",
                                                memoryStream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await memoryStream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }
    }
}