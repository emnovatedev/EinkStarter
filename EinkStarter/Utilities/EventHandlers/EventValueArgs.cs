using System;
using EinkStarter.Models.Device;

namespace EinkStarter.Utilities.EventHandlers
{
    public class EventValueArgs : EventArgs
    {
        public byte[] Value { get; set; }
        public string CharacteristicUuId { get; set; }
        public DeviceSensor Sensor { get; set; }
    }
}
