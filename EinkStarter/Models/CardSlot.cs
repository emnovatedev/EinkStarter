using System;
using System.Collections.Generic;
using System.Text;
using PropertyChanged;

namespace EinkStarter.Models
{
    [AddINotifyPropertyChangedInterface]
    public class CardSlot
    {
        public int Slot { get; set; }
        public string SlotName { get; set; }
    }
}
