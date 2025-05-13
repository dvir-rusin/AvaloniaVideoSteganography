using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AvaloniaLsbProject1.ViewModels;
using AvaloniaLsbProject1.Views;
using System.IO;
using System;
using System.Linq;
using AvaloniaLsbProject1.Services;
using Newtonsoft.Json;


namespace AvaloniaLsbProject1
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public record ProjectPaths(
            string ProjectPath,
            string AllFramesFolder,
            string AllFramesWithMessageFolder,
            string MetaDataFile

        );


        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Load the configuration using a relative path to the Json folder
                string configPath = Path.Combine(AppContext.BaseDirectory, "Json", "projectPaths.json");
                var config = ProjectPathsLoader.LoadConfig(configPath);
                string projectPath = config.BaseProjectPath;

                // Set up project paths
                ProjectPaths paths = SetupProjectPaths(projectPath, config);

                // Optional: Ensure that the app shuts down when the last window closes.
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

                // Subscribe to the desktop lifetime Exit event.
                desktop.Exit += (sender, e) =>
                {
                    DeleteDirectoryAndFiles(paths.AllFramesWithMessageFolder, paths.AllFramesFolder, paths.MetaDataFile);
                };
                // Subscribe to the Exit event to perform cleanup when the app shuts down.
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    DeleteDirectoryAndFiles(paths.AllFramesWithMessageFolder, paths.AllFramesFolder, paths.MetaDataFile);
                };

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                // Initialize MainWindow without passing contentArea
                var listener = new MainWindow("Listener");

                listener.Show();

                var broadcaster = new MainWindow("Broadcaster");
                broadcaster.Show();


            }

            base.OnFrameworkInitializationCompleted();
        }

        private ProjectPaths SetupProjectPaths(string projectPath, ProjectPathsConfig config)
        {
            return new ProjectPaths(
                projectPath,
                Path.Combine(projectPath, config.Paths.AllFramesFolder),
                Path.Combine(projectPath, config.Paths.AllFramesWithMessageFolder),
                Path.Combine(projectPath, config.Paths.MetaDataFile)


            );
        }



        private void DeleteDirectoryAndFiles(string allFramesWithMessageFolder, string allFramesFolder, string metaDataFile)
        {
            try
            {
                if (Directory.Exists(allFramesWithMessageFolder))
                {
                    Directory.Delete(allFramesWithMessageFolder, true);
                }
                if (Directory.Exists(allFramesFolder))
                {
                    Directory.Delete(allFramesFolder, true);
                }
                if (File.Exists(metaDataFile))
                {
                    File.Delete(metaDataFile);
                }

            }
            catch (Exception ex)
            {
                // Log the exception if necessary.
                Console.WriteLine($"Error during deletion: {ex.Message}");
            }
        }


        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}