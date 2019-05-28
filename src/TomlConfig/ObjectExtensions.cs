namespace TomlConfiguration
{
    using System;
    using System.Collections;
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

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }
}