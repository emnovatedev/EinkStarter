using System;
using System.Collections.Generic;
using System.Text;
using EinkStarter.Models.Device;

namespace EinkStarter.Utilities.EventHandlers
{
    public class SensorEventArgs : EventArgs
    {
        public DeviceSensor Sensor { get; set; }
    }
}
