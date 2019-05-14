namespace Test
{
    using System;
    using System.Text.RegularExpressions;
    using FluentAssertions.Common;
    using NFluent;
    using TomlConfig;
    using Xunit;

    public class CompilerTest
    {
        public class Config
        {
        }

        private string RemoveWhiteSpace(string value)
        {
            return Regex.Replace(value, @"\s+", string.Empty);
        }

        private static void VerifyPropertyType(Type type, string propName, Type propType)
        {
            var prop = type.GetProperty(propName);
            prop.PropertyType.IsSameOrInherits(propType);
        }

        [Fact]
        public void ShouldCompileCodeForContainer()
        {
            var compiler = new Compiler();
            var type = compiler.CompileContainer(typeof(Config), new[] {"Site"});
            VerifyPropertyType(type, "Site", typeof(Config[]));
        }

        [Fact]
        public void ShouldCompileCodeForContainerWithMultipleDimensions()
        {
            var compiler = new Compiler();
            var type = compiler.CompileContainer(typeof(Config), new[] {"Site", "User"});

            VerifyPropertyType(type, "Site", typeof(Config[]));
            VerifyPropertyType(type, "User", typeof(Config[]));
        }

        [Fact]
        public void ShouldGenerateContainerCode()
        {
            var compiler = new Compiler();

            var code = compiler.GetCode("CompilerTest.Test", "Class42", new[] {"Site", "User"});
            Check.That(RemoveWhiteSpace(code))
                .IsEqualTo(RemoveWhiteSpace(Resources.LoadText("code.txt")));
        }
    }
}