// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolSharp.Compiler.Preprocessor;

namespace CobolSharp.CLI;

/// <summary>
/// Entry point for the CobolSharp CLI. Dispatches to compile (default) or preprocess subcommands.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return 0;
        }

        // Explicit subcommand: preprocess
        if (args[0] == "preprocess")
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Error: no source file specified.");
                Console.Error.WriteLine("Usage: cobolsharp preprocess <file.cob> [-o <output>]");
                return 1;
            }

            return RunPreprocess(args[1..]);
        }

        // Default: compile. Accept "compile" as optional explicit subcommand.
        var compileArgs = args[0] == "compile" ? args[1..] : args;
        return RunCompile(compileArgs);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("CobolSharp — COBOL compiler for .NET");
        Console.WriteLine();
        Console.WriteLine("Usage: cobolsharp [options] <file.cob>");
        Console.WriteLine("       cobolsharp preprocess <file.cob> [-o <output>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <output>              Output file path (default: <program-id>.dll)");
        Console.WriteLine("  --standard <version>     COBOL standard version (default: cobol85)");
        Console.WriteLine("                           Values: cobol85, cobol2002, cobol2014, cobol2023");
        Console.WriteLine("  --nist [name]            Enable NIST test suite preprocessing");
        Console.WriteLine("                           Replaces XXXXX### placeholders; derives test");
        Console.WriteLine("                           name from source filename if not specified");
        Console.WriteLine("  -h, --help               Show this help message");
    }

    private static int RunPreprocess(string[] args)
    {
        string? sourcePath = null;
        string? outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
                outputPath = args[++i];
            else if (!args[i].StartsWith('-'))
                sourcePath = args[i];
        }

        if (sourcePath is null || !File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: source file not found: {sourcePath}");
            return 1;
        }

        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        // Phase 0a: Reference format normalization
        string normalized = ReferenceFormatProcessor.NormalizeToFreeForm(rawText);

        // Phase 0b: COPY/REPLACE expansion
        var copyProcessor = new CopyProcessor([]);
        string processed = copyProcessor.Process(normalized, sourceDir);

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, processed);
            Console.WriteLine($"Preprocessed to: {outputPath}");
        }
        else
        {
            Console.Write(processed);
        }

        return 0;
    }

    private static int RunCompile(string[] args)
    {
        string? sourcePath = null;
        string? outputPath = null;
        string standard = "cobol85";
        string? nistTestName = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else if (args[i] == "--standard" && i + 1 < args.Length)
            {
                standard = args[++i].ToLowerInvariant();
                if (standard is not ("cobol85" or "cobol2002" or "cobol2014" or "cobol2023"))
                {
                    Console.Error.WriteLine($"Error: unknown standard '{standard}'. Use: cobol85, cobol2002, cobol2014, cobol2023");
                    return 1;
                }
            }
            else if (args[i] == "--nist")
            {
                // Enable NIST preprocessing; test name derived from source filename
                nistTestName = ""; // will be derived from source path later
            }
            else if (!args[i].StartsWith('-'))
            {
                sourcePath = args[i];
            }
            else
            {
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                return 1;
            }
        }

        if (sourcePath is null)
        {
            Console.Error.WriteLine("Error: no source file specified.");
            return 1;
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: source file not found: {sourcePath}");
            return 1;
        }

        // TODO: pass standard to Compilation when grammar overlays are wired up
        var compilation = new Compilation();

        // NIST mode: derive test name from source filename if not explicit
        if (nistTestName != null)
        {
            if (nistTestName == "")
                nistTestName = Path.GetFileNameWithoutExtension(sourcePath);
            compilation.NistTestName = nistTestName;
        }

        var result = compilation.Compile(sourcePath, outputPath);

        foreach (var diagnostic in result.Diagnostics)
        {
            Console.Error.WriteLine(diagnostic);
        }

        if (result.Success)
        {
            Console.WriteLine($"Compiled successfully: {result.OutputPath}");
            return 0;
        }
        else
        {
            Console.Error.WriteLine("Compilation failed.");
            return 1;
        }
    }
}
