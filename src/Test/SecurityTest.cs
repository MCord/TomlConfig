namespace Test
{
    using NFluent;
    using Xunit;

    public class SecurityTest
    {
        [Fact]
        public void ShouldGenerateHashFromValue()
        {
            Check.That(Security.GenerateHash("data", 4)).IsEqualTo(new byte[] {203, 93, 139, 20});
        }

        [Fact]
        public void CheckThatSaltChangesTheHash()
        {
            Check.That(Security.GenerateHash("data", 4)).Not.IsEqualTo(Security.GenerateHash("data", 4, "salt"));
        }

        [Fact]
        public void ShouldEncryptData()
        {
            var key = Security.GenerateKey();

            var nonEncryptedData = new byte[] {255, 255, 255, 255};

            var cypher = Security.Encrypt("my little secret", key, nonEncryptedData);

            Check.That("my little secret")
                .IsEqualTo(Security.Decrypt(cypher, key, nonEncryptedData.Length));
        }

        [Fact]
        public void SameSecretEncryptedTwiceShouldGenerateTwoDifferentCyphers()
        {
            var key = Security.GenerateKey();

            var nonEncryptedData = new byte[] {255, 255, 255, 255};

            Check.That(Security.Encrypt("my little secret", key, nonEncryptedData))
                .Not.IsEqualTo(Security.Encrypt("my little secret", key, nonEncryptedData));
        }
    }
}