using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using EinkStarter.Droid.DependencyServices.Ble;
using EinkStarter.Utilities;
using EinkStarter.Interfaces;
using EinkStarter.Models.Device;
using EinkStarter.Utilities.EventHandlers;
using Xamarin.Forms;
using Android.Content;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Runtime;

using Java.Lang.Reflect;
using Java.Util;

using Array = System.Array;
using System.Text;
using EinkStarter.Enums;
using System.Drawing;
using System.ComponentModel.Design;

[assembly: Dependency(typeof(BleService))]
namespace EinkStarter.Droid.DependencyServices.Ble
{
    public class BleService : BluetoothGattCallback, IBleService
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
        // public event EventHandler<DataUpdatedEventArgs> SensorDataUpdated;

        private BluetoothManager _manager;
        private BluetoothAdapter _adapter;
        private GattScanCallback _callback;

        private bool _isScanning = false;
        private readonly object _lockScanning = new object();

        private BluetoothLeScanner _bleScanner;


        private readonly int _manufacturerId = 0xFFE0;
    
        private static string deviceMacAddress = string.Empty;

        public ConcurrentDictionary<string, ScanRecordHelper> DiscoveredSensors;
        public ConcurrentDictionary<string, string> DiscoveredShares;
        private DeviceSensor CurrentPairedSensor;
        private readonly object _deviceAddLock = new object();

        public string Identifier = Guid.NewGuid().ToString();

        #region hard_code_ble_characteristis_and_service_names
        private BluetoothGattCharacteristic _cardXferMemoryIndexCharacteristic;
        private BluetoothGattCharacteristic _cardXferCompressedCharacteristic;
        private BluetoothGattCharacteristic _cardXferDataCharacteristic;
        private BluetoothGattCharacteristic _cardXferWriteDoneCharacteristic;
        private BluetoothGattCharacteristic _cardXferBackCharacteristic;
        private BluetoothGattCharacteristic _commandIdCharacteristic;
        private BluetoothGattCharacteristic _commandDataCharacteristic;
        private BluetoothGattCharacteristic _commandResultCharacteristic;
        private BluetoothGattCharacteristic _infoBatteryLevelCharacteristic;
        private BluetoothGattCharacteristic _infoRfidTidCharacteristic;
        private BluetoothGattCharacteristic _infoRfidUserMemoryCharacteristic;
        private BluetoothGattCharacteristic _infoDeviceInfoCharacteristic;
        private BluetoothGatt _gatt;
        private BluetoothDevice _device;
        #endregion  hard_code_ble_characteristis_and_service_names

        public void Initialize()
        {
            var appContext = Android.App.Application.Context;
            DiscoveredSensors = new ConcurrentDictionary<string, ScanRecordHelper>();
            DiscoveredShares = new ConcurrentDictionary<string, string>();

            _manager = (BluetoothManager)appContext.GetSystemService(Context.BluetoothService);
          
            _adapter = _manager?.Adapter;
            if (_adapter != null && _adapter.IsEnabled)
            {
                if (_adapter != null)
                {
                   
                        _callback = new GattScanCallback();
                        _bleScanner = _adapter.BluetoothLeScanner;

                    BLEReadyEvent?.Invoke(this, new EventRequestStatusArgs
                    {
                        IsSuccess = true,
                        Message = string.Empty,
                    });
                }
            }
            else
            {
                BLEReadyEvent?.Invoke(this, new EventRequestStatusArgs
                {
                    IsSuccess = false,
                    Message = "Cannot connect to Device. Bluetooth is currently turned off.",
                });
            }

        }

        private void AddDeviceDetectedHandler()
        {
            if (_callback == null) return;

            _callback.DeviceDetected -= BleOnDeviceDetected;
            _callback.DeviceDetected += BleOnDeviceDetected;
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
        private void BleOnDeviceDetected(object sender, ScanRecordEventArgs e)
        {
            var result = e.ScanResult;

            var address = result.Device?.Address;
            var name = result.Device?.Name;
            var iName = e.ScanResult.ScanRecord?.DeviceName;

           // System.Diagnostics.Debug.WriteLine($"Device detected: {name} Address {address}");
            byte[] dataBytes = null;

            #region check_manufacturerData
            var manufacturerSpecificData = result.ScanRecord?.ManufacturerSpecificData;
            if (manufacturerSpecificData != null)
            {
                var arrayList = new List<byte[]>(manufacturerSpecificData.Size());

                for (var i = 0; i < manufacturerSpecificData.Size(); i++)
                    arrayList.Add(manufacturerSpecificData.ValueAt(i)?.ToArray<byte>());

                if (arrayList.Count > 0)
                {

                    dataBytes = new byte[arrayList[0].Length];
                    Array.Copy(arrayList[0], 0, dataBytes, 0, arrayList[0].Length);
                }
            }
            #endregion check_manufacturerData

            DeviceSensor device = null;
            DiscoveredSensors.TryGetValue(address, out var helper);
            if (helper != null)
            {
                device = helper.Sensor;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {

               
                if (name.ToLower().Contains(Constants.SensorName) || name.ToLower().Contains(Constants.SensorName1))
                {
                   
                    System.Diagnostics.Debug.WriteLine($"Device detected: {name}");
                    var id = result.Device.Address;
                    //in paring mode

                    lock (_deviceAddLock)
                    {
                        if (CurrentPairedSensor != null) return;

                        CurrentPairedSensor = new DeviceSensor()
                        {
                            Peripheral = result.Device,
                            Id = id,
                            MacAddress = id,
                            RSSI = result.Rssi.ToString(),
                            Name = name
                        };
                        _device = result.Device;

                        NewSensorDetectedEvent?.Invoke(this, new NewSensorDetectedEventArgs()
                        {
                            Sensor = CurrentPairedSensor,
                            DataBytes = dataBytes,
                        });
                    }
                }

               
            }
         

        }
        public override async void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            switch (newState)
            {
                // disconnected
                case ProfileState.Disconnected:

                    await Task.Run(() => _gatt?.Close()).ContinueWith(async (result) =>
                    {
                        await Task.Delay(10).ContinueWith((innerResult) =>
                        {
                            ClearLists();
                            _gatt?.Dispose();
                            _gatt = null;
                            _device?.Dispose();
                            _device = null;
                            ClearCharacteristics();

                            if (CurrentPairedSensor == null) return;

                            SensorDisconnected?.Invoke(this, new SensorDisconnectEventArgs
                            {
                                Sensor = CurrentPairedSensor,
                                DisconnectReason = status.ToString()
                            });
 
                            CurrentPairedSensor = null;

                        });

//#if DEBUG
                        Console.WriteLine($"disconnected device for gatt -> {gatt.Device.Address} with Status {status} ") ;
//#endif
                    });
                    break;

                case ProfileState.Connecting:

                    Console.WriteLine("Connecting");

                    break;

                // connected
                case ProfileState.Connected:
                    Console.WriteLine("Connected");
                    gatt.RequestMtu(512);
                    break;

                default: break;
            }
        }

        private void InitiateCommandService()
        {
            var cmdService = _gatt?.GetService(UUID.FromString(Constants.CommandServiceUuid));
            if (cmdService != null)
            {
                _commandIdCharacteristic = cmdService.GetCharacteristic(UUID.FromString(Constants.CommandIdUuid));
                _commandDataCharacteristic = cmdService.GetCharacteristic(UUID.FromString(Constants.CommandDataUuid));
                _commandResultCharacteristic = cmdService.GetCharacteristic(UUID.FromString(Constants.CommandResultUuid));

                ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
                {
                    ServiceUuid = Constants.CommandServiceUuid
                });
            }
        }

     

        private void InitiateInformationService()
        {
            var infoService = _gatt?.GetService(UUID.FromString(Constants.InformationServiceUuid));
            if (infoService != null)
            {
                _infoBatteryLevelCharacteristic = infoService.GetCharacteristic(UUID.FromString(Constants.BatteryLevelUuid));
                _infoDeviceInfoCharacteristic = infoService.GetCharacteristic(UUID.FromString(Constants.DeviceInformationUuid));
                ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
                {
                    ServiceUuid = Constants.InformationServiceUuid
                });
            }

        }

        private void InitiateCardTransferService()
        {
            var cardService = _gatt?.GetService(UUID.FromString(Constants.CommandTransferServiceUuid));
            if (cardService != null)
            {
                _cardXferMemoryIndexCharacteristic = cardService.GetCharacteristic(UUID.FromString(Constants.MemoryIndexIdUuid));
                _cardXferCompressedCharacteristic = cardService.GetCharacteristic(UUID.FromString(Constants.CompressedUuid));
                _cardXferDataCharacteristic = cardService.GetCharacteristic(UUID.FromString(Constants.CardDataUuid));
                _cardXferBackCharacteristic = cardService.GetCharacteristic(UUID.FromString(Constants.CardSideUuid));
                _cardXferWriteDoneCharacteristic = cardService.GetCharacteristic(UUID.FromString(Constants.WriteDoneUuid));

               
                ServiceCharacteristicsEventCompleted?.Invoke(this, new BleServiceDiscoveredEventArgs()
                {
                    ServiceUuid = Constants.CommandTransferServiceUuid
                });
            }
        }
        public BluetoothGattCharacteristic GetCharacteristic(string uuid)
        {
            return uuid switch
            {
                Constants.CommandIdUuid => _commandIdCharacteristic,
                Constants.CommandDataUuid => _commandDataCharacteristic,
                Constants.CommandResultUuid => _commandResultCharacteristic,
                Constants.MemoryIndexIdUuid => _cardXferMemoryIndexCharacteristic,
                Constants.CompressedUuid => _cardXferCompressedCharacteristic,
                Constants.CardDataUuid => _cardXferDataCharacteristic,
                Constants.CardSideUuid => _cardXferBackCharacteristic,
                Constants.WriteDoneUuid => _cardXferWriteDoneCharacteristic,
                Constants.BatteryLevelUuid => _infoBatteryLevelCharacteristic,
                Constants.DeviceInformationUuid => _infoDeviceInfoCharacteristic,
                _ => null
            };
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            if (CurrentPairedSensor != null)
            {
               
                InitiateCommandService();
                InitiateInformationService();
                InitiateCardTransferService();
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic,
            GattStatus status)
        {
            if (CurrentPairedSensor == null || characteristic == null) return;

            CharacteristicValueReadEvent?.Invoke(this, new EventValueArgs
            {
                CharacteristicUuId = characteristic?.Uuid?.ToString(),
                Sensor = CurrentPairedSensor,
                Value = characteristic.GetValue()
            });
            //BleCharManuallyRead
            //MessagingCenter.Send<IBleService, EventValueArgs>(this,
            //    Constants.BleCharReadEventMsg,
            //    new EventValueArgs
            //    {
            //        CharacteristicUuId = characteristic?.Uuid?.ToString(),
            //        Sensor = CurrentPairedSensor,
            //        Value = characteristic.GetValue()
            //    });
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            if (characteristic == null || CurrentPairedSensor == null)
                return;


            CharacteristicNotifyEvent?.Invoke(this, new CharacteristicNotifyEventArgs
            {
                Value = characteristic.GetValue(),
                CharacteristicUuId = characteristic.Uuid?.ToString(),
                Sensor = CurrentPairedSensor,
            });

            //MessagingCenter.Send<IBleService, CharacteristicNotifyEventArgs>(this,
            //    Constants.BleCharNotifyEventMsg,
            //    new CharacteristicNotifyEventArgs
            //    {
            //        Value = characteristic.GetValue(),
            //        CharacteristicUuId = characteristic.Uuid?.ToString(),
            //        Sensor = CurrentPairedSensor,
            //    });
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, [GeneratedEnum] GattStatus status)
        {
            try
            {
                if (CurrentPairedSensor == null) return;


                //if (characteristic.Uuid.ToString().ToLower() == Constants.CommandDataUuid.ToLower())
                //{
                //    //CommandIndex[0] = GetCommandData();
                //    //await BleCommandDispatcher(Constants.CommandServiceUuid, Constants.CommandIdUuid,
                //    //    value: CommandIndex,
                //    //    isWriteCommand: true, writeType: BLECharacteristicWriteType.WithResponse);
                //}

                CharacteristicWriteResponseEvent?.Invoke(this, new EventValueArgs
                {
                    Value = characteristic?.GetValue(),
                    CharacteristicUuId = characteristic?.Uuid?.ToString(),
                    Sensor = CurrentPairedSensor,
                });

                //MessagingCenter.Send<IBleService, WriteResponseEventArgs>(this,
                //    Constants.BleCharWriteResponseEventMsg,
                //    new WriteResponseEventArgs
                //    {
                //        Value = characteristic?.GetValue(),
                //        CharacteristicUuId = characteristic?.Uuid?.ToString(),
                //        Sensor = CurrentPairedSensor,
                //    });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Writing: " + ex.ToString());
            }
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            NotifyCallbackEvent?.Invoke(this, new NotifyEnableEventArgs
            {
                Value = (int)status,
                CharacteristicUuId = descriptor.Characteristic.Uuid.ToString(),
            });

            Task.Delay(200);
        }

        private int Mtu = 20;
        public override void OnMtuChanged(BluetoothGatt gatt, int mtu, GattStatus status)
        {
            //limit MTU to 200
            Mtu = Math.Min(mtu, 200);
            gatt?.DiscoverServices();
        }

        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorRead(gatt, descriptor, status);
        }


        #endregion native_ble_handlers_receiving

        #region native_ble_handlers_sharing
        private void AdvertiserOnAdvertisementStatus(object sender, AdvertiseEventArgs e)
        {
            if (!e.Success)
            {
                BLEReadyEvent?.Invoke(this, new EventRequestStatusArgs
                {
                    IsSuccess = e.Success,
                    Message = e.Error,
                });
            }
        }
        #endregion native_ble_handlers_sharing

        #region IBleService_interface_implementation
        public void StartScanning()
        {
            var sc = new ScanSettings.Builder().SetMatchMode(BluetoothScanMatchMode.Aggressive)?.Build();
            lock (_lockScanning)
            {
                if (_isScanning) return;

                AddDeviceDetectedHandler();
                CurrentPairedSensor = null;
                _device = null;
                _gatt = null;

                _isScanning = true;
                _bleScanner.StartScan(null, sc, _callback);              
            }
        }
        public void StopScanning(bool clear = false)
        {
            try
            {
                if (_callback == null) return;

                lock (_lockScanning)
                {
                    if (_isScanning)
                    {
                        _adapter?.BluetoothLeScanner?.FlushPendingScanResults(_callback);

                        _bleScanner.StopScan(_callback);
                        _callback.DeviceDetected -= BleOnDeviceDetected;

                        _isScanning = false;
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        public void ConnectBLEDevice(DeviceSensor device)
        {
            try
            {

                if (CurrentPairedSensor != null)
                {
                    var bondState = _device.BondState;

                    _gatt = _device.ConnectGatt(Android.App.Application.Context,
                        false,
                        this);
                }
            }
            catch (Exception ex)
            {
                NotifyErrorEvent?.Invoke(this, new ErrorEventValueArgs()
                {
                    ErrorMessage = $"Unable to Connect to Device {ex}",
                });

             //   Crashes.TrackError(ex);
            }
        }
        public void DisconnectPeripheral(DeviceSensor device)
        {
            _gatt?.Disconnect();

            //_callback?.Dispose();
            //_manager?.Dispose();
            //_adapter?.Dispose();

            //_device?.Dispose();
            //_gatt?.Close();

            //_callback = null;
            //_manager = null;
            //_adapter = null;
            //_gatt = null;
            //_device = null;
        }
        public int RequestMtuNativeAsync(int requestValue, DeviceSensor sensor)
        {
            return Mtu;
        }

        public int RequestMTU()
        {
            return Mtu;
        }
        public void BeginReadCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUuid)
        {
            var characteristic = GetCharacteristic(characteristicUuid);

            if (characteristic != null && _gatt != null)
            {
                bool response = _gatt.ReadCharacteristic(characteristic);
            }
        }

        public static string ToHexString(byte[] ba, bool reverseOrder)
        {
            if (reverseOrder)
                ba = ba.Reverse().ToArray();

            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString().ToUpper();
        }
        private object _writeLock = new object();
        public void BeginWriteCharacteristicValue(DeviceSensor device, string serviceUuid, string characteristicUuid,
            byte[] value, BLECharacteristicWriteType writeType = BLECharacteristicWriteType.WithResponse)
        {
            lock (_writeLock)
            {
                var characteristic = GetCharacteristic(characteristicUuid);
                if (characteristic != null && _gatt != null)
                {
                    try
                    {
                        characteristic.SetValue(value);
                        characteristic.WriteType = writeType == BLECharacteristicWriteType.WithoutResponse
                            ? GattWriteType.NoResponse
                            : GattWriteType.Default;

                        bool response = _gatt.WriteCharacteristic(characteristic);
                        Console.WriteLine($"Characteristic ResponseP {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else
                {

                }
                /*
                if (device == null)
                {
                    NotifyErrorEvent?.Invoke(this, new ErrorEventValueArgs()
                    {
                        ErrorMessage = "Device Reporting Null",
                    });
                    return;
                }
                var service = device.Services?.FirstOrDefault(y => y.UUID.ToLower() == serviceUuid.ToLower());
                if (service == null)
                    return;

                var characteristic = (BluetoothGattCharacteristic) (service.Characteristics.FirstOrDefault(s =>
                    (s as BluetoothGattCharacteristic)?.Uuid?.ToString().ToLower() == characteristicUuid.ToLower()));
                var helper = GetRecordFor(device.MacAddress);
                if (characteristic != null && helper != null)
                {
                    try
                    {
                        characteristic.SetValue(value);
                        characteristic.WriteType = writeType == BLECharacteristicWriteType.WithoutResponse
                            ? GattWriteType.NoResponse
                            : GattWriteType.Default;

                        bool response = helper.Gatt.WriteCharacteristic(characteristic);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else
                {

                }
                */
            }
        }
        public void SetCharacteristicNotify(DeviceSensor device, string serviceUuid, string characteristicUuid, bool state)
        {
            var characteristic = GetCharacteristic(characteristicUuid);
            if (characteristic != null && _gatt != null)
            {
                try
                {
                    if (characteristic.Descriptors?.Count > 0)
                    {

                        var val = _gatt.SetCharacteristicNotification(characteristic, state);
                        characteristic.WriteType = GattWriteType.Default;

                        foreach (var descriptor in characteristic.Descriptors)
                        {
                            var value = new byte[BluetoothGattDescriptor.EnableNotificationValue.Count];
                            BluetoothGattDescriptor.EnableNotificationValue.CopyTo(value, 0);

                            var canSetNotification = descriptor.SetValue(value);
                            if (canSetNotification)
                            {
                                var writeSuccess = _gatt.WriteDescriptor(descriptor);
                            }
                        }

                    }
                    else
                    {
                        Console.WriteLine(
                            $"{characteristicUuid.ToLower()}, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't write descriptors: " + ex);
                }
            }
            else
            {

            }

            /*
            var service = device.Services?.FirstOrDefault(y => y.UUID.ToLower() == serviceUuid.ToLower());
            if (service == null) return;

            var characteristic = (BluetoothGattCharacteristic)(service.Characteristics.FirstOrDefault(s =>
                (s as BluetoothGattCharacteristic)?.Uuid?.ToString().ToLower() == characteristicUuid.ToLower())); var peripheral = GetRecordFor(device.MacAddress);

            if (characteristic != null && peripheral != null)
            {
                //need to write this

                try
                {
                    if (characteristic.Descriptors?.Count > 0)
                    {

                        var val = peripheral.Gatt.SetCharacteristicNotification(characteristic, state);
                        characteristic.WriteType = GattWriteType.Default;

                        foreach (var descriptor in characteristic.Descriptors)
                        {
                            var value = new byte[BluetoothGattDescriptor.EnableNotificationValue.Count];
                            BluetoothGattDescriptor.EnableNotificationValue.CopyTo(value, 0);

                            var canSetNotification = descriptor.SetValue(value);
                            if (canSetNotification)
                            {
                                var writeSuccess = peripheral.Gatt.WriteDescriptor(descriptor);
                            }
                        }

                    }
                    else
                    {
                        Console.WriteLine(
                            $"{characteristicUuid.ToLower()}, FAILED: _nativeCharacteristic.Descriptors was empty, not sure why");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't write descriptors: " + ex);
                }

            }
            */
        }
        public void ClearLists()
        {
            DiscoveredSensors?.Clear();
            DiscoveredShares?.Clear();
        }


        //public void StartAdvertising(long imageId)
        //{
        //    var builder = new AdvertiseSettings.Builder();
        //    builder.SetAdvertiseMode(AdvertiseMode.LowLatency);
        //    builder.SetConnectable(false);
        //    builder.SetTimeout(0);
        //    builder.SetTxPowerLevel(AdvertiseTx.PowerHigh);


        //    AdvertiseData.Builder dataBuilder = new AdvertiseData.Builder();

        //    byte[] manufacturerData = GetManufacturerData(imageId);

        //    dataBuilder.SetIncludeDeviceName(true);
        //    dataBuilder.SetIncludeTxPowerLevel(false);
        //    dataBuilder.AddManufacturerData(_manufacturerId, manufacturerData);


        //    if (_bleAdvertiser != null)
        //    {
        //        _bleAdvertiser.StartAdvertising(builder.Build(), dataBuilder.Build(), _advertiseCallback);
        //    }

        //}
        //public void StopAdvertising()
        //{
        //    _bleAdvertiser.StopAdvertising(_advertiseCallback);
        //}
        #endregion IBleService_interface_implementation

        #region helper_methods
        public byte[] GetManufacturerData(long imageId)
        {
            var rawManufactureData = new byte[15];
            rawManufactureData[0] = 0xFF;
            rawManufactureData[1] = 0xE0;


            var _imageId = BitConverter.GetBytes(imageId);
            Array.Copy(_imageId, 0, rawManufactureData, 2, _imageId.Length);

            return rawManufactureData;
        }

        private ScanRecordHelper GetRecordFor(string address)
        {
            DiscoveredSensors.TryGetValue(address, out var helper);

            return helper;
        }
        #endregion helper_methods

    

    }

    public class ScanRecordHelper
    {
        public DeviceSensor Sensor { get; set; }
        public BluetoothDevice Device { get; set; }
        public BluetoothGatt Gatt { get; set; }
    }
}