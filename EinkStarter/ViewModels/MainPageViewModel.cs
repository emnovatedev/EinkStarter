using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EinkStarter.Interfaces;
using EinkStarter.Models.Device;
using EinkStarter.Utilities.EventHandlers;
using EinkStarter.ViewModels;
using EinkStarter.Models;
using EinkStarter.Utilities;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Prism.Services.Dialogs;
using System.Diagnostics;
using PropertyChanged;
using EinkStarter.Enums;
using System.ComponentModel.Design;
using Example;
using System.Reflection;
using System.Data;
using ImTools;

namespace EinkStarter.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public string Image { get; set; }
        public byte[] ImageBytes { get; set; }
        public DelegateCommand ShowImagePickerCommand { get; }
        public DelegateCommand LoadCardCommand { get; }
        public DelegateCommand WriteCardCommand { get; }
        public DelegateCommand ConnectDeviceCommand { get; }
        public DelegateCommand DisconnectDeviceCommand { get; }
        public DelegateCommand DeleteCardCommand { get; }
        public DelegateCommand DisplayCardCommand { get; }
        public DelegateCommand ResetDeviceCommand { get; }
        public ObservableCollection<CardSlot> CardSlots { get; private set; }
        public ObservableCollection<ImageSlot> ImageSlots { get; private set; }


        public CardSlot SelectedCardSlot { get; set; }

        public ImageSlot SelectedImageSlot { get; set; }

        public DelegateCommand<object> CardSlotSelectionChangedCommand { get; private set; }

        public DelegateCommand<object> ImageSlotSelectionChangedCommand { get; private set; }

        private readonly IActionSheetButton _takePhoto;
        private readonly IActionSheetButton _pickPhoto;
        private readonly IActionSheetButton _cancelOption;
        private readonly IPageDialogService _dialogService;

        [DoNotNotify]
        protected object DeviceSensorLock = new object();
        protected DeviceSensor DeviceSensor { get; set; }
        /* Progress bar related variable */
        public int Progress { get; set; }
        public int CurrentIndex { get; set; } //Chunk index in current writing activity
        public int TotalIndex { get; set; } //Maximum number of chunks in the current writing activity
        public int CompletionPercentage { get; set; } //percentage completion
        public string ActivityDescription { get; set; }
        /* End progress bar related variable */

        public bool ConnectEnabled { get; set; }
        public bool ButtonEnabled { get; set; }
        public bool ProgressEnabled { get; set; }
        public int BatteryLevel { get; set; }

        public byte CurrentCardIndex;
        public string CurrentImageName;


        private int _currentIndex;
        private bool _lastChunk;
        private bool _finalwrite;
        private int _numTrips;
        private int _remainder;
        private byte[] _transferBytes;

        int MtuSize;
        private const int CommandDelayInterval = 1000;

        IBleService BleService = DependencyService.Resolve<IBleService>();

        public DeviceCommandRequestedState DeviceCommandState = DeviceCommandRequestedState.None;

        public MainPageViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            ShowImagePickerCommand = new DelegateCommand(OnShowImagePickerCommand);
            CardSlotSelectionChangedCommand = new DelegateCommand<object>(OnCardTypeSelectionChangedCommand);
            ImageSlotSelectionChangedCommand = new DelegateCommand<object>(OnImageTypeSelectionChangedCommand);

            WriteCardCommand = new DelegateCommand(OnWriteCardCommand);
            DeleteCardCommand = new DelegateCommand(OnDeleteCardCommand);
            DisplayCardCommand = new DelegateCommand(OnDisplayCardCommand);
            ResetDeviceCommand = new DelegateCommand(OnResetDeviceCommand);
            ConnectDeviceCommand = new DelegateCommand(OnConnectDeviceCommand);
            DisconnectDeviceCommand = new DelegateCommand(OnDisconnectDeviceCommand);

            _cancelOption = ActionSheetButton.CreateCancelButton("Cancel", () => { });
            _takePhoto = ActionSheetButton.CreateButton("Take Photo", () => DoMediaSourceCommand(true));
            _pickPhoto = ActionSheetButton.CreateButton("Choose from library", () => DoMediaSourceCommand(false));
            ConnectEnabled = true;
            ButtonEnabled = false;
            ProgressEnabled = false;

            InitializeCardSlots();
            Loadcards();
        }

        private void InitializeCardSlots()
        {
            CardSlots = new ObservableCollection<CardSlot>();
            foreach (var i in Enumerable.Range(1, 5))
            {
                CardSlots.Add(new CardSlot() { Slot = i, SlotName = $"Slot {i}" });
            }

            //set the selected value to the 1st card in the list
            SelectedCardSlot = CardSlots[0];
            CurrentCardIndex = Convert.ToByte(SelectedCardSlot.Slot.ToString("X"));
        }
        private void OnShowImagePickerCommand()
        {
            //  ShowCameraLogic();
        }

        private void OnCardTypeSelectionChangedCommand(object obj)
        {
            var comboBoxItemArgs = obj as Syncfusion.XForms.ComboBox.SelectionChangedEventArgs;
            var cardSlot = comboBoxItemArgs?.Value as CardSlot;

            //  Debug.WriteLine(cardSlot.Slot);

            //this is the index that will be used to write the loaded card.
            CurrentCardIndex = Convert.ToByte(cardSlot.Slot);
            if (cardSlot == null) return;


        }

        private void OnImageTypeSelectionChangedCommand(object obj)
        {
            var comboBoxItemArgs = obj as Syncfusion.XForms.ComboBox.SelectionChangedEventArgs;
            var imageSlot = comboBoxItemArgs?.Value as ImageSlot;


            //this is the Card data that  will be used to write the selected slot.
            CurrentImageName = imageSlot.SlotNameLong;
            if (imageSlot == null) return;


        }


        public async void OnWriteCardCommand()
        {
            DeviceCommandState = DeviceCommandRequestedState.WriteCard;

        
            if (CurrentImageName == null)
            {
                UpdateDescription("Please load and select a card");
                return;
            }

            //load the card into memory from the embedded resources folder

            var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MainPageViewModel)).Assembly;
            var CardBin = assembly.GetManifestResourceStream($"{CurrentImageName}");
            byte[] CurrentCard;

            using (MemoryStream CurrentCardStream = new MemoryStream())
            {
                await CardBin.CopyToAsync(CurrentCardStream);
                CurrentCard = CurrentCardStream.ToArray();
            }


            _transferBytes = CurrentCard;
            MtuSize = BleService.RequestMTU();
            setuptransfer();


            //transfer process by sending the compression option.  state machines picks up after in callback notify functions
            await BleCommandDispatcher(Constants.CommandTransferServiceUuid, Constants.CompressedUuid,
                      value: new byte[] { 0x00 }, isWriteCommand: true,
                      writeType: BLECharacteristicWriteType.WithResponse);


            ProgressEnabled = true;
            ButtonEnabled = false;

        }
        public async void OnDeleteCardCommand()
        {

            //run the delete command for the current selected index.
            await DeleteCard();


        }
        public async void OnDisplayCardCommand()
        {

            //runs the command to display card on the device.
            await DisplayCard();



        }
        public async void OnResetDeviceCommand()
        {
            await DeleteAllCards();

        }

        public async void OnConnectDeviceCommand()
        {
            Debug.WriteLine("Connection command");

            UpdateDescription("Connect Device");

            AddBleHandlers();
           
            await InitiateConnection();
          
        }

        public async void OnDisconnectDeviceCommand()
        {
             DoDisconnect();

        }

        private void UpdateProgressStatus(int currentChunk, int totalChunks, string statusMessage)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ActivityDescription = $"Status: {statusMessage}";
                CurrentIndex = currentChunk;
                TotalIndex = totalChunks;
                Progress = CompletionPercentage = (int)Math.Min(((double)currentChunk / totalChunks) * 100, 100);
            });
        }

        private void UpdateBatteryLevel(int batteryLevel)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                BatteryLevel = batteryLevel;
            });
        }

        private void UpdateDescription(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ActivityDescription = message;
            });
        }
        private async Task ShowCameraLogic()
        {
            var cameraStatus = await CheckAndRequestPermission.CheckAndRequestPermissionAsync(new Permissions.Camera());
            var storageStatus =
                await CheckAndRequestPermission.CheckAndRequestPermissionAsync(new Permissions.StorageWrite());
            if (cameraStatus != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
            {
                bool proceed = await _dialogService.DisplayAlertAsync("Media Permissions",
                    "We need permission to access your photos or camera. Would you like to grant us access?", "Yes",
                    "No");
                if (proceed)
                {
                    AppInfo.ShowSettingsUI();
                }

                return;
            }

            await _dialogService.DisplayActionSheetAsync("Select Image", _cancelOption, null,
                _takePhoto, _pickPhoto);
        }

        private void SetDefaultImage()
        {
            if (ImageBytes == null || ImageBytes?.Length < 1)
            {
                Image = AppConstants.PlaceholderImageName;
            }
        }

        private async void DoMediaSourceCommand(bool isCamera)
        {
            try
            {

                await CrossMedia.Current.Initialize();

                MediaFile selectedImage = null;
                if (isCamera)
                {
                    selectedImage = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                    {
                        PhotoSize = PhotoSize.Custom,
                        CustomPhotoSize = 60,
                        Directory = "Eink",
                        Name = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss_ff"),
                        DefaultCamera = CameraDevice.Rear,
                        AllowCropping = false,
                    });
                }
                else
                {
                    selectedImage = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions()
                    {
                        PhotoSize = PhotoSize.Medium,
                    });
                }

                if (selectedImage == null)
                {
                    ImageBytes = null;
                    SetDefaultImage();
                    return;
                }

                using (var memory = new MemoryStream())
                {

                    var stream = selectedImage.GetStream();
                    await stream.CopyToAsync(memory);
                    ImageBytes = memory.ToArray();
                }
                Image = selectedImage.Path;


            }
            catch (Exception ex)
            {
                ImageBytes = null;
                SetDefaultImage();
                Debug.Write(ex.ToString());
            }
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            SetDefaultImage();
            base.OnNavigatedTo(parameters);
        }

        protected void DoDisconnect()
        {
            if (DeviceSensor != null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    
                    BleService.DisconnectPeripheral(DeviceSensor);
                    ConnectEnabled = true;
                    ButtonEnabled = false;
                    ProgressEnabled = false;
                    DeviceSensor = null;
                });
               
            }
        }

        private void AddBleHandlers()
        {
            //BLE related handlers
            RemoveHandlers();

            BleService.NotifyErrorEvent += BleServiceOnNotifyErrorEvent;
            BleService.NewSensorDetectedEvent += BleServiceOnNewSensorDetectedEvent;
            BleService.ServiceCharacteristicsEventCompleted += BleServiceOnServiceCharacteristicsEventCompleted;
            BleService.NotifyCallbackEvent += BleServiceOnNotifyCallbackEvent;
            BleService.SensorDisconnected += BleServiceOnSensorDisconnected;
            BleService.BLEReadyEvent += BleServiceOnBLEReadyEvent;
            BleService.CharacteristicWriteResponseEvent += BleServiceCharacteristicWriteResponseEvent;
            BleService.CharacteristicNotifyEvent += BleServiceCharacteristicNotifyEvent;
            BleService.CharacteristicValueReadEvent += BleServiceCharacteristicReadEvent;
        }


        private void RemoveHandlers()
        {
            BleService.NotifyErrorEvent -= BleServiceOnNotifyErrorEvent;
            BleService.NewSensorDetectedEvent -= BleServiceOnNewSensorDetectedEvent;
            BleService.ServiceCharacteristicsEventCompleted -= BleServiceOnServiceCharacteristicsEventCompleted;
            BleService.NotifyCallbackEvent -= BleServiceOnNotifyCallbackEvent;
            BleService.SensorDisconnected -= BleServiceOnSensorDisconnected;
            BleService.BLEReadyEvent -= BleServiceOnBLEReadyEvent;
            BleService.CharacteristicWriteResponseEvent -= BleServiceCharacteristicWriteResponseEvent;
            BleService.CharacteristicNotifyEvent -= BleServiceCharacteristicNotifyEvent;
            BleService.CharacteristicValueReadEvent -= BleServiceCharacteristicReadEvent;
        }


        protected async Task InitiateConnection()
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                //for android check Location permission
                var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (locationStatus == PermissionStatus.Denied || locationStatus == PermissionStatus.Unknown)
                    {
                  
                        AppInfo.ShowSettingsUI();
 
                    }
                }

                if (locationStatus == PermissionStatus.Granted)
                {
                    if (BleService != null)
                    {
                        BleService.Initialize();
                     
                    }
                }
            }
            else
            {
                Debug.WriteLine("Initialize");
                BleService.Initialize();

            }
        }

        #region BLECallbacks

        protected void BleServiceOnNotifyCallbackEvent(object sender, NotifyEnableEventArgs e)
        {

            //enable notify after intial serveric is set.
            if (e.CharacteristicUuId == Constants.CommandResultUuid)
            {
                //Debug.WriteLine("notify for  transfer service");
                Task.Delay(300);
                BleService.SetCharacteristicNotify(DeviceSensor, Constants.CommandTransferServiceUuid, Constants.WriteDoneUuid, true);

            }

        }

        protected void BleServiceOnSensorDisconnected(object sender, SensorDisconnectEventArgs e)
        {

           
        }

        protected void BleServiceOnNewSensorDetectedEvent(object sender, NewSensorDetectedEventArgs e)
        {
            Debug.WriteLine("New Sensor Detected");
            lock (DeviceSensorLock)
            {
                if (DeviceSensor != null) return;

                DeviceSensor = e.Sensor;
                Debug.WriteLine($"Connect to Device Name {DeviceSensor.Name}");
                BleService.ConnectBLEDevice(DeviceSensor);

                if (Device.RuntimePlatform == Device.Android)
                {
                    BleService.StopScanning();
                }


                //  Debug.WriteLine("BleServiceOnNewSensorDetectedEvent called");
            }
        }

        #endregion device_scan_logic

        #region BleNotifyCallBack
        protected void BleServiceOnNotifyErrorEvent(object sender, ErrorEventValueArgs e)
        {
            //  await DialogService.DisplayAlertAsync("Device Communication Error", e.ErrorMessage, "OK");
            Debug.WriteLine($"Device Communication Error {e.ErrorMessage}");
        }


        protected async void BleServiceOnServiceCharacteristicsEventCompleted(object sender, BleServiceDiscoveredEventArgs e)
        {
          //  Debug.WriteLine($"BleServiceDiscoveredEventArgs for Service {e.ServiceUuid} Max Serices {DeviceSensor.MaxServicesDiscovered}");
            Device.BeginInvokeOnMainThread(() =>
            {
                DeviceSensor?.OnBoardDiscoveredService(e.ServiceUuid);
    
                if (e.ServiceUuid == Constants.CommandServiceUuid)
                {
                    Debug.WriteLine("notify for command service");
                    Task.Delay(1000);
                    BleService.SetCharacteristicNotify(DeviceSensor, Constants.CommandServiceUuid, Constants.CommandResultUuid, true);
                }


                if (DeviceSensor.MaxServicesDiscovered)
                {
                    ConnectEnabled = false;
                    ButtonEnabled = true;
                    ProgressEnabled = false;

                    UpdateDescription("Device Connected");
                    BleService.StopScanning();
                }
            });
        }

        protected void BleServiceOnBLEReadyEvent(object sender, EventRequestStatusArgs e)
        {
            if (e.IsSuccess)
            {
                //DoDeviceScan(_deviceMacAddress);
                //DisconnectRequested = false;
                Debug.Write("BLE On");
                Debug.WriteLine("StartScanning");
                BleService.StartScanning();
            }
            else
            {
                Debug.Write("Error in BLE On");
                // DialogService.DisplayAlertAsync("Error Connecting", e.Message, "Ok");
            }
        }

        protected async void BleServiceCharacteristicWriteResponseEvent(object sender, EventValueArgs e)
        {

         //   Debug.WriteLine($"Previous command sent a Value of {ByteArrayToString(e.Value)}");

            if (e.CharacteristicUuId.ToLower() == Constants.CommandDataUuid.ToLower())
            {
                byte[] CommandValue = { GetCommandData() };
                await Task.Run(() =>
                {
                    BleService.BeginWriteCharacteristicValue(DeviceSensor, Constants.CommandServiceUuid, Constants.CommandIdUuid, CommandValue, BLECharacteristicWriteType.WithResponse);
                });
            }
            else if (e.CharacteristicUuId.ToLower() == Constants.CompressedUuid.ToLower())
            {
                var commandByte = new byte[] { 0x00 };

                //send command for a front card
                Debug.WriteLine("send command for a front card");
                await Task.Delay(CommandDelayInterval).ContinueWith(async (result) =>
                {
                    await BleCommandDispatcher(
                        Constants.CommandTransferServiceUuid, Constants.CardSideUuid,
                        value: commandByte, isWriteCommand: true,
                        writeType: BLECharacteristicWriteType.WithResponse);
                });
            }
            else if (e.CharacteristicUuId.ToLower() == Constants.CardSideUuid.ToLower())
            {
                //send command for a card location 
                Debug.WriteLine("send command for a card location ");

                Byte[] memoryIndexByte = { checked((byte)CurrentCardIndex) };
                //  Byte[] memoryIndexByte2 = { CurrentCardIndex };
                await Task.Delay(CommandDelayInterval).ContinueWith(async (result) =>
                {
                    await BleCommandDispatcher(
                        Constants.CommandTransferServiceUuid, Constants.MemoryIndexIdUuid,
                        value: memoryIndexByte, isWriteCommand: true,
                        writeType: BLECharacteristicWriteType.WithResponse);
                });
            }
            else if (e.CharacteristicUuId.ToLower() == Constants.MemoryIndexIdUuid.ToLower())
            {


                byte[] currentChunk = GetNextChunk();
                //send command for the first chunk
                Debug.WriteLine("send command for a first chunk ");
                await Task.Delay(CommandDelayInterval).ContinueWith(async (result) =>
                {
                    await BleCommandDispatcher(Constants.CommandTransferServiceUuid, Constants.CardDataUuid,
                        value: currentChunk, isWriteCommand: true,
                        writeType: BLECharacteristicWriteType.WithoutResponse);
                });
            }
        }
        protected async void BleServiceCharacteristicReadEvent(object sender, EventValueArgs e)
        {


            Debug.Write($"read value {e.Value}");

        }

        protected async void BleServiceCharacteristicNotifyEvent(object sender, EventValueArgs e)
        {

            //  Debug.WriteLine("BleServiceCharacteristicNotifyEvent");
            var readVal = e.Value?.FirstOrDefault();
            if (e.CharacteristicUuId.ToLower() == Constants.CommandResultUuid.ToLower())
            {
                //Checking for a notify event for a command result
                ActivityDescription = $"Command Complete with Value of {ByteArrayToString(e.Value)}";
                //Debug.WriteLine($"Command Complete with Value of {ByteArrayToString(e.Value)}");
                ButtonEnabled = true;
            }
            else if (e?.CharacteristicUuId?.ToLower() == Constants.WriteDoneUuid.ToLower())
            {
                //chekcing for a notify event for a write command for a card.
                if (readVal == 0x01)
                {

                    byte[] currentChunk = GetNextChunk();

                    if (_lastChunk == true)
                    {
                        if (_remainder > 0 && !_finalwrite)
                        {
                            UpdateProgressStatus(_currentIndex, _numTrips, "Writing Card Index");
                            Debug.WriteLine($"Final Trip {_currentIndex} of {_numTrips} LastChunk {_lastChunk} ChunkSize {currentChunk.Length}");
                            await BleCommandDispatcher(Constants.CommandTransferServiceUuid, Constants.CardDataUuid,
                                                      value: currentChunk, isWriteCommand: true,
                                                      writeType: BLECharacteristicWriteType.WithoutResponse);
                            _finalwrite = true;
                        }
                        else
                        {

                            Debug.Write("Done writing all Chunks");
                            UpdateProgressStatus(_numTrips, _numTrips, "Writing Card Complete");
                            Debug.WriteLine("Writing 0xBB to device\n\n\nCurrent Card Done ");
                            await BleCommandDispatcher(Constants.CommandTransferServiceUuid, Constants.WriteDoneUuid,
                                value: new byte[] { 0xBB }, isWriteCommand: true,
                                writeType: BLECharacteristicWriteType.WithResponse);
                        }

                    }
                    else
                    {
                        //send current chunk data.
                        UpdateProgressStatus(_currentIndex, _numTrips, "Writing Card Index");
                        Debug.WriteLine($"Trip {_currentIndex} of {_numTrips} LastChunk {_lastChunk} ChunkSize {currentChunk.Length}");
                        await BleCommandDispatcher(Constants.CommandTransferServiceUuid, Constants.CardDataUuid,
                      value: currentChunk, isWriteCommand: true,
                      writeType: BLECharacteristicWriteType.WithoutResponse);
                    }

                }
                else if (readVal == 0x00)
                {

                    ButtonEnabled = true;

                }
            }
        }
        protected async Task BleCommandDispatcher(string serviceUuid, string commandUuid, bool isWriteCommand = true,
          byte[] value = null, BLECharacteristicWriteType writeType = BLECharacteristicWriteType.WithoutResponse)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (isWriteCommand)
                        BleService.BeginWriteCharacteristicValue(DeviceSensor, serviceUuid, commandUuid, value, writeType);
                    else
                        BleService.BeginReadCharacteristicValue(DeviceSensor, serviceUuid, commandUuid);
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Exception occurred as follows: {ex.Message}\n\n{ex.StackTrace}");
                }
            });
        }


        #endregion BLECallbacks

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

        #region cardmethods

        private async Task DisplayCard()
        {
            try
            {
                ButtonEnabled = false;
                ProgressEnabled = false;
                DeviceCommandState = DeviceCommandRequestedState.DisplayCard;

                UpdateDescription($"Display Card {CurrentCardIndex}");
                Byte[] DataValue = { CurrentCardIndex };
    
                await BleCommandDispatcher(Constants.CommandServiceUuid, Constants.CommandDataUuid,
                          value: DataValue, isWriteCommand: true,
                          writeType: BLECharacteristicWriteType.WithResponse);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task DeleteCard()
        {
            try
            {
                ButtonEnabled = false;
                ProgressEnabled = false;
                DeviceCommandState = DeviceCommandRequestedState.RemoveCard;


                Debug.WriteLine("Delete Card");
                UpdateDescription($"Delete Card {CurrentCardIndex}");

                Byte[] DataValue = { CurrentCardIndex };
                await BleCommandDispatcher(Constants.CommandServiceUuid, Constants.CommandDataUuid,
                         value: DataValue, isWriteCommand: true,
                         writeType: BLECharacteristicWriteType.WithResponse);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task DeleteAllCards()
        {
            try
            {
                ButtonEnabled = false;
                ProgressEnabled = false;
                DeviceCommandState = DeviceCommandRequestedState.RemoveCards;


                Debug.WriteLine("Delete All Cards");
                UpdateDescription($"Delete All Cards");


                Byte[] DataValue = { (byte)0xAA };
                await BleCommandDispatcher(Constants.CommandServiceUuid, Constants.CommandDataUuid,
                         value: DataValue, isWriteCommand: true,
                         writeType: BLECharacteristicWriteType.WithResponse);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        #endregion cardmethods

        private byte GetCommandData()
        {
            switch (DeviceCommandState)
            {
                case DeviceCommandRequestedState.DisplayCard:
                    return Constants.CommandDisplayCard;
                case DeviceCommandRequestedState.RemoveCard:
                    return Constants.CommandRemoveCard;
                case DeviceCommandRequestedState.RemoveCards:
                    return Constants.CommandRemoveCard;     
                default:
                    return Constants.CommandDisplayCard;
            }
        }

        private async Task Loadcards()

        {

            //this example shows you how to load a bin file into a byte array, and copy it into a array, 
            //and using the setuptransfer() function to parse the bytes beased on the MTU for transfer

            var CardBins= typeof(MainPageViewModel).Assembly.GetManifestResourceNames();

            ImageSlots = new ObservableCollection<ImageSlot>();
  
            for (int i = 0; i < CardBins.Length; i++)
            {
               // var card = CardBins[i];
                ImageSlots.Add(new ImageSlot() {SlotName = CardBins[i].Replace("EinkStarter.EmbeddedResources.",""), SlotNameLong = CardBins[i] });
            }




         
        }

        public byte[] GetNextChunk()
        {
            if (_transferBytes == null)
                return new byte[] { };

            var i = _currentIndex * MtuSize;
            byte[] payload;
            if (_currentIndex == _numTrips)
            {
                payload = new byte[_remainder];
                _lastChunk = true;
                //  Debug.Write("Getting Last chunk");
            }
            else
            {
                payload = new byte[MtuSize];
            }

            for (var j = 0; j < payload.Length; j++)
            {
                if (i >= _transferBytes.Length) break;

                payload[j] = _transferBytes[i];
                i++;

            }

            _currentIndex++;

            //Console.WriteLine($"Requested chunk {_currentIndex} of {_numTrips}");

            return payload;
        }
        public void setuptransfer()
        {
            _lastChunk = false;
            _finalwrite = false;
            _currentIndex = 0;
            if (_transferBytes != null && MtuSize > 0)
            {
                _numTrips = _transferBytes.Length / MtuSize;
                _remainder = _transferBytes.Length % MtuSize;
            }

        }

    }
}
