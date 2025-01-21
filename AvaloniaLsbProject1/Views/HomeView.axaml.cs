using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaLsbProject1.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void OnEmbedButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.OnEmbedButtonClick(sender, e);
        }

        private void OnExtractButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.OnExtractButtonClick(sender, e);
        }

        private void OnStreamVideoButtonClick(object sender, RoutedEventArgs e)
        {
            (this.VisualRoot as MainWindow)?.NavigateToStreamVideoView();
        }
    }
}
