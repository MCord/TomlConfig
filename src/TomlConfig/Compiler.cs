namespace TomlConfig
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Loader;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    internal class Compiler
    {
        public Type CompileContainer(Type type, string[] dimensions)
        {
            var outTypeName = $"Class{Guid.NewGuid():N}";
            var sourceTypeName = type.FullName?.Replace("+", ".");

            var code = GetCode(sourceTypeName, outTypeName, dimensions);

            var compilation = CSharpCompilation.Create(
                Guid.NewGuid().ToString(),
                new[] {CSharpSyntaxTree.ParseText(code)},
                GetDependencies(type),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(x => $"Line:{x.Location} -> {x.GetMessage()}");

                    throw new ApplicationException(
                        $"Codegen failed with {string.Join("\n", failures)} while compiling \n\n\n {code}");
                }

                ms.Seek(0, SeekOrigin.Begin);

                var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                return assembly.GetType($"generated.{outTypeName}");
            }
        }

        private static MetadataReference[] GetDependencies(Type type)
        {
            var coreDir = Directory.GetParent(typeof(Enumerable).Assembly.Location);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll"),
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Runtime.dll"),
                MetadataReference.CreateFromFile(type.Assembly.Location)
            };
            return references;
        }

        internal string GetCode(string className, string generatedTypeName, string[] dimensions)
        {
            var props = string.Join("\n        ",
                dimensions.Select(x => $"public {generatedTypeName}[] {x} {{ get; set; }}"));

            var code = @"
namespace generated
{
    public class __CLASS_NAME__ : __TYPE_NAME__ 
    {
        __PROPS__
    }
}

";
            return code.Replace("__TYPE_NAME__", className)
                .Replace("__CLASS_NAME__", generatedTypeName)
                .Replace("__PROPS__", props)
                .Trim();
        }
    }
}