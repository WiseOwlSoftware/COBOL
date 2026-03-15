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

        // Phase 3: Semantic analysis — Pass 1: declaration collection
        string programId = ExtractProgramId(tree) ?? Path.GetFileNameWithoutExtension(sourcePath);
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1);
        semanticBuilder.Visit(tree);

        // Phase 3b: Semantic analysis — Pass 2: reference resolution
        var semDiagnostics = new List<Diagnostic>(semanticBuilder.Diagnostics);
        var resolver = new Semantics.ReferenceResolver(semanticBuilder.Symbols, semDiagnostics);
        resolver.Visit(tree);

        foreach (var d in semDiagnostics)
            diagnostics.Add(d);

        // Phase 4: Build semantic model — populate from symbol table
        var semanticModel = new Semantics.SemanticModel(
            semanticBuilder.Symbols.Program,
            semanticBuilder.Symbols,
            diagnostics);

        // Populate paragraphs, sections, and data records from symbol table
        foreach (var sym in semanticBuilder.Symbols.Program.ProcedureDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.ParagraphSymbol para)
                semanticModel.AddParagraph(para);
            else if (sym is Semantics.SectionSymbol sect)
                semanticModel.AddSection(sect);
        }
        foreach (var sym in semanticBuilder.Symbols.Program.DataDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.DataSymbol data && (data.LevelNumber == 1 || data.LevelNumber == 77))
                semanticModel.AddDataRecord(data);
        }

        // Phase 4b: Compute storage layout — assign byte offsets to all data items
        ComputeStorageLayout(semanticModel);

        // Phase 5: Bind → IR
        var binder = new CodeGen.Binder(semanticModel, diagnostics);
        var irModule = binder.Bind(tree);

        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            programId + ".dll");

        // Phase 6: CIL emission
        try
        {
            var assembly = CodeGen.CilEmitter.EmitAssembly(irModule, programId);
            string dir = Path.GetDirectoryName(outputPath) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            assembly.Write(outputPath);

            EmitRuntimeConfig(outputPath);
            CopyRuntimeLibrary(outputPath);

            return new CompilationResult(
                !diagnostics.HasErrors,
                outputPath,
                diagnostics.Diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.ReportError("CIL", $"CIL emission failed: {ex.Message}",
                new Common.SourceLocation(sourcePath, 0, 0, 0),
                new Common.TextSpan(0, 0));
            return new CompilationResult(false, outputPath, diagnostics.Diagnostics);
        }
    }

    private static void EmitRuntimeConfig(string outputPath)
    {
        string configPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        File.WriteAllText(configPath, """
            {
              "runtimeOptions": {
                "tfm": "net8.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "8.0.0"
                }
              }
            }
            """);
    }

    private static void CopyRuntimeLibrary(string outputPath)
    {
        string outputDir = Path.GetDirectoryName(outputPath) ?? ".";
        string compilerDir = Path.GetDirectoryName(typeof(Compilation).Assembly.Location)!;
        string candidatePath = Path.Combine(compilerDir, "CobolSharp.Runtime.dll");

        if (File.Exists(candidatePath))
        {
            string destPath = Path.Combine(outputDir, "CobolSharp.Runtime.dll");
            if (!File.Exists(destPath) ||
                new FileInfo(candidatePath).LastWriteTimeUtc > new FileInfo(destPath).LastWriteTimeUtc)
            {
                File.Copy(candidatePath, destPath, overwrite: true);
            }
        }
    }

    /// <summary>
    /// Assign byte offsets to all data items in working-storage and file section.
    /// Populates SemanticModel.StorageLocations for the binder to use.
    /// </summary>
    private static void ComputeStorageLayout(Semantics.SemanticModel model)
    {
        int wsOffset = 0;

        foreach (var sym in model.Symbols.Program.DataDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.DataSymbol data)
            {
                // Compute size from PIC or default
                int size = ComputeFieldSize(data);

                var pic = CodeGen.PicDescriptor.FromDataSymbol(data, size);
                var loc = new CodeGen.StorageLocation(
                    CodeGen.StorageAreaKind.WorkingStorage,
                    wsOffset, size, pic);

                model.RegisterStorageLocation(data, loc);
                wsOffset += size;
            }
        }

        // Store total sizes for CIL emitter
        model.WorkingStorageSize = wsOffset > 0 ? wsOffset : 256;
        model.FileSectionSize = 1024;
    }

    private static int ComputeFieldSize(Semantics.DataSymbol data)
    {
        var pic = data.ResolvedType?.Pic;
        if (pic != null && pic.Length > 0)
            return pic.Length + (pic.IsSigned ? 1 : 0);

        // Group items or no PIC: use default
        if (data.LevelNumber == 1 || data.LevelNumber == 77)
            return 120; // default record size

        return 1; // minimum
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
