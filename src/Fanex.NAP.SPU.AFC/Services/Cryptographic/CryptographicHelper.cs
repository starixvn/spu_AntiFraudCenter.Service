namespace SPU.AntiFraudCenter.Nova88.Services.Cryptographic
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class CryptographicHelper
    {
        public static string AESEncrypt(string plainText, string serectKey, string participantName)
        {
            if (!string.IsNullOrEmpty(plainText))
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(serectKey);
                byte[] saltBytes = Encoding.UTF8.GetBytes(string.Concat(participantName, serectKey));

                string encrypted;

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = CryptographicConfig.Aes128;
                    aes.BlockSize = CryptographicConfig.Aes128;
                    aes.Key = keyBytes.Take(aes.KeySize / 8).ToArray();
                    aes.IV = saltBytes.Take(aes.BlockSize / 8).ToArray();

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(cs))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }

                        encrypted = Convert.ToBase64String(ms.ToArray());
                    }
                }

                return encrypted;
            }

            return string.Empty;
        }

        public static string CreateMD5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}