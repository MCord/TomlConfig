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

        public static T Read<T>(string data, Dictionary<string, string> overrides = null, SecretKeeper keeper = null) 
            where T : class
        {
            return Read<T>(new MemoryStream(Encoding.UTF8.GetBytes(data)), overrides, keeper);
        }
        
        public static T Read<T>(Stream data, Dictionary<string, string> overrides = null, SecretKeeper keeper = null) 
            where T : class
        {
            var tc = new TomlConfigReader();
            tc.AddTypeConverter(new PasswordTypeConverter(keeper ?? new SecretKeeper()));
            var stack = new Stack<T>();

            tc.OnTableParsingStarted += (i, table, @default) =>
            {
                var t = i as T;
                if (t == null)
                {
                    return @default;
                }


                var newDefault = stack.Count > 0 ? stack.Peek() : @default;

                stack.Push(t);

                return newDefault;
            };

            tc.OnTableParsingFinished += (o, table) =>
            {
                var t = o as T;
                if (t == null)
                {
                    return;
                }

                stack.Pop();
            };
            var instance = (T) tc.ReadWithDefault(typeof(T), data, null);
            
            SetOverrides<T>(instance, overrides);

            return instance;
        }

        private static void SetOverrides<T>(object instance, Dictionary<string,string> overrides)
        {
            if (overrides == null || instance == null)
            {
                return;
            }
            
            foreach (var (property, value) in instance
                .GetType()
                .GetProperties()
                .Select(p=> (p,p.GetValue(instance)))
                .Where(v=> v.Item2 != null))
            {
                
                if (value is T)
                {
                    SetOverrides<T>(value, overrides);
                    continue;
                }

                if (value is IEnumerable<T> enumerable)
                {
                    foreach (var eValue in enumerable)
                    {
                        SetOverrides<T>(eValue, overrides);
                    }

                    continue;
                }
                
                if (overrides.TryGetValue(property.Name, out var overrideValue))
                {
                    property.SetValue(instance, Convert.ChangeType(overrideValue, property.PropertyType));
                }
            }
        }
        
        public static IEnumerable<T> GetAllConfigEntries<T>(T instance, Func<T,IEnumerable<T>> selector)
        {
            yield return instance;
            var subs = selector(instance);

            if (subs == null)
            {
                yield break;
            }
            
            foreach (var sub in subs.SelectMany(x=> GetAllConfigEntries(x, selector)))
            {
                yield return sub;
            }
        }
    }
}