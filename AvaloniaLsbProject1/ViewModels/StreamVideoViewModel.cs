using AvaloniaLsbProject1.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

        private Process? ffmpegProcess; // Reference to the FFmpeg process for stopping

        public StreamVideoViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            StreamVideoCommand = new AsyncRelayCommand(StreamVideoAsync);
            StopStreamCommand = new RelayCommand(StopStream);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand StreamVideoCommand { get; }
        public IAsyncRelayCommand PlayVideoCommand { get; }
        public IRelayCommand StopStreamCommand { get; }

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

        private async Task StreamVideoAsync()
        {
            if (string.IsNullOrEmpty(SelectedVideoPath) || string.IsNullOrEmpty(MulticastIP) || string.IsNullOrEmpty(Port))
            {
                ErrorMessage = "Video path, multicast IP, or port is missing.";
                return;
            }

            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Update with your FFmpeg path
                string arguments = $"-re -i \"{SelectedVideoPath}\" -f mpegts udp://{MulticastIP}:{Port}";

                Process ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true
                    }
                };

                ffmpegProcess.Start();
                ErrorMessage = $"Streaming to {MulticastIP}:{Port}...";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error streaming video: {ex.Message}";
            }
        }

        private void StopStream()
        {
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                try
                {
                    ffmpegProcess.Kill(); // Terminate the FFmpeg process
                    ffmpegProcess.Dispose();
                    ffmpegProcess = null;
                    ErrorMessage = "Streaming stopped.";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error stopping stream: {ex.Message}";
                }
            }
            else
            {
                ErrorMessage = "No active stream to stop.";
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
    }
}
