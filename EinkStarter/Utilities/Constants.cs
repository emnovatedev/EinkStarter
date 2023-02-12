using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace EinkStarter.Utilities
{
    public static class Constants
    {
        
        #region  ServiceUuids

        public const string CommandServiceUuid = "1a3a1400-5ff5-1987-7143-4e97e8d45b49";
        public const string InformationServiceUuid = "24461600-080f-4104-ab3a-4749341255b2";
        public const string CommandTransferServiceUuid = "cbd11500-bb16-416d-a5d9-f39ef128750e";

    

        #endregion

    

        #region Display_Command

        public const string CommandIdUuid = "1a3a1401-5ff5-1987-7143-4e97e8d45b49";
        public const string CommandDataUuid = "1a3a1402-5ff5-1987-7143-4e97e8d45b49";
        public const string CommandResultUuid = "1a3a1403-5ff5-1987-7143-4e97e8d45b49";
   
        public const byte CommandDisplayCard = 0x00;
        public const byte CommandWipeDeviceClean = 0x03;
        public const byte CommandRemoveCard = 0x06;
        public const byte CommandRemoveAllCards = 0xAA;

        #endregion

        #region Command_Result_Status

        public const byte CommandResultSuccess = 0x01;
        public const byte CommandResultCardIndexOor = 0xF1;
        public const byte CommandResultCardSlotEmpty = 0xF2;
        public const byte WriteDone = 0xBB;
        #endregion Command_Result_Status

        #region TransferCommand

        public const string MemoryIndexIdUuid = "cbd11501-bb16-416d-a5d9-f39ef128750e";
        public const string CardDataUuid = "cbd11503-bb16-416d-a5d9-f39ef128750e";
        public const string WriteDoneUuid = "cbd11504-bb16-416d-a5d9-f39ef128750e";
        public const string CompressedUuid = "cbd11502-bb16-416d-a5d9-f39ef128750e";
        public const string CardSideUuid = "cbd11505-bb16-416d-a5d9-f39ef128750e";
  

        #endregion TransferCommand

        #region InformationCommand
        public const string BatteryLevelUuid = "24461601-080f-4104-ab3a-4749341255b2";
        public const string DeviceInformationUuid = "24461604-080f-4104-ab3a-4749341255b2";
        #endregion InformationCommand


        public const string BleCharNotifyEventMsg = "BleCharNotifyEventMsg";
        //BleServiceOnCharacteristicWriteResponseEvent
        public const string BleCharWriteResponseEventMsg = "BleCharWriteResponseEventMsg";
        //BleServiceOnNotifyCallbackEvent
        public const string BleCharNotifyEnabledMsg = "BleCharNotifyEnabledMsg";
        //BleServiceOnCharacteristicValueReadEvent
        public const string BleCharReadEventMsg = "BleCharReadEventMsg";
        //BleServiceOnSensorDisconnected
        public const string BleSensorDisconnectedEventMsg = "BleSensorDisconnectedEventMsg";

        public const string SensorName = "e-ink";
        public const string SensorName1 = "digme";

    }
}
