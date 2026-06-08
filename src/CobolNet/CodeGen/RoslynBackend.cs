// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// Compiles COBOL.NET-generated C# source into a runnable .NET assembly using Roslyn, and writes the
/// accompanying <c>.runtimeconfig.json</c> so the result runs with <c>dotnet &lt;name&gt;.dll</c>.
/// </summary>
public static class RoslynBackend
{
    /// <summary>The outcome of a backend compilation.</summary>
    /// <param name="Success">True if the assembly was produced.</param>
    /// <param name="Diagnostics">Roslyn diagnostics (errors + warnings) from compiling the generated C#.</param>
    public readonly record struct Result(bool Success, IReadOnlyList<Diagnostic> Diagnostics);

    /// <summary>
    /// Compile <paramref name="csharpSource"/> to a console assembly at <paramref name="outputDllPath"/>.
    /// </summary>
    /// <param name="assemblyName">The emitted assembly's simple name (the COBOL PROGRAM-ID).</param>
    public static Result Compile(string csharpSource, string outputDllPath, string assemblyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            csharpSource, new CSharpParseOptions(LanguageVersion.Latest));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            ReferenceAssemblies(),
            new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                // The generated source is machine-emitted and self-consistent; nullable warnings on it are noise.
                nullableContextOptions: NullableContextOptions.Disable));

        EmitResult emit = compilation.Emit(outputDllPath);
        if (emit.Success)
        {
            WriteRuntimeConfig(outputDllPath);
            DeployRuntime(outputDllPath);
        }

        return new Result(emit.Success, emit.Diagnostics);
    }

    /// <summary>The COBOL.NET runtime assembly the generated program calls (alongside the compiler).</summary>
    private static string RuntimePath =>
        Path.Combine(AppContext.BaseDirectory, "CobolNet.Runtime.dll");

    /// <summary>Copy <c>CobolNet.Runtime.dll</c> next to the compiled program so it resolves at run time.</summary>
    private static void DeployRuntime(string outputDllPath)
    {
        string dest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputDllPath))!, "CobolNet.Runtime.dll");
        if (!string.Equals(RuntimePath, dest, StringComparison.OrdinalIgnoreCase) && File.Exists(RuntimePath))
            File.Copy(RuntimePath, dest, overwrite: true);
    }

    /// <summary>
    /// The framework reference set: every assembly currently loaded as a trusted-platform assembly. This makes
    /// the generated program able to reference the same BCL the compiler itself runs on (System.Runtime,
    /// System.Console, System.Linq, …) without bundling a reference pack.
    /// </summary>
    private static IReadOnlyList<MetadataReference> ReferenceAssemblies()
    {
        string tpa = (string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "");
        var refs = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
        // The COBOL.NET runtime the generated program calls (CobolNum / CobolString).
        if (File.Exists(RuntimePath)) refs.Add(MetadataReference.CreateFromFile(RuntimePath));
        return refs;
    }

    /// <summary>
    /// Write the <c>.runtimeconfig.json</c> next to the emitted assembly so it is launchable via
    /// <c>dotnet &lt;name&gt;.dll</c>, targeting the same shared framework the compiler is running on.
    /// </summary>
    private static void WriteRuntimeConfig(string outputDllPath)
    {
        var v = Environment.Version;
        string json = $$"""
        {
          "runtimeOptions": {
            "tfm": "net{{v.Major}}.{{v.Minor}}",
            "framework": {
              "name": "Microsoft.NETCore.App",
              "version": "{{v.Major}}.{{v.Minor}}.0"
            }
          }
        }
        """;
        File.WriteAllText(Path.ChangeExtension(outputDllPath, ".runtimeconfig.json"), json);
    }
}
