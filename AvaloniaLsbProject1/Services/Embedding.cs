
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaLsbProject1.Services
{
    internal class Embedding
    {

        public static string EmbedMessageInFramesTestInVideo
            (string inputframesDirectory,
            string outputframesDirectrory,
            int[] IframesLocation,
            string text,
            string password)
        {
            string ErrorMessage;
            Console.WriteLine("Enter text to encrypt:");
            string UserInputMessage = text; // Read the plaintext input from the user

            string UserPassword = password; // Get the user-provided custom key

            string EncryptedMessage = EncryptionAes.Encrypt(UserInputMessage, UserPassword); // Encrypt the plaintext using the custom key
            Console.WriteLine($"Encrypted Text: {EncryptedMessage}");

            if (!Directory.Exists(inputframesDirectory))
            {
                ErrorMessage = "EmbedMessageInFramesTestInVideo function message : inputframesDirectory does not exist ";
                return ErrorMessage;
            }
            EncryptedMessage += char.MinValue;
            Console.WriteLine(EncryptedMessage);
            string binaryEncryptedMessage = HelperFunctions.StringToBinary(EncryptedMessage);
            Console.WriteLine("ORIGINAL binaryMessage: " + binaryEncryptedMessage);
            Console.WriteLine("Message : " + HelperFunctions.BinaryToString(binaryEncryptedMessage));

            string binaryLength = Convert.ToString(binaryEncryptedMessage.Length, 2).PadLeft(12, '0');
            Console.WriteLine("Binary Length: " + binaryLength);
            int indexer = 0;



            foreach (string filePath in Directory.GetFiles(inputframesDirectory, "*.png"))
            {
                bool isEmpty = false;
                bool messageComplete = false;
                int messageIndex = 0;
                bool isNumberInFile = false;

                int numberInFile = HelperFunctions.extractNumberFromFilePath(filePath);
                using (Bitmap bitmap = new Bitmap(filePath))
                {



                    for (int i = indexer; isNumberInFile != true && i < IframesLocation.Length; i++)
                    {
                        if (numberInFile == IframesLocation.ElementAt(i))
                        {
                            isNumberInFile = true;
                            indexer++;

                        }


                    }
                    if (isNumberInFile == true)
                    {
                        
                        for (int y = 0; y < bitmap.Height && !messageComplete; y++)
                        {
                            for (int x = 0; x < bitmap.Width && !messageComplete; x++)
                            {
                                Color pixelColor = bitmap.GetPixel(x, y);
                                
                                int r = pixelColor.R, g = pixelColor.G, b = pixelColor.B;

                                r = EmbedBitInColorChannel(r, binaryEncryptedMessage, ref messageIndex);
                                g = EmbedBitInColorChannel(g, binaryEncryptedMessage, ref messageIndex);
                                b = EmbedBitInColorChannel(b, binaryEncryptedMessage, ref messageIndex);

                                if (messageIndex >= binaryEncryptedMessage.Length)
                                {
                                    messageComplete = true;

                                    //bitmap.SetPixel(1000, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1001, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1002, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1003, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1004, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1005, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1006, 1000, Color.FromArgb(254, 0, 0));
                                    //bitmap.SetPixel(1007, 1000, Color.FromArgb(254, 0, 0));

                                    //bitmap.SetPixel(bitmap.Width - 1, bitmap.Height - 1, Color.FromArgb(254, 1, 1));
                                    //bitmap.SetPixel(bitmap.Width - 2, bitmap.Height - 2, Color.FromArgb(254, 1, 1));
                                    //bitmap.SetPixel(bitmap.Width - 3, bitmap.Height - 3, Color.FromArgb(254, 1, 1));
                                    //bitmap.SetPixel(bitmap.Width - 4, bitmap.Height - 4, Color.FromArgb(254, 1, 1));
                                }

                                bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                                Console.WriteLine("bitmap has been saved with the number: " + numberInFile);
                            }
                        }
                    }


                    string outputFilePath = Path.Combine(outputframesDirectrory, Path.GetFileName(filePath));
                    bitmap.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Png);

                    //if (messageComplete) break; // Stop for only checking the first frame break will be removed later on
                }
            }

            ErrorMessage = "Message embedded in all frames.";
            return ErrorMessage;
        }

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
