using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace AvaloniaLsbProject1.Services
{
    internal class Extraction
    {
       
        
        public static async Task ExtractAllFrames(string videoFilePath, string outputDirectory,double FPS)
        {

            FFmpeg.SetExecutablesPath("C:/ffmpeg/bin"); // Change to your FFmpeg path

            // Define the output pattern for frame images
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            string outputPattern = Path.Combine(outputDirectory, "frame_%04d.png");

            // Create the conversion with input and output
            var conversion = FFmpeg.Conversions.New()
               .AddParameter($"-i \"{videoFilePath}\"") // Input file path
               .AddParameter($" -vf fps=\"{FPS}\"") // Extract 30 frame per second (adjust as needed
               .AddParameter("-vsync vfr")  // Avoid duplicate frames
               .SetOutput(outputPattern);

            Console.WriteLine("Extracting all frames...");

            // Execute the conversion
            await conversion.Start();

            Console.WriteLine("All frames have been extracted and saved to the output directory.");
        }

        public static async Task ExtractIFrames(string videoFilePath, string outputDirectory)
        {

            FFmpeg.SetExecutablesPath("C:/ffmpeg/bin");   

            // output pattern for frame images
            string outputPattern = Path.Combine(outputDirectory, "frame_%04d.png");

            // Create the conversion with input and output
            var conversion = FFmpeg.Conversions.New()
               .AddParameter($"-i \"{videoFilePath}\"")           // Input file path
               .AddParameter("-vf select='eq(pict_type,I)'")      // Filter for only I-frames
               .AddParameter("-vsync vfr")                        // Avoid duplicate frames
               .SetOutput(outputPattern);

            Console.WriteLine("Extracting I-frames...");

            // Execute the conversion
            await conversion.Start();

            Console.WriteLine("All I-frames have been extracted and saved to the output directory.");
        }

        public static string ExtractMessageFromIFrames(string IframeDirectory,string password , ref string sucssesOrErorr)
        {
           
            string firstFramePath = Directory.GetFiles(IframeDirectory, "*.png").FirstOrDefault();
            
            for(int i = 0; firstFramePath == null&& i < 20;i++)
            {
                //added delay so it does not look for an I frame not existed yet
                firstFramePath = Directory.GetFiles(IframeDirectory, "*.png").FirstOrDefault();
                Thread.Sleep(50);
                  
            }

           
            if (firstFramePath == null)
            {
                sucssesOrErorr = "Error";
                return "No frames found in the specified folder.";
            }

            try
            {
                foreach (string filePath in Directory.GetFiles(IframeDirectory, "*.png"))
                {

                    string message;
                    
                    using (Bitmap frameBitmap = new Bitmap(filePath))
                    {
                        string result = GetHiddenMessage(frameBitmap, password, ref sucssesOrErorr);

                        // If we successfully extracted a message, return it
                        if (sucssesOrErorr == "Success")
                        {
                            return result;
                        }
                    }

                }
                // If we checked all frames but found no message
                sucssesOrErorr = "Error";
                return "No valid message found in any of the video frames.";
            }

            catch (Exception ex)
            {
                sucssesOrErorr = "Error";
                return $"Error extracting message: {ex.Message}";
            }
            

        }

        

        private static string GetHiddenMessage(Bitmap frameBitmap,string password,ref string sucssesOrErorr)
        {

            StringBuilder binaryMessage = new StringBuilder();
            int messageBitsExtracted = 0;
            string hiddenMessage = "null";
            bool containsMessage = true;
            bool exit = false;

            // Check if the frame contains a message by examining marker pixels
            
                
            if (
                (frameBitmap.GetPixel(frameBitmap.Width - 1, frameBitmap.Height - 1) != System.Drawing.Color.FromArgb(254, 1, 1))
                && (frameBitmap.GetPixel(frameBitmap.Width - 2, frameBitmap.Height - 1) != System.Drawing.Color.FromArgb(1, 254, 1))
                    && (frameBitmap.GetPixel(frameBitmap.Width - 3, frameBitmap.Height - 1) != System.Drawing.Color.FromArgb(1, 1, 254))
                && (frameBitmap.GetPixel(frameBitmap.Width - 4, frameBitmap.Height - 1) != System.Drawing.Color.FromArgb(1, 1, 1))
            )
            {
                containsMessage = false;
                sucssesOrErorr = "Error";
                return "This video does not contain a message";
            }



            // Start reading the pixels
            for (int i = 0; i < frameBitmap.Width * frameBitmap.Height && containsMessage && !exit; i++)
            {
                int x = i % frameBitmap.Width;
                int y = i / frameBitmap.Width;
                System.Drawing.Color pixelColor = frameBitmap.GetPixel(x, y);

                // Extract LSBs from each color channel using a switch
                for (int channel = 0; channel < 3 && !exit; channel++) // 0 = R, 1 = G, 2 = B
                {
                    int bit = ExtractBitFromPixel(pixelColor, channel);
                    binaryMessage.Append(bit == 1 ? "1" : "0");
                    messageBitsExtracted++;

                    if (messageBitsExtracted % 8 == 0)
                    {
                        hiddenMessage = HelperFunctions.BinaryToString(binaryMessage.ToString());

                        if (NullCheck(hiddenMessage, messageBitsExtracted))
                        {
                            hiddenMessage = hiddenMessage.Remove(hiddenMessage.Length - 1); // remove \0
                            exit = true;
                        }
                    }
                }
            }

            #if debug
            Console.WriteLine("Binary Message: " + binaryMessage);
            #endif

            string decrypted = "This video does not contain a message.";
            if (containsMessage)
            {
                try
                {
                    decrypted = "This video contains a message but was unable to decrypt it.";
                    decrypted = EncryptionAes.Decrypt(hiddenMessage, password);
                    Console.WriteLine($"Decrypted Text: {decrypted}");
                    sucssesOrErorr = "Success";
                    return decrypted;
                }
                catch
                {
                    sucssesOrErorr = "Error";
                    return "Decryption failed. Check your custom key.";
                }
            }

            return "This video does not contain a message.";
        }

        /// <summary>
        /// Extracts the LSB from a specific color channel of a pixel.
        /// </summary>
        /// <param name="color">The pixel color.</param>
        /// <param name="channel">0 = R, 1 = G, 2 = B</param>
        /// <returns>0 or 1 depending on the LSB.</returns>
        private static int ExtractBitFromPixel(System.Drawing.Color color, int channel)
        {
            return channel switch
            {
                0 => color.R & 1, // Red
                1 => color.G & 1, // Green
                2 => color.B & 1, // Blue
                _ => throw new ArgumentOutOfRangeException(nameof(channel), "Invalid color channel index")
            };
        }

        //this functions prints the last 4 pixels of the image
        public static void GetPixelColor(string filePath)
        {
            
            foreach (string file in Directory.GetFiles(filePath, "*.png"))
            {
                
                Bitmap frameBitmap = new Bitmap(file);
                using (frameBitmap)
                {

                    for (int i = 1; i < 5; i++)
                    {

                        Console.WriteLine(frameBitmap.GetPixel(frameBitmap.Width - i, frameBitmap.Height - i));
                    }
                }
            }



        }

        //this functions checks if the last character in the hidden message is a null character
        private static bool NullCheck(string HiddenMsg, int messageBitsExtracted)
        {


            if (HiddenMsg[HiddenMsg.Length - 1] == char.MinValue)
            {
                return true;
            }
            return false;


        }


        public static async Task ExtractFrameMetadata(string videoFilePath, string metadataFile)
        {
            // Set the FFmpeg executables path
            FFmpeg.SetExecutablesPath("C:/ffmpeg/bin"); // Adjust this path as necessary

            // Use ffprobe to extract frame metadata
            var ffprobeCommand = $"ffprobe -v error -i \"{videoFilePath}\" -select_streams v:0 -show_entries frame=pict_type -of csv=p=0 > \"{metadataFile}\"";


            Console.WriteLine("Extracting frame metadata...");
            await RunCommand(ffprobeCommand);
            Console.WriteLine($"Frame metadata saved to {metadataFile}");
        }

        private static async Task RunCommand(string command)
        {
            var processStartInfo = new ProcessStartInfo("cmd", "/C " + command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            await process.WaitForExitAsync();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine("Error: " + error);
            }
            else
            {
                Console.WriteLine(output);
            }
        }





        public static async Task<int[]> GetIFrameLocations(string metadataFile)
        {
            int retryCount = 5;
            int delay = 500;

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    string[] lines = await File.ReadAllLinesAsync(metadataFile);
                    List<int> iFrameLocations = new List<int>();

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string frameType = lines[i].Trim();
                        if (frameType == "I" || frameType == "I,")
                        {
                            iFrameLocations.Add(i + 1);
                        }
                    }

                    Console.WriteLine($"I-Frame Locations: {string.Join(", ", iFrameLocations)}");
                    return iFrameLocations.ToArray();
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    Console.WriteLine($"Attempt {attempt}/{retryCount}: File is in use. Retrying in {delay}ms...");
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error reading metadata file: {ex.Message}");
                    return Array.Empty<int>();
                }
            }

            Console.WriteLine($"Failed to read metadata file after {retryCount} attempts.");
            return Array.Empty<int>();
        }



    }
}
