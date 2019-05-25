namespace TomlConfig
{
    using System;
    using System.Collections.Generic;
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
    }
}