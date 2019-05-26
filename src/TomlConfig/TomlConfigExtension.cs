namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tomlyn.Syntax;

    public static class TomlConfigExtension
    {
        public static IEnumerable<T> GetAllConfigEntries<T>(this T instance, Func<T, IEnumerable<T>> selector)
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

        private static IEnumerable<KeyValueSyntax> GetAllKeys(this TableSyntaxBase table)
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