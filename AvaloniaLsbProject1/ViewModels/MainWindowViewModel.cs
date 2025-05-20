using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaLsbProject1.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Xabe.FFmpeg.Exceptions;
using System.ComponentModel;

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
        public ICommand NavigateToLibraryCommand { get; }


        private readonly ContentControl _contentArea;

        private readonly byte[]? _sharedKey;
        private readonly string _role;

        [ObservableProperty]
        private bool _isUnlocked;

        private readonly VideoLibraryViewModel _libraryVm;

        public MainWindowViewModel(ContentControl contentArea, byte[]? sharedKey, string role)
        {
            _contentArea = contentArea;
            _sharedKey = sharedKey;
            _role = role;

            // 1) Create library VM once and listen for unlock
            _libraryVm = new VideoLibraryViewModel();
            _libraryVm.PropertyChanged += LibraryVm_PropertyChanged;
            _libraryVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(VideoLibraryViewModel.IsUnlocked))
                    IsUnlocked = _libraryVm.IsUnlocked;
             };

            // 2) Commands
            NavigateToHomeCommand = new RelayCommand(_ => NavigateToHome());
            NavigateToLibraryCommand = new RelayCommand(_ => NavigateToLibrary());

            // Embed/Extract/Stream only need the file to exist
            NavigateToEmbedCommand = new RelayCommand(_ => NavigateToEmbed(), _ => _libraryVm.IsMasterKeySet);
            NavigateToExtractCommand = new RelayCommand(_ => NavigateToExtract(), _ => _libraryVm.IsMasterKeySet);
            NavigateToStreamCommand = new RelayCommand(_ => NavigateToStream(), _ => _libraryVm.IsMasterKeySet);

            // 3) start on Library page if no master key exists
            if (!_libraryVm.IsMasterKeySet)
                NavigateToLibrary();
            else
                CurrentPage = "Home";
        }

        private void LibraryVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VideoLibraryViewModel.IsMasterKeySet))
            {
                // re query nav button enabled state
                (NavigateToEmbedCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NavigateToExtractCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NavigateToStreamCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            else if (e.PropertyName == nameof(VideoLibraryViewModel.IsUnlocked))
            {
                // flow unlock
                IsUnlocked = _libraryVm.IsUnlocked;
            }
        }

        partial void OnIsUnlockedChanged(bool unlocked)
        {
            (NavigateToEmbedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NavigateToExtractCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NavigateToStreamCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async void GuardedNavigate(Action navigateAction, string lockedMessage)
        {
            if (!IsUnlocked)
            {
                await ShowLockedDialog(lockedMessage);
                NavigateToLibrary();
                return;
            }
            navigateAction();
        }

        
        public async Task ShowLockedDialog(string text)
        {
            // find the main window
            var parent = (_contentArea.GetVisualRoot() as Window)
                      ?? throw new InvalidOperationException("No parent window found");

            var dialog = new Window
            {
                Title = "Locked",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            await dialog.ShowDialog(parent);
        }


        private void NavigateToHome()
        {
            _contentArea.Content = new HomeView();
            CurrentPage = "Home";
        }

        private void NavigateToEmbed()
        {
            _contentArea.Content = new EmbedView(_sharedKey, _role);
            CurrentPage = "Embed";
        }

        private void NavigateToExtract()
        {
            _contentArea.Content = new ExtractView(_sharedKey, _role);
            CurrentPage = "Extract";
        }

        private void NavigateToStream()
        {
            _contentArea.Content = new StreamVideoView();
            CurrentPage = "Stream";
        }

        private void NavigateToLibrary()
        {
            // bind the same VM instance so its state is preserved
            var page = new VideoLibraryPage { DataContext = _libraryVm };
            _contentArea.Content = page;
            CurrentPage = "Library";
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute,
                            Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
            => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
            => _execute(parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }


}
