using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class SensorDisconnectEventArgs : SensorEventArgs
    {
        public string DisconnectReason { get; set; }
    }
}
