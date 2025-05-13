using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel; // Or your custom ViewModelBase

namespace NoorRAC.Services
{
    // Represents a service that navigates to a ViewModel of type TViewModel
    public interface INavigationService<TViewModel> where TViewModel : ObservableObject // Or ViewModelBase
    {
        void Navigate();
    }


}