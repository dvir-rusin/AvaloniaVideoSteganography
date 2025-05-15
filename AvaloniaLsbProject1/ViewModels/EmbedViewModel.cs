﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.IO;
using AvaloniaLsbProject1.Services;
using Xabe.FFmpeg;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using System.Text;
using Avalonia.Media.Imaging;
using Avalonia;
using System.Text.RegularExpressions;
using Tmds.DBus.Protocol;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections;


namespace AvaloniaLsbProject1.ViewModels
{
    /// <summary>
    /// ViewModel for embedding messages into video files.
    /// This class handles video selection, metadata extraction, message embedding, video playback, thumbnail generation,
    /// and capacity calculations.
    /// </summary>
    public partial class EmbedViewModel : ObservableObject
    {
        
        // Properties

        #region Video Input Properties
        /// <summary>
        /// Gets or sets the path of the selected video.
        /// </summary>
        [ObservableProperty] private string? selectedVideoPath;

        /// <summary>
        /// Gets or sets the message text to embed in the video.
        /// </summary>
        [ObservableProperty] private string? messageText;

        /// <summary>
        /// Gets or sets the encryption password used during message embedding.
        /// </summary>
        [ObservableProperty] private string? encryptionPassword;

        /// <summary>
        /// Gets or sets the generated thumbnail image from the video.
        /// </summary>
        [ObservableProperty] private Bitmap thumbnailImage;
        #endregion

        #region Video Metadata Properties
        /// <summary>
        /// Gets or sets the height of the video.
        /// </summary>
        [ObservableProperty] private string? height;

        /// <summary>
        /// Gets or sets the width of the video.
        /// </summary>
        [ObservableProperty] private string? width;

        /// <summary>
        /// Gets or sets the frame rate of the video.
        /// </summary>
        [ObservableProperty] private string? frameRate;

        /// <summary>
        /// Gets or sets the bit rate of the video.
        /// </summary>
        [ObservableProperty] private string? bitRate;

        /// <summary>
        /// Gets or sets the duration of the video.
        /// </summary>
        [ObservableProperty] private string? duration;
        #endregion

        #region Capacity and Processing Properties
        /// <summary>
        /// Gets or sets the estimated capacity available for embedding the message.
        /// </summary>
        [ObservableProperty] private string estimatedCapacity;

        /// <summary>
        /// Gets or sets the percentage of capacity used.
        /// </summary>
        [ObservableProperty] private double capacityUsagePercentage;

        /// <summary>
        /// Gets or sets the text representation of the capacity usage.
        /// </summary>
        [ObservableProperty] private string capacityUsageText;

        /// <summary>
        /// Gets or sets the brush used to indicate capacity usage (e.g., red for high usage).
        /// </summary>
        [ObservableProperty] private IBrush capacityColor;

        /// <summary>
        /// Gets or sets information about the length of the message text.
        /// </summary>
        [ObservableProperty] private string messageLengthInfo;

        /// <summary>
        /// Gets or sets a value indicating whether the video is currently being processed.
        /// </summary>
        [ObservableProperty] public bool isProcessing;

        /// <summary>
        /// Gets or sets the processing status text.
        /// </summary>
        [ObservableProperty] public string processingStatusText;

        /// <summary>
        /// Gets or sets the text displayed on the embed button.
        /// </summary>
        [ObservableProperty] private string embedButtonText;

        /// <summary>
        /// Gets or sets any error message produced during processing.
        /// </summary>
        [ObservableProperty] private string? errorMessage;

        /// <summary>
        /// Gets or sets the video name and format.
        /// </summary>
        [ObservableProperty] private string? videoNameAndFormat;

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
        /// <summary>
        /// Gets the command for selecting a video.
        /// </summary>
        public IAsyncRelayCommand SelectVideoCommand { get; }

        /// <summary>
        /// Gets the command for embedding a message in the video.
        /// </summary>
        public IAsyncRelayCommand EmbeddMessageCommand { get; }

        /// <summary>
        /// Gets the command for playing the video.
        /// </summary>
        public IAsyncRelayCommand PlayVideoCommand { get; }

        /// <summary>
        /// Gets the command for previewing the video.
        /// </summary>
        public IAsyncRelayCommand PreviewVideoCommand { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Gets the shared key used in the key exchange process.
        /// </summary>
        public byte[]? SharedKey { get; }

        /// <summary>
        /// Gets the role of the current instance.
        /// </summary>
        public string? Role { get; }

        public Window? ParentWindow { get; set; }

        public double seconds {  get; set; }    

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbedViewModel"/> class.
        /// Sets up commands and initializes default values.
        /// </summary>
        /// <param name="sharedKey">The shared key used for encryption.</param>
        /// <param name="role">The role of the current user (e.g., "Broadcaster" or "Listener").</param>
        public EmbedViewModel(byte[]? sharedKey,string role)
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            EmbeddMessageCommand = new AsyncRelayCommand(EmbeddMessageAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            PreviewVideoCommand = new AsyncRelayCommand(PreviewVideoAsync);
            videoNameAndFormat = "NewVideo2april.mp4";
            EmbedButtonText = "Embed Message";
            // Initialize default colors and status.
            errorBoardercolor = new SolidColorBrush(Color.Parse("#CCCCCC"));
            errorcolor = new SolidColorBrush(Color.Parse("#000000"));
            sucssesOrErorr = "None";
            sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));
            SharedKey = sharedKey;
            Role = role;
            
            if (Role.Equals("Broadcaster"))
            {
                encryptionPassword = Convert.ToBase64String(SharedKey!);
            }
        }
        #endregion


        #region Message Status Helper Methods

        /// <summary>
        /// Displays an error message with red styling.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void DisplayErrorMessage(string message)
        {
            ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444")); // Red border
            Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e")); // Dark red text
            SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444")); // Dark red text
            SucssesOrErorr = "Error";
            ErrorMessage = message;
        }

        /// <summary>
        /// Displays a success message with green styling.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        private void DisplaySuccessMessage(string message)
        {
            ErrorBoardercolor = new SolidColorBrush(Color.Parse("#44FF44")); // Green border
            Errorcolor = new SolidColorBrush(Color.Parse("#1e2a1e")); // Dark green text
            SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#44FF44")); // Dark green text
            SucssesOrErorr = "Success";
            ErrorMessage = message;
        }

        /// <summary>
        /// Resets the message display to default state.
        /// </summary>
        private void ResetMessageDisplay()
        {
            ErrorBoardercolor = new SolidColorBrush(Color.Parse("#CCCCCC"));
            Errorcolor = new SolidColorBrush(Color.Parse("#000000"));
            SucssesOrErorr = "None";
            SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));
            ErrorMessage = null;
        }

        #endregion
        #region Partial Methods
        /// <summary>
        /// Partial method triggered when the message text changes.
        /// Updates the capacity usage based on the new message text.
        /// </summary>
        /// <param name="oldValue">The previous message text.</param>
        /// <param name="newValue">The new message text.</param>
        partial void OnMessageTextChanged(string? oldValue, string? newValue)
        {
            UpdateCapacityUsage();
        }
        #endregion

        #region Configuration Helper
        /// <summary>
        /// Loads the project paths configuration from the JSON file.
        /// </summary>
        /// <returns>A <see cref="ProjectPathsConfig"/> instance with the configuration data.</returns>
        private ProjectPathsConfig LoadProjectConfig()
        {
            // Load the config file from the Json folder relative to the executable directory
            string configPath = Path.Combine(AppContext.BaseDirectory, "Json", "projectPaths.json");
            return ProjectPathsLoader.LoadConfig(configPath);
        }

        #endregion

        #region Video Selection and Metadata Methods

        /// <summary>
        /// Opens a file dialog to select a video and processes the selected video.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SelectVideoAsync()
        {
            // Load configuration to retrieve the base project path and metadata file location.
            var config = LoadProjectConfig();
            string projectPath = config.BaseProjectPath;
            string metaDataFile = Path.Combine(projectPath, config.Paths.MetaDataFile);

            try
            {
                var dialog = CreateVideoFileDialog();
                var result = await dialog.ShowAsync(ParentWindow);


                if (result?.Length > 0)
                {
                    await ProcessSelectedVideo(result[0], metaDataFile);
                    ResetMessageDisplay(); // Reset any previous error/success messages
                }
                else
                {
                    DisplayErrorMessage("No video file was selected.");
                }
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error selecting video: {ex.Message}") ;
            }
        }

        /// <summary>
        /// Creates and configures an OpenFileDialog for selecting video files.
        /// </summary>
        /// <returns>An instance of <see cref="OpenFileDialog"/> configured for video selection.</returns>
        private OpenFileDialog CreateVideoFileDialog()
        {
            return new OpenFileDialog
            {
                Title = "Select a Video File",
                Filters = { new FileDialogFilter { Name = "Video Files", Extensions = { "mp4", "avi", "mkv", "mov", "wmv" } } },
                AllowMultiple = false
            };
        }

        /// <summary>
        /// Processes the selected video by loading its attributes and extracting metadata if necessary.
        /// </summary>
        /// <param name="videoPath">The file path of the selected video.</param>
        /// <param name="metaDataFile">The file path for saving metadata.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessSelectedVideo(string videoPath, string metaDataFile)
        {
            SelectedVideoPath = videoPath;
            await LoadVideoAttributesAsync(SelectedVideoPath);

            if (!File.Exists(metaDataFile))
            {
                using (File.Create(metaDataFile)) { }
                Extraction.ExtractFrameMetadata(SelectedVideoPath, metaDataFile);
            }
        }

        /// <summary>
        /// Loads video attributes such as metadata and generates a thumbnail.
        /// </summary>
        /// <param name="videoPath">The file path of the video.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LoadVideoAttributesAsync(string videoPath)
        {
            try
            {
                SetProcessingState(true, "Analyzing video...");
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                if (videoStream != null)
                {
                    PopulateVideoAttributes(videoStream, mediaInfo);
                    await GenerateThumbnailAsync(videoPath);
                }
                else
                {
                    DisplayErrorMessage("No video stream found.");
                }
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error extracting video attributes: {ex.Message}");
            }
            finally
            {
                SetProcessingState(false);
            }
        }

        /// <summary>
        /// Populates the video metadata properties using the provided video stream and media information.
        /// </summary>
        /// <param name="videoStream">The video stream containing video data.</param>
        /// <param name="mediaInfo">The media information for the video.</param>
        private void PopulateVideoAttributes(IVideoStream videoStream, IMediaInfo mediaInfo)
        {
            Height = $"{videoStream.Height}";
            Width = $"{videoStream.Width}";
            FrameRate = $"{videoStream.Framerate} fps";
            BitRate = $"{videoStream.Bitrate / 1000} kbps";
            Duration = mediaInfo.Duration.ToString(@"hh\:mm\:ss\.fff");
            seconds = mediaInfo.Duration.TotalSeconds;
            // -146 - ( 1 null byte, 2 byte( end for message indication (12 bits)), 16 bytes IV, 127 padding (worst case sanerio))
            long estimatedBytes = (((videoStream.Width * videoStream.Height) - 146) * 3 / 8);
            EstimatedCapacity = FormatByteSize(estimatedBytes);
            UpdateCapacityUsage();

        }

        /// <summary>
        /// Sets the processing state and optionally updates the processing status text.
        /// </summary>
        /// <param name="isProcessing">True if processing is ongoing; otherwise, false.</param>
        /// <param name="statusText">Optional status text to display.</param>
        private void SetProcessingState(bool isProcessing, string statusText = null)
        {
            IsProcessing = isProcessing;
            ProcessingStatusText = statusText ?? string.Empty;
        }
        #endregion

        #region Message Embedding Methods

        /// <summary>
        /// Embeds a message into the selected video.
        /// Validates conditions, processes frames, embeds the message, and reconstructs the video.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EmbeddMessageAsync()
        {
            // Validate initial conditions
            if (!ValidateInitialConditions())
            {
                return;
            }

            // Set up processing state
            InitializeProcessingState();

            // Load configuration to set up project paths.
            var config = LoadProjectConfig();
            string projectPath = config.BaseProjectPath;
            ProjectPaths paths = SetupProjectPaths(projectPath, config);

            try
            {
                // Ensure project directory structure exists
                CreateProjectDirectories(paths);

                // Extract frames if necessary

                string numberPart = frameRate.Split(' ')[0];
                double FrameRate = double.Parse(numberPart);

                await ExtractFramesIfNeeded(paths, FrameRate);

                // Embed message in frames
                int[] iframesLocation = await EmbedMessageInFrames(paths);

                // Reconstruct video
                ReconstructVideoAndCleanup(paths, iframesLocation);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                ResetProcessingState();
            }
        }

        /// <summary>
        /// Validates the initial conditions required for embedding the message.
        /// Checks if the video is already processing, if the message or password is missing, or if a video has been selected.
        /// </summary>
        /// <returns>True if all initial conditions are met; otherwise, false.</returns>
        private bool ValidateInitialConditions()
        {
            if (IsProcessing)
            {
                return false;
            }
            

            if (string.IsNullOrEmpty(MessageText) || string.IsNullOrEmpty(EncryptionPassword))
            {
                DisplayErrorMessage("Message text or password is missing.");
                return false;
            }

            if (string.IsNullOrEmpty(SelectedVideoPath))
            {
                DisplayErrorMessage("No video file selected. Please select a video file before embedding a message.");
                return false;
            }

            string pattern = @"^.+\.(mp4|mkv|avi)$";
            if (string.IsNullOrEmpty(videoNameAndFormat))
            {
                DisplayErrorMessage("the new video name is null");
                return false;
            }
            else if (!Regex.IsMatch(videoNameAndFormat, pattern))
            {
                DisplayErrorMessage("The video name must end with LOWER CASED .mp4, .mkv, or .avi");
                return false;
            } 


            ErrorMessage = null;
            return true;
        }

        /// <summary>
        /// Initializes the processing state by setting flags and updating UI texts.
        /// </summary>
        private void InitializeProcessingState()
        {
            IsProcessing = true;
            EmbedButtonText = "Processing...";
            ProcessingStatusText = "Preparing video processing...";
        }

        /// <summary>
        /// Record that encapsulates various project paths required during processing.
        /// </summary>
        /// <param name="ProjectPath">The root project path.</param>
        /// <param name="AllFramesFolder">The folder path for storing all frames.</param>
        /// <param name="AllFramesWithMessageFolder">The folder path for storing frames with embedded messages.</param>
        /// <param name="MetaDataFile">The file path for video metadata.</param>
        /// <param name="NewVideo">The file path for the newly created video.</param>
        private record ProjectPaths(
            string ProjectPath,
            string AllFramesFolder,
            string AllFramesWithMessageFolder,
            string MetaDataFile,
            string NewVideo
        );

        /// <summary>
        /// Sets up project paths based on the specified project directory and configuration.
        /// </summary>
        private ProjectPaths SetupProjectPaths(string projectPath, ProjectPathsConfig config)
        {
            return new ProjectPaths(
                ProjectPath: projectPath,
                AllFramesFolder: Path.Combine(projectPath, config.Paths.AllFramesFolder),
                AllFramesWithMessageFolder: Path.Combine(projectPath, config.Paths.AllFramesWithMessageFolder),
                MetaDataFile: Path.Combine(projectPath, config.Paths.MetaDataFile),
                NewVideo: Path.Combine(projectPath, videoNameAndFormat)
            );
        }

        /// <summary>
        /// Creates the necessary project directories if they do not exist.
        /// </summary>
        /// <param name="paths">The project paths to create.</param>
        private void CreateProjectDirectories(ProjectPaths paths)
        {
            Directory.CreateDirectory(paths.ProjectPath);
            Directory.CreateDirectory(paths.AllFramesFolder);
        }

        /// <summary>
        /// Extracts frames from the video if they have not been extracted already.
        /// </summary>
        /// <param name="paths">The project paths containing the frames folder.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExtractFramesIfNeeded(ProjectPaths paths,double FPS)
        {
            if (!string.IsNullOrEmpty(SelectedVideoPath) &&
                Directory.GetFiles(paths.AllFramesFolder).Length == 0)
            {
                await Task.Run(() =>
                    Services.Extraction.ExtractAllFrames(SelectedVideoPath, paths.AllFramesFolder,FPS)
                );
            }
        }

        /// <summary>
        /// Embeds the message into video frames by processing I-frames and updating frame images.
        /// </summary>
        /// <param name="paths">The project paths used during embedding.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<int[]> EmbedMessageInFrames(ProjectPaths paths)
        {
            // Check that the metadata file exists
            if (!File.Exists(paths.MetaDataFile))
            {
                ErrorMessage = "MetaData file does not exist";
                return null;
            }

            // Extract I-frame locations
            int[] iframesLocation = await Services.Extraction.GetIFrameLocations(paths.MetaDataFile);

            // Create output directory if it does not exist
            Directory.CreateDirectory(paths.AllFramesWithMessageFolder);

            // Logging message to indicate embedding started
            ErrorMessage = "EMBEDDING MESSAGE IN FRAMES";

            // Embed the message in the frames
            ErrorMessage = Services.Embedding.EmbedMessageInFramesInVideo(
                paths.AllFramesFolder,
                paths.AllFramesWithMessageFolder,
                iframesLocation,
                MessageText,
                EncryptionPassword,
                Duration,
                FrameRate,
                seconds
            );

            // Ensure that at least one frame has been created
            string firstFramePath = Directory.GetFiles(paths.AllFramesWithMessageFolder, "*.png").FirstOrDefault();
            if (firstFramePath == null)
            {
                DisplayErrorMessage("No frames found in allFramesWithMessageFolder directory.");
                throw new InvalidOperationException("No frames created during embedding");
            }

            // Return the array of I-frame locations for use in reconstruction
            return iframesLocation;
        }

        /// <summary>
        /// Reconstructs the video from frames and performs cleanup of temporary files and directories.
        /// </summary>
        /// <param name="paths">The project paths used during processing.</param>
        private void ReconstructVideoAndCleanup(ProjectPaths paths,int[] iFramesLocations)
        {
           

            string numberPart = frameRate.Split(' ')[0];
            double FrameRate = double.Parse(numberPart);
            ErrorMessage = Services.HelperFunctions.ReconstructVideo(paths.AllFramesWithMessageFolder, paths.NewVideo, FrameRate,iFramesLocations);
            if(ErrorMessage.Equals ("Video reconstruction completed successfully.")&&encryptionPassword!=null)
            {
                // Store the encryption password and video name in the JSON file.
                VideoKeyStorage(paths.NewVideo, encryptionPassword);
                DisplaySuccessMessage("Video reconstruction completed successfully.");
            }
            else
            {
                DisplayErrorMessage(ErrorMessage);
            }
            DeleteDirectoryAndFiles(paths.AllFramesWithMessageFolder, paths.AllFramesFolder, paths.MetaDataFile);
        }

        private void VideoKeyStorage(string newVideoPath, string encryptionPassword)
        {
            // Define the storage file path. You can adjust this path if necessary.
            string storageFile = Path.Combine(AppContext.BaseDirectory, "Json", "VideoKeyStorage.json");
            string MasterPath = Path.Combine(AppContext.BaseDirectory, "Json", "MasterKey.txt");

            // Load existing dictionary or create a new one.
            Dictionary<string, string> videoKeyDict;
            if (File.Exists(storageFile))
            {
                string json = File.ReadAllText(storageFile);
                videoKeyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                               ?? new Dictionary<string, string>();
            }
            else
            {
                videoKeyDict = new Dictionary<string, string>();
            }

            // Extract the video name from the new video path.
            string videoName = Path.GetFileName(newVideoPath);
            var storedHash = File.ReadAllText(MasterPath);

            // Check if the video name already exists in the dictionary (as a value).
            var Encryptedname = EncryptionAes.Encrypt(videoName, storedHash);
            var Encryptedpass = EncryptionAes.Encrypt(encryptionPassword, storedHash);
            if (!videoKeyDict.ContainsValue(Encryptedname))
            {
                // Add the encryption password as the key and video name as the value.
                videoKeyDict[Encryptedpass] = Encryptedname;

                // Write the updated dictionary back to the JSON file.
                string newJson = JsonConvert.SerializeObject(videoKeyDict, Formatting.Indented);
                File.WriteAllText(storageFile, newJson);
            }
        }
        /// <summary>
        /// Handles exceptions by setting the error message and logging the error.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        private void HandleException(Exception ex)
        {
            ErrorMessage = ex.Message;
            Console.WriteLine(ex.Message);
        }

        /// <summary>
        /// Resets the processing state once video processing has completed.
        /// </summary>
        private void ResetProcessingState()
        {
            IsProcessing = false;
            EmbedButtonText = "Embed Message";
        }
        #endregion

        #region Video Playback Methods

        /// <summary>
        /// Previews the selected video by playing it.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PreviewVideoAsync()
        {
            if (!string.IsNullOrEmpty(SelectedVideoPath))
            {
                try
                {
                    HelperFunctions.PlayVideo(SelectedVideoPath);
                }
                catch (Exception ex)
                {
                     DisplayErrorMessage($"Error previewing video: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Plays the selected video.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PlayVideoAsync()
        {
            if (SelectedVideoPath != null)
            {
                HelperFunctions.PlayVideo(SelectedVideoPath);
            }
            else
            {
                DisplayErrorMessage("CANT PLAY VIDEO selectedVideoPath is null");
            }
        }
        #endregion

        #region Thumbnail Generation

        /// <summary>
        /// Generates a thumbnail image for the video by extracting a frame.
        /// </summary>
        /// <param name="videoPath">The file path of the video.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task GenerateThumbnailAsync(string videoPath)
        {
            // Load configuration to retrieve the base project path.
            var config = LoadProjectConfig();
            string projectPath = config.BaseProjectPath;
            try
            {
                string thumbnailPath = Path.Combine(projectPath, "temp_thumbnail.png");

                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 -f image2 \"{thumbnailPath}\"");

                await conversion.Start();

                if (File.Exists(thumbnailPath))
                {
                    using (var fs = File.OpenRead(thumbnailPath))
                    {
                        ThumbnailImage = new Bitmap(fs);
                        ThumbnailImage = ThumbnailImage.CreateScaledBitmap(new PixelSize(160, 90), BitmapInterpolationMode.HighQuality);
                    }

                    try { File.Delete(thumbnailPath); } catch { }
                }
            }
            catch
            {
                ThumbnailImage = null;
            }
        }
        #endregion

        #region Capacity and Size Utility Methods

        /// <summary>
        /// Updates the capacity usage information based on the message size and estimated capacity.
        /// </summary>
        private void UpdateCapacityUsage()
        {
            if (string.IsNullOrEmpty(EstimatedCapacity))
            {
                ResetCapacityUsage();
                return;
            }

            long dataSize = CalculateDataSize();
            UpdateCapacityUsageDetails(dataSize);
        }

        /// <summary>
        /// Calculates the size in bytes of the message text.
        /// Also updates the message length information.
        /// </summary>
        /// <returns>The size of the message in bytes.</returns>
        private long CalculateDataSize()
        {
            MessageLengthInfo = string.IsNullOrEmpty(MessageText)
                ? "0 chars"
                : $"{MessageText.Length} chars ({FormatByteSize(Encoding.UTF8.GetByteCount(MessageText))})";

            return string.IsNullOrEmpty(MessageText)
                ? 0
                : Encoding.UTF8.GetByteCount(MessageText);
        }

        /// <summary>
        /// Updates capacity usage details by comparing the message size to the estimated capacity.
        /// </summary>
        /// <param name="dataSize">The size of the message data in bytes.</param>
        private void UpdateCapacityUsageDetails(long dataSize)
        {
            if (TryParseByteSize(EstimatedCapacity, out long capacityInBytes) && capacityInBytes > 0)
            {
                CalculateAndSetCapacityUsage(dataSize, capacityInBytes);
            }
            else
            {
                ResetCapacityUsage();
            }
        }

        /// <summary>
        /// Calculates the percentage of capacity used and updates related UI properties.
        /// </summary>
        /// <param name="dataSize">The size of the message in bytes.</param>
        /// <param name="capacityInBytes">The total estimated capacity in bytes.</param>
        private void CalculateAndSetCapacityUsage(long dataSize, long capacityInBytes)
        {
            double percentage = (double)dataSize / capacityInBytes * 100;
            CapacityUsagePercentage = Math.Min(percentage, 100);
            CapacityUsageText = $"{dataSize:N0} / {capacityInBytes:N0} bytes ({percentage:F1}%)";

            CapacityColor = percentage > 90 ? Brushes.Red
                : percentage > 70 ? Brushes.Orange
                : Brushes.LightGreen;
        }

        /// <summary>
        /// Resets the capacity usage information to default values.
        /// </summary>
        private void ResetCapacityUsage()
        {
            CapacityUsagePercentage = 0;
            CapacityUsageText = "N/A";
            CapacityColor = Brushes.Gray;
            MessageLengthInfo = "0 chars";
        }
        #endregion

        #region Utility Methods for Byte Size Conversion

        /// <summary>
        /// Formats a byte count into a human-readable string with appropriate units.
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        /// <returns>A formatted string representing the byte size.</returns>
        private string FormatByteSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {suffixes[order]}";
        }

        /// <summary>
        /// Attempts to parse a formatted byte size string into a long representing the number of bytes.
        /// </summary>
        /// <param name="formattedSize">The formatted byte size string.</param>
        /// <param name="bytes">When this method returns, contains the parsed number of bytes if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        private bool TryParseByteSize(string formattedSize, out long bytes)
        {
            bytes = 0;
            if (string.IsNullOrEmpty(formattedSize)) return false;

            try
            {
                string[] parts = formattedSize.Split(' ');
                if (parts.Length != 2) return false;
                if (!double.TryParse(parts[0], out double value)) return false;

                return ParseByteUnit(parts[1].ToUpperInvariant(), value, out bytes);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a unit suffix and value into a byte count.
        /// </summary>
        /// <param name="suffix">The unit suffix (e.g., "KB", "MB").</param>
        /// <param name="value">The numeric value.</param>
        /// <param name="bytes">When this method returns, contains the parsed byte count if successful.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        private bool ParseByteUnit(string suffix, double value, out long bytes)
        {
            bytes = suffix switch
            {
                "B" => (long)value,
                "KB" => (long)(value * 1024),
                "MB" => (long)(value * 1024 * 1024),
                "GB" => (long)(value * 1024 * 1024 * 1024),
                "TB" => (long)(value * 1024 * 1024 * 1024 * 1024),
                _ => 0
            };

            return bytes > 0;
        }
        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Deletes temporary directories and files used during video processing.
        /// </summary>
        /// <param name="allFramesWithMessageFolder">The directory containing frames with embedded messages.</param>
        /// <param name="allFramesFolder">The directory containing all extracted frames.</param>
        /// <param name="metaDataFile">The metadata file path.</param>
        private void DeleteDirectoryAndFiles(string allFramesWithMessageFolder, string allFramesFolder, string metaDataFile)
        {
            try
            {
                if (Directory.Exists(allFramesWithMessageFolder))
                {
                    Directory.Delete(allFramesWithMessageFolder, true);
                }
                if(Directory.Exists(allFramesFolder))
                {
                    Directory.Delete(allFramesFolder, true);
                }
                if (File.Exists(metaDataFile))
                {
                    File.Delete(metaDataFile);
                }
                    
            }
            catch (Exception ex)
            {
                DisplayErrorMessage(ex.Message);
            }
        }
        #endregion
    }
}
