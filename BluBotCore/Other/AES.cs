using BluBotCore.Ignore;
using System;
using System.IO;
using System.Security.Cryptography;

namespace BluBotCore.Other
{
    public class AES
    {
        public static string Encrypt(string entry)
        {
            string temp = EncryptStringToBase64String(entry, Crypt.Key);
            return temp;
        }

        public static string Decrypt(string entry)
        {
            string temp = DecryptStringFromBase64String(entry, Crypt.Key);
            return temp;
        }

        private const int KeySize = 256;
        static string EncryptStringToBase64String(string plainText, byte[] Key)
        {
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            byte[] returnValue;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                byte[] iv = aes.IV;
                if (string.IsNullOrEmpty(plainText))
                    return Convert.ToBase64String(iv);
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, iv);
                using MemoryStream msEncrypt = new();
                using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
                using (StreamWriter swEncrypt = new(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                byte[] encrypted = msEncrypt.ToArray();
                returnValue = new byte[encrypted.Length + iv.Length];
                Array.Copy(iv, returnValue, iv.Length);
                Array.Copy(encrypted, 0, returnValue, iv.Length, encrypted.Length);
            }
            return Convert.ToBase64String(returnValue);
        }

        static string DecryptStringFromBase64String(string cipherText, byte[] Key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            string plaintext = null;
            byte[] allBytes = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.Mode = CipherMode.CBC;
                byte[] iv = new byte[aes.BlockSize / 8];
                if (allBytes.Length < iv.Length)
                    throw new ArgumentException("Message was less than IV size.");
                Array.Copy(allBytes, iv, iv.Length);
                byte[] cipherBytes = new byte[allBytes.Length - iv.Length];
                Array.Copy(allBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);
                ICryptoTransform decryptor = aes.CreateDecryptor(Key, iv);
                using MemoryStream msDecrypt = new(cipherBytes);
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new(csDecrypt);
                plaintext = srDecrypt.ReadToEnd();
            }
            return plaintext;
        }
    }
}
