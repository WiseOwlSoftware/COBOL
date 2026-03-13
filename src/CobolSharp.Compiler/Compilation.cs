using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Lexing;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Compiler;

/// <summary>
/// Top-level compilation facade. Orchestrates all compiler phases:
/// Source → Lex → Parse → Analyze → Emit.
/// </summary>
public sealed class Compilation
{
    public CompilationResult Compile(string sourcePath, string? outputPath = null)
    {
        var diagnostics = new DiagnosticBag();

        // Phase 1: Load source
        var source = SourceText.FromFile(sourcePath);

        // Phase 2: Lex
        var lexer = new Lexer(source, diagnostics);
        var tokens = lexer.Tokenize();
        if (diagnostics.HasErrors)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 3: Parse
        var parser = new Parser(tokens, diagnostics);
        var ast = parser.ParseCompilationUnit();
        if (diagnostics.HasErrors)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 4: Semantic analysis
        var analyzer = new SemanticAnalyzer(diagnostics);
        var model = analyzer.Analyze(ast);
        if (diagnostics.HasErrors)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Determine output path
        string programId = ast.Programs[0].Identification.ProgramId;
        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            programId + ".dll");

        // Phase 5: CIL emission
        var emitter = new CilEmitter(diagnostics);
        var emitResult = emitter.Emit(ast, model, outputPath);

        if (emitResult.Success)
        {
            // Generate runtimeconfig.json alongside the output assembly
            EmitRuntimeConfig(outputPath);

            // Copy runtime DLL alongside the output assembly
            CopyRuntimeLibrary(outputPath);
        }

        return new CompilationResult(
            emitResult.Success && !diagnostics.HasErrors,
            emitResult.OutputPath,
            diagnostics.Diagnostics);
    }
    private static void EmitRuntimeConfig(string outputPath)
    {
        string configPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        string json = """
            {
              "runtimeOptions": {
                "tfm": "net8.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "8.0.0"
                }
              }
            }
            """;
        File.WriteAllText(configPath, json);
    }

    private static void CopyRuntimeLibrary(string outputPath)
    {
        string outputDir = Path.GetDirectoryName(outputPath) ?? ".";

        // Find the runtime DLL — first check alongside the compiler, then via loaded assembly
        string? runtimeSource = null;
        string compilerDir = Path.GetDirectoryName(typeof(Compilation).Assembly.Location)!;
        string candidatePath = Path.Combine(compilerDir, "CobolSharp.Runtime.dll");

        if (File.Exists(candidatePath))
        {
            runtimeSource = candidatePath;
        }
        else
        {
            // Fallback: use the loaded assembly location
            runtimeSource = typeof(Runtime.CobolProgram).Assembly.Location;
        }

        if (runtimeSource != null && File.Exists(runtimeSource))
        {
            string destPath = Path.Combine(outputDir, "CobolSharp.Runtime.dll");
            if (!File.Exists(destPath) || new FileInfo(runtimeSource).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
            {
                File.Copy(runtimeSource, destPath, overwrite: true);
            }
        }
    }
}

public sealed class CompilationResult
{
    public bool Success { get; }
    public string OutputPath { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public CompilationResult(bool success, string outputPath, IReadOnlyList<Diagnostic> diagnostics)
    {
        Success = success;
        OutputPath = outputPath;
        Diagnostics = diagnostics;
    }
}
