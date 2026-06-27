using System;
using System.IO;
using System.Security.Cryptography;

/// <summary>
/// Contains general helpful functions.
/// </summary>
/// <remarks>
/// The autograder AES key lives in a separate, git-ignored partial file
/// (Utilities.Secret.cs) so the secret never enters source control. Copy
/// Utilities.Secret.cs.example to Utilities.Secret.cs and fill in the key to
/// build. See that file for details.
/// </remarks>
public static partial class Utilities
{
    /// <summary>
    /// Encrypts a plaintext message.
    /// </summary>
    /// <param name="message">The message to encrypt.</param>
    /// <returns>The encrypted message, represented as a string of hex characters.</returns>
    public static string Encrypt(string message)
    {
        string output = string.Empty;
        byte[] messageBytes = new byte[16 * ((message.Length + 15) / 16)];
        for (int i = 0; i < message.Length; i++)
        {
            messageBytes[i] = (byte)message[i];
        }

        using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
        {
            aes.Key = Utilities.key;
            aes.IV = new byte[aes.BlockSize / 8];

            for (int i = 0; i < messageBytes.Length; i += 16)
            {
                ICryptoTransform transform = aes.CreateEncryptor();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(messageBytes, i, 16);
                        byte[] ciphertext = memoryStream.ToArray();
                        output += BitConverter.ToString(ciphertext).Replace("-", "");
                    }
                }
            }
        }

        return output;
    }
}
