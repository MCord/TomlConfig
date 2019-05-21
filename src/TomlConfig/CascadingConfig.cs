using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Test")]

namespace TomlConfig
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class CascadingConfig
    {
        public static T Read<T>(Stream data, Dictionary<string, string> overrides = null) 
            where T : class
        {
            var tc = new TomlConfigReader();
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

        public bool IsSubConfigEntityContainer<T>(Type type)
        {
            return type == typeof(T)
                   || (type.IsArray && type.GetElementType() == typeof(T))
                   || (type == typeof(List<T>));
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