namespace TomlConfigTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TomlConfig;
    using Tomlyn.Model;
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
                EncryptFile(file);
            }
        }

        private void EncryptFile(string file)
        {
            var table = TomlConfig.ReadTable(file);
            bool hasChanges = false;
            foreach (var keyValue in table.KeyValues.Where(IsSecretKey))
            {
                if (keyValue.Value is StringValueSyntax token 
                    && EncryptValue(token.Value, out var cypher))
                {
                    keyValue.Value = new StringValueSyntax(cypher);
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                Console.WriteLine($"Warning: no secrets where found in {file}");
                return;
            }

            TomlConfig.WriteDocument(file, table);
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