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
                //for each char c it converts it to binary and adds padding to left side if less then 8 bits are generated 1000,000 -> 0100,0000
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
                //converts to ascii value 
                int asciiValue = Convert.ToInt32(byteString, 2);
                //converts ascii to character by casting to char 
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
        public static string ReconstructVideo(string framesPath, string outputFilePath, double frameRate,int[] iFramesLocations)
        {
            string message;
            string ffmpegPath = "ffmpeg"; 
            string inputPattern = $"{framesPath}\\frame_%04d.png"; // frame_0001.png
            string arguments;

            // FFmpeg command to reconstruct and compress video
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libx264  -qp 0 -preset ultrafast -an -x264-params \"intra-refresh=0:intra-block-copy=1\" \"{outputFilePath}\"";
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v ffv1 \"{outputFilePath}\"";
            //"/C ffmpeg -i C:\\VideoSteganography\\allCombinedFrames\\%06d.bmp -pix_fmt bgr24 -c:v libx264rgb -preset veryslow -qp 0 C:\\VideoSteganography\\stegovideo.avi";
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v copy \"{outputFilePath}\"";

            //-qp 0 option tells ffmpeg to use a quantizer of 0, which means no compression loss
            //-pix_fmt bgr24 ensures that the pixel format remains unchanged

            //last worked on 
            //string arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -pix_fmt bgr24 -c:v libx264rgb -preset veryfast -qp 0 \"{outputFilePath}\"";

            if (iFramesLocations != null && iFramesLocations.Length > 0)
            {
                /// <summary>
                /// Adjusts frame indices from 1-based to 0-based (as required by FFmpeg),
                /// while ensuring no negative indices are included.
                /// </summary>
                /// <remarks>
                /// FFmpeg expects 0-based frame numbers for its force_key_frames expression.
                /// If any frameNum is 0 or less, it will be clamped to 0.
                /// </remarks>
                var adjustedFrames = iFramesLocations.Select(frameNum => Math.Max(0, frameNum - 1)).ToArray();

                /// <summary>
                /// Builds an expression for FFmpeg's -force_key_frames option,
                /// where each frame is marked as a forced keyframe using the eq(n,frameNum) format.
                /// </summary>
                /// <example>
                /// For frames [0, 50, 100], the result will be:
                ///     eq(n,0)+eq(n,50)+eq(n,100)
                /// </example>
                /// <remarks>
                /// This expression will be passed to FFmpeg to ensure that the specified frames
                /// become I-frames (keyframes) during encoding.
                /// </remarks>
                var frameExpressions = adjustedFrames.Select(frameNum => $"eq(n,{frameNum})");


                /// <summary>
                /// Joins all FFmpeg frame expressions with '+' to create a single valid
                /// -force_key_frames expression for use in the FFmpeg command.
                /// </summary>
                /// <example>
                /// Resulting string: "eq(n,0)+eq(n,50)+eq(n,100)"
                /// </example>
                string keyframeExpr = string.Join("+", frameExpressions);

                // Use the expression format for force_key_frames
                arguments = $"-framerate {frameRate} -i \"{inputPattern}\" " +
                           $"-c:v libx264rgb -preset ultrafast -qp 0 -pix_fmt bgr24 " +
                           $"-force_key_frames \"expr:{keyframeExpr}\" " +
                           $"\"{outputFilePath}\"";
            }
            else
            {
                arguments = $"-framerate {frameRate} -i \"{inputPattern}\" " +
                           $"-c:v libx264 -preset ultrafast -qp 0 \"{outputFilePath}\"";
            }

            try
            {
                // Configuring the process
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Starting the process
                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                    Console.WriteLine("Starting video reconstruction...");
                    process.Start();

                    // reading output/error
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    message =  "Video reconstruction completed successfully.";
                }
            }
            catch (Exception ex)
            {
                message = ($"An error occurred while reconstructing the video: {ex.Message}");
            }
            return message;

        }

        public static void PlayVideo(string videoPath, int windowWidth = 1920, int windowHeight = 1080)
        {
            // Path to ffplay executable
            string ffplayPath = @"C:\ffmpeg\bin\ffplay.exe"; 

            // Command arguments with window size
            string arguments = $"-i \"{videoPath}\" -x {windowWidth} -y {windowHeight}";

            Process ffplayProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffplayPath,
                    Arguments = arguments,
                    UseShellExecute = false, 
                    RedirectStandardOutput = false, 
                    RedirectStandardError = false,  
                    CreateNoWindow = true 
                }
            };

            try
            {
                ffplayProcess.Start();
                Console.WriteLine("Playing video...");
                ffplayProcess.WaitForExit(); // Waiting for the process to finish
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

        
        

    }
}
