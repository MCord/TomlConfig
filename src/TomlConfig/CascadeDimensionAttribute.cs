namespace TomlConfig
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Property)]
    public class CascadeDimensionAttribute : Attribute
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public PropertyInfo Target { get; set; } 

        public CascadeDimensionAttribute(int order, string name)
        {
            Order = order;
            Name = name;
        }
    }
}