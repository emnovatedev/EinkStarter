using Android.Runtime;
using System;
using Android.Bluetooth.LE;

namespace EinkStarter.Droid.DependencyServices.Ble
{
    public class GattScanCallback : ScanCallback
    {
        public event EventHandler<ScanRecordEventArgs> DeviceDetected;
        public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult result)
        {
            DeviceDetected?.Invoke(this, new ScanRecordEventArgs
                {
                    ScanResult = result
                }
            );
            base.OnScanResult(callbackType, result);
        }

        public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
        {
            base.OnScanFailed(errorCode);
        }
    }

    public class ScanRecordEventArgs : EventArgs
    {
        public ScanResult ScanResult { get; set; }
    }
}