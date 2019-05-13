namespace Test
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public static class Resources
    {
        public static Stream Load(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(name));

            if (resourceName == null)
            {
                throw new Exception($"Resource not found ending in '{name}'");
            }

            return assembly.GetManifestResourceStream(resourceName);
        }

        public static string LoadText(string name)
        {
            var reader = new StreamReader(Load(name));
            return reader.ReadToEnd();
        }
    }
}