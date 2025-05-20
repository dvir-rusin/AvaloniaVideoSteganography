using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaLsbProject1.Services;
using AvaloniaLsbProject1.ViewModels;
using Xabe.FFmpeg; 

namespace AvaloniaLsbProject1.Services
{
    /// <summary>
    /// Provides methods for embedding messages into video frames.
    /// </summary>
    internal class Embedding
    {
        /// <summary>
        /// Loads the default project paths from the configuration JSON file.
        /// </summary>
        /// <returns>
        /// A tuple containing the input frames directory and the output frames directory.
        /// </returns>
        private static (string inputFramesDirectory, string outputFramesDirectory) GetDefaultPaths()
        {
            // Load the configuration from the JSON file (adjust the path as needed)
            var configFilePath = Path.Combine(AppContext.BaseDirectory, "Json", "projectPaths.json");

            // loadding the JSON ,this gives ProjectPathsLoader.Config object
            var config = ProjectPathsLoader.LoadConfig(configFilePath);

            // Pull out the base project folder and sub folders from that config object
            string basePath = config.BaseProjectPath;
            string inputFramesDirectory = Path.Combine(basePath, config.Paths.AllFramesFolder);
            string outputFramesDirectory = Path.Combine(basePath, config.Paths.AllFramesWithMessageFolder);
            return (inputFramesDirectory, outputFramesDirectory);
        }

        /// <summary>
        /// Embeds an encrypted message into frames contained in a video.
        /// The method uses project paths from the static JSON configuration if the provided directories are not used.
        /// </summary>
        /// <param name="inputframesDirectory">
        /// The directory containing the input frames.
        /// If null or empty, the directory from the JSON configuration will be used.
        /// </param>
        /// <param name="outputframesDirectrory">
        /// The directory where output frames will be saved.
        /// If null or empty, the directory from the JSON configuration will be used.
        /// </param>
        /// <param name="IframesLocation">An array of frame numbers (or identifiers) indicating which frames to embed bits into.</param>
        /// <param name="text">The plaintext message to be embedded.</param>
        /// <param name="password">The password used for AES encryption of the message.</param>
        /// <returns>A string indicating success or an error message.</returns>
        public static string EmbedMessageInFramesInVideo(
            string inputframesDirectory,
            string outputframesDirectrory,
            int[] IframesLocation,
            string text,
            string password,
            string duration,
            string fps,
            double seconds
            
            )
        {
            // Use default paths from JSON config if the provided paths are null or empty.
            if (string.IsNullOrEmpty(inputframesDirectory) || string.IsNullOrEmpty(outputframesDirectrory))
            {
                var defaultPaths = GetDefaultPaths();
                inputframesDirectory = defaultPaths.inputFramesDirectory;
                outputframesDirectrory = defaultPaths.outputFramesDirectory;
            }

            
            
            string UserInputMessage = text; // Read the plaintext input from the user
            string UserPassword = password; // Get the user provided custom key

            // Encrypt the plaintext using the custom key
            string EncryptedMessage = EncryptionAes.Encrypt(UserInputMessage, UserPassword);
            Console.WriteLine($"Encrypted Text: {EncryptedMessage}");

            if (!Directory.Exists(inputframesDirectory))
            {
                return "EmbedMessageInFramesTestInVideo function message: inputframesDirectory does not exist";
                
            }

            // Append a null character to indicate the end of the message
            EncryptedMessage += char.MinValue;
            Console.WriteLine(EncryptedMessage);

            // Convert the encrypted message to its binary representation
            string binaryEncryptedMessage = HelperFunctions.StringToBinary(EncryptedMessage);
            Console.WriteLine("ORIGINAL binaryMessage: " + binaryEncryptedMessage);
            Console.WriteLine("Message : " + HelperFunctions.BinaryToString(binaryEncryptedMessage));

            
            int indexer = 0;

            // Parse duration and fps values
            
            string fpsNumber = fps.Split(' ')[0];
            double doubleFpsNumber = double.Parse(fpsNumber);

            // Calculate the expected frame count
            int expectedFrameCount = (int)Math.Round(doubleFpsNumber * seconds);

            
            double timeoutSeconds = 30;
            DateTime startTime = DateTime.Now;

            // Continuously check until the expected number of PNG files exist
            // Continuously check until the expected number of PNG files exist or timeout is reached
            while ( Directory.GetFiles(inputframesDirectory, "*.png").Length < expectedFrameCount)
            {
                // Check if the timeout has been reached
                if ((DateTime.Now - startTime).TotalSeconds > timeoutSeconds)
                {
                    return ("Timeout reached: expected PNG files were not extracted in time.");
                     
                }

                // Sleep to reduce cpu usage
                Thread.Sleep(100);
            }


            //used to debug
            string ErrorMessage;
            #if debug

            if (Directory.GetFiles(inputframesDirectory, "*.png").Length >= expectedFrameCount)
            {
                ErrorMessage =("Expected PNG files have been extracted.");
            }

            #endif

            if (Directory.GetFiles(inputframesDirectory, "*.png").Length < expectedFrameCount)
            {
                return ("Exiting due to timeout.");
            }

            string[] filePaths = Directory.GetFiles(inputframesDirectory, "*.png");

            Parallel.ForEach(filePaths, filePath =>
            {
                try
                {
                    int numberInFile = HelperFunctions.extractNumberFromFilePath(filePath);
                    bool isIFrame = IframesLocation.Contains(numberInFile);

                    using (Bitmap bitmap = new Bitmap(filePath))
                    {
                        if (isIFrame)
                        {
                            bool messageComplete = false;
                            int messageIndex = 0;
                            int w = bitmap.Width;
                            int h = bitmap.Height;
                            // Process pixels to embed the message
                            for (int y = 0; y < bitmap.Height && !messageComplete; y++)
                            {
                                for (int x = 0; x < bitmap.Width && !messageComplete; x++)
                                {
                                    // Is this one of the 4 signature pixels?
                                    bool isSignaturePixel =
                                        (x == w - 1 && y == h - 1) ||
                                        (x == w - 2 && y == h - 1) ||
                                        (x == w - 3 && y == h - 1) ||
                                        (x == w - 4 && y == h - 1);

                                    if (!isSignaturePixel)
                                    {
                                        Color pixelColor = bitmap.GetPixel(x, y);
                                        int r = pixelColor.R, g = pixelColor.G, b = pixelColor.B;

                                        r = EmbedBitInColorChannel(r, binaryEncryptedMessage, ref messageIndex);
                                        g = EmbedBitInColorChannel(g, binaryEncryptedMessage, ref messageIndex);
                                        b = EmbedBitInColorChannel(b, binaryEncryptedMessage, ref messageIndex);

                                        try
                                        {
                                            bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                                            if (messageIndex >= binaryEncryptedMessage.Length)
                                            {
                                                messageComplete = true;
                                            }
                                       
                                        }
                                        catch (ArgumentOutOfRangeException ex)
                                        {
                                            Console.WriteLine($"Error at ({x},{y}): {ex.Message}");
                                            break;
                                        }
                                    }
                                
                                
                                }
                            }
                            // once done, whether messageComplete or loops exhausted, paint the 4‐pixel signature:
                            if (messageComplete)
                            {
                                bitmap.SetPixel(w - 1, h - 1, Color.FromArgb(254, 1, 1));
                                bitmap.SetPixel(w - 2, h - 1, Color.FromArgb(1, 254, 1));
                                bitmap.SetPixel(w - 3, h - 1, Color.FromArgb(1, 1, 254));
                                bitmap.SetPixel(w - 4, h - 1, Color.FromArgb(1, 1, 1));
                            }
                            Console.WriteLine("Processed I-frame: " + numberInFile);
                        }
                        else
                        {
                            // For non-I-frames, simply proceed without embedding.
                            Console.WriteLine("Copying non I-frame: " + numberInFile);
                        }

                        // Save every processed frame so you can recreate the video later
                        string outputFilePath = Path.Combine(outputframesDirectrory, Path.GetFileName(filePath));
                        bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unhandled error while processing {filePath}: {ex.Message}");
                }
            });



            string firstFramePath = Directory.GetFiles(outputframesDirectrory, "*.png").FirstOrDefault();
            if (firstFramePath == null)
            {
                return "No frames found in allFramesWithMessageFolder directory.";
            }
            else
            {
               return "Message embedded in all frames.";
            }

            
        }

        /// <summary>
        /// Embeds a bit into the least significant bit of a color channel value.
        /// </summary>
        /// <param name="colorChannelValue">The original color channel value.</param>
        /// <param name="binaryMessage">The binary representation of the message.</param>
        /// <param name="messageIndex">The current index in the binary message (passed by reference).</param>
        /// <returns>The modified color channel value with the embedded bit.</returns>
        public static int EmbedBitInColorChannel(int colorChannelValue, string binaryMessage, ref int messageIndex)
        {
            // Only embed if there are bits left to embed
            if (messageIndex < binaryMessage.Length)
            {
                // Get the bit to embed (0 or 1)
                int bit = binaryMessage[messageIndex] == '1' ? 1 : 0;

                // Embed the bit in the LSB
                colorChannelValue = (colorChannelValue & 0xFE) | bit;

                // Move to the next bit in the message
                messageIndex++;
            }

            return colorChannelValue;
        }
    }
}
