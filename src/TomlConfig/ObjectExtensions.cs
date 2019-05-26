namespace TomlConfiguration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    internal static class ObjectExtensions
    {
        internal static object WithOverrides<T>(this object instance, Dictionary<string,string> overrides)
        {
            if (overrides == null || instance == null || overrides.Count == 0)
            {
                return instance;
            }
            
            foreach (var (property, value) in instance
                .GetType()
                .GetProperties()
                .Select(p=> (p,p.GetValue(instance)))
                .Where(v=> v.Item2 != null))
            {
                
                if (value is T)
                {
                    WithOverrides<T>(value, overrides);
                    continue;
                }

                if (value is IEnumerable<T> enumerable)
                {
                    foreach (var eValue in enumerable)
                    {
                        WithOverrides<T>(eValue, overrides);
                    }

                    continue;
                }
                
                if (overrides.TryGetValue(property.Name, out var overrideValue))
                {
                    property.SetValue(instance, Convert.ChangeType(overrideValue, property.PropertyType));
                }
            }
            return instance;
        }

        internal static object GetPropertyValueByName(this object instance, params string[] names)
        {
            if (instance == null)
            {
                return null;
            }

            if (!names.Any())
            {
                throw new ArgumentException("No name is specified.");
            }

            if (instance is Array a && int.TryParse(names.First(), out var index))
            {
                var propertyValueByName = a.GetValue(index);
                return names.Length == 1 
                    ? propertyValueByName : 
                    GetPropertyValueByName(propertyValueByName, names.Skip(1).ToArray());
            }
            
            if (instance is ICollection l && int.TryParse(names.First(), out var listIndex))
            {
                var propertyValueByName = l.Cast<object>().ToArray()[listIndex];
                return names.Length == 1 
                    ? propertyValueByName : 
                    GetPropertyValueByName(propertyValueByName, names.Skip(1).ToArray());
            }
            
            var type = instance.GetType();
            var prop = type.GetProperty(names.First());
            if (prop == null)
            {
                return null;
            }

            var value = prop.GetValue(instance);

            if (names.Length == 1)
            {
                return value;
            }

            return GetPropertyValueByName(value, names.Skip(1).ToArray());
        }
    }
}