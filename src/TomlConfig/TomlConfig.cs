namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

        public static IEnumerable<KeyValueSyntax> GetAllKeys(this DocumentSyntax document)
        {
            foreach (var value in document.KeyValues)
            {
                yield return value;
            }

            foreach (var value in document.Tables.SelectMany(GetAllKeys))
            {
                yield return value;
            }
        }

        private static IEnumerable<KeyValueSyntax> GetAllKeys(TableSyntaxBase table)
        {
            foreach (var item in table.Items)
            {
                yield return item;
            }
            
            foreach (var sub in table.Items.OfType<TableSyntax>().SelectMany(GetAllKeys))
            {
                yield return sub;
            }
        }
    }
}