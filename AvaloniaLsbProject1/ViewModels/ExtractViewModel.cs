using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.IO;
using AvaloniaLsbProject1.Services;
using Avalonia.Media;
using System.Text.RegularExpressions;

namespace AvaloniaLsbProject1.ViewModels
{
    public partial class ExtractViewModel : ObservableObject
    {

        [ObservableProperty]
        private string? selectedVideoPath;

        [ObservableProperty]
        private string? decryptionPassword;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        public bool isProcessing;

        [ObservableProperty]
        public string processingStatusText;

        [ObservableProperty]
        private string extractButtonText;

        /// <summary>
        /// color for error / sucess message
        /// </summary>
        [ObservableProperty] public IBrush? errorBoardercolor;

        /// <summary>
        /// color for error / sucess message
        /// </summary>
        [ObservableProperty] public IBrush? errorcolor;

        [ObservableProperty] public IBrush? sucssesOrErorrTextColor;

        [ObservableProperty] public string? sucssesOrErorr;
        public ExtractViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            ExtractMessageCommand = new AsyncRelayCommand(ExtractMessageAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            extractButtonText = "Extract Message";

            // Initialize these properties with default values
            errorBoardercolor = new SolidColorBrush(Color.Parse("#CCCCCC"));
            errorcolor = new SolidColorBrush(Color.Parse("#000000"));
            sucssesOrErorr = "None";       // Default status
            sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));        // Black text

        }

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand ExtractMessageCommand { get; }

        public IAsyncRelayCommand PlayVideoCommand { get; }

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
        
        
        private async Task ExtractMessageAsync()
        {
            if (!ValidateInitialConditions())
            {
                return;
            }
           ErrorMessage = string.Empty;
            ProcessingStatusText = "Extracting Message";
            ExtractButtonText = "Extracting Message";
            IsProcessing = true;
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            string allFramesFolder = Path.Combine(projectPath, "AllFrames");
            string allFramesWithMessageFolder = Path.Combine(projectPath, "allFramesWithMessage");
            string metaDataFile = Path.Combine(projectPath, "MetaData.csv");
            string NewVideoIframes = Path.Combine(projectPath, "NewVideoIframes");
            
                
            try
            {
                //creates i frames folder 
                if (!Directory.Exists(NewVideoIframes))
                {
                     Directory.CreateDirectory(NewVideoIframes);
                }

                //inserts new video i frames into folder 
                await Services.Extraction.ExtractIFrames(selectedVideoPath, NewVideoIframes);

                
                if (decryptionPassword != null)
                {
                    // Create a local variable for sucssesOrErorr
                    string localSuccessOrError = "None";

                    // Make a copy of the brush references for modification
                    IBrush localBorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
                    IBrush localColorBrush = new SolidColorBrush(Color.Parse("#000000"));
                    IBrush localSucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));

                    // Call with local variables
                    ErrorMessage = Services.Extraction.ExtractMessageFromIFrames(
                        NewVideoIframes,
                        decryptionPassword,
                        ref localBorderBrush,
                        ref localColorBrush,
                        ref localSuccessOrError,
                        ref localSucssesOrErorrTextColor);

                    // Update the ViewModel properties with the modified values
                    ErrorBoardercolor = localBorderBrush;
                    Errorcolor = localColorBrush;
                    SucssesOrErorr = localSuccessOrError;
                    SucssesOrErorrTextColor = localSucssesOrErorrTextColor;
                    Directory.Delete(NewVideoIframes, true);
                }
            }
            catch (Exception ex)
            {

                ErrorMessage = ex.Message;
                ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));        // Dark red text
                SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));        // Dark red text
                SucssesOrErorr = "Error";
            }
            finally
            {
                IsProcessing = false;
                ExtractButtonText = "Extract Message";
            }
                
            
            
        }
        private async Task PlayVideoAsync()
        {
            if (selectedVideoPath != null)
            {
                HelperFunctions.PlayVideo(selectedVideoPath);
            }
            else
            {
                ErrorMessage = "CANT PLAY VIDEO selectedVideoPath is null";
                ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));        // Dark red text
                SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));        // Dark red text
                SucssesOrErorr = "Error";
            }
        }

        private bool ValidateInitialConditions()
        {
            if (IsProcessing)
            {
                return false;
            }


            if (string.IsNullOrEmpty(SelectedVideoPath) || string.IsNullOrEmpty(DecryptionPassword))
            {
                ErrorMessage = "Video path or password is missing.";
                ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));        // Dark red text
                SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));        // Dark red text
                SucssesOrErorr = "Error";
                return false;
            }

            if (string.IsNullOrEmpty(SelectedVideoPath))
            {
                ErrorMessage = "No video file selected. Please select a video file before embedding a message.";
                ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));        // Dark red text
                SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));        // Dark red text
                SucssesOrErorr = "Error";
                return false;
            }

           
            ErrorMessage = string.Empty;
            return true;
        }

    }
}
