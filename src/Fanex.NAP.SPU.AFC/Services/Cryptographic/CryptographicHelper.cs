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
    }
}