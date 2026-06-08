// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.CodeGen;
using CobolNet.Frontend;
using CobolSharp.Compiler.Diagnostics;

namespace CobolNet;

/// <summary>
/// The <c>cobol</c> command-line driver: COBOL source → typed-native C# → a runnable .NET assembly.
/// </summary>
/// <remarks>
/// Usage: <c>cobol &lt;source.cob&gt; [-o out.dll] [--nist NAME] [--std 85|2002|2014|2023] [--copy DIR] [--run]</c>.
/// The generated C# is always written next to the output assembly (<c>&lt;name&gt;.g.cs</c>) so the translation is
/// directly inspectable.
/// </remarks>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.Error.WriteLine(
                "cobol <source.cob> [-o out.dll] [--nist NAME] [--std 85|2002|2014|2023] [--copy DIR] [--run]");
            return args.Length == 0 ? 64 : 0;
        }

        var options = CliOptions.Parse(args);
        if (options is null) return 64;

        if (!File.Exists(options.SourcePath))
        {
            Console.Error.WriteLine($"error: source file not found: {options.SourcePath}");
            return 66;
        }

        // Phase 1 — front-end: preprocess + parse.
        var diagnostics = new DiagnosticBag();
        var frontend = new Frontend.Frontend { NistTestName = options.NistTestName, DialectLevel = options.DialectLevel };
        foreach (string dir in options.CopyPaths) frontend.AddCopySearchPath(dir);

        var tree = frontend.Parse(options.SourcePath, diagnostics);
        if (tree is null || diagnostics.HasErrors)
        {
            foreach (var d in diagnostics.Diagnostics) Console.Error.WriteLine(d);
            return 65;
        }

        // Phase 2 — emit typed-native C#.
        string csharp = new CSharpEmitter().Emit(tree);

        string outputDll = options.OutputPath ?? Path.ChangeExtension(options.SourcePath, ".dll");
        string outDir = Path.GetDirectoryName(Path.GetFullPath(outputDll)) is { Length: > 0 } d2 ? d2 : ".";
        Directory.CreateDirectory(outDir);
        string csPath = Path.ChangeExtension(outputDll, ".g.cs");
        File.WriteAllText(csPath, csharp);

        // Phase 3 — compile the generated C# with Roslyn.
        string assemblyName = Path.GetFileNameWithoutExtension(outputDll);
        var result = RoslynBackend.Compile(csharp, outputDll, assemblyName);
        if (!result.Success)
        {
            Console.Error.WriteLine($"error: backend compilation failed (generated C# at {csPath}):");
            foreach (var d in result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
                Console.Error.WriteLine($"  {d}");
            return 70;
        }

        // Optional — run the compiled program.
        return options.Run ? Run(outputDll) : 0;
    }

    /// <summary>Launch the compiled assembly via the shared <c>dotnet</c> host and forward its exit code.</summary>
    private static int Run(string dllPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"\"{dllPath}\"") { UseShellExecute = false };
        using var proc = Process.Start(psi);
        proc!.WaitForExit();
        return proc.ExitCode;
    }

    /// <summary>Parsed command-line options.</summary>
    private sealed record CliOptions(
        string SourcePath, string? OutputPath, string? NistTestName, int DialectLevel,
        IReadOnlyList<string> CopyPaths, bool Run)
    {
        /// <summary>Parse <paramref name="args"/>, or return <see langword="null"/> on a usage error.</summary>
        public static CliOptions? Parse(string[] args)
        {
            string? source = null, output = null, nist = null;
            int std = 85;
            var copy = new List<string>();
            bool run = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-o": output = Next(args, ref i); break;
                    case "--nist": nist = Next(args, ref i); break;
                    case "--copy": if (Next(args, ref i) is { } c) copy.Add(c); break;
                    case "--run": run = true; break;
                    case "--std":
                        if (!int.TryParse(Next(args, ref i), out std)) { Console.Error.WriteLine("error: --std expects a year"); return null; }
                        break;
                    default:
                        if (args[i].StartsWith('-')) { Console.Error.WriteLine($"error: unknown option {args[i]}"); return null; }
                        source = args[i];
                        break;
                }
            }

            if (source is null) { Console.Error.WriteLine("error: no source file"); return null; }
            return new CliOptions(source, output, nist, std, copy, run);

            static string? Next(string[] a, ref int i) => ++i < a.Length ? a[i] : null;
        }
    }
}
