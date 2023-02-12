using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreBluetooth;
using EinkStarter.Enums;
using EinkStarter.Interfaces;
using EinkStarter.iOS.DependencyServices.Ble;
using EinkStarter.Models.Device;
using EinkStarter.Utilities;
using EinkStarter.Utilities.EventHandlers;
using Foundation;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(BleService))]
namespace EinkStarter.iOS.DependencyServices.Ble
{
    public class BleService : IBleService
    {
        public event EventHandler<SensorDisconnectEventArgs> SensorDisconnected;
        public event EventHandler<EventRequestStatusArgs> BLEReadyEvent;
        public event EventHandler<ErrorEventValueArgs> NotifyErrorEvent;
        public event EventHandler<BleServiceDiscoveredEventArgs> ServiceCharacteristicsEventCompleted;
        public event EventHandler<EventValueArgs> CharacteristicValueReadEvent;
        public event EventHandler<NotifyEnableEventArgs> NotifyCallbackEvent;
        public event EventHandler<EventValueArgs> CharacteristicWriteResponseEvent;
        public event EventHandler<NewSensorDetectedEventArgs> NewSensorDetectedEvent;
        public event EventHandler<EventValueArgs> CharacteristicNotifyEvent;
        public event EventHandler<DataUpdatedEventArgs> SensorDataUpdated;

        private CBCentralManager _manager;
        private CBPeripheralManager _cbPeripheralManger;

        private ConcurrentDictionary<CBPeripheral, DeviceSensor> DiscoveredSensors =
            new ConcurrentDictionary<CBPeripheral, DeviceSensor>();

        public ConcurrentDictionary<string, string> DiscoveredShares = new ConcurrentDictionary<string, string>();
        private PeripheralScanningOptions _options;
        private bool _shouldConnect;
        private static string deviceMacAddress = string.Empty;
        private DeviceSensor CurrentPairedSensor;
        private object _setHandlersLock = new object();
        private readonly object _deviceAddLock = new object();
        private bool _handlerSet;

        #region hard_code_ble_characteristis_and_service_names
        private CBCharacteristic _cardXferMemoryIndexCharacteristic;
        private CBCharacteristic _cardXferCompressedCharacteristic;
        private CBCharacteristic _cardXferDataCharacteristic;
        private CBCharacteristic _cardXferWriteDoneCharacteristic;
        private CBCharacteristic _cardXferBackCharacteristic;
        private CBCharacteristic _commandIdCharacteristic;
        private CBCharacteristic _commandDataCharacteristic;
        private CBCharacteristic _commandResultCharacteristic;
        private CBCharacteristic _infoBatteryLevelCharacteristic;
        private CBCharacteristic _infoRfidTidCharacteristic;
        private CBCharacteristic _infoRfidUserMemoryCharacteristic;
        private CBCharacteristic _infoDeviceInfoCharacteristic;

        private CBPeripheral _device { get; set; }
        #endregion  hard_code_ble_characteristis_and_service_names

        public void Initialize()
        {
            
           
                _manager = new CBCentralManager();
          

                _manager.DiscoveredPeripheral += CbManagerOnDiscoveredPeripheral;
                _manager.UpdatedState += CbManagerOnUpdatedState;
                _manager.ConnectedPeripheral += CbManagerOnConnectedPeripheral;
                _manager.FailedToConnectPeripheral += CbManagerOnFailedToConnectPeripheral;
                _manager.WillRestoreState += CbManagerOnWillRestoreState;
                _manager.DisconnectedPeripheral += CbManagerOnDisconnectedPeripheral;
            
        }

        private void ClearCharacteristics()
        {
            _cardXferMemoryIndexCharacteristic = null;
            _cardXferCompressedCharacteristic = null;
            _cardXferDataCharacteristic = null;
            _cardXferWriteDoneCharacteristic = null;
            _cardXferBackCharacteristic = null;
            _commandIdCharacteristic = null;
            _commandDataCharacteristic = null;
            _commandResultCharacteristic = null;
            _infoBatteryLevelCharacteristic = null;
            _infoRfidTidCharacteristic = null;
            _infoRfidUserMemoryCharacteristic = null;
            _infoDeviceInfoCharacteristic = null;
        }

        #region native_ble_handlers_receiving

        void CbManagerOnDiscoveredPeripheral(object sender, CBDiscoveredPeripheralEventArgs e)
        {
            try
            {
                //from eInkDevice
                // if (e.Peripheral.Name.ToLower().Contains(Constants.SensorName) || e.Peripheral.Name.ToLower().Contains(Constants.SensorName1))
                if (e?.Peripheral != null && !string.IsNullOrWhiteSpace(e.Peripheral.Name) && (e.Peripheral.Name.ToLower().Contains(Constants.SensorName) || e.Peripheral.Name.ToLower().Contains(Constants.SensorName1)) )
                {

                    Console.WriteLine("Device detected " + e.Peripheral.Name);

                    var kcbAdvDataManufacturerData =
                        e.AdvertisementData?.ObjectForKey(new NSString("kCBAdvDataManufacturerData")) as NSObject;
                    var manufacturerData = kcbAdvDataManufacturerData as NSData;
                    byte[] dataBytes = manufacturerData?.ToArray();

                    if (dataBytes?.Length < 9) return;

                    var macRaw = new byte[6];

                    for (int i = 0; i < macRaw.Length; i++)
                    {
                        macRaw[i] = dataBytes[4 + i];
                    }
                    macRaw = macRaw.Reverse<byte>().ToArray();
                    string realMacAddr = BitConverter.ToString(macRaw).Replace("-", ":");

                    DeviceSensor device = null;
                    var id = e.Peripheral.Identifier.ToString();

                    
                        //in paring mode

                        lock (_deviceAddLock)
                        {
                            if (CurrentPairedSensor != null) return;

                        
                            CurrentPairedSensor = new DeviceSensor()
                            {
                                Peripheral = e.Peripheral,
                                Id = id,
                                MacAddress = realMacAddr,
                                RSSI = e?.RSSI?.StringValue,
                                Name = e?.Peripheral?.Name,
                            };
                            _device = e.Peripheral;

                            NewSensorDetectedEvent?.Invoke(this, new NewSensorDetectedEventArgs()
                            {
                                Sensor = CurrentPairedSensor,
                                DataBytes = dataBytes,
                               // DeviceType = DeviceType.Physical
                            });

                        }
                    

                                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void CbManagerOnUpdatedState(object sender, EventArgs e)
        {
           
            var message = string.Empty;
            var isPoweredOn = false;

            switch (_manager.State)
            {
                case CBCentralManagerState.PoweredOn:
                    isPoweredOn = true;
                    break;
                case CBCentralManagerState.Unsupported:
                    message = "The platform or hardware does not support Bluetooth Low Energy.";
                    break;
                case CBCentralManagerState.Unauthorized:
                    message = "The application is not authorized to use Bluetooth Low Energy.";
                    break;
                case CBCentralManagerState.PoweredOff:
                    message = "Bluetooth is currently powered off.";
                    break;
                default:
                    message = "Unhandled state: " + _manager.State;
                    break;
            }


           // Console.WriteLine(message);
            BLEReadyEvent?.Invoke(this, new EventRequestStatusArgs
            {
                IsSuccess = isPoweredOn,
                Message = message
            });
        }

        private void CbManagerOnConnectedPeripheral(object sender, CBPeripheralEventArgs e)
        {
            _device = e.Peripheral;
            if (_device != null)
            {
                SetHandlers();

                //Device is connected to, handlers are set, start discovering services
                if (e.Peripheral.State == CBPeripheralState.Connected)
                {
                    e.Peripheral.DiscoverServices();
                }
                else
                {
                    ConnectPeripheral(e.Peripheral);
                }

            }
        }

        private void CbManagerOnFailedToConnectPeripheral(object sender, CBPeripheralErrorEventArgs e)
        {
            //Todo: any thing needed here?
            Console.WriteLine("Failed to connect to peripheral " + e.Error);
        }

        private void CbManagerOnWillRestoreState(object sender, CBWillRestoreEventArgs e)
        {
            //Todo: any thing needed here?
        }

        private void CbManagerOnDisconnectedPeripheral(object sender, CBPeripheralErrorEventArgs e)
        {
            //sensor was probably turned off
            Console.WriteLine($"Disconnected with error message: {e.Error?.Code}");
            if (CurrentPairedSensor != null)
            {
                SensorDisconnected?.Invoke(this, new SensorDisconnectEventArgs
                {
                    Sensor = CurrentPairedSensor,
                    DisconnectReason = e.Error?.Code.ToString()
                });
            }
        }

        #endregion

        #region native_ble_handlers_sharing

        private void CbpManagerOnAdvertisingStarted(object sender, NSErrorEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine("Advertise start failure {0}", e.Error.Description);
            }

        }

        private void CbpManagerOnStateUpdated(object sender, EventArgs e)
        {
            string message = string.Empty;
            bool isPoweredOn = false;

            switch (_cbPeripheralManger.State)
            {
                case CBPeripheralManagerState.PoweredOn:
                    //Refresh(sender, e);
                    //we are good
                    isPoweredOn = true;
                    break;
                case CBPeripheralManagerState.Unsupported:
                    message = "The platform or hardware does not support Bluetooth Low Energy.";
                    break;
                case CBPeripheralManagerState.Unauthorized:
                    message = "The application is not authorized to use Bluetooth Low Energy.";
                    break;
                case CBPeripheralManagerState.PoweredOff:
                    message = "Bluetooth is currently powered off.";
                    break;
                case CBPeripheralManagerState.Resetting:
                    message = "Bluetooth is currently resetting.";
                    break;
                case CBPeripheralManagerState.Unknown:
                    message = "Bluetooth is currently unknown.";
                    break;
                default:
                    message = "Unhandled state: " + _manager.State;
                    break;
            }

            Console.WriteLine(message);
            BLEReadyEvent?.Invoke(this, new EventRequestStatusArgs
            {
                IsSuccess = isPoweredOn,
                Message = message
            });
        }

        #endregion

        #region IBleService_interface_implementation

        public void StartScanning()
        {
            //deviceMacAddress = deviceId;
            _options = new PeripheralScanningOptions()
            {
                AllowDuplicatesKey = true,
            };

            _manager?.ScanForPeripherals(null, _options);
        }

        public void StopScanning(bool clear)
        {
            try
            {
                if (_manager != null)
                {
                    if (clear)
                    {
                        DiscoveredSensors.Clear();
                        DiscoveredShares.Clear();
                        ClearCharacteristics();
                        CurrentPairedSensor = null;
                        _device = null;
                        deviceMacAddress = string.Empty;
                    }

                    _manager.StopScan();
                    _manager.DiscoveredPeripheral -= CbManagerOnDiscoveredPeripheral;
                    _manager.UpdatedState -= CbManagerOnUpdatedState;
                    _manager.ConnectedPeripheral -= CbManagerOnConnectedPeripheral;
                    _manager.FailedToConnectPeripheral -= CbManagerOnFailedToConnectPeripheral;
                    _manager.WillRestoreState -= CbManagerOnWillRestoreState;
                    _manager.DisconnectedPeripheral -= CbManagerOnDisconnectedPeripheral;

                   // _manager = null;
                }
            }
            catch (Exception Ex)
            {
                Console.Write(Ex.ToString());
            }

        }

        public void ClearLists()
        {
            DiscoveredSensors.Clear();
            DiscoveredShares.Clear();
            _handlerSet = false;
            _device = null;
            CurrentPairedSensor = null;
            deviceMacAddress = string.Empty;
        }

        public async void ConnectBLEDevice(DeviceSensor device)
        {
            if (_device == null) return;
            if (_device.State == CBPeripheralState.Connecting)
            {
                _manager.CancelPeripheralConnection(_device);
                await System.Threading.Tasks.Task.Delay(500).ContinueWith((result) =>
                {
                    ConnectPeripheral(_device);
                });
            }
            else
            {
                ConnectPeripheral(_device);
            }

        }

        public void DisconnectPeripheral(DeviceSensor device)
        {
            if (_device == null) return;

            if (_device.State == CBPeripheralState.Connected ||
                _device.State == CBPeripheralState.Connecting)
            {
                try
                {
                    _manager.CancelPeripheralConnection(_device);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            CurrentPairedSensor = null;
            _device = null;
            _handlerSet = false;
            ClearCharacteristics();

        }

        public void SetCharacteristicNotify(DeviceSensor device, string serviceUuid, string characteristicUuid, bool on)
        {

            if (_device == null) return;

            var characteristic = GetCharacteristic(characteristicUuid);
            if (characteristic != null)
            {
                _device.SetNotifyValue(on, characteristic);
            }
        }

        public int RequestMtuNativeAsync(int requestValue, DeviceSensor sensor)
        {
            if (_device != null)
                return (int)_device?.GetMaximumWriteValueLength(CBCharacteristicWriteType
                    .WithoutResponse);
            else
            {
                return 20;
            }
        }

        public int RequestMTU()
        {
            return 200;
        }
        public void BeginWriteCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUUID,
            byte[] value, BLECharacteristicWriteType WriteType = BLECharacteristicWriteType.WithResponse)
        {

            if (_device == null) return;

            var characteristic = GetCharacteristic(characteristicUUID);
            if (characteristic != null && value != null)
            {
                var data = NSData.FromArray(value);

                _device.WriteValue(data, characteristic,
                    WriteType == BLECharacteristicWriteType.WithResponse
                        ? CBCharacteristicWriteType.WithResponse
                        : CBCharacteristicWriteType.WithoutResponse);
            }
            else
            {
                if (value == null)
                {
                    Console.WriteLine($"Weird: Write value is null for characteristic {characteristic?.UUID.ToString()}");
                }
            }
        }

        public void BeginReadCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUuid)
        {
            if (_device == null) return;

            var characteristic = GetCharacteristic(characteristicUuid);
            if (characteristic != null)
            {
                _device.ReadValue(characteristic);
            }
        }

        public void RefreshConnectedDevices(List<DeviceSensor> devices)
        {
            DeviceSensor removed;
            foreach (var device in devices)
            {
                if (!device.Paired)
                {
                    DiscoveredSensors.TryRemove(device.Peripheral as CBPeripheral, out removed);
                }
            }
        }

        public void StartAdvertising(long imageId)
        {
            string nameValue = $"digme_{imageId}";

            var advertisementData = new NSDictionary(
                CBAdvertisement.DataLocalNameKey, nameValue,
                CBAdvertisement.IsConnectable, false);

            if (_cbPeripheralManger.Advertising)
                _cbPeripheralManger.StopAdvertising();

            _cbPeripheralManger.StartAdvertising(advertisementData);
        }

        public void StopAdvertising()
        {
            try
            {
                _cbPeripheralManger.AdvertisingStarted -= CbpManagerOnAdvertisingStarted;
                _cbPeripheralManger.StateUpdated -= CbpManagerOnStateUpdated;
                _cbPeripheralManger.StopAdvertising();
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());

            }
        }

        #endregion IBleService_interface_implementation

        #region helper_methods

        public byte[] GetManufactureData(long imageId)
        {

            byte[] rawManufactureData = new byte[15];
            rawManufactureData[0] = 0xFF;
            rawManufactureData[1] = 0xE0;

            //var senderId = BitConverter.GetBytes(userId);
            //Array.Copy(senderId, 0, rawManufactureData, 2, senderId.Length);

            var _imageId = BitConverter.GetBytes(imageId);
            Array.Copy(_imageId, 0, rawManufactureData, 2, _imageId.Length);

            return rawManufactureData;
        }

        private CBCharacteristic GetCharacteristic(string uuid)
        {
            switch (uuid)
            {
                case Constants.CommandIdUuid:
                    return _commandIdCharacteristic;
                case Constants.CommandDataUuid:
                    return _commandDataCharacteristic;
                case Constants.CommandResultUuid:
                    return _commandResultCharacteristic;
                case Constants.MemoryIndexIdUuid:
                    return _cardXferMemoryIndexCharacteristic;
                case Constants.CompressedUuid:
                    return _cardXferCompressedCharacteristic;
                case Constants.CardDataUuid:
                    return _cardXferDataCharacteristic;
                case Constants.CardSideUuid:
                    return _cardXferBackCharacteristic;
                case Constants.WriteDoneUuid:
                    return _cardXferWriteDoneCharacteristic;
                case Constants.BatteryLevelUuid:
                    return _infoBatteryLevelCharacteristic;
                case Constants.DeviceInformationUuid:
                    return _infoDeviceInfoCharacteristic;
            
                default:
                    return null;
            }
        }

        private void InitiateCommandService(CBService service)
        {
            _commandIdCharacteristic = service?.Characteristics?.FirstOrDefault(x => string.Equals(x.UUID.ToString(), Constants.CommandIdUuid, StringComparison.CurrentCultureIgnoreCase));
            _commandDataCharacteristic = service?.Characteristics?.FirstOrDefault(x => string.Equals(x.UUID.ToString(), Constants.CommandDataUuid, StringComparison.CurrentCultureIgnoreCase));
            _commandResultCharacteristic = service?.Characteristics?.FirstOrDefault(x => string.Equals(x.UUID.ToString(), Constants.CommandResultUuid, StringComparison.CurrentCultureIgnoreCase));

            ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
            {
                ServiceUuid = Constants.CommandServiceUuid
            });

        }

        private void InitiateCardTransferService(CBService service)
        {
            _cardXferMemoryIndexCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.MemoryIndexIdUuid,
                    StringComparison.CurrentCultureIgnoreCase));
            _cardXferCompressedCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.CompressedUuid, StringComparison.CurrentCultureIgnoreCase));
            _cardXferDataCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.CardDataUuid, StringComparison.CurrentCultureIgnoreCase));
            _cardXferBackCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.CardSideUuid, StringComparison.CurrentCultureIgnoreCase));
            _cardXferWriteDoneCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.WriteDoneUuid, StringComparison.CurrentCultureIgnoreCase));

            ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
            {
                ServiceUuid = Constants.CommandTransferServiceUuid
            });
        }

        private void InitiateInformationService(CBService service)
        {
            _infoBatteryLevelCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.BatteryLevelUuid,
                    StringComparison.CurrentCultureIgnoreCase));
            _infoDeviceInfoCharacteristic = service?.Characteristics?.FirstOrDefault(x =>
                string.Equals(x.UUID.ToString(), Constants.DeviceInformationUuid,
                    StringComparison.CurrentCultureIgnoreCase));
          

            ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
            {
                ServiceUuid = Constants.InformationServiceUuid
            });

        }

        private void SetHandlers()
        {
            lock (_setHandlersLock)
            {
                if (!_handlerSet) //(!device.HandlersSet)
                {
                    Debug.WriteLine("Setting handlers for " + CurrentPairedSensor.Name);
                    try
                    {
                        CBPeripheral peripheral = _device; //device.Peripheral as CBPeripheral;
                        var mtu = peripheral.GetMaximumWriteValueLength(CBCharacteristicWriteType
                            .WithoutResponse);

                        peripheral.DiscoveredService += (object sender, NSErrorEventArgs err) =>
                        {
                            try
                            {
                                foreach (CBService service in ((CBPeripheral)sender).Services)
                                {
                                    ((CBPeripheral)sender).DiscoverCharacteristics(service);
                                }

                                                          }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Something Bad Went wrong discovering Services: " + ex.Message);
                                NotifyErrorEvent?.Invoke(this, new ErrorEventValueArgs()
                                {
                                    ErrorMessage = "An error occured as follows: " + ex.Message,
                                });
                            }
                        };

                        peripheral.DiscoveredCharacteristic += (object sender, CBServiceEventArgs e) =>
                        {
                            var serviceUUID = e.Service.UUID.ToString().ToLower();
                            if (serviceUUID == Constants.CommandServiceUuid)
                            {
                                InitiateCommandService(e.Service);
                            }
                            else if (serviceUUID == Constants.CommandTransferServiceUuid)
                            {
                                InitiateCardTransferService(e.Service);
                            }
                            else if (serviceUUID == Constants.InformationServiceUuid)
                            {
                                InitiateInformationService(e.Service);
                            }
                        };
                        peripheral.DiscoveredDescriptor += (object sender, CBCharacteristicEventArgs c) => { };
                        peripheral.WroteDescriptorValue += (object sender, CBDescriptorEventArgs c) => { };


                        peripheral.UpdatedCharacterteristicValue += (object sender, CBCharacteristicEventArgs e) =>
                        {
                            try
                            {

                                if (e.Characteristic.Value != null)
                                {
                                    var characteristic = e.Characteristic.UUID.ToString().ToLower();
                                    var data = e.Characteristic.Value?.ToArray();

                                    if (characteristic == Constants.BatteryLevelUuid.ToLower())
                                    {
                                        MessagingCenter.Send<IBleService, EventValueArgs>(this,
                                            Constants.BleCharReadEventMsg,
                                            new EventValueArgs
                                            {
                                                CharacteristicUuId = e.Characteristic.UUID.ToString(),
                                                Sensor = CurrentPairedSensor,
                                                Value = data
                                            });
                                    }
                                    else
                                    {
                                        CharacteristicNotifyEvent?.Invoke(this, new EventValueArgs
                                        {
                                            Value = data,
                                            Sensor = CurrentPairedSensor,
                                            CharacteristicUuId = e.Characteristic.UUID.ToString(),
                                        });
                       
                                    }
                                }
                               
                            }
                            catch (Exception Ex)
                            {
                                Debug.WriteLine(Ex.ToString());
                            }
                        };

                        peripheral.UpdatedNotificationState += (object sender, CBCharacteristicEventArgs e) =>
                        {
                            try
                            {
                                var data = e.Characteristic.Value?.ToArray();
                                bool notifying = e.Characteristic.IsNotifying;
                                NotifyCallbackEvent?.Invoke(sender, new NotifyEnableEventArgs
                                {
                                    Value = 1,
                                    CharacteristicUuId = e.Characteristic.UUID.ToString(),
                                });
  
                            }
                            catch (Exception Ex)
                            {
                                Debug.WriteLine(Ex.ToString());
                            }

                        };

                        //call back for when a WriteWithResponse is successfully invoked?
                        peripheral.WroteCharacteristicValue += (object sender, CBCharacteristicEventArgs e) =>
                        {
                           // Console.WriteLine("write data");
                            try
                            {
                                var data = e.Characteristic.Value?.ToArray();
                                CharacteristicWriteResponseEvent?.Invoke(this, new EventValueArgs
                                {
                                    Value = data,
                                    Sensor = CurrentPairedSensor,
                                    CharacteristicUuId = e.Characteristic.UUID.ToString(),
                                });
                           
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.ToString());
                            }
                        };
                        //device.HandlersSet = true;

                        _handlerSet = true;

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Something Bad Went wrong: " + ex.Message);
                    }
                }
            }
        }

        private void ConnectPeripheral(CBPeripheral peripheral)
        {
            if (peripheral != null)
            {
                _manager.ConnectPeripheral(peripheral, new PeripheralConnectionOptions()
                {
                    NotifyOnConnection = true,
                    NotifyOnDisconnection = true,
                    NotifyOnNotification = true,

                });
            }
        }

        #endregion
    }
}