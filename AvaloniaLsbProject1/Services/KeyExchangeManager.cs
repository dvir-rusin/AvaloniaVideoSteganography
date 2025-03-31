using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Security.Cryptography;

namespace AvaloniaLsbProject1.Services
{
    public class KeyExchangeManager
    {
        private readonly ECDiffieHellman _ecdh;
        public byte[] PublicKey { get; }

        public byte[]? SharedKey { get; private set; }

        public KeyExchangeManager()
        {
            _ecdh = ECDiffieHellman.Create();
            PublicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        }

        public void GenerateSharedKey(byte[] otherPublicKeyBytes)
        {
            using var otherPublicKey = ECDiffieHellman.Create();
            otherPublicKey.ImportSubjectPublicKeyInfo(otherPublicKeyBytes, out _);

            SharedKey = _ecdh.DeriveKeyMaterial(otherPublicKey.PublicKey);
        }
    }
}
