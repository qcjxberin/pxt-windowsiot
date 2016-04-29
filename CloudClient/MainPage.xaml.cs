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
        Storyboard serialDataTransfer;
        Storyboard cloudDataTransfer;

        static Storyboard MakeDataTransferStoryBoard(UIElement uiElement)
        {
            var storyBoard = new Storyboard();
            TranslateTransform moveTransform = new TranslateTransform();
            uiElement.RenderTransform = moveTransform;
            Duration duration = new Duration(TimeSpan.FromSeconds(2));
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            myDoubleAnimationX.Duration = duration;
            myDoubleAnimationX.To = 125;
            storyBoard.Children.Add(myDoubleAnimationX);
            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            storyBoard.AutoReverse = false;
            storyBoard.RepeatBehavior = RepeatBehavior.Forever;
            return storyBoard;
        }

        public MainPage()
        {
            this.InitializeComponent();

            imgWire1.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire-disconnected.png"));

            serialDataTransfer = MakeDataTransferStoryBoard(this.imgBall1);
            //serialDataTransfer.Begin();

            cloudDataTransfer = MakeDataTransferStoryBoard(this.imgBall2);
            //cloudDataTransfer.Begin();

            SerialDevices = new ObservableCollection<SerialDevice>();
            Data = new ObservableCollection<int>();

            UpdateStartStopButton();

            this.startStopButton.Click += StartStopButton_Click;

            work();
        }

        bool buttonStateIsStart = true;

        void UpdateStartStopButton()
        {
            if (buttonStateIsStart)
            {
                this.startStopButton.Content = "Start Sending To Cloud";
            }
            else
            {
                this.startStopButton.Content = "Stop Sending To Cloud";
            }
            this.startStopButton.IsEnabled = Data.Any();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            buttonStateIsStart = !buttonStateIsStart;
            UpdateStartStopButton();
        }

        // Dictionary<string, string> serialDevices;
        public ObservableCollection<SerialDevice> SerialDevices { get; set; }

        public ObservableCollection<int> Data { get; set; }

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
                            this.imgWire1.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire0.png"));
                        });
                    // this.Process(args.Id);
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
                            if (SerialDevices.Count == 0)
                            {
                                imgWire1.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire-disconnected.png"));
                            }
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
