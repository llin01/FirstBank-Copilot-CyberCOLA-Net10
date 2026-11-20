using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

#nullable enable

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Core utility class providing common functions for authorization processing, database operations, 
    /// encryption/decryption, SMS messaging, and ISO 8583 message handling.
    /// </summary>
    public static partial class ColaCommUtility
    {

        /// <summary>
        /// Decrypts data for bank-specific use cases.
        /// (This method currently returns the input data directly without actual decryption.)
        /// </summary>
        /// <param name="encryptData">The encrypted data string.</param>
        /// <returns>The decrypted string (currently returns input directly).</returns>
        public static string? DecryptForBank(string encryptData)
        {
            try
            {
                string connectionString = encryptData;
                if (connectionString == null)
                {
                    return null;
                }
                return encryptData;
            }
            catch (Exception ex)
            {
                Log.Fatal($"DecryptForBank Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Encrypts the given plain text using AES-GCM algorithm.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The Base64 encoded ciphertext string, or <c>null</c> if encryption fails.</returns>
        public static string? EncryptAesGcm(string plainText)
        {
            if (String.IsNullOrEmpty(plainText))
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          (new StackTrace(true)).GetFrame(0)?.GetMethod()?.Name ?? "Unknown",
                          "EncryptAesGcm Data Error",
                          "Input String Is Null Or Empty");
                return null;
            }
            try
            {
                string? returnString = null;
                var keyString = SystemParameterInfo.DatabaseInfo.EncryptedKey.Trim();
                byte[] keyBytes = Encoding.UTF8.GetBytes(keyString.PadRight(32));
                byte[] ivBytes = Encoding.UTF8.GetBytes(ivString.PadRight(12));
                byte[] tag = new byte[16];
                using (AesGcm aesGcm = new AesGcm(keyBytes, 16))
                {
                    byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);

                    // Use AesGcm's Encrypt method to perform encryption and store the result in ciphertext
                    byte[] cipherText = new byte[plaintextBytes.Length];
                    aesGcm.Encrypt(ivBytes, plaintextBytes, cipherText, tag);

                    // Combine tag and ciphertext to form the final encrypted result
                    byte[] result = new byte[cipherText.Length + tag.Length];
                    Buffer.BlockCopy(cipherText, 0, result, 0, cipherText.Length);
                    Buffer.BlockCopy(tag, 0, result, cipherText.Length, tag.Length);
                    returnString = Convert.ToBase64String(result);
                }
                return returnString;
            }
            catch (Exception ex)
            {
                Log.Fatal($"EncryptAesGcm Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decrypts the given AES-GCM ciphertext.
        /// </summary>
        /// <param name="cipherText">The Base64 encoded ciphertext string to decrypt.</param>
        /// <returns>The decrypted ASC string, or <c>null</c> if decryption fails.</returns>
        public static string? DecryptAesGcm(string cipherText)
        {
            // Check for null first, then perform the conversion
            if (String.IsNullOrEmpty(cipherText))
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                          (new StackTrace(true)).GetFrame(0)?.GetMethod()?.Name ?? "Unknown",
                          "DecryptAesGcm Data Error",
                          "Input String Is Null Or Empty");
                return null;
            }

            var cipherBytes = Convert.FromBase64String(cipherText);

            try
            {
                string? returnString = null;
                var keyString = SystemParameterInfo.DatabaseInfo.EncryptedKey.Trim();
                byte[] keyBytes = Encoding.UTF8.GetBytes(keyString.PadRight(32));
                byte[] ivBytes = Encoding.UTF8.GetBytes(ivString.PadRight(12));
                byte[] tag = new byte[16];

                using (AesGcm aesGcm = new AesGcm(keyBytes, 16))
                {
                    Buffer.BlockCopy(cipherBytes, cipherBytes.Length - 16, tag, 0, 16);

                    // Get the part without the tag
                    byte[] cipherBytesWithoutTag = new byte[cipherBytes.Length - 16];
                    Buffer.BlockCopy(cipherBytes, 0, cipherBytesWithoutTag, 0, cipherBytes.Length - 16);

                    // Use AesGcm's Decrypt method to perform decryption and store the result in plaintext
                    byte[] decryptedText = new byte[cipherBytesWithoutTag.Length];
                    aesGcm.Decrypt(ivBytes, cipherBytesWithoutTag, tag, decryptedText);
                    returnString = Encoding.UTF8.GetString(decryptedText);
                }
                return returnString;
            }
            catch (Exception ex)
            {
                Log.Fatal($"DecryptAesGcm Exception:{ex.Message}");
                return null;
            }
        }
    }
}
