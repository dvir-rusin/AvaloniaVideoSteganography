using System;
using System.IO;
using Newtonsoft.Json;

namespace AvaloniaLsbProject1.Services
{
    /// <summary>
    /// Represents the configuration for project paths.
    /// </summary>
    public class ProjectPathsConfig
    {
        /// <summary>
        /// Gets or sets the base project path.
        /// </summary>
        public string BaseProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the sub-paths used in the project.
        /// </summary>
        public ProjectPaths Paths { get; set; }
    }

    /// <summary>
    /// Encapsulates the sub-paths for the project.
    /// </summary>
    public class ProjectPaths
    {
        public string AllFramesFolder { get; set; }
        public string AllFramesWithMessageFolder { get; set; }
        public string MetaDataFile { get; set; }
        public string NewVideoIframes { get; set; }
        public string NewVideo { get; set; }
    }

    /// <summary>
    /// A static helper class to load the project paths configuration from a JSON file.
    /// </summary>
    public static class ProjectPathsLoader
    {
        /// <summary>
        /// Loads the project paths configuration from the specified JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON configuration file.</param>
        /// <returns>An instance of <see cref="ProjectPathsConfig"/> containing the project paths.</returns>
        public static ProjectPathsConfig LoadConfig(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("The project paths configuration file was not found.", jsonFilePath);
            }

            string json = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<ProjectPathsConfig>(json)
                ?? throw new Exception("Failed to deserialize the project paths configuration.");
        }
    }
}
