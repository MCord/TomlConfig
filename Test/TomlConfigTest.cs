namespace Test
{
    using FluentAssertions;
    using TomlConfig;
    using Xunit;

    public class TomlConfigTest
    {
        public class SampleConfig
        {
            public string Value { get; set; }
            public DatabaseConfig Database { get; set; }
            public UserAccountConfig[] Account { get; set; }

            public class DatabaseConfig
            {
                public string User { get; set; }
                public int Port { get; set; }
            }

            public class UserAccountConfig
            {
                public string UserName { get; set; }
                public bool IsAdmin { get; set; }
            }
        }

        [Fact]
        public void ShouldReadStreamToObject()
        {
            var instance = TomlConfig.Read<SampleConfig>(Resources.Load("sample.toml"));

            instance.Value.Should().Be("Simple Value");
            instance.Database.User.Should().Be("root");
            instance.Database.Port.Should().Be(5432);

            instance.Account[0].UserName.Should().Be("root");
            instance.Account[0].IsAdmin.Should().BeTrue();

            instance.Account[1].UserName.Should().Be("guest");
            instance.Account[1].IsAdmin.Should().BeFalse();

        }
    }
}