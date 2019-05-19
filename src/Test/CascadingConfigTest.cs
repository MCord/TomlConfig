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
            [CascadeDimension(0, "Site")] public string Host { get; set; }

            public string CopyRight { get; set; }
            public string WebServerRoot { get; set; }
            public string Stack { get; set; }
            public UserConfig[] User { get; set; }

            [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
            public class UserConfig
            {
                public string Name { get; set; }
                public string[] Rights { get; set; }
            }
        }

        public class SampleConfigWithTowLevelDimension
        {
            [CascadeDimension(0, "SubDomain")] public string Host { get; set; }

            [CascadeDimension(0, "Paths")] public string Path { get; set; }

            public string FileType { get; set; }
        }

        [Fact]
        public void ShouldInheritNonSpecifiedValuesFromParent()
        {
            var subject = new CascadingConfig<SampleConfig>(Resources.Load("cascading-sample.toml"));
            Check.That(subject.GetConfigAtLevel("www.myproject.com").CopyRight)
                .IsEqualTo("ACME LTD.");
        }

        [Fact]
        public void ShouldReadConfigWithOneDimension()
        {
            var subject = new CascadingConfig<SampleConfig>(Resources.Load("cascading-sample.toml"));

            var root = subject.GetConfigAtLevel();

            Check.That(root.Host).IsEqualTo("www.default-hosting.com");
            Check.That(root.WebServerRoot).IsEqualTo("/var/sites/default/www");
            Check.That(root.Stack).IsEqualTo("java");
            Check.That(root.User[0].Name).IsEqualTo("root");
            Check.That(root.User[0].Rights).IsEquivalentTo("read", "write", "create", "remove");

            var site1 = subject.GetConfigAtLevel("www.myproject.com");

            Check.That(site1.Host).IsEqualTo("www.myproject.com");
            Check.That(site1.WebServerRoot).IsEqualTo("/var/sites/myproject/www");
            Check.That(site1.Stack).IsEqualTo("php");
            Check.That(site1.User[0].Name).IsEqualTo("root");
            Check.That(site1.User[0].Rights).IsEquivalentTo("read", "write", "create", "remove");
            Check.That(site1.User[1].Name).IsEqualTo("john");
            Check.That(site1.User[1].Rights).IsEquivalentTo("read", "write");

            var site2 = subject.GetConfigAtLevel("www.second-project.com");

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
            var subject = new CascadingConfig<SampleConfigWithTowLevelDimension>(data);
            var root = subject.GetConfigAtLevel();

            Check.That(root.Host).IsEqualTo("www.default.com");
            Check.That(root.Path).IsEqualTo("/");
            Check.That(root.FileType).IsEqualTo("html");

            var subDomainHost = "www.site1.com";
            var subdomain1 = subject.GetConfigAtLevel(subDomainHost);

            Check.That(subdomain1.Host).IsEqualTo(subDomainHost);
            Check.That(subdomain1.Path).IsEqualTo("/");
            Check.That(subdomain1.FileType).IsEqualTo("java");

            var subdomain1PhpPath = subject.GetConfigAtLevel(subDomainHost, "/site1/php");
            
            Check.That(subdomain1PhpPath.Host).IsEqualTo(subDomainHost);
            Check.That(subdomain1PhpPath.Path).IsEqualTo("/site1/php");
            Check.That(subdomain1PhpPath.FileType).IsEqualTo("php");
            
            var subDomainHostPath2Value = "/site1/java";
            var subdomain1PhpPath2 = subject.GetConfigAtLevel(subDomainHost, "/site1/java");
            
            Check.That(subdomain1PhpPath2.Host).IsEqualTo(subDomainHost);
            Check.That(subdomain1PhpPath2.Path).IsEqualTo("/site1/java");
            Check.That(subdomain1PhpPath2.FileType).IsEqualTo("java");


            var subDomain2Path2 = subject.GetConfigAtLevel("www.site2.com", "/site2/dotnet");

            Check.That(subDomain2Path2.FileType).IsEqualTo("dotnet");
        }

        public class MultiLevelConfig
        {
            [CascadeDimension(0, "SubLevel")]
            public int Value { get; set; }
            public string Path { get; set; }
        }

        [Fact]
        public void ShouldLoadMultiLevelConfig()
        {
            var data = Resources.Load("multi-level.toml");
            var subject = new CascadingConfig<MultiLevelConfig>(data);

            foreach (var entry in subject.GetAllConfigEntries())
            {
                Check.That(entry.Item2.Path)
                    .IsEqualTo(string.Join(",", entry.Item1));
            }
        }

        [Fact]
        public void ShouldOverrideProperties()
        {
            var data = Resources.Load("multi-level.toml");
            var over = "overridden";
            var subject = new CascadingConfig<MultiLevelConfig>(data, new Dictionary<string, string>()
            {
                {"Value", "42"},
                {"Path", over}
            });

            foreach (var entry in subject.GetAllConfigEntries().Select(x=> x.Item2))
            {
                Check.That(entry.Value).IsEqualTo(42);
                Check.That(entry.Path).IsEqualTo(over);
            }
        }
    }
}