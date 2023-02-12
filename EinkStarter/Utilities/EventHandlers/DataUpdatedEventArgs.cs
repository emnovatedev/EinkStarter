using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class DataUpdatedEventArgs : EventArgs
    {
        public string MacAddress { get; set; }
        public byte[] DataBytes { get; set; }
    }
}
