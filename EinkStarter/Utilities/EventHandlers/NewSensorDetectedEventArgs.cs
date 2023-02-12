using System;
using System.Collections.Generic;
using System.Text;
using EinkStarter.Enums;
using EinkStarter.Models.Device;

namespace EinkStarter.Utilities.EventHandlers
{
    public class NewSensorDetectedEventArgs : EventArgs
    {
        public DeviceSensor Sensor { get; set; }
        public byte[] DataBytes { get; set; }
        public int CardIndex { get; set; }
    }
}
