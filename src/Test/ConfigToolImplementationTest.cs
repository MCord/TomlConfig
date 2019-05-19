namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NFluent;
    using TomlConfig;
    using TomlConfigTool;
    using Tomlyn;
    using Xunit;

    public class ConfigToolImplementationTest
    {
        [Fact]
        public void ShouldEncryptFile()
        {
            var file = Guid.NewGuid().ToString();
            
            File.WriteAllText(file, "MyPassword = \"ABC\"");

            var key = Guid.NewGuid().ToString();
            
            var subject = new ConfigToolImplementation(new List<string> {file}, false, 
                key, new List<string>
                {
                    ".+Password.+"
                } );
            
            subject.Encrypt();

            var table = TomlConfig.ReadTable(file);

            var keeper = new SecretKeeper(() => key);

            Check.That(keeper.Decrypt(table.ToModel()["MyPassword"].ToString()))
                .IsEqualTo("ABC");
        }

    }
}