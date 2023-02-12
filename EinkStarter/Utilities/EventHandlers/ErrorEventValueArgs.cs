using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Utilities.EventHandlers
{
    public class ErrorEventValueArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
    }
}
