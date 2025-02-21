﻿using AvaloniaLsbProject1.Views;
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

        public StreamVideoViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            StreamVideoCommand = new AsyncRelayCommand(StreamVideoAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            DownloadStreamCommand = new AsyncRelayCommand(DownloadStreamAsync);
        }

        public IAsyncRelayCommand SelectVideoCommand { get; }
        public IAsyncRelayCommand StreamVideoCommand { get; }
        public IAsyncRelayCommand PlayVideoCommand { get; }

        public IAsyncRelayCommand DownloadStreamCommand { get; }

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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error extracting video attributes: {ex.Message}";
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

            try
            {
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
                //string arguments = $"-re -i \"{SelectedVideoPath}\" -c:v libx264rgb -t 2.2 -preset veryfast -qp 0 -pix_fmt bgr24 -f mpegts udp://{MulticastIP}:{Port}";
                string arguments = $" -re -i \"{SelectedVideoPath}\" -c:v libx264 -f mpegts udp://{MulticastIP}:{Port}";
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
                string outputFile = Path.Combine(outputDirectory, "stream_capture.mp4feb11"); // Combine directory and filename
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
