using System;
using System.Collections.Generic;
using System.Text;
using PropertyChanged;

namespace EinkStarter.Models
{
    [AddINotifyPropertyChangedInterface]
    public class ImageSlot
    {
        public string SlotNumber { get; set; }
        public string SlotName { get; set; }

        public string SlotNameLong { get; set; }

    }
}
