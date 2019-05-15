using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Test")]

namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class CascadingConfig<T> where T : class
    {
        private readonly Dictionary<KeyPath, T> mappings;

        public CascadingConfig(Stream data)
        {
            var compiler = new Compiler();

            var dimensions = GetDimensions();
            var dimensionNames = dimensions.Select(x => x.Name).ToArray();

            var containerType = compiler.CompileContainer(typeof(T), dimensionNames);

            var tc = new TomlConfig();
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
            var instance = (T) tc.ReadWithDefault(containerType, data);

            mappings = ReadMappingsFromInstance(instance, dimensions);
        }

        private Dictionary<KeyPath, T> ReadMappingsFromInstance(T instance, CascadeDimensionAttribute[] dimensions)
        {
            var rootPath = KeyPath.Empty;
            var result = new Dictionary<KeyPath, T> {{rootPath, instance}};

            var ancestry = new Stack<T>();
            ancestry.Push(instance);
            AddDimension(result, ancestry, dimensions[0], dimensions.Skip(1).ToArray(), rootPath);

            return result;
        }

        private void AddDimension(Dictionary<KeyPath, T> result, Stack<T> ancestry,
            CascadeDimensionAttribute currentDimension,
            CascadeDimensionAttribute[] remaining, KeyPath path)
        {
            var valueArray = (T[]) ancestry.Peek().GetType().GetProperty(currentDimension.Name)
                ?.GetValue(ancestry.Peek());

            if (valueArray == null || valueArray.Length == 0)
            {
                return;
            }

            foreach (var dimensionInstance in valueArray)
            {
                var subPath = path.GetSubPath(currentDimension.Target.GetValue(dimensionInstance)?.ToString());
                result.Add(subPath, dimensionInstance);

                if (remaining.Any())
                {
                    ancestry.Push(dimensionInstance);
                    AddDimension(result, ancestry, remaining[0], remaining.Skip(1).ToArray(), subPath);
                    ancestry.Pop();
                }
            }
        }


        private static string[] GetSubPath(string[] path, string toAdd)
        {
            var subPath = new List<string>(path) {toAdd};
            return subPath.Where(x => x != null).ToArray();
        }

        public T GetConfigAtLevel(params string[] dimensions)
        {
            if (mappings.TryGetValue(new KeyPath(dimensions), out var value))
            {
                return value;
            }

            if (dimensions.Length > 1)
            {
                return GetConfigAtLevel(SkipLast(dimensions));
            }

            throw new TomlConfigurationException("Not configuration matched.");
        }

        private string[] SkipLast(string[] dimensions)
        {
            var result = new string[dimensions.Length - 1];
            Array.Copy(dimensions, result, result.Length);
            return result;
        }

        private static CascadeDimensionAttribute[] GetDimensions()
        {
            var dimensions = typeof(T).GetProperties().Select(x =>
                {
                    var customAttribute = x.GetCustomAttribute<CascadeDimensionAttribute>();
                    if (customAttribute != null)
                    {
                        customAttribute.Target = x;
                    }

                    return customAttribute;
                })
                .Where(x => x != null)
                .OrderBy(x => x.Order)
                .ToArray();

            if (!dimensions.Any())
            {
                throw new TomlConfigurationException($"No dimension is specified on the type {typeof(T).FullName} " +
                                                     "at least one CascadeDimensionAttribute should be specified" +
                                                     " on the type.");
            }

            return dimensions;
        }

        public IEnumerable<T> GetAllConfigEntries()
        {
            return mappings.Values;
        }

        private class KeyPath
        {
            private readonly string[] values;

            public KeyPath(string[] values)
            {
                this.values = values;
            }
            
            public static KeyPath Empty => new KeyPath(new string[0]);

            public KeyPath GetSubPath(string value)
            {
                if (value == null)
                {
                    throw new TomlConfigurationException("Key cannot be null");
                }
                
                var keys = new List<string>(values) {value};
                return new KeyPath(keys.ToArray());
            }

            protected bool Equals(KeyPath other)
            {
                if (other.values.Length != values.Length)
                {
                    return false;
                }

                for (int i = 0; i < other.values.Length; i++)
                {
                    if(other.values[i] != values[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return Equals((KeyPath) obj);
            }

            public override int GetHashCode()
            {
                return string.Join("", values).GetHashCode();
            }
        }
    }
    
}