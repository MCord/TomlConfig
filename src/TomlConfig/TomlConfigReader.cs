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

            return ConvertTable(type, tomlTable.ToModel(), parent);
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

            if (include.Length == 1)
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
            return ConvertTable(type, tomlTable.ToModel(), @default);
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

        private object ConvertTable(Type type, TomlTable tomlTable, object parent)
        {
            var instance = Cloner.Clone(parent, type);

            if (type.IsGenericDictionary())
            {
                return ConvertDictionary(type, tomlTable, parent, instance);
            }
            
            return ConvertComplexObject(type, tomlTable, instance);
        }

        private object ConvertComplexObject(Type type, TomlTable tomlTable, object instance)
        {
            foreach (var key in tomlTable.Keys)
            {
                var match = type.GetProperty(key )??
                            throw new TomlConfigurationException(
                                $"No public instance property named '{key}' is found on '{type.FullName}'");
                try
                {
                    var localParent = GetLocalParent(instance, match);
                    var convertedValue = ConvertToType(match.PropertyType,
                        tomlTable[key], localParent, match.GetCustomAttributes().ToArray());

                    match.SetValue(instance, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new TomlConfigurationException(
                        $"Unable to convert value '{tomlTable[key]}' to type {match.PropertyType} from key '{key}'"
                        , ex);
                }
            }

            return instance;
        }

        private static object GetLocalParent(object instance, PropertyInfo match)
        {
            if (match.PropertyType.IsArray 
                || match.PropertyType.IsGenericDictionary()
                || match.PropertyType.IsGenericList())
            {
                return instance;
            }

            return match.GetValue(instance);
        }

        private object ConvertDictionary(Type type, TomlTable tomlTable, object parent, object instance)
        {
            var keyType = type.GenericTypeArguments[0];
            var valueType = type.GenericTypeArguments[1];
            foreach (var key in tomlTable.Keys)
            {
                try
                {
                    var convertedKey = ConvertToType(keyType, key, parent, Array.Empty<Attribute>());
                    var convertedValue = ConvertToType(valueType, tomlTable[key], parent, Array.Empty<Attribute>());
                    type.GetMethod("Add")?.Invoke(instance, new[] {convertedKey, convertedValue});
                }
                catch (Exception ex)
                {
                    throw new TomlConfigurationException(
                        $"Unable to convert dictionary element with key  {key} to dictionary", ex);
                }
            }

            return instance;
        }

        private object ConvertToType(Type targetType, object value, object parent, Attribute[] metadata)
        {
            switch (value)
            {
                case TomlValue v:
                    return ConvertValue(v.ValueAsObject, targetType, parent, metadata);
                case TomlArray array:
                    return ConvertArray(array.GetTomlEnumerator(), targetType, parent);
                case TomlTableArray tableArray:
                    return ConvertArray(tableArray, targetType, parent);
                case TomlTable table:
                    return ConvertTable(targetType, table, parent);
                default:
                    return ConvertValue(value, targetType, parent, metadata);
            }
        }

        private object ConvertValue(object value, Type t, object parent, Attribute[] metadata)
        {
            foreach (var cnv in settings.CustomTypeConverters)
            {
                if (cnv.CanConvert(t, metadata ?? Array.Empty<Attribute>()))
                {
                    return cnv.Convert(value, t, parent);
                }
            }

            if (t.IsEnum && value is string enumString)
            {
                return Enum.Parse(t, enumString);
            }

            return Convert.ChangeType(value, t);
        }

        private object ConvertArray(IEnumerable<TomlObject> items, Type targetType, object parent)
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

                var converted = items.Select((x, i) => ConvertToType(elementType, x, parent, null)).ToArray();
                var result = Array.CreateInstance(elementType, converted.Length);
                converted.CopyTo(result, 0);
                return result;
            }

            if (targetType.IsGenericList())
            {
                var result = (IList) Activator.CreateInstance(targetType);
                var genericArgument = targetType.GetGenericArguments()[0];

                foreach (var converted in items.Select((x,i) => 
                    ConvertToType(genericArgument, x, parent, null)))
                {
                    result.Add(converted);
                }

                return result;
            }

            throw new TomlConfigurationException(
                $"Conversion from toml array to {targetType.FullName} is not supported. " +
                "Please use an Array or a generic List<T>.");
        }
    }
    
}