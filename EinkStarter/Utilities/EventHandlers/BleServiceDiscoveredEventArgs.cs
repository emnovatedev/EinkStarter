using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class BleServiceDiscoveredEventArgs : EventArgs
    {
        public string ServiceUuid { get; set; }
    }
}
