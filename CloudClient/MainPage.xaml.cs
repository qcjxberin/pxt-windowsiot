using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CloudClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.textBlock.Text = "...";
            work();
        }

        async Task work()
        {
            var serialDeviceSelector = Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector();

            // var watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(serialDeviceSelector, [] as any);
            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(serialDeviceSelector);

            var count = devices.Count;
            foreach (var device in devices)
            {
                Debug.WriteLine(string.Format("Found device '{0}'", device.Name));
                foreach (var prop in device.Properties)
                {
                    Debug.WriteLine("{0}-{1}", prop.Key, prop.Value);
                }
                if (device.Name.Contains("mbed"))
                {
                    this.Process(device.Id);
                }
            }

            Debug.WriteLine("Here");

            var watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher();
            watcher.Added += DeviceAdded;
        }

        private static void DeviceAdded(Windows.Devices.Enumeration.DeviceWatcher sender, Windows.Devices.Enumeration.DeviceInformation args)
        {
            Debug.WriteLine("Device added: {0}", args.Id);
        }
    }
}
