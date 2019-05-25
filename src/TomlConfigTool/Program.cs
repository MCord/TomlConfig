namespace TomlConfigTool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Mono.Options;
    using Test;
    using TomlConfig;

    class Program
    {
        enum Action
        {
            Encrypt,
            Decrypt,
            Verify,
            Help
        }

        static int Main(string[] args)
        {
            var filePatterns = new List<string>();
            var configKeyNames = new List<string>();
            var resursive = false;
            var help = false;
            var auto = false;

            var masterKey = string.Empty;

            var options = new OptionSet
            {
                {"f|file=", "input file name, can use wildcards or specify multiple times.", filePatterns.Add},
                {
                    "k|key=", "config keys containing secret data. This in a regex that should match " +
                              "properties to be encrypted/decrypted. it defaults to any key containing 'password' in the name ",
                    x => configKeyNames.Add(x)
                },
                {"r|recursive", "when specified would search subfolders for files", (bool r) => resursive = r},
                {
                    "m|master_key=",
                    "the master key to use, defaults to the value provided by 'MASTER_KEY' environment variable",
                    m => masterKey = m
                },
                {
                    "a|auto", "Will generate and use a new master key, the value will be written to console.",
                    a => auto = a != null
                },
                {"h|help", "show this message and exit", h => help = h != null},
            };

            if (help)
            {
                ShowHelp(options);
                return 0;
            }
            
            try
            {
                var extra = options.Parse(args);

                SetDefaults(filePatterns, configKeyNames);
                
                
                switch (GetAction(extra))
                {
                    case Action.Encrypt:
                        var key = GetMasterKey(masterKey, auto);
                        VerifyKeyLength(key);
                        GetImplementation(filePatterns, resursive, key, configKeyNames)
                            .Encrypt();
                        return 0;
                    
                    case Action.Decrypt:
                        key = GetMasterKey(masterKey, auto);
                        GetImplementation(filePatterns, resursive, key, configKeyNames)
                            .Decrypt();
                        return 0;
                    
                    case Action.Verify:
                        key = GetMasterKey(masterKey, auto);
                        GetImplementation(filePatterns, resursive, key, configKeyNames)
                            .Verify();
                        return 0;
                        
                    default:
                        ShowHelp(options);
                        return 0;
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                ShowHelp(options);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\n Error: {ex.Message}");
                return 1;
            }
        }

        private static ConfigToolImplementation GetImplementation(List<string> filePatterns, bool resursive, string key,
            List<string> configKeyNames)
        {
            var implementation = new ConfigToolImplementation(filePatterns,
                resursive,
                key,
                configKeyNames);
            return implementation;
        }

        private static void VerifyKeyLength(string key)
        {
            if (key.Length < 16)
            {
                Console.WriteLine(
                    $"WARNING !!! the key length ({key.Length}) is too short, please consider using a longer key.");
            }
        }

        private static string GetMasterKey(string masterKey, bool auto)
        {
            if (auto)
            {
                if (!string.IsNullOrWhiteSpace(masterKey))
                {
                    throw new Exception("--auto and -master_key options can not be used together.");
                }

                var key = Security.GenerateKey();
                Console.WriteLine($"Generated a new master key: '{Convert.ToBase64String(key)}'");
                return Security.ToHexString(key);
            }

            if (string.IsNullOrWhiteSpace(masterKey))
            {
                masterKey = Environment.GetEnvironmentVariable("MASTER_KEY") ??
                            throw new TomlConfigurationException("No master key is provided");
            }


            return masterKey;
        }

        private static Action GetAction(List<string> extra)
        {
            var mapping = new[]
            {
                ("encrypt", Action.Encrypt),
                ("decrypt", Action.Decrypt),
                ("verify", Action.Verify),
            };

            foreach (var m in mapping)
            {
                if (extra.Contains(m.Item1, StringComparer.OrdinalIgnoreCase))
                {
                    return m.Item2;
                }
            }

            return Action.Help;
        }

        private static void SetDefaults(List<string> filePatterns, List<string> configKeyNames)
        {
            if (!filePatterns.Any())
            {
                filePatterns.Add("*.toml");
            }

            if (!configKeyNames.Any())
            {
                configKeyNames.Add(".*Password.*");
                configKeyNames.Add(".*Secret.*");
            }
        }

        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine("\n\nToml Config Management Tool v" + Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Verbs [encrypt | decrypt | verify]");
            Console.WriteLine("USAGE: toml-config-tool [VERB] [OPTIONS]");
            Console.WriteLine("");
            Console.WriteLine("Example: toml-config-tool encrypt --auto -f config.toml -k .+Password.+ ");
            Console.WriteLine(
                "This will generate a new key and encrypt the config.toml file with it. all fields containing password will be encrypted");
            Console.WriteLine("");
            Console.WriteLine("Use this tool to encrypt, decrypt and verify secrets in your toml files");
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}