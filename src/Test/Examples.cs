namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using JetBrains.Annotations;
    using Newtonsoft.Json;
    using NFluent;
    using TomlConfiguration;
    using Xunit;

    public class Examples
    {
        /// <summary>
        /// A simple DTO class that will contain the application configuration.
        /// only properties can be used. Any field that need encryption should be marked
        /// with <c>SecretAttribute</c> any valid toml type can be used for properties
        /// </summary>
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class MyApplicationConfiguration
        {
            public string ApplicationName { get; set; }
            public string CopyRight { get; set; }
            public string Environment { get; set; }
            public string LogPath { get; set; }
            
            /// <summary>
            /// Properties can contain complex objects
            /// </summary>
            public DatabaseConfiguration Database { get; set; }

            public class DatabaseConfiguration
            {
                public string Server { get; set; }
                public int Port { get; set; }
                public string DatabaseName { get; set; }
                public string User { get; set; }
                
                [Secret]
                public string Password { get; set; }
            }
        }

        /// <summary>
        /// This test demonstrates a simple read from a file,
        /// </summary>
        [Fact]
        public void ReadConfigFromFile()
        {
            var file = "./files/my-application/common.toml";
            
            var config = TomlConfig
                .FromFile(file)
                .Read<MyApplicationConfiguration>();

            CompareToExpectedResult(config, file);
        }

        /// <summary>
        /// Add a <c>SecretAttribute</c> attribute to your sensitive properties and use toml-config-tool to
        /// encrypt your file. You can then specify the master key (or set MASTER_KEY environment variable)
        /// to decrypt your file. This way you can commit your secrets to source control without worrying
        /// about them being exposed.
        /// </summary>
        [Fact]
        public void ReadConfigFromFileWithSecrets()
        {
            var file = "./files/my-application/production.toml";
            
            var config = TomlConfig
                .FromFile(file)
                .WithMasterKey("masterkey")
                .Read<MyApplicationConfiguration>();

            CompareToExpectedResult(config, file);
        }

        /// <summary>
        /// You can also override values either from environment variables or just any dictionary.
        /// </summary>
        [Fact]
        public void ReadConfigAndOverrideFromEnvironmentValue()
        {
            Environment.SetEnvironmentVariable("ApplicationName", "overriden");
            
            var file = "./files/my-application/common.toml";
            
            var config = TomlConfig
                .FromFile(file)
                .WithOverrideFromEnvironmentVariables() /* Override from environment values */
                .Read<MyApplicationConfiguration>();

            Check.That(config.ApplicationName)
                .IsEqualTo("overriden");
            
            var config2 = TomlConfig
                .FromFile(file)
                /* Override by value*/
                .WithOverride(nameof(MyApplicationConfiguration.ApplicationName), "Another Value") 
                .Read<MyApplicationConfiguration>();

            Check.That(config2.ApplicationName)
                .IsEqualTo("Another Value");
        }
        
        
        /// <summary>
        /// You can use includes to remove duplication from config files by defining a common configuration
        /// file and including it in each specific files. 
        /// </summary>
        [Fact]
        public void RemoveDuplicationByIncludingACommonFile()
        {
            var file = "./files/my-application/staging.toml";
            
            var config = TomlConfig
                .FromFile(file)
                .Read<MyApplicationConfiguration>();

            CompareToExpectedResult(config, file);
        }

        /// <summary>
        /// If you config class forms a tree then nested complex objects would inherit the values from their parent.
        /// This feature is handy if you don't want to split your config file into multiple config files but you want
        /// objects to inherit values from their parents. Look into ./files/my-application/ftp-config.toml for an example
        /// of using nested configuration values. In this case <c>FtpConfig</c> contains a List of <c>FtpConfig</c> that
        /// would inherit the value from parent definitions.
        /// </summary>
        [Fact]
        public void ShouldCascadeValuesForHierarchicalValues()
        {
            var file = "./files/my-application/ftp-config.toml";
            
            var config = TomlConfig
                .FromFile(file)
                .Read<FtpConfig>();

            CompareToExpectedResult(config, file);
        }


        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class FtpConfig
        {
            public string AdminUser { get; set; }
            public string AdminPassword { get; set; }
            public string[] Users { get; set; }
            public string Path { get; set; }
            public string Domain { get; set; }
            public List<FtpConfig> Domains { get; set; }
        }

        private static void CompareToExpectedResult<T>(T config, string file)
        {
            var changeExtension = Path.ChangeExtension(file, "json")
                .Replace("my-application", "my-application.json");
            
            var expected =
                JsonConvert.DeserializeObject<T>(
                    File.ReadAllText(changeExtension));

            TestTools.AssertEqual(expected, config);
        }
    }
}