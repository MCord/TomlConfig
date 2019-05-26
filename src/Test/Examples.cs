namespace Test
{
    using System.IO;
    using Newtonsoft.Json;
    using TomlConfiguration;
    using Xunit;

    public class Examples
    {
        public class MyApplicationConfiguration
        {
            public string ApplicationName { get; set; }
            public string CopyRight { get; set; }
            public string Environment { get; set; }
            public string LogPath { get; set; }
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
        /// Add a <c>Secret</c> attribute to your secret properties and use toml-config-tool to
        /// encrypt your file. You can then specify the master key (or set MASTER_KEY environment variable)
        /// to decrypt your file.
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

        private static void CompareToExpectedResult(MyApplicationConfiguration config, string file)
        {
            var expected =
                JsonConvert.DeserializeObject<MyApplicationConfiguration>(
                    File.ReadAllText(Path.ChangeExtension(file, "json")));

            TestTools.AssertEqual(expected, config);
        }
    }
}