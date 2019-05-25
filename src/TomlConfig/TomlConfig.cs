using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Test")]

namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Tomlyn;
    using Tomlyn.Syntax;

    public static class TomlConfig
    {
        public static T ReadWithDefault<T>(Stream data, T @default)
            => new TomlConfigReader(TomlConfigSettings.Default).ReadWithDefault(data, @default);

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

        public static T Read<T>(string data, Dictionary<string, string> overrides = null, SecretKeeper keeper = null)
            where T : class
        {
            return Read<T>(new MemoryStream(Encoding.UTF8.GetBytes(data)), overrides, keeper);
        }

        public static T Read<T>(Stream data, Dictionary<string, string> overrides = null, SecretKeeper keeper = null)
            where T : class
        {
            return Read<T>(data, new TomlConfigSettings
            {
                Overrides = overrides,
                CustomTypeConverters = new List<ITypeConverter>
                {
                    new PasswordTypeConverter(keeper ?? SecretKeeper.Default)
                }
            });
        }

        public static T Read<T>(Stream data, TomlConfigSettings settings)
            where T : class
        {
            var tc = new TomlConfigReader(settings ?? TomlConfigSettings.Default);
            var instance = (T) tc.ReadWithDefault(typeof(T), data, null);
            return (T) instance.WithOverrides<T>(settings?.Overrides);
        }

        public static IEnumerable<T> GetAllConfigEntries<T>(T instance, Func<T, IEnumerable<T>> selector)
        {
            yield return instance;
            var subs = selector(instance);

            if (subs == null)
            {
                yield break;
            }

            foreach (var sub in subs.SelectMany(x => GetAllConfigEntries(x, selector)))
            {
                yield return sub;
            }
        }
    }
}