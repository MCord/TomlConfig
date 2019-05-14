namespace TomlConfig
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class CascadeDimensionAttribute : Attribute
    {
        public CascadeDimensionAttribute(int order, string name)
        {
            Order = order;
            Name = name;
        }

        public int Order { get; set; }
        public string Name { get; set; }
        public PropertyInfo Target { get; set; }
    }
}