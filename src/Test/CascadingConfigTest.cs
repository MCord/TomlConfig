namespace Test
{
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;
    using NFluent;
    using TomlConfig;
    using Xunit;

    public class CascadingConfigTest
    {
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class SampleConfig
        {
            public string Host { get; set; }
            public string CopyRight { get; set; }
            public string WebServerRoot { get; set; }
            public string Stack { get; set; }
            public UserConfig[] User { get; set; }
            
            public List<SiteConfig> Site { get; set; }

            public class SiteConfig : SampleConfig
            {
                
            }
            [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
            public class UserConfig
            {
                public string Name { get; set; }
                public string[] Rights { get; set; }
            }
        }

        public class SampleConfigWithTowLevelDimension
        {
            public string Host { get; set; }
            public string Path { get; set; }
            public string FileType { get; set; }

            public List<SubConfig> Hosts { get; set; }

            public class SubConfig : SampleConfigWithTowLevelDimension
            {
                public List<SubConfig> Paths { get; set; }
            }
        }

        [Fact]
        public void ShouldInheritNonSpecifiedValuesFromParent()
        {
            var subject = TomlConfig.Read<SampleConfig>(Resources.Load("cascading-sample.toml"));
            Check.That(subject.Site.Select(x=>x.CopyRight).Distinct())
                .IsEquivalentTo("ACME LTD.");
        }

        [Fact]
        public void ShouldReadConfigWithOneDimension()
        {
            var subject = TomlConfig.Read<SampleConfig>(Resources.Load("cascading-sample.toml"));

            var root = subject;

            Check.That(root.Host).IsEqualTo("www.default-hosting.com");
            Check.That(root.WebServerRoot).IsEqualTo("/var/sites/default/www");
            Check.That(root.Stack).IsEqualTo("java");
            Check.That(root.User[0].Name).IsEqualTo("root");
            Check.That(root.User[0].Rights).IsEquivalentTo("read", "write", "create", "remove");

            var site1 = subject.Site[0];

            Check.That(site1.Host).IsEqualTo("www.myproject.com");
            Check.That(site1.WebServerRoot).IsEqualTo("/var/sites/myproject/www");
            Check.That(site1.Stack).IsEqualTo("php");
            Check.That(site1.User[0].Name).IsEqualTo("root");
            Check.That(site1.User[0].Rights).IsEquivalentTo("read", "write", "create", "remove");
            Check.That(site1.User[1].Name).IsEqualTo("john");
            Check.That(site1.User[1].Rights).IsEquivalentTo("read", "write");

            var site2 = subject.Site[1];

            Check.That(site2.Host).IsEqualTo("www.second-project.com");
            Check.That(site2.WebServerRoot).IsEqualTo("/var/sites/second-project/www");
            Check.That(site2.Stack).IsEqualTo("haskell");
            Check.That(site2.User[0].Name).IsEqualTo("root");
            Check.That(site2.User[0].Rights).IsEquivalentTo("read", "write", "create", "remove");
            Check.That(site2.User[1].Name).IsEqualTo("jess");
            Check.That(site2.User[1].Rights).IsEquivalentTo("read", "write", "remove");
        }

        [Fact]
        public void ShouldReadConfigWith3Levels()
        {
            var data = Resources.Load("sample-3-levels.toml");
            var subject = TomlConfig.Read<SampleConfigWithTowLevelDimension>(data);
            var root = subject;

            Check.That(root.Host).IsEqualTo("www.default.com");
            Check.That(root.Path).IsEqualTo("/");
            Check.That(root.FileType).IsEqualTo("html");

            var subdomain1 = subject.Hosts.Single(x=> x.Host == "www.site1.com");
            Check.That(subdomain1.Path).IsEqualTo("/");
            Check.That(subdomain1.FileType).IsEqualTo("java");

            var subdomain1PhpPath = subdomain1.Paths.Single(x => x.Path == "/site1/php");
            
            Check.That(subdomain1PhpPath.Host).IsEqualTo(subdomain1.Host);
            Check.That(subdomain1PhpPath.Path).IsEqualTo("/site1/php");
            Check.That(subdomain1PhpPath.FileType).IsEqualTo("php");
            
            var subDomainHostPath2Value = "/site1/java";
            var subdomain1PhpPath2 = subdomain1.Paths.Single(x => x.Path == "/site1/java");
            
            Check.That(subdomain1PhpPath2.Host).IsEqualTo(subdomain1.Host);
            Check.That(subdomain1PhpPath2.Path).IsEqualTo("/site1/java");
            Check.That(subdomain1PhpPath2.FileType).IsEqualTo("java");


            var subDomain2Path2 = subject.Hosts.Single(x=> x.Host == "www.site2.com")
                .Paths.Single(x=> x.Path =="/site2/dotnet");

            Check.That(subDomain2Path2.FileType).IsEqualTo("dotnet");
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class MultiLevelConfig
        {
            public int Value { get; set; }
            public string Path { get; set; }
            
            public List<MultiLevelConfig> SubPaths { get; set; }
        }

        [Fact]
        public void ShouldOverrideProperties()
        {
            var data = Resources.Load("multi-level.toml");
            var over = "overridden";
            var subject = TomlConfig.Read<MultiLevelConfig>(data, new Dictionary<string, string>()
            {
                {"Value", "42"},
                {"Path", over}
            });

            foreach (var entry in TomlConfig.GetAllConfigEntries(subject, x=> x.SubPaths))
            {
                Check.That(entry.Value).IsEqualTo(42);
                Check.That(entry.Path).IsEqualTo(over);
            }
        }
    }
}