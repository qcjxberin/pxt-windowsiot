using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace CloudClient
{
    enum WireState
    {
        Solid,
        Cut
    }

    enum DataFlow
    {
        Active,
        Stopped
    }

    class ConnectionState
    {
        private WireState wireState;
        private DataFlow dataFlowState;
        private Image image;
        private Storyboard storyboard;
        private Action<Action> guiDispatcher;

        public ConnectionState(Image image, Storyboard storyboard, Action<Action> guiDispatcher)
        {
            this.image = image;
            this.storyboard = storyboard;
            this.guiDispatcher = guiDispatcher;
        }

        public void Update(WireState desiredWireState)
        {
            if (wireState == WireState.Solid && desiredWireState == WireState.Cut)
            {
                guiDispatcher(() => { image.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire-disconnected.png")); });
            }
            else if (wireState == WireState.Cut && desiredWireState == WireState.Solid)
            {
                guiDispatcher(() => { image.Source = new BitmapImage(new Uri("ms-appx:///Assets/wire0.png")); });
            }
            wireState = desiredWireState;
        }

        public void Update(DataFlow desiredFlowState)
        {
            if (dataFlowState == DataFlow.Active && desiredFlowState == DataFlow.Stopped)
            {
                guiDispatcher(() => { storyboard.Stop(); });
            }
            else if (dataFlowState == DataFlow.Stopped && desiredFlowState == DataFlow.Active)
            {
                guiDispatcher(() => { storyboard.Begin(); });
            }
            dataFlowState = desiredFlowState;
        }
    }

    struct StateHolder
    {
        public ConnectionState serialWire;
        public ConnectionState cloudWire;
    }

    public sealed partial class MainPage
    {
        StateHolder state = new StateHolder();
    }
}
