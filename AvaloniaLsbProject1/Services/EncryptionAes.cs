using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaLsbProject1.Services
{
    internal class EncryptionAes
    {

        /// <summary>
        /// Prompts the user to enter a custom key for additional security.
        /// </summary>
        /// <returns>User-provided custom key as a string.</returns>
        public static string GetUserCustomKey()
        {
            Console.Write("Enter your custom key: ");
            return Console.ReadLine(); // Read custom key from user input
        }

        /// <summary>
        /// Generates a 256-bit AES key from a custom string provided by the user.
        /// Uses SHA256 to ensure a fixed-size key (32 bytes).
        /// </summary>
        /// <param name="customCode">The user-provided custom string.</param>
        /// <returns>256-bit key as a byte array.</returns>
        public static byte[] GenerateKeyFromCustomCode(string customCode)
        {
            using (var sha256 = SHA256.Create()) // Create an SHA256 hash generator
            {
                // Hash the custom code to generate a consistent 256-bit key
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(customCode));
            }
        }


        /// <summary>
        /// Encrypts plaintext using AES encryption with a user-provided custom key.
        /// </summary>
        /// <param name="plainText">The plaintext to encrypt.</param>
        /// <param name="customCode">The user-provided custom key.</param>
        /// <returns>Base64-encoded ciphertext.</returns>
        public static string Encrypt(string plainText, string customCode)
        {
            byte[] key = GenerateKeyFromCustomCode(customCode); // Generate AES key from custom code

            using (Aes aes = Aes.Create()) // Create a new AES instance
            {
                aes.Key = key; // Set the AES key
                aes.GenerateIV(); // Generate a random initialization vector (IV)
                byte[] iv = aes.IV; // Store the IV for later use in decryption

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv)) // Create an encryptor with the key and IV
                using (var ms = new MemoryStream()) // MemoryStream to hold encrypted data
                {
                    ms.Write(iv, 0, iv.Length); // Write the IV to the start of the stream for decryption
                    using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cryptoStream))
                    {
                        sw.Write(plainText); // Encrypt the plaintext and write it to the crypto stream
                    }
                    return Convert.ToBase64String(ms.ToArray()); // Convert encrypted data to Base64 for easy storage
                }
            }
        }

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext using AES decryption with a user-provided custom key.
        /// </summary>
        /// <param name="cipherText">The Base64-encoded ciphertext.</param>
        /// <param name="customCode">The user-provided custom key.</param>
        /// <returns>Decrypted plaintext.</returns>
        public static string Decrypt(string cipherText, string customCode)
        {
            byte[] key = GenerateKeyFromCustomCode(customCode); // Generate AES key from custom code
            byte[] cipherBytes = Convert.FromBase64String(cipherText); // Convert Base64 ciphertext to byte array

            using (Aes aes = Aes.Create()) // Create a new AES instance
            {
                aes.Key = key; // Set the AES key

                byte[] iv = new byte[16]; // AES block size is 16 bytes; IV must match this size
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length); // Extract the IV from the ciphertext
                aes.IV = iv; // Set the IV for decryption

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV)) // Create a decryptor with the key and IV
                using (var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cryptoStream))
                {
                    return sr.ReadToEnd(); // Decrypt the data and return the plaintext
                }
            }
        }

        /// <summary>
        /// One-time utility function to encrypt an existing video key storage file
        /// Use this when migrating from plaintext to encrypted storage
        /// </summary>
        /// <param name="masterPassword">Master password to use for encryption</param>
        /// <returns>True if encryption was successful</returns>
        public static bool EncryptExistingVideoKeyStorage(string masterPassword)
        {
            // Build relative path to VideoKeyStorage.json inside the Json folder
            string storagePath = Path.Combine(AppContext.BaseDirectory, "Json", "VideoKeyStorage.json");

            try
            {
                // Check if file exists
                if (!File.Exists(storagePath))
                {
                    return false;
                }

                // Load existing dictionary with plaintext entries
                string json = File.ReadAllText(storagePath);
                var plaintextDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                                 ?? new Dictionary<string, string>();

                // Create new dictionary for encrypted entries
                var encryptedDict = new Dictionary<string, string>();

                // Encrypt each entry
                foreach (var entry in plaintextDict)
                {
                    string videoName = entry.Key;
                    string password = entry.Value;

                    // Encrypt both values
                    string encryptedName = EncryptionAes.Encrypt(videoName, masterPassword);
                    string encryptedPassword = EncryptionAes.Encrypt(password, masterPassword);

                    // Add to new dictionary
                    encryptedDict[encryptedName] = encryptedPassword;
                }

                // Create backup of original file
                string backupPath = storagePath + ".backup";
                File.Copy(storagePath, backupPath, true);

                // Save encrypted dictionary back to the same file
                string encryptedJson = JsonConvert.SerializeObject(encryptedDict, Formatting.Indented);
                File.WriteAllText(storagePath, encryptedJson);

                return true;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error encrypting storage: {ex.Message}");
                return false;
            }
        }

    }
}
