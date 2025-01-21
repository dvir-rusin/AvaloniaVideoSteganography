using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.ViewModels;

namespace AvaloniaLsbProject1.Views
{

    public partial class StreamVideoView : UserControl
    {
        public StreamVideoView()
        {
            InitializeComponent();
            DataContext = new StreamVideoViewModel();
            BackButton.Click += OnBackButtonClick;

        }
        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.NavigateToHome();
        }
    }



}

