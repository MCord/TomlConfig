namespace TomlConfiguration
{
    using System;
    using System.IO;

    public class NamedStream : IDisposable
    {
        public string Path { get; set; }
        public Stream Stream { get; set; }

        public NamedStream(string path, Stream stream)
        {
            Path = path;
            Stream = stream;
        }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}