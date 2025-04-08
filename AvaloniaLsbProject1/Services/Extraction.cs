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
        // Example usage of the ExtractIFrames function
        //string inputFilePath = "C:/Users/user 2017/Videos/WireShark/ArcVideo_clip.ts";
        //string outputDirectory = "C:/Users/user 2017/Videos/WireShark/LSBextractedFrames";
        
        public static async Task ExtractAllFrames(string videoFilePath, string outputDirectory,double FPS)
        {
            // Set the FFmpeg path if not already set
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

        public  static async Task ExtractIFrames(string videoFilePath, string outputDirectory)
        {
            // Set the FFmpeg path if not already set
            FFmpeg.SetExecutablesPath("C:/ffmpeg/bin"); // Change to your FFmpeg path  

            // Define the output pattern for frame images
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
                    Bitmap frameBitmap = new Bitmap(filePath);
                    using (frameBitmap)
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
            System.Drawing.Color pixelColor;
            string HiddenMsg = "null";
            bool doesContainMessage = true;
            bool exit = false;

            // Check if the frame contains a message by examining marker pixels
            for (int i = 1;i<=4&& !exit; i++)
            {
                
                if (frameBitmap.GetPixel(frameBitmap.Width - i, frameBitmap.Height - 1) != System.Drawing.Color.FromArgb(254, 1, 1))
                {
                    doesContainMessage = false;
                    sucssesOrErorr = "Error";
                    return "This video does not contain a message";
                }
            }
            
            for (int i = 0; i < frameBitmap.Width * frameBitmap.Height && doesContainMessage && !exit; i++)
            {
                int x = i % frameBitmap.Width;
                int y = i / frameBitmap.Width;
                pixelColor = frameBitmap.GetPixel(x, y);

                // Extract the LSB from each color channel
                binaryMessage.Append((pixelColor.R & 1) == 1 ? "1" : "0");
                messageBitsExtracted++;
                if (messageBitsExtracted % 8 == 0)
                {
                    HiddenMsg = HelperFunctions.BinaryToString(binaryMessage.ToString());
                    if (NullCheck(HiddenMsg, messageBitsExtracted) == true)
                    {
                        HiddenMsg = HiddenMsg.Remove(HiddenMsg.Length - 1);
                        exit = true;
                    }

                }
                if(!exit)
                {
                    // Extract LSB from Green channel
                    binaryMessage.Append((pixelColor.G & 1) == 1 ? "1" : "0");
                    messageBitsExtracted++;
                    if (messageBitsExtracted % 8 == 0)
                    {
                        HiddenMsg = HelperFunctions.BinaryToString(binaryMessage.ToString());
                        if (NullCheck(HiddenMsg, messageBitsExtracted) == true)
                        {
                            HiddenMsg = HiddenMsg.Remove(HiddenMsg.Length - 1);
                            exit = true;
                        }

                    }
                }

                if (!exit)
                {
                    // Extract LSB from Blue channel
                    binaryMessage.Append((pixelColor.B & 1) == 1 ? "1" : "0");
                    messageBitsExtracted++;
                    if (messageBitsExtracted % 8 == 0)
                    {
                        HiddenMsg = HelperFunctions.BinaryToString(binaryMessage.ToString());
                        if (NullCheck(HiddenMsg, messageBitsExtracted) == true)
                        {
                            HiddenMsg = HiddenMsg.Remove(HiddenMsg.Length - 1);
                            exit = true;
                        }

                    }
                }
                    

            }
            //binary message still includes the /0 at the end the one that was removed from each hidden message if statement 
            Console.WriteLine("Binary Message: " + binaryMessage);
            //HiddenMsg = HelperFunctions.BinaryToString(binaryMessage.ToString());

            Console.WriteLine("\nEnter the custom key to decrypt:");
            string inputKey = password; // Read the custom key from the user for decryption
            string decrypted = "this video does not contain a message ";
            if (doesContainMessage == true)
            {
                try
                {
                    decrypted = "this video contains a message was unable to decrypt ";
                    // Attempt to decrypt the ciphertext with the user-provided key
                    decrypted = EncryptionAes.Decrypt(HiddenMsg, inputKey);
                    Console.WriteLine($"Decrypted Text: {decrypted}");
                    // Successful extraction
                    sucssesOrErorr = "Success";
                    return decrypted;
                    
                }
                catch
                {
                    // Handle decryption failure (e.g., incorrect custom key)
                    sucssesOrErorr = "Error";
                    return "Decryption failed. Check your custom key.";

                }
            }
            return "This video does not contain a message";
            //string extractedMessage = lsbExtractIFramesProject.HelperFunctions.BinaryToString(binaryMessage.ToString());
            //Console.WriteLine($"Extracted Message: {extractedMessage}");

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
            List<int> iFrameLocations = new List<int>();
            int retryCount = 5; // Number of retries
            int delay = 500;    // Delay in milliseconds between retries

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    // Read the metadata file line by line
                    string[] lines = await File.ReadAllLinesAsync(metadataFile);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        // Each line should correspond to a frame's pict_type
                        string frameType = lines[i].Trim();

                        // Check if the frame type is "I"  or "I," (for CSV format)
                        if (frameType == "I,")
                        {
                            // Add the frame number to the array (1-based indexing)
                            iFrameLocations.Add(i + 1);
                        }
                        else if (frameType == "I")
                        {
                            // Add the frame number to the array (1-based indexing)
                            iFrameLocations.Add(i + 1);
                        }
                    }

                    // Output the locations for debugging purposes
                    Console.Write($"I-Frame Locations: {string.Join(", ", iFrameLocations)}");
                    break; // Exit the loop if successful
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    Console.WriteLine($"Attempt {attempt}/{retryCount}: File is in use. Retrying in {delay}ms...");
                    await Task.Delay(delay);

                    if (attempt == retryCount)
                    {
                        Console.WriteLine($"Error reading metadata file after {retryCount} attempts: {ex.Message}");
                        return Array.Empty<int>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error reading metadata file: {ex.Message}");
                    return Array.Empty<int>();
                }
            }

            return iFrameLocations.ToArray();
        }


    }
}
