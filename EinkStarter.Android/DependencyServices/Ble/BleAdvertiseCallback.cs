using System;
using Android.Bluetooth.LE;

namespace EinkStarter.Droid.DependencyServices.Ble
{
    public class BleAdvertiseCallback : AdvertiseCallback
    {
        public event EventHandler<AdvertiseEventArgs> AdvertisementStatus;
        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            Console.WriteLine("Advertise start failure {0}", errorCode);
            AdvertisementStatus?.Invoke(this, new AdvertiseEventArgs
            {
                Error = "Error occurred while trying to advertise. Error code is: " + errorCode,
                Success = false,
            });
            base.OnStartFailure(errorCode);
        }

        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            Console.WriteLine("Advertise start success {0}", settingsInEffect.Mode);
            AdvertisementStatus?.Invoke(this, new AdvertiseEventArgs
            {
                Error = string.Empty,
                Success = true,
            });

            base.OnStartSuccess(settingsInEffect);
        }
    }

    public class AdvertiseEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}