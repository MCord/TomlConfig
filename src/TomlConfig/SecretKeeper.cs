namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Test;

    public class SecretKeeper
    {
        private readonly string key;

        public SecretKeeper(string key)
        {
            this.key = key;
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

            AssertSecretThumbnail(thumbnail);
            var keyBytes = Security.GenerateHash(key);
            return Security.Decrypt(cypherBytes, keyBytes, thumbnail.Length);
        }

        public void AssertSecretThumbnail(byte[] thumbnail)
        {
            var expectedThumbnail = CalculateThumbnail();
            
            if (VerifySecretThumbnail(thumbnail))
            {
                return;
            }

            throw new TomlConfigurationException(
                $"Provided master key with thumbnail {Security.ToHexString(expectedThumbnail)} does not match the master key with which" +
                $" the secret was encrypted (thumbnail {Security.ToHexString(thumbnail)})");
        }
        
        public bool VerifySecretThumbnail(byte[] thumbnail)
        {
            var expectedThumbnail = CalculateThumbnail();

            if (thumbnail.Length != expectedThumbnail.Length)
            {
                return false;
            }

            for (var i = 0; i < expectedThumbnail.Length; i++)
            {
                if (thumbnail[i] != expectedThumbnail[i])
                {
                    return false;
                }
            }

            return true;
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
        public static readonly SecretKeeper Default = new SecretKeeper(GetMasterKey());
        
        private static string GetMasterKey()
        {
            return Environment.GetEnvironmentVariable("MASTER_KEY") ??
                   throw new TomlConfigurationException(
                       $"No master key provided in environment variable 'MASTER_KEY'");
        }
    }
}