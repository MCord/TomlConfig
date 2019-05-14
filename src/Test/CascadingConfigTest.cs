namespace Test
{
    using JetBrains.Annotations;
    using NFluent;
    using TomlConfig;
    using Xunit;

    public class CascadingConfigTest
    {
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class SampleConfig
        {
            [CascadeDimension(0, "Site")]
            public string Host { get; set; }
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
        public void ShouldInheritNonSpecifiedValuesFromParent()
        {
            var subject = new CascadingConfig<SampleConfig>(Resources.Load("cascading-sample.toml"));
            Check.That(subject.GetConfigAtLevel("www.myproject.com").CopyRight)
                .IsEqualTo("ACME LTD.");

        }
        
        public class SampleConfigWithTowLevelDimension
        {
            [CascadeDimension(0, "SubDomain")]
            public string Host { get; set; }
            [CascadeDimension(0, "Paths")]
            public string Path { get; set; }
            public string FileType { get; set; }
        }

        [Fact]
        public void Test()
        {
            var data = Resources.Load("sample -two-levels.toml");
            var subject = new CascadingConfig<SampleConfigWithTowLevelDimension>(data);
            var root = subject.GetConfigAtLevel();

            Check.That(root.Host).IsEqualTo("www.default.com");
            Check.That(root.Path).IsEqualTo("/");
            Check.That(root.FileType).IsEqualTo("html");
        }


    }
}