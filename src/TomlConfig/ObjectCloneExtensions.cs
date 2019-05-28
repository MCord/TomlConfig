namespace System
{
    using Newtonsoft.Json;

    public static class Cloner
    {
        /// <summary>
        /// Creates a copy of an object into a different type using json serialization. This insures that
        /// all properties with the same name have the same values on the newly created object.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object Clone(object source, Type targetType)
        {
            if (source == null || !source.GetType().IsAssignableFrom(targetType))
            {
                return Activator.CreateInstance(targetType);
            }

            var json = JsonConvert.SerializeObject(source);

            return JsonConvert.DeserializeObject(json, targetType);
        }
    }

}
