using System;
using System.Windows.Input;
using Avalonia.Controls;

namespace AvaloniaLsbProject1.ViewModels
{
    public class MainWindowViewModel
    {
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToEmbedCommand { get; }
        public ICommand NavigateToExtractCommand { get; }
        public ICommand NavigateToStreamCommand { get; }

        private readonly ContentControl _contentArea;

        public MainWindowViewModel(ContentControl contentArea)
        {
            _contentArea = contentArea;

            NavigateToHomeCommand = new RelayCommand(_ => NavigateToHome());
            NavigateToEmbedCommand = new RelayCommand(_ => NavigateToEmbed());
            NavigateToExtractCommand = new RelayCommand(_ => NavigateToExtract());
            NavigateToStreamCommand = new RelayCommand(_ => NavigateToStream());
        }

        private void NavigateToHome()
        {
            _contentArea.Content = new Views.HomeView();
        }

        private void NavigateToEmbed()
        {
            _contentArea.Content = new Views.EmbedView();
        }

        private void NavigateToExtract()
        {
            _contentArea.Content = new Views.ExtractView();
        }

        private void NavigateToStream()
        {
            _contentArea.Content = new Views.StreamVideoView();
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
