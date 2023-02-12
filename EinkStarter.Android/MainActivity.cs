using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Prism;
using Prism.Ioc;
using EinkStarter.Interfaces;
using EinkStarter.Droid.DependencyServices.Ble;
using Xamarin.Forms;
using static Xamarin.Essentials.Permissions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Android.Content.Res;
using Android.Widget;
using System.IO;
using Java.IO;
using Android.Graphics;


namespace EinkStarter.Droid
{
    [Activity(Label = "EinkStarter", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);


            float OS = float.Parse(Xamarin.Essentials.DeviceInfo.Version.ToString());

            if (OS >= 12)
            { //only do for Andrio 12
                while (true)
                {
                    //hack to hold unitl permissions are granted before loading the app.
                    if (await Permissions.CheckStatusAsync<BluetoothConnectPermission>() == PermissionStatus.Granted)
                        break;

                    Permissions.RequestAsync<BluetoothConnectPermission>();

                    await Task.Delay(1000);

                }
            }
            else
            {
                Permissions.RequestAsync<BluetoothConnectPermission>();
                await Task.Delay(1000);
            }
            LoadApplication(new App(new AndroidInitializer()));

            DependencyService.Register<IBleService, BleService>();

            const string lfn = "AboutAssets.txt";
            string settings = string.Empty;

            // context could be ApplicationContext, Activity or
            // any other object of type Context
            //using (var input = Assets.Open(lfn))
            //using (StreamReader sr = new System.IO.StreamReader(input))
            //{
            //    settings = sr.ReadToEnd();
            //}


        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        internal class BluetoothConnectPermission : BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new List<(string androidPermission, bool isRuntime)>
              {
                  (Android.Manifest.Permission.BluetoothScan, true),
                  (Android.Manifest.Permission.BluetoothConnect, true),
                  (Android.Manifest.Permission.BluetoothAdmin, true),
              //    (Android.Manifest.Permission.AccessCoarseLocation,true),
                   (Android.Manifest.Permission.AccessFineLocation,true),
              //    (Android.Manifest.Permission.BluetoothPrivileged, true),
                 // (Android.Manifest.Permission.Bluetooth, true),
                  (Android.Manifest.Permission.AccessNetworkState, true)
              }.ToArray();
        }

    }

    public class AndroidInitializer : IPlatformInitializer
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register any platform specific implementations
            //containerRegistry.RegisterSingleton<IBleService, BleService>();
        }
    }

}