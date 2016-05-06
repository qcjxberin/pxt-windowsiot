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


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

#pragma warning disable 4014

namespace CloudClient
{
    [DebuggerDisplay("Name = {Name}, IsEnabled = {IsEnabled}")]
    public class UsbDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public bool IsEnabled { get; set; }
    }

    [DebuggerDisplay("Name = {Name}, IsEnabled = {IsEnabled}")]
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
        private Storyboard serialDataTransfer;
        private Storyboard cloudDataTransfer;
        private DispatcherTimer timer;

        static Storyboard MakeDataTransferStoryBoard(UIElement uiElement, double seconds)
        {
            var storyBoard = new Storyboard();
            TranslateTransform moveTransform = new TranslateTransform();
            uiElement.RenderTransform = moveTransform;
            Duration duration = new Duration(TimeSpan.FromSeconds(seconds));
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

        private async Task<string> GetStreamName()
        {
            // ARM runs headless
            if (Windows.ApplicationModel.Package.Current.Id.Architecture == Windows.System.ProcessorArchitecture.Arm)
            {
                return string.Format("Stream_{0}", new Random().Next(1024 * 1024));
            }

            var changeDlg = new ChangeStreamNameDialog("");
            await changeDlg.ShowAsync();
            return changeDlg.StreamName;
        }

        public MainPage()
        {
            this.usbDevices = new List<UsbDevice>();
            this.serialDevices = new List<SerialDevice>();

            this.InitializeComponent();
            SetupControls();
        }

        async Task SetupControls()
        {
            imgWire1.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire-disconnected.png"));

            this.serialDataTransfer = MakeDataTransferStoryBoard(this.imgBall1, 2);
            this.cloudDataTransfer = MakeDataTransferStoryBoard(this.imgBall2, 3);

            this.state.serialWire = new ConnectionState(this.imgWire1, this.serialDataTransfer, RunOnGUI);
            this.state.cloudWire = new ConnectionState(this.imgWire2, this.cloudDataTransfer, RunOnGUI);

            this.state.serialWire.Update(WireState.Cut);
            this.state.serialWire.Update(DataFlow.Stopped);
            this.state.cloudWire.Update(WireState.Solid);
            this.state.cloudWire.Update(DataFlow.Stopped);

            SetupDeviceWatchers();

            timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Start();
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
