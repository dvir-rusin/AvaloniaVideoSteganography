using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.Views;
using AvaloniaLsbProject1;
using AvaloniaLsbProject1.ViewModels;
using AvaloniaLsbProject1.Services;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;


namespace AvaloniaLsbProject1.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        public static MainWindow? BroadcasterInstance { get; private set; }
        public static MainWindow? ListenerInstance { get; private set; }

        public string Role { get; }
        public KeyExchangeManager KeyExchange { get; }


        public MainWindow(string role)
        {
            InitializeComponent();
            Title = $"Video Steganography - {role}";
            Role = role;
            KeyExchange = new KeyExchangeManager();

            

            
            // Load the HomeView as the initial content
            ContentArea.Content = new HomeView();
            
            this.Icon = new WindowIcon("C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\steganographyreactivedemo.ico");

            // Call async initializer
            Loaded += async (_, _) =>
            {
                await PerformKeyExchangeAsync();

                DataContext = new MainWindowViewModel(ContentArea, KeyExchange.SharedKey, Role);
            };

        }

        
        private async Task PerformKeyExchangeAsync()
        {
            if (Role == "Listener")
            {
                Console.WriteLine("Listener waiting for public key...");
                var otherPublicKey = await KeyExchangeService.ListenForPublicKeyAsync(Role);
                KeyExchange.GenerateSharedKey(otherPublicKey);
                Console.WriteLine("Listener Shared Key: " + Convert.ToBase64String(KeyExchange.SharedKey!));

                // Send listener's public key back to broadcaster
                await KeyExchangeService.SendPublicKeyAsync(KeyExchange.PublicKey, Role);
                Console.WriteLine("Listener public key sent.");
            }

            else if (Role == "Broadcaster")
            {
                await Task.Delay(1000); // Small delay to ensure Listener is ready
                Console.WriteLine("Broadcaster sending public key...");
                await KeyExchangeService.SendPublicKeyAsync(KeyExchange.PublicKey, Role);
                Console.WriteLine("Broadcaster public key sent.");

                // Also get the listener's public key (simulate round-trip for bidirectional)
                var otherPublicKey = await KeyExchangeService.ListenForPublicKeyAsync(Role);
                KeyExchange.GenerateSharedKey(otherPublicKey);
                Console.WriteLine("Broadcaster Shared Key: " + Convert.ToBase64String(KeyExchange.SharedKey!));
            }
        }

        // Navigate to the EmbedView
        public void OnEmbedButtonClick(object sender, RoutedEventArgs e)
        {
            var embedView = new EmbedView(KeyExchange.SharedKey,Role);
            ContentArea.Content = embedView;
        }

        // Navigate to the ExtractView
        public void OnExtractButtonClick(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ExtractView(KeyExchange.SharedKey, Role);
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
