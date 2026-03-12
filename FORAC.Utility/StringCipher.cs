using System.Security.Cryptography;
using System.Text;

namespace FORAC.Utility
{
    public static class StringCipher
    {
        private const int KEY_SIZE_BYTE = 16;
        private const int BLOCK_SIZE_BYTE = 16;
        private const int DERIVATION_ITERATIONS = 5123;

        public static string Encrypt(string plainText, string encryptionKey)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate128BitsOfRandomEntropy(KEY_SIZE_BYTE);
            var ivStringBytes = Generate128BitsOfRandomEntropy(KEY_SIZE_BYTE);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(encryptionKey, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA512))
            {
                var keyBytes = password.GetBytes(KEY_SIZE_BYTE);
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KEY_SIZE_BYTE * 8;
                    aes.BlockSize = BLOCK_SIZE_BYTE * 8;
                    aes.Key = keyBytes;
                    aes.IV = ivStringBytes;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();

                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();

                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string cipherText, string encryptionKey)
        {
            // Get the complete stream of bytes that represent:
            // [16 bytes of Salt] + [16 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 16 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(KEY_SIZE_BYTE).ToArray();
            // Get the IV bytes by extracting the next 16 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(KEY_SIZE_BYTE).Take(KEY_SIZE_BYTE).ToArray();
            // Get the actual cipher text bytes by removing the first 32 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(KEY_SIZE_BYTE * 2).Take(cipherTextBytesWithSaltAndIv.Length - (KEY_SIZE_BYTE * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(encryptionKey, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA512))
            {
                var keyBytes = password.GetBytes(KEY_SIZE_BYTE);
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KEY_SIZE_BYTE * 8;
                    aes.BlockSize = BLOCK_SIZE_BYTE * 8;
                    aes.Key = keyBytes;
                    aes.IV = ivStringBytes;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                using (var output = new MemoryStream())
                                {
                                    cryptoStream.CopyTo(output);

                                    return Encoding.UTF8.GetString(output.ToArray(), 0, (int)output.Length);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void EncryptFile(string inputPath, string outputPath, string encryptionKey)
        {
            var saltStringBytes = Generate128BitsOfRandomEntropy(KEY_SIZE_BYTE);
            var ivStringBytes = Generate128BitsOfRandomEntropy(KEY_SIZE_BYTE);

            using (var password = new Rfc2898DeriveBytes(encryptionKey, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA512))
            {
                var keyBytes = password.GetBytes(16);
                using (var aes = Aes.Create())
                {
                    aes.KeySize = KEY_SIZE_BYTE * 8;
                    aes.BlockSize = BLOCK_SIZE_BYTE * 8;
                    aes.Key = keyBytes;
                    aes.IV = ivStringBytes;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Mode = CipherMode.CBC;
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                        {
                            outputFileStream.Write(saltStringBytes, 0, KEY_SIZE_BYTE);
                            outputFileStream.Write(ivStringBytes, 0, KEY_SIZE_BYTE);
                            using (var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                            {
                                using (FileStream inputFileStream = new FileStream(inputPath, FileMode.Open))
                                {
                                    inputFileStream.CopyTo(cryptoStream);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DecryptFile(string inputPath, string outputPath, string encryptionKey)
        {
            byte[] saltStringBytes = new byte[KEY_SIZE_BYTE];
            byte[] ivStringBytes = new byte[KEY_SIZE_BYTE];
            using (FileStream inputFileStream = new FileStream(inputPath, FileMode.Open))
            {
                inputFileStream.Seek(0, SeekOrigin.Begin);
                inputFileStream.Read(saltStringBytes, 0, KEY_SIZE_BYTE);
                inputFileStream.Seek(KEY_SIZE_BYTE, SeekOrigin.Begin);
                inputFileStream.Read(ivStringBytes, 0, KEY_SIZE_BYTE);

                using (var password = new Rfc2898DeriveBytes(encryptionKey, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA512))
                {
                    var keyBytes = password.GetBytes(KEY_SIZE_BYTE);
                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = KEY_SIZE_BYTE * 8;
                        aes.BlockSize = BLOCK_SIZE_BYTE * 8;
                        aes.Key = keyBytes;
                        aes.IV = ivStringBytes;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Mode = CipherMode.CBC;
                        using (var decryptor = aes.CreateDecryptor())
                        {
                            using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                            {
                                //Skip salt (16 bytes) and iv (16 bytes)
                                inputFileStream.Seek(KEY_SIZE_BYTE * 2, SeekOrigin.Begin);
                                using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read))
                                {
                                    cryptoStream.CopyTo(outputFileStream);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static byte[] Generate128BitsOfRandomEntropy(int arraySize)
        {
            return RandomNumberGenerator.GetBytes(arraySize);
        }
    }
}
