using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EinkStarter.Models.Device
{
    public class PeripheralServices
    {
        public object Service { get; set; }
        public string UUID { get; set; }
        public ConcurrentBag<object> Characteristics { get; set; }

        public PeripheralServices()
        {
            Characteristics = new ConcurrentBag<object>();
        }
    }
}
