using CobolSharp.Compiler;

namespace CobolSharp.CLI;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return 0;
        }

        if (args[0] == "compile")
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Error: no source file specified.");
                Console.Error.WriteLine("Usage: cobolsharp compile <file.cob> [options]");
                return 1;
            }

            return RunCompile(args[1..]);
        }

        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("CobolSharp — COBOL compiler for .NET");
        Console.WriteLine();
        Console.WriteLine("Usage: cobolsharp <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile <file.cob>   Compile a COBOL source file to a .NET assembly");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <output>          Output file path (default: <program-id>.dll)");
        Console.WriteLine("  -h, --help           Show this help message");
    }

    private static int RunCompile(string[] args)
    {
        string? sourcePath = null;
        string? outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputPath = args[++i];
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

        var compilation = new Compilation();
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
