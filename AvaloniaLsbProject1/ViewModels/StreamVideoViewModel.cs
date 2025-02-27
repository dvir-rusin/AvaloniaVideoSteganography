using AvaloniaLsbProject1.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.Runtime.InteropServices;
using System.IO;
using AvaloniaLsbProject1.Services;
using Xabe.FFmpeg;
using System.Linq;
using static Emgu.CV.DISOpticalFlow;
using Avalonia.Media.Imaging;
using Avalonia;

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

        private Process? ffmpegProcess; // Reference to the FFmpeg process for stopping

        [ObservableProperty]
        public bool isProcessing;

        [ObservableProperty]
        public string processingStatusText;

        [ObservableProperty]
        private string streamButtonText;

        // New properties
        [ObservableProperty]
        private Bitmap thumbnailImage;

        public StreamVideoViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            StreamVideoCommand = new AsyncRelayCommand(StreamVideoAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            DownloadStreamCommand = new AsyncRelayCommand(DownloadStreamAsync);
            PreviewVideoCommand = new AsyncRelayCommand(PreviewVideoAsync);
            streamButtonText = "Start Stream";
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand StreamVideoCommand { get; }
        public IAsyncRelayCommand PlayVideoCommand { get; }

        public IAsyncRelayCommand DownloadStreamCommand { get; }

        public IAsyncRelayCommand PreviewVideoCommand { get; }

        // Select a video file loads its attributes 
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

                var result = await dialog.ShowAsync(MainWindow.Instance); // Replace with proper dialog handling
                if (result?.Length > 0)
                {
                    SelectedVideoPath = result[0];
                    await LoadVideoAttributesAsync(SelectedVideoPath);

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
                long estimatedBytes = (((videoStream.Width * videoStream.Height) - 21) * 3 / 8); // minus 4 cuz last 4 pixel are for message validation minus 16 cuz of 16 byte iv and minus 1 cus null termainator =minus 21
                //still need to calc possible padding for the message
                
                await GenerateThumbnailAsync(videoPath);
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

        private async Task StreamVideoAsync()
        {
            if (string.IsNullOrEmpty(SelectedVideoPath) || string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                ErrorMessage = "Video path, multicast IP, or port is missing.";
                return;
            }

            if (string.IsNullOrEmpty(Duration))
            {
                ErrorMessage = "video artribute : Duration, is null.  ";
                return;
            }

            // Set processing state
            IsProcessing = true;
            StreamButtonText = "Streaming...";
            ProcessingStatusText = "Preparing Stream...";
            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";

                string fileExtension = Path.GetExtension(SelectedVideoPath).ToLower();
                //string arguments = $"-re -i \"{SelectedVideoPath}\" -c:v libx264rgb -t 2.2 -preset veryfast -qp 0 -pix_fmt bgr24 -f mpegts udp://{MulticastIP}:{Port}";
                //string arguments = $" -re -i \"{SelectedVideoPath}\" -c:v libx264 -f mpegts udp://{MulticastIP}:{Port}";
                string arguments;

                // Configure streaming parameters based on file format
                switch (fileExtension)
                {
                    case ".mp4":
                        // For MP4: Direct stream with minimal processing when possible
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t 2.2 -f mpegts udp://{MulticastIP}:{Port}";
                        break;

                    case ".avi":
                        // For AVI: Often needs transcoding due to container limitations
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;

                    case ".mkv":
                        // For MKV: Try to copy video stream but normalize audio
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v copy -c:a aac -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;

                    case ".mov":
                        // For MOV: Apple formats sometimes need specific handling
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;

                    default:
                        // Generic approach for other formats - more conversion but more compatible
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 6 -c:a mp2 -b:a 128k -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                }
                
                Process ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(ffmpegPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = true,  // Redirect error stream
                        CreateNoWindow = false
                    }
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        // You can log this output or display it in your UI.
                        Debug.WriteLine(e.Data);
                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                ErrorMessage = $"Streaming to {MulticastIP}:{Port}...";


            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error streaming video: {ex.Message}";
            }
        }

        private async Task GenerateThumbnailAsync(string videoPath)
        {
            string projectPath = "C:\\AvaloniaVideoStenagraphy";
            try
            {
                string thumbnailPath = Path.Combine(projectPath, "temp_thumbnail.jpg");

                // Use FFmpeg to extract a thumbnail
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 -f image2 \"{thumbnailPath}\"");

                await conversion.Start();

                // Load the thumbnail
                if (File.Exists(thumbnailPath))
                {
                    using (var fs = File.OpenRead(thumbnailPath))
                    {
                        ThumbnailImage = new Bitmap(fs);

                        ThumbnailImage = ThumbnailImage.CreateScaledBitmap(new PixelSize(160, 90), BitmapInterpolationMode.HighQuality);
                    }

                    // Clean up
                    try { File.Delete(thumbnailPath); } catch { }
                }
            }
            catch
            {
                // Silently fail - thumbnail is not critical
                ThumbnailImage = null;
            }
        }
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
                    ErrorMessage = $"Error previewing video: {ex.Message}";
                }
            }
        }

        private async Task PlayVideoAsync()
        {
            if (string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                ErrorMessage = "Multicast IP or port is missing.";
                return;
            }

            try
            {
                string ffplayPath = @"C:\ffmpeg\bin\ffplay.exe"; // Update with your FFplay path
                string arguments = $"-i udp://{MulticastIP}:{Port}";

                Process ffplayProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffplayPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = false
                    }
                };

                ffplayProcess.Start();
                ErrorMessage = $"Playing video from {MulticastIP}:{Port}...";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error playing video: {ex.Message}";
            }
        }

        private async Task DownloadStreamAsync()
        {
            if (string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                ErrorMessage = "Multicast IP or port is missing.";
                return;
            }

            if (string.IsNullOrEmpty(Duration))
            {
                ErrorMessage = "video artribute : Duration, is null.  ";
                return;
            }

            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Update with your FFmpeg path
                string outputDirectory = @"C:\AvaloniaVideoStenagraphy"; // Desired output directory
                string outputFile = Path.Combine(outputDirectory, "stream_capturefeb25.mp4"); // Combine directory and filename
                string arguments = $"-i udp://{MulticastIP}:{Port} -c copy -t 2 \"{outputFile}\"";


                Process ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = true,
                        CreateNoWindow = false
                    }
                };

                ffmpegProcess.Start();
                ErrorMessage = $"Downloading stream to {outputFile} for 2 seconds...";
                await ffmpegProcess.WaitForExitAsync(); // Wait for the process to finish
                ErrorMessage = "Stream download completed.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error downloading stream: {ex.Message}";
            }
        }

    }
}
