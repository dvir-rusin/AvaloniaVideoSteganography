using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.Runtime.InteropServices;
using System.IO;
using AvaloniaLsbProject1.Services;
using Xabe.FFmpeg;
using System.Linq;
using System.Threading;
using Tmds.DBus.Protocol;
using Avalonia.Media;
using System.Text;

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

        [ObservableProperty]
        private string? height;

        [ObservableProperty]
        private string? width;

        [ObservableProperty]
        private string? frameRate;

        [ObservableProperty]
        private string? bitRate;

        [ObservableProperty]
        private string? duration;

        [ObservableProperty]
        private string estimatedCapacity;

        [ObservableProperty] 
        private double capacityUsagePercentage;

        [ObservableProperty]
        private string capacityUsageText;

        [ObservableProperty]
        private IBrush capacityColor;

        [ObservableProperty]
        private string messageLengthInfo;

        [ObservableProperty]
        public bool isProcessing;

        [ObservableProperty]
        public string processingStatusText;

        [ObservableProperty]
        private string embedButtonText;

        public EmbedViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            EmbeddMessageCommand = new AsyncRelayCommand(EmbeddMessageAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            EmbedButtonText = "Embed Message";

        }

        partial void OnMessageTextChanged(string? oldValue, string? newValue)
        {
            UpdateCapacityUsage();
        }


        public IAsyncRelayCommand SelectVideoCommand { get; }

        public IAsyncRelayCommand EmbeddMessageCommand { get; }

        public IAsyncRelayCommand PlayVideoCommand { get; }

        // Add method to extract video attributes
        private async Task LoadVideoAttributesAsync(string videoPath)
        {
            try
            {
                ProcessingStatusText = "Analyzing video...";
                IsProcessing = true;
                // Use Xabe.FFmpeg to get video metadata
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                
                if (videoStream != null)
                {
                    Height = $"{videoStream.Height}";
                    Width = $"{videoStream.Width}";
                    FrameRate = $"{videoStream.Framerate} fps";
                    BitRate = $"{videoStream.Bitrate / 1000} kbps";
                    Duration = mediaInfo.Duration.ToString(@"hh\:mm\:ss\.fff");
                }
                else
                {
                    ErrorMessage = "No video stream found.";
                }
                long estimatedBytes = (((videoStream.Width * videoStream.Height) -21)*3/8) ; // minus 4 cuz last 4 pixel are for message validation minus 16 cuz of 16 byte iv and minus 1 cus null termainator =minus 21
                //still need to calc possible padding for the message
                EstimatedCapacity = FormatByteSize(estimatedBytes);
                UpdateCapacityUsage();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error extracting video attributes: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
        private async Task SelectVideoAsync()
        {
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            string metaDataFile = Path.Combine(projectPath, "MetaData.csv");
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
                    await LoadVideoAttributesAsync(SelectedVideoPath); // Extract attributes after selection
                    if (!File.Exists(metaDataFile))
                    {
                        using (File.Create(metaDataFile)) { }
                        Extraction.ExtractFrameMetadata(SelectedVideoPath, metaDataFile);
                    }


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
            if (IsProcessing)
            {
                return;
            }

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

            // Clear previous messages
            ErrorMessage = string.Empty;

            // Set processing state
            IsProcessing = true;
            EmbedButtonText = "Processing...";
            ProcessingStatusText = "Preparing video processing...";

            string messageText = MessageText;
            string password = EncryptionPassword;
            // Base project directory
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            string allFramesFolder = Path.Combine(projectPath, "AllFrames");
            string allFramesWithMessageFolder = Path.Combine(projectPath, "allFramesWithMessage");
            //string allFramesWithMessageFolderTest = Path.Combine(projectPath, "allFramesWithMessageTestForAllFrames");
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

                // Ensure a video file is selected
                
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
                    if(File.Exists(metaDataFile))
                    {
                        int[] iframesLocation = await Services.Extraction.GetIFrameLocations(metaDataFile);




                        if (!Directory.Exists(allFramesWithMessageFolder))
                        {
                            Directory.CreateDirectory(allFramesWithMessageFolder);

                            Thread.Sleep(1000);
                            ErrorMessage = "EMBEDDING MESSAGE IN FRAMES";
                            ErrorMessage = Services.Embedding.EmbedMessageInFramesTestInVideo(allFramesFolder, allFramesWithMessageFolder, iframesLocation, messageText, password);

                            string firstFramePath = Directory.GetFiles(allFramesWithMessageFolder, "*.png").FirstOrDefault();

                            if (firstFramePath == null)
                            {
                                ErrorMessage = "No frames found in allFramesWithMessageFolder directory.";
                                return;
                            }
                        }

                    }
                    else
                    {
                        ErrorMessage = "metaDataFile does not exist ";
                        return;
                    }
                    
                    //await Services.Extraction.ExtractFrameMetadata(selectedVideoPath, metaDataFile);
                    


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
                    ErrorMessage = "NEW VIDEO HAS BEEN CREATED ";
                    DeleteDirectoryAndFiles(allFramesWithMessageFolder, allFramesFolder, metaDataFile);

                }
                catch(Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
                finally
                {
                    IsProcessing = false;
                    EmbedButtonText = "Embed Message";
                }
            }
            else
            {
                ErrorMessage = "allFramesWithMessageFolder does not exist";
            }
            
            
        }

        
        private void UpdateCapacityUsage()
        {
            if (string.IsNullOrEmpty(EstimatedCapacity))
            {
                CapacityUsagePercentage = 0;
                CapacityUsageText = "N/A";
                CapacityColor = Brushes.Gray;
                MessageLengthInfo = "0 chars";
                return;
            }

            // Calculate size of data to embed
            long dataSize = 0;

            if (!string.IsNullOrEmpty(MessageText))
            {
                dataSize = Encoding.UTF8.GetByteCount(MessageText);
                MessageLengthInfo = $"{MessageText.Length} chars ({FormatByteSize(dataSize)})";
            }

            //if (!string.IsNullOrEmpty(SelectedVideoPath))
            //{
            //    try
            //    {
            //        var fileInfo = new FileInfo(SelectedVideoPath);
            //        dataSize = fileInfo.Length;
            //        MessageLengthInfo = $"File: {FormatByteSize(dataSize)}";
            //    }
            //    catch
            //    {
            //        // Silently fail
            //    }
            //}

            // Parse estimated capacity
            if (TryParseByteSize(EstimatedCapacity, out long capacityInBytes) && capacityInBytes > 0)
            {
                // Calculate percentage
                double percentage = (double)dataSize / capacityInBytes * 100;
                CapacityUsagePercentage = Math.Min(percentage, 100);
                CapacityUsageText = $"{dataSize:N0} / {capacityInBytes:N0} bytes ({percentage:F1}%)";

                // Set color based on usage
                if (percentage > 90)
                    CapacityColor = Brushes.Red;
                else if (percentage > 70)
                    CapacityColor = Brushes.Orange;
                else
                    CapacityColor = Brushes.LightGreen;
            }
            else
            {
                CapacityUsagePercentage = 0;
                CapacityUsageText = "Unknown capacity";
                CapacityColor = Brushes.Gray;
            }
        }

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
        private bool TryParseByteSize(string formattedSize, out long bytes)
        {
            bytes = 0;

            if (string.IsNullOrEmpty(formattedSize))
                return false;

            try
            {
                string[] parts = formattedSize.Split(' ');
                if (parts.Length != 2)
                    return false;

                if (!double.TryParse(parts[0], out double value))
                    return false;

                string suffix = parts[1].ToUpperInvariant();

                switch (suffix)
                {
                    case "B":
                        bytes = (long)value;
                        return true;
                    case "KB":
                        bytes = (long)(value * 1024);
                        return true;
                    case "MB":
                        bytes = (long)(value * 1024 * 1024);
                        return true;
                    case "GB":
                        bytes = (long)(value * 1024 * 1024 * 1024);
                        return true;
                    case "TB":
                        bytes = (long)(value * 1024 * 1024 * 1024 * 1024);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        private void DeleteDirectoryAndFiles(string allFramesWithMessageFolder, string allFramesFolder, string metaDataFile)
        {
            try
            {
                Directory.Delete(allFramesWithMessageFolder,true);
                Directory.Delete(allFramesFolder,true);
                File.Delete(metaDataFile);
            }
            catch (Exception ex) {
                ErrorMessage = ex.Message;
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
            }
        }
    }
}
