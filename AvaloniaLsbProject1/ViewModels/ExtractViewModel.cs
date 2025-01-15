using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;

namespace AvaloniaLsbProject1.ViewModels
{
    public partial class ExtractViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? selectedVideoPath;
        public ExtractViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }

        private async Task SelectVideoAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Video File",
                Filters =
                {
                    new FileDialogFilter { Name = "Video Files", Extensions = { "mp4", "avi", "mkv", "mov", "wmv" } },
                    //new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(MainWindow.Instance); // Ensure MainWindow.Instance is available in your app.
            if (result?.Length > 0)
            {
                SelectedVideoPath = result[0];
            }

        }
    }
}
