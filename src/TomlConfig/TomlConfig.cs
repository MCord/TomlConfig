namespace TomlConfig
{
    using System;
    using System.IO;
    using Tomlyn;
    using Tomlyn.Syntax;

    public static class TomlConfig
    {
        public static T Read<T>(Stream data) => new TomlConfigReader().Read<T>(data);
        public static object Read(Type type,Stream data) => new TomlConfigReader().Read(type, data);
        public static T ReadWithDefault<T>(Stream data, T @default) => new TomlConfigReader().ReadWithDefault(data,@default);
        public static DocumentSyntax ReadTable(string file) => Toml.Parse(File.ReadAllBytes(file), file);

        public static void WriteDocument(string file, DocumentSyntax doc)
        {
            using (var writer = File.CreateText(file))
            {
                doc.WriteTo(writer);
            }
        }
    }
}