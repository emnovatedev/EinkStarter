using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using EinkStarter.Droid.DependencyServices.Ble;
using EinkStarter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformDetails))]
namespace EinkStarter.Droid.DependencyServices.Ble
{
    public class PlatformDetails : IMyInterface
    {
        public PlatformDetails()
        {
        }
        public string GetPlatformName()

        {
            return "I am Android!";
        }
    }
}