using System;
using System.Collections.Generic;
using System.Text;
using EinkStarter.Enums;
using EinkStarter.Models.Device;
using EinkStarter.Utilities.EventHandlers;

namespace EinkStarter.Interfaces
{
    //https://www.codemag.com/article/1707071/Accessing-Platform-Specific-Functionalities-Using-DependencyService-in-Xamarin.Forms
    public interface IMyInterface
        {
            string GetPlatformName();
        }
   

}
