namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Test;

    public class SecretKeeper
    {
        private readonly string key;

        public SecretKeeper(Func<string> keyProvider = null)
        {
            key = keyProvider?.Invoke()
                  ?? (Environment.GetEnvironmentVariable("MASTER_KEY") ??
                                            throw new TomlConfigurationException(
                                                "No master key provided in environment variable 'MASTER_KEY'"));
        }

        public string Encrypt(string secret)
        {
            var thumbnail = CalculateThumbnail();
            var keyBytes = Security.GenerateHash(key);
            return Convert.ToBase64String(Security.Encrypt(secret, keyBytes, thumbnail));
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
            var keyBytes = Security.GenerateHash(key);
            return Security.Decrypt(cypherBytes, keyBytes, thumbnail.Length);
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
                $" the secret was encrypted (thumbnail {Security.ToHexString(actual)})");
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