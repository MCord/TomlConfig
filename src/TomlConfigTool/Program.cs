namespace TomlConfigTool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Mono.Options;

    class Program
    {
        enum Action
        {
            Encrypt, Decrypt, Verify, Help
        }
        static int Main(string[] args)
        {
            var action = Action.Help;
            var filePatterns = new List<string>();
            var configKeyNames = new List<string>();
            var resursive = false;
            var masterKey = Environment.GetEnvironmentVariable("MASTER_KEY");
            
            var options = new OptionSet { 
                { "e|encrypt", "encrypts the selected files.", e => action= Action.Encrypt}, 
                { "d|decrypt", "decrypt the selected files.", d => action= Action.Decrypt}, 
                { "v|verify", "verifies the validity of configuration and secrets encryption given a master key.", v => action= Action.Verify},
                { "f|file", "input file name, can use wildcards or specify multiple times.", filePatterns.Add},
                
                { "k|key", "config keys containing secret data. This in a regex that should match " +
                           "properties to be encrypted/decrypted. it defaults to any key containing 'password' in the name ", configKeyNames.Add},
                
                { "r|recursive", "when specified would search subfolders for files", (bool r) => resursive = r},
                { "m|master_key", "the master key to use, default to the value provided by 'MASTER_KEY' environment variable", m=> masterKey = m },
                { "h|help", "show this message and exit", h => action= Action.Help},
            };

            if (!filePatterns.Any())
            {
                filePatterns.Add("*.toml");
            }

            if (!configKeyNames.Any())
            {
                configKeyNames.Add(".*password.*");
            }

            var implementation = new ConfigToolImplementation(filePatterns,
                resursive, 
                Convert.FromBase64String(masterKey), 
                configKeyNames);

            try
            {
                switch (action)
                {
                    case Action.Encrypt:
                        implementation.Encrypt();
                        return 0;
                    default:
                        ShowHelp(options);
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("Toml Config Management Tool v"+ Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("");
            Console.WriteLine("Use this tool to encrypt, decrypt and verify secrets in your toml files");
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions (Console.Out);
        }
    }
}