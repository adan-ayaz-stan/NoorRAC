using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoorRAC.Stores;
using NoorRAC.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoorRAC.Services
{
    // Implements navigation specifically TO the LoginViewModel
    public class NavigationService<TViewModel> : INavigationService<TViewModel>
        where TViewModel : ObservableObject // Or ViewModelBase
    {
        private readonly NavigationStore _navigationStore;
        private readonly Func<TViewModel> _createViewModel; // Factory function

        public NavigationService(NavigationStore navigationStore, Func<TViewModel> createViewModel)
        {
            _navigationStore = navigationStore;
            _createViewModel = createViewModel;
        }

        public void Navigate()
        {
            _navigationStore.CurrentViewModel = _createViewModel();
        }
    }
}