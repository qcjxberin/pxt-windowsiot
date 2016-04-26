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
using Windows.Devices.Enumeration;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

#pragma warning disable 4014

namespace CloudClient
{
    public class SerialDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public bool Active { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            SerialDevices = new ObservableCollection<SerialDevice>();
            Data = new ObservableCollection<int>();

            work();
        }

        // Dictionary<string, string> serialDevices;
        public ObservableCollection<SerialDevice> SerialDevices { get; set; }

        public ObservableCollection<int> Data{ get; set; }

        async Task work()
        {
            var serialDeviceSelector = Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector();

            var watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(serialDeviceSelector);
            watcher.Added += (DeviceWatcher sender, DeviceInformation args) =>
            {
                if (args.Name.Contains("mbed"))
                {
                    Debug.WriteLine(string.Format("Found device '{0}'", args.Name));
                    RunOnGUI(
                        () =>
                        {
                            SerialDevices.Add(new SerialDevice { Id = args.Id, Name = args.Name });
                        });
                    this.Process(args.Id);
                }
            };
            watcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                RunOnGUI(
                    () =>
                    {
                        var first = SerialDevices.FirstOrDefault(_ => _.Id == args.Id);
                        if (first != null)
                        {
                            SerialDevices.Remove(first);
                        }
                    });
                Debug.WriteLine(string.Format("removed device '{0}'", args.Id));
                
            };
            watcher.Start();
        }

        private void RunOnGUI(Action action)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    action();
                });
        }
    }
}
