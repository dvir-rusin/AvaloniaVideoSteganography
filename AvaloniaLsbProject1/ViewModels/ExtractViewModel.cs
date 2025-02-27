﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaLsbProject1.Views;
using System.IO;
using AvaloniaLsbProject1.Services;

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
        public ExtractViewModel()
        {
            SelectVideoCommand = new AsyncRelayCommand(SelectVideoAsync);
            ExtractMessageCommand = new AsyncRelayCommand(ExtractMessageAsync);
            PlayVideoCommand = new AsyncRelayCommand(PlayVideoAsync);
            extractButtonText = "Extract Message";
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
            if(IsProcessing)
            {
                return;
            }
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
                    ErrorMessage = Services.Extraction.ExtractMessageFromIFrames(NewVideoIframes, decryptionPassword);
                    Directory.Delete(NewVideoIframes,true);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
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
            }
        }

    }
}
