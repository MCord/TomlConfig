namespace TomlConfiguration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Tomlyn;
    using Tomlyn.Model;
    using Tomlyn.Syntax;

    public class TomlConfigReader
    {
        private readonly TomlConfigSettings settings;

        public TomlConfigReader(TomlConfigSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        ///     Reads an stream containing a toml file and returns an object of type T
        /// </summary>
        /// <param name="data">The stream containing toml</param>
        /// <param name="refPath">The path to file name containing toml data.</param>
        /// <typeparam name="T">The type to be deserialized.</typeparam>
        /// <returns>An object of type T deserialized from file content.</returns>
        public T Read<T>(Stream data, string refPath)
        {
            return (T) Read(typeof(T), data, refPath);
        }

        /// <summary>
        ///     Reads an stream containing a toml file and returns an object.
        /// </summary>
        /// <param name="type">object Type to be deserialized</param>
        /// <param name="data">The stream containing toml</param>
        /// <param name="refPath">The path to file name containing toml data.</param>
        /// <returns>An object deserialized from file content.</returns>
        public object Read(Type type, Stream data, string refPath)
        {
            var tomlTable = Toml.Parse(new StreamReader(data).ReadToEnd());
            var parent = GetInheritInstanceFromDirective(type, tomlTable, refPath);

            var ancestors = new Stack<object>();

            if (parent != null)
            {
                ancestors.Push(parent);
            }

            return ConvertTable(type, tomlTable.ToModel(), ancestors);
        }

        IEnumerable<SyntaxTrivia> GetFileTrivia(DocumentSyntax doc)
        {
            if (doc.LeadingTrivia != null)
            {
                foreach (var trivia in doc.LeadingTrivia)
                {
                    yield return trivia;
                }
            }

            foreach (var kv in doc.KeyValues.Where(x=>x.LeadingTrivia != null))
            {
                foreach (var trivia in kv.LeadingTrivia)
                {
                    yield return trivia;
                }
            }
        }

        private object GetInheritInstanceFromDirective(Type type, DocumentSyntax doc, string refPath)
        {
            var include = GetFileTrivia(doc)
                .Where(trivia => trivia.Kind == TokenKind.Comment)
                .Where(comment => comment.Text.StartsWith("#include"))
                .Select(x => Regex.Split(x.Text, @"\s").LastOrDefault())
                .ToArray();

            if (include.Length > 1)
            {
                throw new TomlConfigurationException("Only one include directive is allowed in config.");
            }

            if (include?.Length == 1)
            {
                var basePath = Path.GetDirectoryName(refPath) ?? ".";
                var parentPath = Path.Combine(basePath, include[0]);

                if (!File.Exists(parentPath))
                {
                    throw new TomlConfigurationException(
                        $"Missing include file {parentPath} included in '{refPath}'");
                }
                
                using (var parentStream = File.Open(parentPath, FileMode.Open))
                {
                    return Read(type, parentStream, parentPath);
                }
            }

            return null;
        }

        /// <summary>
        ///     Deserialize an object using defaults value from another object.
        /// </summary>
        /// <param name="type">The type of class to be deserialized.</param>
        /// <param name="data">The stream containing toml data.</param>
        /// <param name="default">An instance with the same type as the object being deserialized.</param>
        /// <returns>A deserialized object</returns>
        /// <remarks>
        ///     If a property for the instance is not specified in the stream, the value from the default instance will be set
        ///     on the object.
        /// </remarks>
        public object ReadWithDefault(Type type, Stream data, object @default)
        {
            var tomlTable = Toml.Parse(new StreamReader(data).ReadToEnd());
            var ancestors = new Stack<object>();

            if (@default != null)
            {
                ancestors.Push(@default);
            }

            return ConvertTable(type, tomlTable.ToModel(), ancestors);
        }

        /// <summary>
        ///     This function can deserialize an object using default value from another object.
        /// </summary>
        /// <param name="data">The stream containing toml data.</param>
        /// <param name="default">An instance with the same type as the object being deserialized.</param>
        /// <typeparam name="T">The type of the object to be deserialized.</typeparam>
        /// <returns>A deserialized object</returns>
        /// <remarks>
        ///     If a property for the instance is not specified in the stream, the value from the default instance will be set
        ///     on the object.
        /// </remarks>
        public T ReadWithDefault<T>(Stream data, T @default)
        {
            return (T) ReadWithDefault(typeof(T), data, @default);
        }

        private object ConvertTable(Type type, TomlTable tomlTable, Stack<object> ancestors)
        {
            var instance = Activator.CreateInstance(type);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            foreach (var key in tomlTable.Keys)
            {
                var match = properties.FirstOrDefault(x => x.Name == key) ??
                            throw new TomlConfigurationException(
                                $"No public instance property named '{key}' is found on '{type.FullName}'");

                properties.Remove(match);
                try
                {
                    ancestors.Push(instance);
                    var convertedValue = ConvertToType(match.PropertyType, tomlTable[key], match, ancestors);
                    match.SetValue(instance, convertedValue);
                    ancestors.Pop();
                }
                catch (Exception ex)
                {
                    throw new TomlConfigurationException(
                        $"Unable to convert value '{tomlTable[key]}' to type {match.PropertyType} from key '{key}'"
                        , ex);
                }
            }

            if (properties.Any())
            {
                SetUnspecifiedPropertiesFromAncestors(type, ancestors, properties, instance);
            }

            return instance;
        }

        private static void SetUnspecifiedPropertiesFromAncestors(Type type, Stack<object> ancestors,
            List<PropertyInfo> properties, object instance)
        {
            var matchingParent = ancestors.FirstOrDefault(x => x.GetType().IsAssignableFrom(type));

            if (matchingParent != null)
            {
                foreach (var unspecifiedProperty in properties)
                {
                    unspecifiedProperty.SetValue(instance, unspecifiedProperty.GetValue(matchingParent));
                }
            }
        }

        private object ConvertToType(Type targetType, object value, PropertyInfo propInfo, Stack<object> ancestors)
        {
            switch (value)
            {
                case TomlValue v:
                    return Convert(v.ValueAsObject, targetType, propInfo);
                case TomlArray array:
                    return ConvertValueArray(array.GetTomlEnumerator(), targetType, ancestors);
                case TomlTableArray tableArray:
                    return ConvertValueArray(tableArray, targetType, ancestors);
                case TomlTable table:
                    return ConvertTable(targetType, table, ancestors);
                default:
                    return Convert(value, targetType, propInfo);
            }
        }

        private object Convert(object value, Type t, PropertyInfo propInfo)
        {
            foreach (var cnv in settings.CustomTypeConverters)
            {
                if (cnv.CanConvert(t, propInfo))
                {
                    return cnv.Convert(value, t);
                }
            }

            return System.Convert.ChangeType(value, t);
        }

        private object ConvertValueArray(IEnumerable<TomlObject> items, Type targetType, Stack<object> ancestors)
        {
            if (targetType.IsArray)

            {
                var elementType = targetType.GetElementType();

                if (elementType == null)
                {
                    var join = string.Join(",", items.Select(x => x.ToString()));

                    throw new TomlConfigurationException($"array value [{join}] can not be cast to type {targetType}" +
                                                         " because it's not an array.");
                }

                var converted = items
                    .Select(x => ConvertToType(elementType, x, null, ancestors)).ToArray();
                var result = Array.CreateInstance(elementType, converted.Length);
                converted.CopyTo(result, 0);
                return result;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var result = (IList) Activator.CreateInstance(targetType);
                var genericArgument = targetType.GetGenericArguments()[0];

                foreach (var converted in items.Select(x => ConvertToType(genericArgument, x, null, ancestors))
                )
                {
                    result.Add(converted);
                }

                return result;
            }

            throw new TomlConfigurationException(
                $"Conversion from toml array to {targetType.FullName} is not supported. " +
                $"Please use an array or a generic List.");
        }
    }
}