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
        private readonly byte[] masterKey;
        private readonly List<Regex> configKeyNames;

        public ConfigToolImplementation(List<string> patterns, bool checkSubFolders, byte[] masterKey,
            List<string> configKeyNames)
        {
            this.patterns = patterns;
            this.checkSubFolders = checkSubFolders;
            this.masterKey = masterKey;
            this.configKeyNames = configKeyNames
                .Select(x=> new Regex(x)).ToList();
        }

        public void Encrypt()
        {
            var files = GetFiles().ToList();

            if (!files.Any())
            {
                throw new ConfigToolException($"No files found matching {string.Join("|", patterns)}");
            }

            foreach (var file in files)
            {
                var changes = EncryptFile(file);
                if (changes > 0)
                {
                    Console.WriteLine($"Updated {changes} properties in " +
                                      $"{Path.GetRelativePath(Environment.CurrentDirectory,file)}");
                }
            }
        }

        private int EncryptFile(string file)
        {
            try
            {
                var table = TomlConfig.ReadTable(file);
                
                var changes = EncryptValues(table.KeyValues);

                foreach (var subTable in table.Tables)
                {
                    changes += EncryptTable(subTable);
                }

                TomlConfig.WriteDocument(file, table);
                return changes;
            }
            catch (Exception ex)
            {
                throw new TomlConfigurationException(
                    $"Error while encrypting '{Path.GetRelativePath(Environment.CurrentDirectory,file)}'\n\t"+ ex.Message);
            }
        }

        private int EncryptTable(TableSyntaxBase table)
        {
            var changes = EncryptValues(table.Items);

            foreach (var sub in table.Items.OfType<TableSyntax>())
            {
                changes+= EncryptTable(sub);
            }

            return changes;
        }

        private int EncryptValues(SyntaxList<KeyValueSyntax> items)
        {
            var changes = 0;
            foreach (var keyValue in items.Where(IsSecretKey))
            {
                if (keyValue.Value is StringValueSyntax token
                    && EncryptValue(token.Value, out var cypher))
                {
                    keyValue.Value = new StringValueSyntax(cypher);
                    changes++;
                }
            }

            return changes;
        }

        private bool EncryptValue(string value, out string cypherValue)
        {
            var secretKeeper = new SecretKeeper(() => masterKey);

            if (secretKeeper.IsValidCypher(value, out var thumb, out _))
            {
                secretKeeper.VerifySecretThumbnail(thumb);
                cypherValue = null;
                return false;
            }

            cypherValue = secretKeeper.Encrypt(value);
            return true;
        }

        private bool IsSecretKey(KeyValueSyntax key)
        {
            return key.Value?.Kind == SyntaxKind.String 
                   && configKeyNames.Any(rx => rx.IsMatch(key.ToString()));
        }

        private IEnumerable<string> GetFiles()
        {
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
                    yield return Path.GetFullPath(file);
                }
            }
        }
    }
}