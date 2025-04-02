using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using AvaloniaLsbProject1.Services;
using Avalonia.Media;
using System.Text.RegularExpressions;

namespace AvaloniaLsbProject1.ViewModels
{
    /// <summary>
    /// ViewModel for extracting messages from video files.
    /// This class handles video selection, message extraction, video playback, and error reporting.
    /// </summary>
    public partial class ExtractViewModel : ObservableObject
    {
        #region Observable Properties

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
        /// Gets or sets the border brush used for error/success messages.
        /// </summary>
        [ObservableProperty] public IBrush? errorBoardercolor;

        /// <summary>
        /// Gets or sets the text color used for error/success messages.
        /// </summary>
        [ObservableProperty] public IBrush? errorcolor;

        [ObservableProperty] public IBrush? sucssesOrErorrTextColor;

        [ObservableProperty] public string? sucssesOrErorr;

        #endregion

        #region Commands

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand ExtractMessageCommand { get; }
        public IAsyncRelayCommand PlayVideoCommand { get; }

        #endregion

        #region Constructor

        public byte[]? SharedKey { get; }
        public string? Role { get; }

        public Window? ParentWindow { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractViewModel"/> class.
        /// Sets up commands and initializes default values.
        /// </summary>
        /// <param name="sharedKey">The shared key used for decryption.</param>
        /// <param name="role">The role of the current user (typically "Listener").</param>
        public ExtractViewModel(byte[]? sharedKey, string role)
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            ExtractMessageCommand = new AsyncRelayCommand(ExtractMessageAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);

            extractButtonText = "Extract Message";

            // Initialize default colors and status.
            errorBoardercolor = new SolidColorBrush(Color.Parse("#CCCCCC"));
            errorcolor = new SolidColorBrush(Color.Parse("#000000"));
            sucssesOrErorr = "None";
            sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));

            SharedKey = sharedKey;
            Role = role;

            if (Role.Equals("Listener"))
            {
                decryptionPassword = Convert.ToBase64String(SharedKey!);
            }
        }

        #endregion

        #region Configuration Helper

        /// <summary>
        /// Loads the project paths configuration from the JSON file.
        /// </summary>
        /// <returns>A <see cref="ProjectPathsConfig"/> instance containing configuration data.</returns>
        private ProjectPathsConfig LoadProjectConfig()
        {
            // Adjust the JSON file path as necessary.
            return ProjectPathsLoader.LoadConfig("C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\Json\\projectPaths.json");
        }

        #endregion

        #region Video Selection Methods

        /// <summary>
        /// Opens a file dialog for selecting a video.
        /// </summary>
        private async Task SelectVideoAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Video File",
                Filters =
                {
                    new FileDialogFilter { Name = "Video Files", Extensions = { "mp4", "avi", "mkv", "mov", "wmv" } }
                },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(ParentWindow);

            if (result?.Length > 0)
            {
                SelectedVideoPath = result[0];
            }
        }

        #endregion

        #region Message Extraction Methods

        /// <summary>
        /// Extracts the message from the selected video.
        /// Uses project paths from the JSON configuration instead of hardcoded paths.
        /// </summary>
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

            // Load configuration for project paths.
            var config = LoadProjectConfig();
            string projectPath = config.BaseProjectPath;
            string allFramesFolder = Path.Combine(projectPath, config.Paths.AllFramesFolder);
            string allFramesWithMessageFolder = Path.Combine(projectPath, config.Paths.AllFramesWithMessageFolder);
            string metaDataFile = Path.Combine(projectPath, config.Paths.MetaDataFile);
            string newVideoIframes = Path.Combine(projectPath, config.Paths.NewVideoIframes);

            try
            {
                // Ensure the folder for new video I-frames exists.
                if (!Directory.Exists(newVideoIframes))
                {
                    Directory.CreateDirectory(newVideoIframes);
                }

                // Extract I-frames from the selected video into the designated folder.
                await Services.Extraction.ExtractIFrames(SelectedVideoPath, newVideoIframes);

                if (decryptionPassword != null)
                {
                    string DoubleframeRate = "29.6";
                    int FrameRate = (int)double.Parse(DoubleframeRate);
                    // Prepare local variables for status updates.
                    string localSuccessOrError = "None";
                    IBrush localBorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
                    IBrush localColorBrush = new SolidColorBrush(Color.Parse("#000000"));
                    IBrush localSucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));

                    // Extract the hidden message from the frames.
                    ErrorMessage = Services.Extraction.ExtractMessageFromIFrames(
                        newVideoIframes,
                        decryptionPassword,
                        ref localBorderBrush,
                        ref localColorBrush,
                        ref localSuccessOrError,
                        ref localSucssesOrErorrTextColor);

                    // Update view model properties with the result.
                    ErrorBoardercolor = localBorderBrush;
                    Errorcolor = localColorBrush;
                    sucssesOrErorr = localSuccessOrError;
                    sucssesOrErorrTextColor = localSucssesOrErorrTextColor;

                    // Clean up by deleting the I-frames directory.
                    Directory.Delete(newVideoIframes, true);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                errorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));
                sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));
                sucssesOrErorr = "Error";
            }
            finally
            {
                IsProcessing = false;
                ExtractButtonText = "Extract Message";
            }
        }

        #endregion

        #region Video Playback Methods

        /// <summary>
        /// Plays the selected video.
        /// </summary>
        private async Task PlayVideoAsync()
        {
            if (SelectedVideoPath != null)
            {
                HelperFunctions.PlayVideo(SelectedVideoPath);
            }
            else
            {
                ErrorMessage = "Cannot play video; SelectedVideoPath is null.";
                errorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));
                sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));
                sucssesOrErorr = "Error";
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates initial conditions required for message extraction.
        /// </summary>
        /// <returns>True if conditions are met; otherwise, false.</returns>
        private bool ValidateInitialConditions()
        {
            if (IsProcessing)
            {
                return false;
            }

            if (string.IsNullOrEmpty(SelectedVideoPath) || string.IsNullOrEmpty(decryptionPassword))
            {
                ErrorMessage = "Video path or password is missing.";
                errorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));
                sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));
                sucssesOrErorr = "Error";
                return false;
            }

            if (string.IsNullOrEmpty(SelectedVideoPath))
            {
                ErrorMessage = "No video file selected. Please select a video file before extracting a message.";
                errorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444"));
                errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e"));
                sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444"));
                sucssesOrErorr = "Error";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        #endregion
    }
}
