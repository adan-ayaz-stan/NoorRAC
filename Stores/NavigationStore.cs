using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel; // Or your custom ViewModelBase

namespace NoorRAC.Stores
{
    public class NavigationStore : ObservableObject // Inherit from ObservableObject
    {
        private ObservableObject? _currentViewModel; // Or ViewModelBase
        public ObservableObject? CurrentViewModel // Or ViewModelBase
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value); // Provided by ObservableObject
        }
    }
}