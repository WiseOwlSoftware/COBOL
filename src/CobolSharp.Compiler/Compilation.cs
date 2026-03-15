using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Compiler.Preprocessor;

namespace CobolSharp.Compiler;

/// <summary>
/// Top-level compilation facade. Orchestrates all compiler phases:
/// Source → Preprocess → ANTLR4 Lex → ANTLR4 Parse → Semantic → Emit.
/// </summary>
public sealed class Compilation
{
    private readonly List<string> _copySearchPaths = new();

    /// <summary>Add a directory to search for COPY copybooks.</summary>
    public void AddCopySearchPath(string path) => _copySearchPaths.Add(path);

    public CompilationResult Compile(string sourcePath, string? outputPath = null)
    {
        var diagnostics = new DiagnosticBag();

        // Phase 0: Load and preprocess (expand COPY statements)
        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        // Detect and normalize reference format (fixed-form → free-form)
        string normalizedText = ReferenceFormatProcessor.NormalizeToFreeForm(rawText);

        // Expand COPY statements
        var copyProcessor = new CopyProcessor(_copySearchPaths);
        string processedText = copyProcessor.Process(normalizedText, sourceDir);

        // Phase 1: ANTLR4 Lex
        var inputStream = new AntlrInputStream(processedText);
        var lexer = new CobolLexer(inputStream);

        // Phase 2: ANTLR4 Parse
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new CobolParserCore(tokenStream);

        // Replace default error listener with our collecting listener
        parser.RemoveErrorListeners();
        var errorListener = new CobolErrorListener(diagnostics, sourcePath);
        parser.AddErrorListener(errorListener);

        var tree = parser.compilationUnit();

        if (parser.NumberOfSyntaxErrors > 0)
        {
            return new CompilationResult(false, "", diagnostics.Diagnostics);
        }

        // Phase 3: Build semantic model (TODO — walk parse tree with visitor)
        // Phase 4: CIL emission (TODO — emit from semantic model)

        // For now, determine output path from parse tree
        string programId = ExtractProgramId(tree) ?? Path.GetFileNameWithoutExtension(sourcePath);
        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            programId + ".dll");

        // TODO: Emit .NET assembly from parse tree
        // For now, report success if parsing succeeded
        return new CompilationResult(
            !diagnostics.HasErrors,
            outputPath,
            diagnostics.Diagnostics);
    }

    private static string? ExtractProgramId(CobolParserCore.CompilationUnitContext tree)
    {
        // Walk the parse tree to find PROGRAM-ID
        var compilationGroups = tree.compilationGroup();
        if (compilationGroups.Length == 0) return null;

        var programUnit = compilationGroups[0].programUnit();
        if (programUnit.Length == 0) return null;

        var idDiv = programUnit[0].identificationDivision();
        if (idDiv == null) return null;

        var body = idDiv.identificationBody();
        if (body == null) return null;

        var progId = body.programIdParagraph();
        if (progId == null) return null;

        var name = progId.programName();
        return name?.IDENTIFIER()?.GetText();
    }
}

/// <summary>
/// ANTLR4 error listener that feeds into our DiagnosticBag.
/// </summary>
public sealed class CobolErrorListener : BaseErrorListener
{
    private readonly DiagnosticBag _diagnostics;
    private readonly string _sourcePath;

    public CobolErrorListener(DiagnosticBag diagnostics, string sourcePath)
    {
        _diagnostics = diagnostics;
        _sourcePath = sourcePath;
    }

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        var location = new SourceLocation(_sourcePath, 0, line, charPositionInLine);
        var span = new TextSpan(offendingSymbol?.StartIndex ?? 0,
            offendingSymbol?.StopIndex ?? 0);
        _diagnostics.ReportError("ANTLR", msg, location, span);
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
