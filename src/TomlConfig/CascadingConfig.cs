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
        private readonly Dictionary<string, T> mappings;

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

        private Dictionary<string, T> ReadMappingsFromInstance(T instance, CascadeDimensionAttribute[] dimensions)
        {
            var result = new Dictionary<string, T> {{"", instance}};

            var ancestry = new Stack<T>();
            ancestry.Push(instance);
            AddDimension(result, ancestry, dimensions[0], dimensions.Skip(1).ToArray(), string.Empty);

            return result;
        }

        private void AddDimension(Dictionary<string, T> result, Stack<T> ancestry,
            CascadeDimensionAttribute currentDimension,
            CascadeDimensionAttribute[] remaining, string path)
        {
            var valueArray = (T[]) ancestry.Peek().GetType().GetProperty(currentDimension.Name)
                ?.GetValue(ancestry.Peek());

            if (valueArray == null || valueArray.Length == 0)
            {
                return;
            }

            foreach (var dimensionInstance in valueArray)
            {
                var separator = currentDimension.Target.GetValue(dimensionInstance);
                var subPath = GetSubPath(path, separator);
                result.Add(subPath, dimensionInstance);

                if (remaining.Any())
                {
                    ancestry.Push(dimensionInstance);
                    AddDimension(result, ancestry, remaining[0], remaining.Skip(1).ToArray(), subPath);
                    ancestry.Pop();
                }
            }
        }


        private static string GetSubPath(string path, object separator)
        {
            return string.Join("/", new[] {path, separator.ToString()}.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public T GetConfigAtLevel(params string[] dimensions)
        {
            var key = string.Join("/", dimensions);

            if (mappings.TryGetValue(key, out var value))
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
    }
}