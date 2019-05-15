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
            var instance = (T) tc.ReadWithDefault(containerType, data, null);

            mappings = ReadMappingsFromInstance(instance, dimensions);
        }

        private Dictionary<KeyPath, T> ReadMappingsFromInstance(T instance, CascadeDimensionAttribute[] dimensions)
        {
            var rootPath = KeyPath.Empty;
            var result = new Dictionary<KeyPath, T> {{rootPath, instance}};

            AddDimensions(result, instance, dimensions, rootPath);

            return result;
        }

        private void AddDimensions(Dictionary<KeyPath, T> result, T instance,
            CascadeDimensionAttribute[] dimensions, KeyPath path)
        {
            foreach (var dimension in dimensions)
            {
                var valueArray = (T[]) instance.GetType().GetProperty(dimension.Name)
                    ?.GetValue(instance);

                if (valueArray == null || valueArray.Length == 0)
                {
                    continue;
                }

                foreach (var dimensionInstance in valueArray)
                {
                    var subPath = path.GetSubPath(dimension.Target.GetValue(dimensionInstance)?.ToString());
                    result.Add(subPath, dimensionInstance);
                    AddDimensions(result, dimensionInstance, dimensions,subPath);
                }
            }
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

        public IEnumerable<(string[], T)> GetAllConfigEntries()
        {
            return mappings.Select(x => (x.Key.Values, x.Value));
        }

        private class KeyPath
        {
            public readonly string[] Values;

            public KeyPath(string[] values)
            {
                Values = values;
            }

            public static KeyPath Empty => new KeyPath(new string[0]);

            public KeyPath GetSubPath(string value)
            {
                if (value == null)
                {
                    throw new TomlConfigurationException("Key cannot be null");
                }

                var keys = new List<string>(Values) {value};
                return new KeyPath(keys.ToArray());
            }

            protected bool Equals(KeyPath other)
            {
                if (other.Values.Length != Values.Length)
                {
                    return false;
                }

                for (int i = 0; i < other.Values.Length; i++)
                {
                    if (other.Values[i] != Values[i])
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
                return string.Join("", Values).GetHashCode();
            }

            public override string ToString()
            {
                return string.Join(">", Values);
            }
        }
    }
}