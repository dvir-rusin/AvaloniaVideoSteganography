using System;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Xabe.FFmpeg.Exceptions;

namespace AvaloniaLsbProject1.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {
        [ObservableProperty]
        private string _currentPage;

        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToEmbedCommand { get; }
        public ICommand NavigateToExtractCommand { get; }
        public ICommand NavigateToStreamCommand { get; }

        private readonly ContentControl _contentArea;

        private readonly byte[]? _sharedKey;
        private readonly string _role;

        public MainWindowViewModel(ContentControl contentArea, byte[]? sharedKey, string role)
        {
            _contentArea = contentArea;
            _sharedKey = sharedKey;
            _role = role;

            NavigateToHomeCommand = new RelayCommand(_ => NavigateToHome("Home"));
            NavigateToEmbedCommand = new RelayCommand(_ => NavigateToEmbed("Embed"));
            NavigateToExtractCommand = new RelayCommand(_ => NavigateToExtract("Extract"));
            NavigateToStreamCommand = new RelayCommand(_ => NavigateToStream("Stream"));

            CurrentPage = "Home";
        }

        private void NavigateToHome(string page)
        {
            _contentArea.Content = new Views.HomeView();
            CurrentPage = page;
        }

        private void NavigateToEmbed(string page)
        {
            _contentArea.Content = new Views.EmbedView(_sharedKey,_role);
            CurrentPage = page;

        }

        private void NavigateToExtract(string page)
        {
            _contentArea.Content = new Views.ExtractView(_sharedKey, _role);
            CurrentPage = page;
        }

        private void NavigateToStream(string page)
        {
            _contentArea.Content = new Views.StreamVideoView();
            CurrentPage = page;
            
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
