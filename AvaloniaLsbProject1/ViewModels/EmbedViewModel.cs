using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.Runtime.InteropServices;
using System.IO;

namespace AvaloniaLsbProject1.ViewModels
{
    public partial class EmbedViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? selectedVideoPath;
        public EmbedViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            EmbeddMessageCommand = new AsyncRelayCommand(EmbeddMessageAsync);
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }

        public IAsyncRelayCommand EmbeddMessageCommand { get; }

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

        private async Task EmbeddMessageAsync()
        {
            // Base project directory
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            string allFramesFolder = Path.Combine(projectPath, "AllFrames");

            try
            {
                // Create the base project directory if it doesn't exist
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }

                // Create the AllFrames folder
                if (!Directory.Exists(allFramesFolder))
                {
                    Directory.CreateDirectory(allFramesFolder);
                }

                // Ensure a video file is selected
                if (!string.IsNullOrEmpty(SelectedVideoPath))
                {
                    // Extract all frames to the AllFrames folder
                    await Task.Run(() =>
                    {
                        Services.Extraction.ExtractAllFrames(SelectedVideoPath, allFramesFolder);
                    });

                    // Notify the user of success (optional)
                   
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
