namespace TomlConfig
{
    using System;
    using System.Text;
    using Test;

    public class SecretKeeper
    {
        private readonly byte[] key;

        public SecretKeeper(Func<byte[]> keyProvider = null)
        {
            key = keyProvider?.Invoke()
                       ?? Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("MASTER_KEY") ??
                                                 throw new TomlConfigurationException("No master key provided in environment variable 'MASTER_KEY'"));

            if (key.Length != 32)
            {
                throw new TomlConfigurationException(
                    $"The length of the key should be exactly 32 byes," +
                    $" the provided  key has a length of {key.Length}");
            }
        }

        public string Encrypt(string secret)
        {
            var thumbnail = Security.GenerateHash(key, 4);
            return Convert.ToBase64String(Security.Encrypt(secret, key, thumbnail));
        }

        public string Decrypt(string cypher)
        {
            var thumbnail = Security.GenerateHash(key, 4);
            var thumbnailLength = thumbnail.Length;

            var cypherBytes = Convert.FromBase64String(cypher);

            VerifySecretThumbnail(thumbnailLength, cypherBytes, thumbnail);

            return Security.Decrypt(cypherBytes, key, thumbnailLength);
        }

        private static void VerifySecretThumbnail(int thumbnailLength, byte[] cypherBytes, byte[] thumbnail)
        {
            var expected = Security.ToHexString(thumbnail);
            var actual = Security.ToHexString(cypherBytes.AsSpan(0, thumbnailLength).ToArray());

            for (var i = 0; i < thumbnailLength; i++)
            {
                if (cypherBytes[i] != thumbnail[i])
                {
                    throw new TomlConfigurationException(
                        $"Provided master key with thumbnail {expected} does not match the master key with which" +
                        $" the secret was encrypted (thumbnail {actual}");
                }
            }
        }
    }
}