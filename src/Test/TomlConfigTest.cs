namespace Test
{
    using System;
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
            public int IntValue { get; set; }
            public double FloatValue { get; set; }
            public bool BooleanValue { get; set; }
            public DateTime DateTimeOffset { get; set; }
            public DateTime LocalDateTime { get; set; }
            public DateTime LocalTime { get; set; }
            public int[] Array { get; set; }


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
        public void ArrayValuesCanNotBeAssignedToNonArrayProperties()
        {
            Check.ThatCode(() => TomlConfig.Read<ArrayTestConfig>(Resources.Load("array-type-mismatch.toml")))
                .Throws<TomlConfigurationException>()
                .AndWhichMessage()
                .Contains("System.Int32");
        }

        [Fact]
        public void ShouldFailWithExpectedErrorIfFieldIsMissingOnTheType()
        {
            Check.ThatCode(() => TomlConfig.Read<SampleConfig>(Resources.Load("missing-field.toml")))
                .Throws<TomlConfigurationException>().AndWhichMessage()
                .Contains("MagicValue");
        }


        [Fact]
        public void ShouldReadStreamToObject()
        {
            var instance = TomlConfig.Read<SampleConfig>(Resources.Load("read.toml"));

            Check.That(instance.Value).IsEqualTo("Simple Value");
            Check.That(instance.IntValue).IsEqualTo(42);
            Check.That(instance.FloatValue).IsEqualTo(3.14);
            Check.That(instance.BooleanValue).IsEqualTo(true);
            Check.That(instance.LocalDateTime).IsEqualTo(new DateTime(2002, 5, 27, 7, 32, 0));
            Check.That(instance.DateTimeOffset).IsEqualTo(new DateTime(2002, 5, 27, 16, 32, 0, DateTimeKind.Local));
            Check.That(instance.LocalTime.TimeOfDay).IsEqualTo(new TimeSpan(0, 7, 32, 0));
            Check.That(instance.Array).IsEqualTo(new[] {1, 2, 3});

            Check.That(instance.Database.User).IsEqualTo("root");
            Check.That(instance.Database.Port).IsEqualTo(5432);

            Check.That(instance.Account[0].UserName).IsEqualTo("root");
            Check.That(instance.Account[0].IsAdmin).IsEqualTo(true);

            Check.That(instance.Account[1].UserName).IsEqualTo("guest");
            Check.That(instance.Account[1].IsAdmin).IsEqualTo(false);
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
    }
}