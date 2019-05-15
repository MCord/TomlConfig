namespace Test
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;
    
 /*
  * This work (Modern Encryption of a String C#, by James Tuley), 
  * identified by James Tuley, is free of known copyright restrictions.
  * https://gist.github.com/4336842
  * http://creativecommons.org/publicdomain/mark/1.0/ 
  */

    public class Security
    {
        public static byte[] GenerateHash(byte[] data, int hashLengthInBytes = 32, string salt = null)
        {
            return GenerateHash(ToHexString(data), hashLengthInBytes, salt);
        }
        
        public static byte[] GenerateHash(string data, int hashLengthInBytes = 32, string salt = null)
        {
            var saltBytes = salt == null
                ? new byte[0]
                : Encoding.UTF8.GetBytes(salt);

            var valueBytes = KeyDerivation.Pbkdf2(
                data,
                saltBytes,
                KeyDerivationPrf.HMACSHA512,
                10000,
                hashLengthInBytes);

            return valueBytes;
        }

        public static string ToHexString(byte[] data)
        {
            var hex = new StringBuilder(data.Length * 2);

            foreach (var b in data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

        //Preconfigured Encryption Parameters
        public static readonly int BlockBitSize = 128;
        public static readonly int KeyBitSize = 256;

        //Preconfigured Password Key Derivation Parameters
        public static readonly int SaltBitSize = 64;
        public static readonly int Iterations = 10000;
        public static readonly int MinPasswordLength = 12;

        /// <summary>
        /// Helper that generates a random key on each call.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateKey()
        {
            var key = new byte[KeyBitSize / 8];
            Random.GetBytes(key);
            return key;
        }

        /// <summary>
        /// Simple Encryption(AES) then Authentication (HMAC) for a UTF8 Message.
        /// </summary>
        /// <param name="clearMessage">The secret message.</param>
        /// <param name="cryptKey">The crypt key.</param>
        /// <param name="nonEncryptedPayload">(Optional) Non-Secret Payload.</param>
        /// <returns>
        /// Encrypted Message
        /// </returns>
        /// <remarks>
        /// Adds overhead of (Optional-Payload + BlockSize(16) + Message-Padded-To-Blocksize +  HMac-Tag(32)) * 1.33 Base64
        /// </remarks>
        public static byte[] Encrypt(string clearMessage, byte[] cryptKey,
            byte[] nonEncryptedPayload)
        {
            var clearMessageBytes = Encoding.UTF8.GetBytes(clearMessage);
            
            //User Error Checks
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException($"Key needs to be {KeyBitSize} bit!", nameof(cryptKey));

            if (clearMessageBytes == null || clearMessageBytes.Length < 1)
                throw new ArgumentException("Secret Message Required!", nameof(clearMessageBytes));
            

            byte[] cipherText;
            byte[] iv;

            using (var aes = new AesManaged
            {
                KeySize = KeyBitSize,
                BlockSize = BlockBitSize,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {

                //Use random IV
                aes.GenerateIV();
                iv = aes.IV;

                using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        //Encrypt Data
                        binaryWriter.Write(clearMessageBytes);
                    }

                    cipherText = cipherStream.ToArray();
                }

            }

            //Assemble encrypted message and add authentication
            using (var encryptedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(encryptedStream))
                {
                    //Prepend non-secret payload if any
                    binaryWriter.Write(nonEncryptedPayload);
                    //Prepend IV
                    binaryWriter.Write(iv);
                    //Write Ciphertext
                    binaryWriter.Write(cipherText);
                    binaryWriter.Flush();
                }

                return encryptedStream.ToArray();
            }
        }

        /// <summary>
        /// Simple Authentication (HMAC) then Decryption (AES) for a secrets UTF8 Message.
        /// </summary>
        /// <param name="encryptedMessage">The encrypted message.</param>
        /// <param name="cryptKey">The crypt key.</param>
        /// <param name="nonSecretPayloadLength">Length of the non secret payload.</param>
        /// <returns>Decrypted Message</returns>
        public static string Decrypt(byte[] encryptedMessage, byte[] cryptKey,int nonSecretPayloadLength = 0)
        {

            //Basic Usage Error Checks
            if (cryptKey == null || cryptKey.Length != KeyBitSize / 8)
                throw new ArgumentException($"CryptKey needs to be {KeyBitSize} bit!", nameof(cryptKey));

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", nameof(encryptedMessage));

            {
                var ivLength = (BlockBitSize / 8);
                using (var aes = new AesManaged
                {
                    KeySize = KeyBitSize,
                    BlockSize = BlockBitSize,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {

                    //Grab IV from message
                    var iv = new byte[ivLength];
                    Array.Copy(encryptedMessage, nonSecretPayloadLength, iv, 0, iv.Length);

                    using (var decrypter = aes.CreateDecryptor(cryptKey, iv))
                    using (var plainTextStream = new MemoryStream())
                    {
                        using (var decrypterStream =
                            new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                        using (var binaryWriter = new BinaryWriter(decrypterStream))
                        {
                            //Decrypt Cipher Text from Message
                            binaryWriter.Write(
                                encryptedMessage,
                                nonSecretPayloadLength + iv.Length,
                                encryptedMessage.Length - nonSecretPayloadLength - iv.Length
                            );
                        }

                        //Return Plain Text
                        return Encoding.UTF8.GetString(plainTextStream.ToArray());
                    }
                }
            }
        }
    }
}