namespace Test
{
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public static class Resources
    {
        public static Stream Load(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().First(x => x.EndsWith(name));
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}