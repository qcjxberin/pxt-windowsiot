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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.Devices.Usb;

namespace CloudClient
{
    public sealed partial class MainPage
    {
        private List<UsbDevice> usbDevices;
        private List<SerialDevice> serialDevices;
        DeviceWatcher serialDeviceWatcher;
        DeviceWatcher usbConnectedDevicesWatcher;

        private static string GUID_DEVINTERFACE_USB_DEVICE = "A5DCBF10-6530-11D2-901F-00C04FB951ED";

        void SetupDeviceWatchers()
        {
            var serialDeviceSelector = Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector();
            serialDeviceWatcher = DeviceInformation.CreateWatcher(serialDeviceSelector);
            serialDeviceWatcher.Added += (DeviceWatcher sender, DeviceInformation args) =>
            {
                RunOnGUI(() =>
                {
                    if (args.Name.ToLower().Contains("mbed") || args.Name.Contains("DAPLink CMSIS-DAP"))
                    {
                        Debug.WriteLine(string.Format("Found device '{0}'", args.Name));
                        serialDevices.Add(new SerialDevice { Active = args.IsEnabled, Id = args.Id, Name = args.Name });
                        this.state.serialWire.Update(WireState.Solid);
                        timer_Tick(null, null);
                        this.ReadDataFromSerialPort(args.Id);
                    }
                });
            };
            serialDeviceWatcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                RunOnGUI(() =>
                {
                    Debug.WriteLine(string.Format("Removed device '{0}'", args.Id));
                    serialDevices.RemoveAll(_ => _.Id == args.Id);
                    if (!serialDevices.Any())
                    {
                        this.state.serialWire.Update(WireState.Cut);
                        this.state.serialWire.Update(DataFlow.Stopped);
                        this.state.cloudWire.Update(DataFlow.Stopped);
                        timer_Tick(null, null);
                    }
                });
            };
            serialDeviceWatcher.Start();

            var deviceClass = "(System.Devices.InterfaceClassGuid:=\"{" + GUID_DEVINTERFACE_USB_DEVICE + "}\")";

            usbConnectedDevicesWatcher = DeviceInformation.CreateWatcher(deviceClass);
            usbConnectedDevicesWatcher.Added += (DeviceWatcher sender, DeviceInformation args) =>
            {
                RunOnGUI(() =>
                {
                    this.usbDevices.Add(new UsbDevice { Id = args.Id, Name = args.Name, IsEnabled = args.IsEnabled });
                });
            };

            usbConnectedDevicesWatcher.Removed += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                RunOnGUI(() =>
                {
                    this.usbDevices.RemoveAll(_ => _.Id == args.Id);
                });
            };

            usbConnectedDevicesWatcher.Updated += (DeviceWatcher sender, DeviceInformationUpdate args) =>
            {
                var v = args.Properties.Where(_ => _.Key == "System.Devices.InterfaceEnabled").FirstOrDefault();
                bool enabled = (v.Value is bool) && (bool)v.Value;

                RunOnGUI(() =>
                {
                    foreach (var device in this.usbDevices.Where(_ => _.Id == args.Id))
                    {
                        device.IsEnabled = enabled;
                    }
                });
            };

            usbConnectedDevicesWatcher.Start();
        }
    }
}
