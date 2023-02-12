using System;
using System.Collections.Generic;
using System.Text;
using Prism.Commands;
using Prism.Navigation;
using PropertyChanged;

namespace EinkStarter.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ViewModelBase : IInitialize, INavigationAware, IDestructible
    {
        protected INavigationService NavigationService { get; private set; }
        public DelegateCommand GoBackCommand { get; private set; }
        public string Title { get; set; }

        public ViewModelBase(INavigationService navigationService)
        {
            NavigationService = navigationService;
            GoBackCommand = new DelegateCommand(async () => await NavigationService.GoBackAsync());
        }

        public virtual void Initialize(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {
        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {
        }

        public void Destroy()
        {
        }
    }
}
