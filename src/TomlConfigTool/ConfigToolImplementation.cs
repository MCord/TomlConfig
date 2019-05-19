namespace TomlConfigTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TomlConfig;
    using Tomlyn.Syntax;

    public class ConfigToolImplementation
    {
        private readonly List<string> patterns;
        private readonly bool checkSubFolders;
        private readonly string masterKey;
        private readonly List<Regex> configKeyNames;

        public ConfigToolImplementation(List<string> patterns, bool checkSubFolders, string masterKey,
            List<string> configKeyNames)
        {
            this.patterns = patterns;
            this.checkSubFolders = checkSubFolders;
            this.masterKey = masterKey;
            this.configKeyNames = configKeyNames
                .Select(x => new Regex(x)).ToList();
        }

        public void Encrypt()
        {
            foreach (var file in GetFiles())
            {
                var changes = EncryptFile(file);
                if (changes > 0)
                {
                    Console.WriteLine($"Encrypted {changes} properties in " +
                                      $"{Path.GetRelativePath(Environment.CurrentDirectory, file)}");
                }
            }
        }

        public void Decrypt()
        {
            foreach (var file in GetFiles())
            {
                var changes = DecryptFile(file);
                if (changes > 0)
                {
                    Console.WriteLine($"Decrypted {changes} properties in " +
                                      $"{Path.GetRelativePath(Environment.CurrentDirectory, file)}");
                }
            }
        }

        private int DecryptFile(string file)
        {
            try
            {
                var table = TomlConfig.ReadTable(file);
                var found = 0;
                foreach (var keyValue in GetAllProperties(table))
                {
                    if (keyValue.Value is StringValueSyntax token
                        && DecryptValue(token.Value, out var cypher))
                    {
                        keyValue.Value = new StringValueSyntax(cypher);
                        found++;
                        continue;
                    }
                    
                    Console.Write($"Failed to decrypt {keyValue}");
                }


                TomlConfig.WriteDocument(file, table);
                return found;
            }
            catch (Exception ex)
            {
                throw new TomlConfigurationException(
                    $"Error while decrypting '{Path.GetRelativePath(Environment.CurrentDirectory, file)}'\n\t" +
                    ex.Message);
            }
        }

        private int EncryptFile(string file)
        {
            try
            {
                var table = TomlConfig.ReadTable(file);
                var changes = 0;
                foreach (var keyValue in GetAllProperties(table))
                {
                    if (keyValue.Value is StringValueSyntax token
                        && EncryptValue(token.Value, out var cypher))
                    {
                        keyValue.Value = new StringValueSyntax(cypher);
                        changes++;
                    }
                }


                TomlConfig.WriteDocument(file, table);
                return changes;
            }
            catch (Exception ex)
            {
                throw new TomlConfigurationException(
                    $"Error while encrypting '{Path.GetRelativePath(Environment.CurrentDirectory, file)}'\n\t" +
                    ex.Message);
            }
        }

        private IEnumerable<KeyValueSyntax> GetAllProperties(DocumentSyntax table)
        {
            foreach (var prop in table.GetAllKeys())
            {
                if (configKeyNames.Any(c=> c.IsMatch(prop.Key.ToString())))
                {
                    yield return prop;
                }
            }
        }

        private bool EncryptValue(string value, out string cypherValue)
        {
            var secretKeeper = new SecretKeeper(() => masterKey);

            if (secretKeeper.IsValidCypher(value, out var thumb, out _))
            {
                secretKeeper.AssertSecretThumbnail(thumb);
                cypherValue = null;
                return false;
            }

            cypherValue = secretKeeper.Encrypt(value);
            return true;
        }
        
        private bool DecryptValue(string cypherValue, out string clearValue)
        {
            var secretKeeper = new SecretKeeper(() => masterKey);

            if (secretKeeper.IsValidCypher(cypherValue, out var thumb, out _))
            {
                secretKeeper.AssertSecretThumbnail(thumb);
                clearValue = secretKeeper.Decrypt(cypherValue);
                return true;
            }

            clearValue = null;
            return false;
        }

        private IEnumerable<string> GetFiles()
        {
            var matchedAnyFile = false;

            foreach (var pattern in patterns)
            {
                var path = Path.GetDirectoryName(pattern);

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = Environment.CurrentDirectory;
                }

                var actualPattern = Path.GetFileName(pattern);
                var files = Directory.GetFiles(path, actualPattern,
                    checkSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    matchedAnyFile = true;
                    yield return Path.GetFullPath(file);
                }
            }

            if (!matchedAnyFile)
            {
                throw new ConfigToolException($"No files found matching {string.Join("|", patterns)}");
            }
        }

        public void Verify()
        {
            foreach (var file in GetFiles())
            {
                var changes = VerifyFile(file);
                if (changes > 0)
                {
                    Console.WriteLine($"Verified {changes} properties in " +
                                      $"{Path.GetRelativePath(Environment.CurrentDirectory, file)}");
                }
            }
        }

        private int VerifyFile(string file)
        {
            try
            {
                var table = TomlConfig.ReadTable(file);
                var verified = 0;
                foreach (var keyValue in GetAllProperties(table))
                {
                    verified++;

                    if (keyValue.Value is StringValueSyntax token)
                    {
                        VerifyValue(token.Value, keyValue.Key.ToString());
                    }
                }

                if (verified == 0)
                {
                    Console.WriteLine($"No Key matched specified filters : \n {string.Join("\n", configKeyNames)}");
                }

                TomlConfig.WriteDocument(file, table);
                return verified;
            }
            catch (Exception ex)
            {
                throw new TomlConfigurationException(
                    $"Error while verifying '{Path.GetRelativePath(Environment.CurrentDirectory, file)}'\n\t" +
                    ex.Message);
            };
        }

        private void VerifyValue(string cypherValue, string keyName)
        {
            var secretKeeper = new SecretKeeper(() => masterKey);

            try
            {
                secretKeeper.Decrypt(cypherValue);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to decrypt {keyName} from value '{cypherValue}' Error:" + ex.Message);
                return;
            }
        }
    }
}