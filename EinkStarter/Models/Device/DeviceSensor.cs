using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using PropertyChanged;
using System.Diagnostics;

namespace EinkStarter.Models.Device
{
    [AddINotifyPropertyChangedInterface]
    public class DeviceSensor
    {
        private const int MaxServices = 3;
        public string MacAddress { get; set; }
        public string Id { get; set; }
        public string RSSI { set; get; }
        [DoNotNotify]
        public object Peripheral { get; set; }
        public bool Paired { get; set; }
        public bool HandlersSet { get; set; }
        public string DisplayName { get; set; }
        [DoNotNotify]
        public string CodeVersion { set; get; }
        public string Name { get; set; }
        [DoNotNotify]
        public ConcurrentBag<PeripheralServices> Services { get; set; }
        [DoNotNotify]
        private ConcurrentBag<string> _discoveredServiceIds { get; set; }

        public DeviceSensor()
        {
            Services = new ConcurrentBag<PeripheralServices>();
            _discoveredServiceIds = new ConcurrentBag<string>();
        }

        public void OnBoardDiscoveredService(string serviceId)
        {
            Paired = true;
            _discoveredServiceIds.Add(serviceId);
            Debug.WriteLine($"Service Discovered {serviceId}");
        }

        public bool MaxServicesDiscovered => _discoveredServiceIds.Count >= MaxServices;
    }
}
