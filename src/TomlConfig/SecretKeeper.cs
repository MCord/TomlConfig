namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Test;

    public class SecretKeeper
    {
        private readonly byte[] key;

        public SecretKeeper(Func<byte[]> keyProvider = null)
        {
            key = keyProvider?.Invoke()
                  ?? Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("MASTER_KEY") ??
                                            throw new TomlConfigurationException(
                                                "No master key provided in environment variable 'MASTER_KEY'"));

            if (key.Length != 32)
            {
                throw new TomlConfigurationException(
                    "The length of the key should be exactly 32 byes," +
                    $" the provided  key has a length of {key.Length}");
            }
        }

        public string Encrypt(string secret)
        {
            var thumbnail = CalculateThumbnail();
            return Convert.ToBase64String(Security.Encrypt(secret, key, thumbnail));
        }

        private byte[] CalculateThumbnail()
        {
            var hash = Security.GenerateHash(key, 4).ToList();
            hash.Add(CalculateThumbnailCheckSum(hash));
            return hash.ToArray();
        }

        private static byte CalculateThumbnailCheckSum(IEnumerable<byte> hash)
        {
            return (byte) (hash.Take(4).Sum(b => b) % 255);
        }


        public string Decrypt(string cypher)
        {
            if (!IsValidCypher(cypher, out var thumbnail, out var cypherBytes))
            {
                throw new TomlConfigurationException("Cypher is corrupted.");
            }

            VerifySecretThumbnail(thumbnail);
            return Security.Decrypt(cypherBytes, key, thumbnail.Length);
        }

        public void VerifySecretThumbnail(byte[] thumbnail)
        {
            var expectedThumbnail = CalculateThumbnail();

            if (thumbnail.Length != expectedThumbnail.Length)
            {
                ThrowThumbnailMismatch(expectedThumbnail, thumbnail);
            }

            for (var i = 0; i < expectedThumbnail.Length; i++)
            {
                if (thumbnail[i] != expectedThumbnail[i])
                {
                    ThrowThumbnailMismatch(expectedThumbnail, thumbnail);
                }
            }
        }

        private static void ThrowThumbnailMismatch(byte[] expected, byte[] actual)
        {
            throw new TomlConfigurationException(
                $"Provided master key with thumbnail {Security.ToHexString(expected)} does not match the master key with which" +
                $" the secret was encrypted (thumbnail {Security.ToHexString(actual)}");
        }

        public bool IsValidCypher(string value, out byte[] thumbnail, out byte[] cypher)
        {
            thumbnail = null;
            cypher = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(value);

                if (bytes.Length < 5)
                {
                    return false;
                }

                if (bytes[4] != CalculateThumbnailCheckSum(bytes))
                {
                    return false;
                }

                thumbnail = bytes.AsSpan(0, 5).ToArray();
                cypher = bytes;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}