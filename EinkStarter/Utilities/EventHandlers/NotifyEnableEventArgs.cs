using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class NotifyEnableEventArgs
    {
        public int Value { get; set; }
        public string CharacteristicUuId { get; set; }
    }
}
