namespace Test
{
    using System;
    using FluentAssertions;
    using NFluent;
    using NFluent.ApiChecks;
    using TomlConfig;
    using Xunit;

    public class TomlConfigTest
    {
        public class SampleConfig
        {
            //todo: test all possible data types
            public string Value { get; set; }
            public DatabaseConfig Database { get; set; }
            public UserAccountConfig[] Account { get; set; }
            public float PiValue { get; set; }

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

        public class ArrayTestConfig
        {
            public int NotAnArray { get; set; }
        }

        [Fact]
        public void ShouldFailWithExpectedErrorIfFieldIsMissingOnTheType()
        {
            Check.ThatCode(() => TomlConfig.Read<SampleConfig>(Resources.Load("missing-field.toml")))
                .Throws<TomlConfigurationException>().
                AndWhichMessage()
                .Contains("MagicValue");
        }


        [Fact]
        public void ShouldReadStreamToObject()
        {
            var instance = TomlConfig.Read<SampleConfig>(Resources.Load("read.toml"));

            instance.Value.Should().Be("Simple Value");

            instance.Database.User.Should().Be("root");
            instance.Database.Port.Should().Be(5432);

            instance.Account[0].UserName.Should().Be("root");
            instance.Account[0].IsAdmin.Should().BeTrue();

            instance.Account[1].UserName.Should().Be("guest");
            instance.Account[1].IsAdmin.Should().BeFalse();
        }

        [Fact]
        public void ShouldReadWithDefaultValues()
        {
            var @default = new SampleConfig
            {
                PiValue = 3.14159254f
            };
            var objectInstance = TomlConfig.ReadWithDefault(Resources.Load("read.toml"), @default);

            Check.That(objectInstance.PiValue)
                .IsEqualTo(@default.PiValue);
        }

        [Fact]
        public void ArrayValuesCanNotBeAssignedToNonArrayProperties()
        {
            Check.ThatCode(() => TomlConfig.Read<ArrayTestConfig>(Resources.Load("array-type-mismatch.toml")))
                .Throws<TomlConfigurationException>()
                .AndWhichMessage()
                .Contains("System.Int32");
        }
    }
}