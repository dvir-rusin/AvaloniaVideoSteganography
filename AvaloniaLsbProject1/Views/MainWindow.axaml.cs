using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaLsbProject1.Views;
using AvaloniaLsbProject1;
using AvaloniaLsbProject1.ViewModels;
using AvaloniaLsbProject1.Services;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AvaloniaLsbProject1.Views
{
    /// <summary>
    /// Represents the main application window for the Video Steganography project.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets the current instance of the MainWindow.
        /// </summary>
        public static MainWindow? Instance { get; private set; }

        /// <summary>
        /// Gets the broadcaster instance of the MainWindow (if applicable).
        /// </summary>
        public static MainWindow? BroadcasterInstance { get; private set; }

        /// <summary>
        /// Gets the listener instance of the MainWindow (if applicable).
        /// </summary>
        public static MainWindow? ListenerInstance { get; private set; }

        /// <summary>
        /// Gets the role of the current window instance (e.g., "Listener" or "Broadcaster").
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// Gets the key exchange manager for performing Diffie-Hellman key exchange.
        /// </summary>
        public KeyExchangeManager KeyExchange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class with the specified role.
        /// </summary>
        /// <param name="role">The role of the window ("Listener" or "Broadcaster").</param>
        public MainWindow(string role)
        {
            Instance = this; // Set the static instance for use elsewhere

            InitializeComponent();
            Title = $"Video Steganography - {role}";
            Role = role;
            KeyExchange = new KeyExchangeManager();

            // Load the HomeView as the initial content
            ContentArea.Content = new HomeView();

            // Set the window icon.
            this.Icon = new WindowIcon("C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\steganographyreactivedemo.ico");

            // Call async initializer when the window has loaded.
            Loaded += async (_, _) =>
            {
                await PerformKeyExchangeAsync();
                DataContext = new MainWindowViewModel(ContentArea, KeyExchange.SharedKey, Role);
            };
            // Load configuration for project paths.
            var config = LoadProjectConfig();
            string projectPath = config.BaseProjectPath;

            CheckForDeletedVideosInProjectPath(projectPath);

        }

        private ProjectPathsConfig LoadProjectConfig()
        {
            // Adjust the JSON file path as necessary.
            return ProjectPathsLoader.LoadConfig("C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\Json\\projectPaths.json");
        }


        public void CheckForDeletedVideosInProjectPath(string projectPath)
        {
            // Define the storage file path.
            string storageFile = "C:\\Projects\\gitGames\\AvaloniaLsbProject1\\AvaloniaLsbProject1\\Json\\VideoKeyStorage.json";

            // If the storage file doesn't exist, there's nothing to check.
            if (!File.Exists(storageFile))
            {
                return;
            }

            // Load the dictionary from the JSON file.
            string json = File.ReadAllText(storageFile);
            Dictionary<string, string> videoKeyDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                                                       ?? new Dictionary<string, string>();

            // List to hold keys for which the video file no longer exists.
            List<string> keysToRemove = new List<string>();

            // Iterate over each key-value pair.
            foreach (var kvp in videoKeyDict)
            {
                // Build the full path for the video file.
                string videoFilePath = Path.Combine(projectPath, kvp.Value);

                // If the file doesn't exist, mark the key for removal.
                if (!File.Exists(videoFilePath))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // Remove entries for which the video file was not found.
            foreach (var key in keysToRemove)
            {
                videoKeyDict.Remove(key);
            }

            // Save the updated dictionary back to the JSON file.
            string newJson = JsonConvert.SerializeObject(videoKeyDict, Formatting.Indented);
            File.WriteAllText(storageFile, newJson);
        }

        /// <summary>
        /// Performs the Diffie-Hellman key exchange asynchronously based on the current role.
        /// For a listener, it waits for a public key, generates the shared key, and sends its own public key.
        /// For a broadcaster, it sends its public key, waits for the listener's public key, and then generates the shared key.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

                // Also get the listener's public key (simulate round-trip for bidirectional key exchange)
                var otherPublicKey = await KeyExchangeService.ListenForPublicKeyAsync(Role);
                KeyExchange.GenerateSharedKey(otherPublicKey);
                Console.WriteLine("Broadcaster Shared Key: " + Convert.ToBase64String(KeyExchange.SharedKey!));
            }
        }

        /// <summary>
        /// Handles the click event for the Embed button, navigating to the EmbedView.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        public void OnEmbedButtonClick(object sender, RoutedEventArgs e)
        {
            var embedView = new EmbedView(KeyExchange.SharedKey, Role);
            ContentArea.Content = embedView;
        }

        /// <summary>
        /// Handles the click event for the Extract button, navigating to the ExtractView.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        public void OnExtractButtonClick(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ExtractView(KeyExchange.SharedKey, Role);
        }

        /// <summary>
        /// Navigates to the HomeView.
        /// </summary>
        public void NavigateToHome()
        {
            ContentArea.Content = new HomeView();
        }

        /// <summary>
        /// Navigates to the StreamVideoView.
        /// </summary>
        public void NavigateToStreamVideoView()
        {
            ContentArea.Content = new StreamVideoView();
        }
    }
}
