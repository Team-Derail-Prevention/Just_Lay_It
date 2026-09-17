using UnityEngine;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


public static class SaveCrypto
{
    private static readonly byte[] _maskedKey =
    {
        0x3F, 0x8A, 0x12, 0xC4, 0x77, 0x01, 0x9E, 0x5D,
        0x66, 0xB2, 0x44, 0xF0, 0x1A, 0x29, 0x83, 0xD7,
        0x5C, 0x91, 0x3E, 0x0F, 0x88, 0x4B, 0xA6, 0x2D,
        0x77, 0x14, 0xE9, 0x60, 0xC3, 0x0A, 0x9F, 0x52
    };

    private static readonly byte[] _maskedIv =
    {
        0x2B, 0x7E, 0x15, 0x16, 0x28, 0xAE, 0xD2, 0xA6,
        0xAB, 0xF7, 0x15, 0x88, 0x09, 0xCF, 0x4F, 0x3C
    };

    private const byte XorMask = 0xA5;

    private static byte[] Unmask(byte[] maskedBytes)
    {
        byte[] result = new byte[maskedBytes.Length];
        for (int i = 0; i < maskedBytes.Length; i++)
        {
            result[i] = (byte)(maskedBytes[i] ^ XorMask);
        }

        return result;
    }

    public static byte[] Encrypt(string plainText)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Unmask(_maskedKey);
            aes.IV = Unmask(_maskedIv);

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                }

                return memoryStream.ToArray();
            }
        }
    }

    public static string Decrypt(byte[] cipherBytes)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Unmask(_maskedKey);
            aes.IV = Unmask(_maskedIv);

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            using (MemoryStream memoryStream = new MemoryStream(cipherBytes))
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(cryptoStream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
