using System;
using System.Collections.Generic;
using System.Text;
using EinkStarter.Enums;
using EinkStarter.Models.Device;
using EinkStarter.Utilities.EventHandlers;

namespace EinkStarter.Interfaces
{
    public interface IBleService
    {
        void Initialize();
     
        void StartScanning();
        void StopScanning(bool clear = false);
        void ConnectBLEDevice(DeviceSensor device);
        void DisconnectPeripheral(DeviceSensor device);
        void SetCharacteristicNotify(DeviceSensor device, string serviceUuid, string characteristicUuid, bool on);
        void BeginReadCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUuid);
        void BeginWriteCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUuid, byte[] value, BLECharacteristicWriteType writeType = BLECharacteristicWriteType.WithResponse);
        void ClearLists();
        int RequestMtuNativeAsync(int requestValue, DeviceSensor sensor);
        int RequestMTU();
        

        //event handlers
        event EventHandler<BleServiceDiscoveredEventArgs> ServiceCharacteristicsEventCompleted;
        event EventHandler<EventValueArgs> CharacteristicValueReadEvent;
        event EventHandler<NotifyEnableEventArgs> NotifyCallbackEvent;
        event EventHandler<ErrorEventValueArgs> NotifyErrorEvent;
        event EventHandler<NewSensorDetectedEventArgs> NewSensorDetectedEvent;
     //   event EventHandler<DataUpdatedEventArgs> SensorDataUpdated;
        event EventHandler<EventValueArgs> CharacteristicWriteResponseEvent;
        event EventHandler<EventValueArgs> CharacteristicNotifyEvent;
      //  event EventHandler<EventValueArgs> CharacteristicDiscoveredEvent;
        event EventHandler<SensorDisconnectEventArgs> SensorDisconnected;
        event EventHandler<EventRequestStatusArgs> BLEReadyEvent;
      
    }
}
