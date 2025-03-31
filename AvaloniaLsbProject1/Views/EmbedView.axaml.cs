using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.ViewModels;

namespace AvaloniaLsbProject1.Views
{
    public partial class EmbedView : UserControl
    {
        public EmbedView(byte[]? sharedKey,string role)
        {
            InitializeComponent();
            DataContext = new EmbedViewModel(sharedKey, role);
            // Attach the Click event handler
            BackButton.Click += OnBackButtonClick;
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.NavigateToHome();
        }
    }
}
