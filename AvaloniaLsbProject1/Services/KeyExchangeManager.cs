using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AvaloniaLsbProject1.Services
{
    /// <summary>
    /// Manages the Diffie-Hellman key exchange using ECDiffieHellman.
    /// </summary>
    public class KeyExchangeManager
    {
        private readonly ECDiffieHellman _ecdh;

        /// <summary>
        /// Gets the public key of the local party.
        /// </summary>
        public byte[] PublicKey { get; }

        /// <summary>
        /// Gets the derived shared key after performing key exchange.
        /// </summary>
        public byte[]? SharedKey { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyExchangeManager"/> class and generates the local public key.
        /// </summary>
        public KeyExchangeManager()
        {
            _ecdh = ECDiffieHellman.Create();
            PublicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        }

        /// <summary>
        /// Generates the shared key using the provided public key from the remote party.
        /// </summary>
        /// <param name="otherPublicKeyBytes">The public key bytes received from the other party.</param>
        public void GenerateSharedKey(byte[] otherPublicKeyBytes)
        {
            using var otherPublicKey = ECDiffieHellman.Create();
            otherPublicKey.ImportSubjectPublicKeyInfo(otherPublicKeyBytes, out _);

            SharedKey = _ecdh.DeriveKeyMaterial(otherPublicKey.PublicKey);
        }
    }
}
