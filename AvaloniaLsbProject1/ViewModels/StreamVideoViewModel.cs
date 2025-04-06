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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;

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

        // Cancellation token for HTTPS server.
        private CancellationTokenSource _serverCts = new CancellationTokenSource();

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

        // HTTPS server with Basic Authentication for the /download endpoint.
        

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
                ErrorMessage = "Video attribute 'Duration' is null.";
                return;
            }

            IsProcessing = true;
            StreamButtonText = "Streaming...";
            ProcessingStatusText = "Preparing Stream...";
            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string fileExtension = Path.GetExtension(SelectedVideoPath).ToLower();
                string arguments;

                switch (fileExtension)
                {
                    case ".mp4":
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c copy -t 2.2 -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    case ".avi":
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    case ".mkv":
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v copy -c:a aac -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    case ".mov":
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 5 -c:a mp2 -b:a 192k -f mpegts udp://{MulticastIP}:{Port}";
                        break;
                    default:
                        arguments = $"-re -i \"{SelectedVideoPath}\" -c:v mpeg2video -q:v 6 -c:a mp2 -b:a 128k -f mpegts udp://{MulticastIP}:{Port}";
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
                string ffplayPath = @"C:\ffmpeg\bin\ffplay.exe";
                string arguments = $"-i udp://{MulticastIP}:{Port}";

                var ffplayProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffplayPath,
                        Arguments = arguments,
                        UseShellExecute = false,
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
                ErrorMessage = "Video attribute 'Duration' is null.";
                return;
            }

            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                string outputDirectory = @"C:\AvaloniaVideoStenagraphy";
                string outputFileName = "stream_capture" + DateTime.Now.ToString("dd.MM_HH:mm:ss") + ".mp4";
                string outputFile = Path.Combine(outputDirectory, outputFileName);
                string arguments = $"-i udp://{MulticastIP}:{Port} -c copy -t 2 \"{outputFile}\"";

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = false
                    }
                };

                ffmpegProcess.Start();
                ErrorMessage = $"Downloading stream to {outputFile} for 2 seconds...";
                await ffmpegProcess.WaitForExitAsync();
                ErrorMessage = "Stream download completed.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error downloading stream: {ex.Message}";
            }
        }
    }
}
