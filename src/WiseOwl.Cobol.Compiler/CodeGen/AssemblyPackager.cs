// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// Packages a successfully-emitted assembly into a runnable deployment (P7 Step 2 — the side-effecting
/// packaging split out of <see cref="RoslynBackend"/>, whose <c>Compile</c> stays pure C#→assembly): writes the
/// <c>.runtimeconfig.json</c> so the output launches via <c>dotnet &lt;name&gt;.dll</c>, and copies
/// <c>WiseOwl.Cobol.Runtime.dll</c> next to it so the generated program's runtime calls resolve.
/// </summary>
public static class AssemblyPackager
{
    internal const string RuntimeFileName = "WiseOwl.Cobol.Runtime.dll";

    /// <summary>The ReadyToRun platform this process runs on, named as the build names it (<c>win-x64</c>,
    /// <c>linux-x64</c>, <c>osx-arm64</c>): operating-system family and processor architecture, which is exactly what
    /// decides whether a ReadyToRun image's native code is usable here (its PE machine field is the architecture
    /// combined with the operating system). It is not <see cref="RuntimeInformation.RuntimeIdentifier"/>, which a
    /// distribution-built .NET reports as e.g. <c>ubuntu.26.04-x64</c>, naming no image.</summary>
    internal static string ReadyToRunPlatform { get; } =
        (OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsFreeBSD() ? "freebsd"
            : null) is { } os
            ? os + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
            : RuntimeInformation.RuntimeIdentifier;   // an operating system the build cannot target names no image

    /// <summary>The WiseOwl COBOL runtime assembly the generated program calls (deployed alongside the compiler).
    /// Consumed by BOTH halves of the backend: <see cref="RoslynBackend"/> references it at compile time and
    /// <see cref="Package"/> deploys it at packaging time, so a program is compiled against the very file it runs
    /// with.
    /// <para>It is this platform's READYTORUN IMAGE (kb/Work PB2528; DESIGN-test-build-ci §3.16): the build
    /// precompiles the runtime for each platform in <c>CobolRuntimeReadyToRunIdentifiers</c> and lays each beside
    /// the compiler at <c>ReadyToRun/&lt;rid&gt;/</c>, because a COBOL program is its own process and would otherwise
    /// JIT-compile the runtime's methods at every start. On a platform the build precompiled nothing for, it is the
    /// portable assembly beside the compiler, the same source compiled by the JIT at run time, which is also what a
    /// build with <c>CobolRuntimeReadyToRun=false</c> deploys.</para></summary>
    /// <remarks>Public because the Conformance runner's compiled-program cache keys on this file. Declared after
    /// <see cref="ReadyToRunPlatform"/>, which its initializer reads: static initializers run in declaration
    /// order.</remarks>
    public static string RuntimePath { get; } = ResolveRuntimePath();

    /// <summary>The directory of the compiler's deployment that holds the framework's reference assemblies.</summary>
    internal const string ReferenceAssemblyDirectoryName = "reference-assemblies";

    /// <summary>The framework REFERENCE assemblies a generated program is compiled against (kb/Work PB4047): the
    /// Microsoft.NETCore.App targeting pack the compiler was built against, which its build lays beside it at
    /// <c>reference-assemblies/</c> (WiseOwl.Cobol.Compiler.csproj). An emitted assembly therefore references
    /// <c>System.Runtime</c> and the other reference identities, as an assembly an ordinary MSBuild project builds
    /// does, so a .NET host project can reference it (DOC-A.1-141's host mechanism); compiled against the compiler's
    /// own implementation assemblies it referenced <c>System.Private.CoreLib</c>, which no host's reference set
    /// holds. Sorted by file name, so the reference set, and the deterministic assembly built from it, do not depend
    /// on the directory's enumeration order.</summary>
    /// <exception cref="InvalidOperationException">The deployment lacks the directory, or the directory holds no
    /// assembly: the compiler cannot compile a program without its reference set.</exception>
    /// <remarks>Public because the Conformance runner's compiled-program cache keys on these files and its host
    /// tests compile a .NET host against exactly this set.</remarks>
    public static IReadOnlyList<string> ReferenceAssemblyPaths => s_referenceAssemblyPaths.Value;

    private static readonly Lazy<IReadOnlyList<string>> s_referenceAssemblyPaths =
        new(ResolveReferenceAssemblyPaths, LazyThreadSafetyMode.ExecutionAndPublication);

    private static IReadOnlyList<string> ResolveReferenceAssemblyPaths()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, ReferenceAssemblyDirectoryName);
        string[] files = Directory.Exists(dir)   // not a compilation input: the reference set is part of the compiler's own deployment
            ? Directory.GetFiles(dir, "*.dll")   // not a compilation input: the compiler's own deployment, keyed by the cache's tool files; not a COBOL word: a file-system pattern
            : [];
        if (files.Length == 0)
            throw new InvalidOperationException($"The WiseOwl COBOL compiler's deployment has no framework reference "
                + $"assemblies at {dir} (kb/Work PB4047): the build of WiseOwl.Cobol.Compiler lays them there.");
        Array.Sort(files, StringComparer.Ordinal);
        return files;
    }

    private static string ResolveRuntimePath()
    {
        string image = Path.Combine(AppContext.BaseDirectory, "ReadyToRun", ReadyToRunPlatform, RuntimeFileName);
        return File.Exists(image)   // not a compilation input: the images are part of the compiler's own deployment
            ? image
            : Path.Combine(AppContext.BaseDirectory, RuntimeFileName);
    }

    /// <summary>Package the emitted assembly at <paramref name="outputDllPath"/>: runtimeconfig + runtime deploy.
    /// (The design sketch passed the Roslyn <c>EmitResult</c> + options; the output path is the one input the
    /// packaging actually consumes — reduced accordingly.)</summary>
    /// <param name="library">The assembly is a library (a group with no main program, DESIGN-external-repository §7.2):
    /// it is loaded into a host's process, never launched, so it has no runtimeconfig.json.</param>
    /// <returns>Every file packaging wrote, as full paths (kb/Work PB985 — the backend's output set).</returns>
    internal static IReadOnlyList<string> Package(string outputDllPath, bool library)
    {
        var written = new List<string>();
        if (!library) written.Add(WriteRuntimeConfig(outputDllPath));
        if (DeployRuntime(outputDllPath) is { } runtime) written.Add(runtime);
        return written;
    }

    /// <summary>Copy <see cref="RuntimePath"/> next to the compiled program, as <c>WiseOwl.Cobol.Runtime.dll</c>, so it
    /// resolves at run time.</summary>
    private static string? DeployRuntime(string outputDllPath)
    {
        string dest = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputDllPath))!, RuntimeFileName);
        if (string.Equals(RuntimePath, dest, StringComparison.OrdinalIgnoreCase)   // not a COBOL word: two file-system paths
            || !File.Exists(RuntimePath))   // not a compilation input: the runtime is part of the compiler's own deployment
            return null;
        File.Copy(RuntimePath, dest, overwrite: true);
        return dest;
    }

    /// <summary>
    /// Write the <c>.runtimeconfig.json</c> next to the emitted assembly so it is launchable via
    /// <c>dotnet &lt;name&gt;.dll</c>, targeting the same shared framework the compiler is running on.
    /// </summary>
    private static string WriteRuntimeConfig(string outputDllPath)
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
        string path = Path.GetFullPath(Path.ChangeExtension(outputDllPath, ".runtimeconfig.json"));
        File.WriteAllText(path, json);
        return path;
    }
}
