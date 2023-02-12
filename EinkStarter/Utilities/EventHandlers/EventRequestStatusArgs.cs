using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class EventRequestStatusArgs : EventArgs
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
