// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.CodeGen;

/// <summary>
/// Packages a successfully-emitted assembly into a runnable deployment (P7 Step 2 — the side-effecting
/// packaging split out of <see cref="RoslynBackend"/>, whose <c>Compile</c> stays pure C#→assembly): writes the
/// <c>.runtimeconfig.json</c> so the output launches via <c>dotnet &lt;name&gt;.dll</c>, and copies
/// <c>Cobol.Net.Runtime.dll</c> next to it so the generated program's runtime calls resolve.
/// </summary>
internal static class AssemblyPackager
{
    /// <summary>The COBOL.NET runtime assembly the generated program calls (deployed alongside the compiler).
    /// Consumed by BOTH halves of the backend: <see cref="RoslynBackend"/> references it at compile time and
    /// <see cref="Package"/> deploys it at packaging time.</summary>
    internal static string RuntimePath =>
        Path.Combine(AppContext.BaseDirectory, "Cobol.Net.Runtime.dll");

    /// <summary>Package the emitted assembly at <paramref name="outputDllPath"/>: runtimeconfig + runtime deploy.
    /// (The design sketch passed the Roslyn <c>EmitResult</c> + options; the output path is the one input the
    /// packaging actually consumes — reduced accordingly.)</summary>
    public static void Package(string outputDllPath)
    {
        WriteRuntimeConfig(outputDllPath);
        DeployRuntime(outputDllPath);
    }

    /// <summary>Copy <c>Cobol.Net.Runtime.dll</c> next to the compiled program so it resolves at run time.</summary>
    private static void DeployRuntime(string outputDllPath)
    {
        string dest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputDllPath))!, "Cobol.Net.Runtime.dll");
        if (!string.Equals(RuntimePath, dest, StringComparison.OrdinalIgnoreCase) && File.Exists(RuntimePath))
            File.Copy(RuntimePath, dest, overwrite: true);
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
