using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Socean.Rpc.DynamicProxy
{
    internal class EncryptHelper
    {
        private static readonly byte[] EmptyKeyBytes = new byte[32];

        private static readonly byte[] EmptyIVBytes = new byte[16];

        internal static byte[] AesEncrypt(byte[] original, byte[] keyBytes)
        {
            if (keyBytes.Length != 32)
                throw new ArgumentException("AesEncrypt key byte length must be 32 ");

            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
            {
                aesCryptoServiceProvider.Mode = CipherMode.CBC;
                aesCryptoServiceProvider.Key = keyBytes;
                aesCryptoServiceProvider.IV = EmptyIVBytes;

                ICryptoTransform encryptor = aesCryptoServiceProvider.CreateEncryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(original, 0, original.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        internal static byte[] AesDecrypt(byte[] encrypted, byte[] keyBytes)
        {
            if (keyBytes.Length != 32)
                throw new ArgumentException("AesDecrypt key byte length must be 32 ");

            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
            {
                aesCryptoServiceProvider.Mode = CipherMode.CBC;
                aesCryptoServiceProvider.Key = keyBytes;
                aesCryptoServiceProvider.IV = EmptyIVBytes;

                ICryptoTransform decryptor = aesCryptoServiceProvider.CreateDecryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV);

                using (MemoryStream memoryStream = new MemoryStream(encrypted))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var buffer = new byte[encrypted.Length];
                        var readCount = cryptoStream.Read(buffer, 0, buffer.Length);

                        var originalBytes = new byte[readCount];
                        if(originalBytes.Length > 0)
                            Buffer.BlockCopy(buffer, 0, originalBytes, 0, readCount);

                        return originalBytes;
                    }
                }
            }
        }

        internal static string AesEncrypt(string original, byte[] keyBytes)
        {
            if (keyBytes.Length != 32)
                throw new ArgumentException("AesEncrypt key byte length must be 32 ");

            var originalBytes = Encoding.UTF8.GetBytes(original);

            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
            {
                aesCryptoServiceProvider.Mode = CipherMode.CBC;
                aesCryptoServiceProvider.Key = keyBytes;
                aesCryptoServiceProvider.IV = EmptyIVBytes;

                ICryptoTransform encryptor = aesCryptoServiceProvider.CreateEncryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(originalBytes, 0, original.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    var buffer = memoryStream.GetBuffer();
                    var readCount = (int)memoryStream.Length;
                    if (readCount > 0)
                        return Encoding.UTF8.GetString(buffer, 0, readCount);
                    return string.Empty;
                }
            }
        }

        internal static string AesDecrypt(string encrypted, byte[] keyBytes)
        {
            if (keyBytes.Length != 32)
                throw new ArgumentException("AesDecrypt key byte length must be 32 ");

            var encryptedBytes = Encoding.UTF8.GetBytes(encrypted);

            using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
            {
                aesCryptoServiceProvider.Mode = CipherMode.CBC;
                aesCryptoServiceProvider.Key = keyBytes;
                aesCryptoServiceProvider.IV = EmptyIVBytes;

                ICryptoTransform decryptor = aesCryptoServiceProvider.CreateDecryptor(aesCryptoServiceProvider.Key, aesCryptoServiceProvider.IV);

                using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        var buffer = new byte[encrypted.Length];
                        var readCount = cryptoStream.Read(buffer, 0, buffer.Length);
                        if (readCount > 0)
                            return Encoding.UTF8.GetString(buffer, 0, readCount);

                        return string.Empty;
                    }
                }
            }
        }
    }
}
