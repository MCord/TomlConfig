# TomlConfig

TomlConfig is a [Nuget Library](https://www.nuget.org/packages/TomlConfig/) that enables you to use 
[TOML](https://github.com/toml-lang/toml) format for application configuration

## Features
1. Read application configuration from TOML
1. Store encrypted secrets in configuration files and decrypt when reading
1. Override configuration values from environment variables or other sources
1. include another toml file to reduce duplication in config files
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

In this example we deserialize a file into an object. Other overloads can be used 
to deserialize an string or any stream.

```csharp
var file = "./files/my-application/common.toml";
            
var config = TomlConfig
    .FromFile(file)
    .Read<MyApplicationConfiguration>();

```

### Read config with secrets 

To decrypt configuration we need to create a toml file and use `toml-config-tool` command to encrypt
the secrets, Any Key containing the `Password in the name would be encrypted` other filters can be
specified. If no master key is specified the value in `MASTER_KEY` environment variable will be used.

To encrypt your file run:

```bash
dotnet tool install -g TomlConfigTool 
toml-config-tool encrypt -f config.toml -m "MY_MASTER_KEY"
```
Now we need to annotate the property in our POCO with `Secret` attribute.

```csharp
public class MyApplicationConfiguration
{
    public string ApplicationName { get; set; }
    public string CopyRight { get; set; }
    public string Environment { get; set; }
    public string LogPath { get; set; }
    [Secret]
    public string Password { get; set; }

}
```

And to read the config. Again if you don't specify the master key with `WithMasterKey("masterkey")` the value from `MASTER_KEY`
will be used. An exception will be thrown if no master key can be found.

>note: In production you don't want to hard code your master key so use the environment variable on your environments to specify 
your master key. This way you can version you secrets along with you application inside environment specific toml files.

```csharp
 var file = "./files/my-application/production.toml";
            
var config = TomlConfig
    .FromFile(file)
    .WithMasterKey("MY_MASTER_KEY")
    .Read<MyApplicationConfiguration>();
```

### Override configuration values from environment variables

This can be useful if you need to change a value without redeploying you application or to differentiate environments. 
Other variation of `WithOverride` enables you to override from a dictionary or any key/value. 

```csharp
       
var file = "./files/my-application/common.toml";

var config = TomlConfig
    .FromFile(file)
    .WithOverrideFromEnvironmentVariables() 
    .Read<MyApplicationConfiguration>();
```

### Including another file

By adding `#include RELATIVE_PATH_TO_TOML` you can reference another toml file with the same schema as the current one. This way
the values that are not specified in the current file will have the included value.

### Cascasde values inside a file

If your Configuration POCO forms a tree, the objects would inherit values from their parents when no value is specified.

Imagine the configuration for a website. Let's say it's a multi-tenant service that hosts many websites in sub domains. 
Each sub domain has exactly the same configuration schema as the root domain. Only values differ between subdomains and many 
values are the same between the root host subdomains. This can be represented as 

```csharp

class SiteConfig
{
    public string Url {get;set;}
    public int port {get;set;}
    //...
    
    public List<SiteConfig> SubDomains {get;set;}
}

```

In this case `SiteConfig` has a property that is a list of `SiteConfig` called sub domains. Any unspecified value in
sub domains would inherit the value from it's parent.

>note: There is no limit on the number of nesting levels possible but int's not recommended to create more that two levels of 
nesting as it would be very confusing to follow the origin of values inside toml files. For an example on this check 
[ShouldCascadeValuesForHierarchicalValues](./src/Test/Examples.cs#L131)


## Acknowledgement

This library uses the great TOML parser [Tomlyn](https://github.com/xoofx/Tomlyn) developed by *Alexandre Mutel*

## License

This software is released under [MIT License](./LICENSE).