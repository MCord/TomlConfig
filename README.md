# TomlConfig

TomlConfig is a [Nuget Library](https://www.nuget.org/packages/TomlConfig/) that enables you to use 
[TOML](https://github.com/toml-lang/toml) format for application configuration

## Features
1. Read application configuration from TOML
1. Store encrypted secrets in configuration files and decrypt when reading
1. Override configuration values from environment variables or other sources
1. include other another toml file to reduce duplication in config files
1. Cascade configuration values inside a file

## Install 

```bash
dotnet add package TomlConfig 
```

You can also install the configuration tool to encrypt, decrypt and validate your configuration files.

```bash
dotnet tool install -g TomlConfigTool 
```

## Examples

Full list of examples can be found in [here](./src/Test/Examples.cs)

### Read a config files

```csharp
var file = "./files/my-application/common.toml";
            
var config = TomlConfig
    .FromFile(file)
    .Read<MyApplicationConfiguration>();

```