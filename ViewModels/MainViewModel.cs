using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NoorRAC.Stores;

namespace NoorRAC.ViewModels
{
    public class MainViewModel : ObservableObject // Or ViewModelBase
    {
        private readonly NavigationStore _navigationStore;

        public ObservableObject? CurrentViewModel => _navigationStore.CurrentViewModel; // Or ViewModelBase

        public MainViewModel(NavigationStore navigationStore)
        {
            _navigationStore = navigationStore;
            _navigationStore.PropertyChanged += OnCurrentViewModelChanged;
        }

        private void OnCurrentViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NavigationStore.CurrentViewModel))
            {
                OnPropertyChanged(nameof(CurrentViewModel)); // Notify the view
            }
        }

        // Optional: Implement IDisposable if you need to unsubscribe from events
        // public void Dispose() { _navigationStore.PropertyChanged -= OnCurrentViewModelChanged; }
    }
}