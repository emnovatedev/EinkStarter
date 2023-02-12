using System;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Enums
{
    public enum DeviceCommandRequestedState
    {
        None,
        DisplayCard,
        WipeDeviceClean,
        RemoveCard,
        RemoveCards,
        WriteCard
    }
}
