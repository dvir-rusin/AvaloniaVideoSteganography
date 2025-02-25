using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Xabe.FFmpeg;

namespace AvaloniaLsbProject1.Services
{
    internal class HelperFunctions
    {

        public static string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in data)
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        public static string BinaryToString(string binaryData)
        {
            var sb = new StringBuilder();

            // Process each 8 bits (1 byte) at a time
            for (int i = 0; i < binaryData.Length; i += 8)
            {
                // Take 8 bits and convert them to a character
                string byteString = binaryData.Substring(i, 8);

                //add a decryption method for the extracted bits later on 

                int asciiValue = Convert.ToInt32(byteString, 2);
                sb.Append((char)asciiValue);
            }

            return sb.ToString();
        }

        public static int extractNumberFromFilePath(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            // Regular expression to extract the number
            string pattern = @"frame_(\d{4})";
            Match match = Regex.Match(fileName, pattern);
            int number;
            string numberString;
            if (match.Success)
            {
                // Extract the number and parse it as an integer
                numberString = match.Groups[1].Value;
                number = int.Parse(numberString);

                Console.WriteLine($"Extracted number: {number}");
            }
            else
            {
                Console.WriteLine("No number found in the file name.");
            }
            numberString = match.Groups[1].Value;
            number = int.Parse(numberString);
            return number;
        }


        /// <summary>
        /// Reconstructs a video from frames stored in a folder and compresses it using H.264.
        /// </summary>
        /// <param name="framesPath">Path to the folder containing the video frames.</param>
        /// <param name="outputFilePath">Path to save the reconstructed video file.</param>
        /// <param name="frameRate">Frame rate for the output video (e.g., 30).</param>
        public static void ReconstructVideo(string framesPath, string outputFilePath, int frameRate)
        {
            // Check for FFmpeg availability
            string ffmpegPath = "ffmpeg"; // Assumes ffmpeg is in system PATH
            string inputPattern = $"{framesPath}\\frame_%04d.png"; // Adjust for the frame naming format (e.g., frame_0001.png)

            // FFmpeg command to reconstruct and compress video
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libx264  -qp 0 -preset ultrafast -an -x264-params \"intra-refresh=0:intra-block-copy=1\" \"{outputFilePath}\"";
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v ffv1 \"{outputFilePath}\"";
            //"/C ffmpeg -i C:\\VideoSteganography\\allCombinedFrames\\%06d.bmp -pix_fmt bgr24 -c:v libx264rgb -preset veryslow -qp 0 C:\\VideoSteganography\\stegovideo.avi";
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v copy \"{outputFilePath}\"";
            string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -pix_fmt bgr24 -c:v libx264rgb -preset veryfast -qp 0 \"{outputFilePath}\"";



            try
            {
                // Configure the process
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process
                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                    Console.WriteLine("Starting video reconstruction...");
                    process.Start();

                    // Begin async reading of output/error
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.WriteLine("Video reconstruction completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reconstructing the video: {ex.Message}");
            }

        }

        public static void PlayVideo(string videoPath, int windowWidth = 640, int windowHeight = 360)
        {
            // Path to ffplay executable
            string ffplayPath = @"C:\ffmpeg\bin\ffplay.exe"; 

            // Command arguments with window size
            string arguments = $"-i \"{videoPath}\" -x {windowWidth} -y {windowHeight}";

            Process ffplayProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffplayPath, // Use ffplay instead of ffmpeg
                    Arguments = arguments,
                    UseShellExecute = false, // Do not use the shell to execute the process
                    RedirectStandardOutput = false, // We do not need to capture the output
                    RedirectStandardError = false,  // We do not need to capture the error output
                    CreateNoWindow = false // Allow ffplay to open its own playback window
                }
            };

            try
            {
                ffplayProcess.Start();
                Console.WriteLine("Playing video...");
                ffplayProcess.WaitForExit(); // Wait for the process to finish
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                ffplayProcess.Close();
                
            }
        }

        public static void ReconstructVideoWithQuality(string framesFolder, string outputPath, double frameRate, int qualityFactor, string metadataSource = null)
        {
            // Create a temporary script file
            string scriptPath = Path.Combine(Path.GetTempPath(), "ffmpeg_concat.txt");

            try
            {
                // Get all frames and sort them
                var frameFiles = Directory.GetFiles(framesFolder, "*.png")
                                         .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                                         .ToArray();

                if (frameFiles.Length == 0)
                {
                    throw new Exception("No frames found in the specified folder.");
                }

                // Build FFmpeg command
                var ffmpegArgs = $"-framerate {frameRate} -i \"{framesFolder}/%d.png\" -c:v libx264 -crf {qualityFactor} -pix_fmt yuv420p";

                // Add metadata copying if specified
                if (!string.IsNullOrEmpty(metadataSource))
                {
                    ffmpegArgs += $" -map_metadata 0 -movflags use_metadata_tags";
                }

                ffmpegArgs += $" \"{outputPath}\"";

                // Execute FFmpeg command
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        throw new Exception($"FFmpeg exited with code {process.ExitCode}: {error}");
                    }
                }
            }
            finally
            {
                // Clean up
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
        }

    }
}
