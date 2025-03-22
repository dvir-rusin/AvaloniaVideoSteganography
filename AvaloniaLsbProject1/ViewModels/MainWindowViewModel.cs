using System;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

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

        public MainWindowViewModel(ContentControl contentArea)
        {
            _contentArea = contentArea;
            

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
            _contentArea.Content = new Views.EmbedView();
            CurrentPage = page;

        }

        private void NavigateToExtract(string page)
        {
            _contentArea.Content = new Views.ExtractView();
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
