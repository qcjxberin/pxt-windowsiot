using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CloudClient
{
    public sealed partial class MainPage
    {
        private void timer_Tick(object sender, object e)
        {
            RunOnGUI(() =>
            {
                // Do we have mbed USB devices that don't show up as serial devices?
                bool showError = false;
                if (!this.serialDevices.Any())
                {
                    if (this.usbDevices.Any(_ => _.IsEnabled && _.Name.ToLower().Contains("mbed")))
                    {
                        Debug.WriteLine("Found inactive mbed device");
                        showError = true;
                    }
                }

                if (showError && textBlockStatus.Text == "")
                {
                    textBlockStatus.Text = "Error: update firmware on you micro:bit device!";
                }

                if (!showError && textBlockStatus.Text != "")
                {
                    textBlockStatus.Text = "";
                }

                UpdateSerialThroughput();
            });
        }

        object lockObj = new object();
        // These fields must be accessed under the lock:
        // {
        bool bPauseDataRead = false;
        uint totalBytesReadFromSerial = 0;
        uint totalBytesReadFromSerialAtLastTick = 0;
        bool newStreamRequest = false;
        // }

        double bytesPerSecond = 0.0;
        DateTime lastTick = DateTime.Now;

        // Must be called on GUI thread
        private void UpdateSerialThroughput()
        {
            var now = DateTime.Now;
            var timePassed = now - lastTick;
            this.lastTick = now;
            this.bytesPerSecond = 0.0;

            uint nBytesReadSinceLastTick;

            lock (lockObj)
            {
                nBytesReadSinceLastTick = this.totalBytesReadFromSerial - this.totalBytesReadFromSerialAtLastTick;
            }

            var timeInMilliseconds = timePassed.TotalMilliseconds;
            if (timeInMilliseconds == 0)
            {
                this.bytesPerSecond = 0.0;
            }
            else
            {
                this.bytesPerSecond = (this.bytesPerSecond + ((double)nBytesReadSinceLastTick / timeInMilliseconds) * 1000) / 2;
            }

            double rate = bytesPerSecond;
            string unit = "bytes per second";

            if (rate > 1024)
            {
                rate = rate / 1024;
                unit = "KB per second";
            }
            if (rate > 1024)
            {
                rate = rate / 1024;
                unit = "MB per second";
            }

            if (nBytesReadSinceLastTick == 0)
            {
                this.state.serialWire.Update(DataFlow.Stopped);
                this.state.cloudWire.Update(DataFlow.Stopped);
            }

            lock (lockObj)
            {
                this.totalBytesReadFromSerialAtLastTick = this.totalBytesReadFromSerial;
            }

            this.textBlockDataRate.Text = string.Format("{0:0.0} {1}", rate, unit);
        }
    }
}
