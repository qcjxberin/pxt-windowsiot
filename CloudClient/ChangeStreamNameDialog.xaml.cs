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

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CloudClient
{
    public sealed partial class ChangeStreamNameDialog : ContentDialog
    {
        string streamName;

        public ChangeStreamNameDialog(string streamName)
        {
            this.streamName = streamName;
            this.IsNameChanged = false;
            this.InitializeComponent();

            // First time cannot cancel out
            if (streamName == string.Empty)
            {
                this.IsSecondaryButtonEnabled = false;
            }

            this.textBox.Text = streamName;
            this.IsPrimaryButtonEnabled = streamName.Trim().Length > 0;
            this.textBox.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.IsPrimaryButtonEnabled = this.textBox.Text.Trim().Length > 0;
        }

        public bool IsNameChanged { get; private set; }

        public string StreamName { get { return this.textBox.Text; } }

        private void OKButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.IsNameChanged = true;
        }

        private void CancelButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.IsNameChanged = false;
        }
    }
}
