namespace Test
{
    using System;
    using System.Text;
    using NFluent;
    using NFluent.ApiChecks;
    using TomlConfig;
    using Xunit;

    public class SecretKeeperTest
    {
        [Fact]
        public void ShouldDoSecretRoundTrip()
        {
            var sc = new SecretKeeper(Security.GenerateKey);

            var iLovePink = "I love pink!";

            var cypher = sc.Encrypt(iLovePink);
            Check.That(sc.Decrypt(cypher))
                .IsEqualTo(iLovePink);
        }

        [Fact]
        public void ShouldFailWithExceptionIfMasterKeyIsNotMatched()
        {
            var sc = new SecretKeeper(Security.GenerateKey);

            var iLovePink = "I love pink!";

            var cypher = sc.Encrypt(iLovePink);

            sc = new SecretKeeper(Security.GenerateKey);

            Check.ThatCode(() => sc.Decrypt(cypher))
                .Throws<TomlConfigurationException>()
                .AndWhichMessage()
                .Contains("thumbnail");
        }

        [Fact]
        public void ShouldFailWithErrorOnCorruptedCypher()
        {
            var sc = new SecretKeeper(Security.GenerateKey);
            
            Check.ThatCode(() => sc.Decrypt(Convert.ToBase64String(Encoding.UTF8.GetBytes("Not a cypher"))))
                .Throws<TomlConfigurationException>()
                .AndWhichMessage()
                .Contains("corrupted");
        }
    }
}