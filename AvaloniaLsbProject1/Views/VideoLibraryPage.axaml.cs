using Avalonia.Controls;
using AvaloniaLsbProject1.ViewModels;

namespace AvaloniaLsbProject1.Views
{
    public partial class VideoLibraryPage : UserControl
    {
        public VideoLibraryPage()
        {
            InitializeComponent();
            DataContext = new VideoLibraryViewModel();
        }
    }
}
