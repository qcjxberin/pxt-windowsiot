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

        private int port = 8002;

        public delegate void HttpRequestReceivedEvent(HTTPRequest request);
        public event HttpRequestReceivedEvent OnRequestReceived;

        public HttpServer(int serverPort)
        {
            if (listener == null)
            {
                listener = new StreamSocketListener();
            }
            port = serverPort;

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
                    throw new InvalidDataException("HTTP method not supported: " + request.Method);
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

                byte[] bodyArray = result.ToArray();

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