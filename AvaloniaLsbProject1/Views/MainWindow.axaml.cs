using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.Views;
using AvaloniaLsbProject1;
using AvaloniaLsbProject1.ViewModels;


namespace AvaloniaLsbProject1.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }


        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            // Load the HomeView as the initial content
            ContentArea.Content = new HomeView();
            DataContext = new MainWindowViewModel(ContentArea);
            this.Icon = new WindowIcon("C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\steganographyreactivedemo.ico");


        }

        // Navigate to the EmbedView
        public void OnEmbedButtonClick(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new EmbedView();
        }

        // Navigate to the ExtractView
        public void OnExtractButtonClick(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ExtractView();
        }
        public void NavigateToHome()
        {
            ContentArea.Content = new HomeView();
        }
        public void NavigateToStreamVideoView()
        {
            ContentArea.Content = new StreamVideoView();
        }


    }
}
