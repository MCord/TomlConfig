namespace Test
{
    using Xunit;

    public class Examples
    {
        public class MyApplicationConfiguration
        {
            
            public string ApplicationName { get; set; }
            public string CopyRight { get; set; }
            public string Environment { get; set; }
            public string LogPath { get; set; }
            public DatabaseConfiguration Database{ get; set; }
            
            public class DatabaseConfiguration
            {
                public string Server { get; set; }
                public int Port { get; set; }
                public string DatabaseName { get; set; }
                public string User { get; set; }
                public string Password { get; set; }
            }
        }
        
        [Fact]
        public void ReadConfigFromFileExample()
        {
        }

    }
}