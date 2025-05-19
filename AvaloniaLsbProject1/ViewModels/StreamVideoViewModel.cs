using Avalonia;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Avalonia.Media;
using System.ComponentModel.Design;


namespace AvaloniaLsbProject1.ViewModels
{
    public partial class StreamVideoViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? selectedVideoPath;

        [ObservableProperty]
        private string? multicastIP;

        [ObservableProperty]
        private string? port;

        [ObservableProperty]
        private string? errorMessage;

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

        private Process? ffmpegProcess; // Reference to the FFmpeg process

        [ObservableProperty]
        public bool isProcessing;

        [ObservableProperty]
        public string processingStatusText;

        [ObservableProperty]
        private string streamButtonText;

        // New property for thumbnail image.
        [ObservableProperty]
        private Avalonia.Media.Imaging.Bitmap thumbnailImage;

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

        public StreamVideoViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            StreamVideoCommand = new AsyncRelayCommand(StreamVideoAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            DownloadStreamCommand = new AsyncRelayCommand(DownloadStreamAsync);
            PreviewVideoCommand = new AsyncRelayCommand(PreviewVideoAsync);
            streamButtonText = "Start Stream";

            // Initialize default colors and status.
            errorBoardercolor = new SolidColorBrush(Color.Parse("#CCCCCC"));
            errorcolor = new SolidColorBrush(Color.Parse("#000000"));
            sucssesOrErorr = "None";
            sucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#000000"));
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand StreamVideoCommand { get; }
        public IAsyncRelayCommand PlayVideoCommand { get; }
        public IAsyncRelayCommand DownloadStreamCommand { get; }
        public IAsyncRelayCommand PreviewVideoCommand { get; }

        

        private async Task SelectVideoAsync()
        {
            try
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

                var result = await dialog.ShowAsync(MainWindow.Instance);
                if (result?.Length > 0)
                {
                    SelectedVideoPath = result[0];
                    await LoadVideoAttributesAsync(SelectedVideoPath);
                }
                else
                {
                    DisplayErrorMessage("No video file was selected.");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error selecting video: {ex.Message}";
            }
        }

        private async Task LoadVideoAttributesAsync(string videoPath)
        {
            try
            {
                ProcessingStatusText = "Analyzing video...";
                IsProcessing = true;
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
                    DisplayErrorMessage("No video stream found.");
                }
                await GenerateThumbnailAsync(videoPath);
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error extracting video attributes: {ex.Message}") ;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task StreamVideoAsync()
        {
            int durationInSeconds;
            if (string.IsNullOrEmpty(SelectedVideoPath) || string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                DisplayErrorMessage("Video path, multicast IP, or port is missing.");
                return;
            }
            if (string.IsNullOrEmpty(Duration))
            {
                DisplayErrorMessage("Video attribute 'Duration' is null.");
                return;
            }
            if (int.TryParse(Duration.ToString(), out int parsedDuration) && parsedDuration > 5)
            {
                 durationInSeconds = 5;
            }
            else if( parsedDuration <= 5 && parsedDuration>=2)
            {
                 durationInSeconds = parsedDuration-1;
            }
            else
            {
                DisplayErrorMessage("duration value is too little");
                return;
            }



            IsProcessing = true;
            ErrorMessage = null;
            StreamButtonText = "Streaming...";
            ProcessingStatusText = "Preparing Stream...";
            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string fileExtension = Path.GetExtension(SelectedVideoPath).ToLower();
                string arguments;

                switch (fileExtension)
                {
                    //arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t 2.2 -f mpegts udp://{MulticastIP}:{Port}";
                    case ".mp4":
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t  \"{durationInSeconds}\" -f mpegts udp://{MulticastIP}:{Port}"; 
                        break;
                    case ".avi":
                        //arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        // Use uncompressed rawvideo for LSB-safe streaming
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t \"{durationInSeconds}\" -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    case ".mkv":
                        //arguments = $"-re -i \"{SelectedVideoPath}\" -c:v copy -c:a aac -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        // Use rawvideo in MKV container for LSB-safe streaming
                        //arguments = $"-re -i \"{SelectedVideoPath}\" -c:v rawvideo -pix_fmt rgb24 -allow_raw_vfw 1 -t 4.2 -f matroska udp://{MulticastIP}:{Port}";
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t \"{durationInSeconds}\" -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    case ".mov":
                        //arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        // MOV also supports rawvideo; use this for LSB safety
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t \" {durationInSeconds} \" -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    default:
                        //arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 6 -c:a mp2 -b:a 128k -f mpegts udp://{MulticastIP}:{Port}";
                        // Fallback: safe general-purpose rawvideo
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t \" {durationInSeconds} \" -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                }

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(ffmpegPath),
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = false
                    }
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        Debug.WriteLine(e.Data); 


                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                // Update view model properties with the result.
                DisplaySuccessMessage($"Streaming to {MulticastIP}:{Port}...");

            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error streaming video: {ex.Message}");
            }
        }

        private async Task GenerateThumbnailAsync(string videoPath)
        {
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            try
            {
                string thumbnailPath = Path.Combine(projectPath, "temp_thumbnail.jpg");
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 -f image2 \"{thumbnailPath}\"");
                await conversion.Start();

                if (File.Exists(thumbnailPath))
                {
                    using (var fs = File.OpenRead(thumbnailPath))
                    {
                        ThumbnailImage = new Avalonia.Media.Imaging.Bitmap(fs);
                        ThumbnailImage = ThumbnailImage.CreateScaledBitmap(new PixelSize(160, 90), Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
                    }
                    try { File.Delete(thumbnailPath); } catch { }
                }
            }
            catch
            {
                ThumbnailImage = null;
            }
        }

        private async Task PreviewVideoAsync()
        {
            if (!string.IsNullOrEmpty(SelectedVideoPath))
            {
                try
                {
                    Services.HelperFunctions.PlayVideo(SelectedVideoPath);
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage($"Error previewing video: {ex.Message}");
                }
            }
        }

        private async Task PlayVideoAsync()
        {
            if (string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                DisplayErrorMessage("Multicast IP or port is missing.");
                return;
            }

            try
            {
                string ffplayPath = @"C:\ffmpeg\bin\ffplay.exe";
                string arguments = $"-i udp://{MulticastIP}:{Port}";

                var ffplayProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffplayPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };

                ffplayProcess.Start();
                DisplaySuccessMessage($"Playing video from {MulticastIP}:{Port}...");
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error playing video: {ex.Message}");
            }
        }

        private async Task DownloadStreamAsync()
        {
            ErrorMessage = null;
            if (string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                
                DisplayErrorMessage("Multicast IP or port is missing.");
                return;
            }
            //if (string.IsNullOrEmpty(Duration))
            //{
                
            //    DisplayErrorMessage("Video attribute 'Duration' is null.");
            //    return;
            //}

            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string outputDirectory = @"C:\AvaloniaVideoStenagraphy";
                string outputFileName = "stream_capture" + DateTime.Now.ToString("dd.MM_HH.mm.ss") + ".mp4";
                string outputFile = Path.Combine(outputDirectory, outputFileName);
                //fixed recording duration 4 seconds
                int recordingDuration = 4;
                string arguments = $"-i udp://{MulticastIP}:{Port} -c copy -t {recordingDuration} \"{outputFile}\"";
                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = false,
                    }
                };

                ffmpegProcess.Start();
                DisplaySuccessMessage($"Downloading stream to {outputFile} for 2 seconds...");
                await ffmpegProcess.WaitForExitAsync();
                DisplaySuccessMessage("Stream download completed.");
            }
            catch (Exception ex)
            {
                DisplayErrorMessage($"Error downloading stream: {ex.Message}");
            }
        }

        private void DisplayErrorMessage(string message)
        {
            ErrorBoardercolor = new SolidColorBrush(Color.Parse("#FF4444")); // Red border
            Errorcolor = new SolidColorBrush(Color.Parse("#2a1e1e")); // Dark red text
            SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#FF4444")); // Dark red text
            SucssesOrErorr = "Error";
            ErrorMessage = message;
        }

        private void DisplaySuccessMessage(string message)
        {
            ErrorBoardercolor = new SolidColorBrush(Color.Parse("#44FF44")); // Green border
            Errorcolor = new SolidColorBrush(Color.Parse("#1e2a1e")); // Dark green text
            SucssesOrErorrTextColor = new SolidColorBrush(Color.Parse("#44FF44")); // Dark green text
            SucssesOrErorr = "Success";
            ErrorMessage = message;
        }
    }
}
