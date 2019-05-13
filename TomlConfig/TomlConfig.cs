namespace TomlConfig
{
    using System.IO;
    using Nett;

    public class TomlConfig
    {
        public static T Read<T>(Stream data)
        {
            return Toml.ReadStream<T>(data);
        }
    }
}
