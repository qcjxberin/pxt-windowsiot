﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace CloudClient
{
    class DataRecord
    {
        public int Light;
    }

    public sealed partial class MainPage : Page
    {
        const int bufferSize = 32;

        enum State
        {
            Outside_of_object,
            Inside_object
        }

        public async Task Process(string id)
        {
            var device = await Windows.Devices.SerialCommunication.SerialDevice.FromIdAsync(id);
            device.BaudRate = 115200;

            var currentStr = "";
            State state = State.Outside_of_object;
            while (true)
            {
                var buffer = new Windows.Storage.Streams.Buffer(bufferSize);
                try
                {
                    var result = await device.InputStream.ReadAsync(buffer, bufferSize, Windows.Storage.Streams.InputStreamOptions.None);
                    if (result.Length > 0)
                    {
                        var str = System.Text.Encoding.ASCII.GetString(result.ToArray());
                        Debug.WriteLine(string.Format("-->{0}<--", str));
                        foreach (var c in str)
                        {
                            switch (c)
                            {
                                case '{':
                                    switch (state)
                                    {
                                        case State.Outside_of_object:
                                            currentStr = c.ToString();
                                            state = State.Inside_object;
                                            break;
                                        case State.Inside_object:
                                            // throw new NotSupportedException("Nested JSON valued are not supported");
                                            // We got into a weird state. Get out.
                                            currentStr = c.ToString();
                                            state = State.Inside_object;
                                            break;
                                    }
                                    break;
                                case '}':
                                    switch (state)
                                    {
                                        case State.Outside_of_object:
                                            // we started reading mid-stream, but now we're truly outside
                                            break;
                                        case State.Inside_object:
                                            currentStr += c;
                                            await HandleJSONObject(currentStr);
                                            // Nested are not supported
                                            state = State.Outside_of_object;
                                            currentStr = "";
                                            break;
                                    }
                                    break;
                                default:
                                    switch (state)
                                    {
                                        case State.Outside_of_object:
                                            // skip this char
                                            break;
                                        case State.Inside_object:
                                            // accumulate it:
                                            currentStr += c;
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    device.Dispose();
                    break;
                }
            }
        }

        private async Task HandleJSONObject(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<DataRecord>(json);
                await SendDataToCloud(data);
                RunOnGUI(() =>
                {
                    this.Data.Insert(0, data.Light);
                    this.data.SelectedIndex = 0;
                });
            }
            catch (JsonException)
            {
                // We have bad data, ignore
            }

        }
    }
}