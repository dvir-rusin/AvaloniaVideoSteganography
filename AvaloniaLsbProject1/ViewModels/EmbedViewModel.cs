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

        [ObservableProperty]
        private string? messageText;

        [ObservableProperty]
        private string? encryptionPassword;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? videoNameAndFormat;

        public EmbedViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            EmbeddMessageCommand = new AsyncRelayCommand(EmbeddMessageAsync);
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }

        public IAsyncRelayCommand EmbeddMessageCommand { get; }

        private async Task SelectVideoAsync()
        {
            try
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
                else
                {
                    ErrorMessage = "No video file was selected.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error selecting video: {ex.Message}";
            }
        }

        private async Task EmbeddMessageAsync()
        {
            if (string.IsNullOrEmpty(MessageText) || string.IsNullOrEmpty(EncryptionPassword))
            {
                ErrorMessage = "Message text or password is missing.";
                return;
            }

            if (string.IsNullOrEmpty(SelectedVideoPath))
            {
                ErrorMessage = "No video file selected. Please select a video file before embedding a message.";
                return;
            }

            string messageText = MessageText;
            string password = EncryptionPassword;
            // Base project directory
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            string allFramesFolder = Path.Combine(projectPath, "AllFrames");
            string allFramesWithMessageFolder = Path.Combine(projectPath, "allFramesWithMessage");
            string metaDataFile = Path.Combine(projectPath, "MetaData.csv");
            string NewVideo = Path.Combine(projectPath, videoNameAndFormat);

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
                    Console.WriteLine("SelectedVideoPath is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //check if directory exists and is not empty
            if (Directory.Exists(allFramesFolder))
            {
                try
                {
                    //await Services.Extraction.ExtractFrameMetadata(selectedVideoPath, metaDataFile);
                    int[] iframesLocation = await Services.Extraction.GetIFrameLocations(metaDataFile);

                    if (!Directory.Exists(allFramesWithMessageFolder))
                    {
                        Directory.CreateDirectory(allFramesWithMessageFolder);
                        //ErrorMessage = Services.Embedding.EmbedMessageInFramesTestInVideo(allFramesFolder, allFramesWithMessageFolder, iframesLocation,messageText,password);
                    }


                }
                catch (Exception ex)
                {
                   ErrorMessage =  (ex.Message);
                }
            }

            if(Directory.Exists(allFramesWithMessageFolder))
            {
                try
                {
                    Services.HelperFunctions.ReconstructVideo(allFramesWithMessageFolder, NewVideo, 30);
                    //ErrorMessage = Services.Extraction.ExtractMessageFromIFrames(allFramesWithMessageFolder,"123");
                }
                catch(Exception ex)
                {
                    ErrorMessage = ex.Message; 
                }
            }
            else
            {
                ErrorMessage = "allFramesWithMessageFolder does not exist";
            }
            
        }
    }
}
