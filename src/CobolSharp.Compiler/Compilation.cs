// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
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

    /// <summary>
    /// When set, enables NIST preprocessing: replaces XXXXX### placeholders
    /// with CobolSharp-appropriate values. The value is the test name (e.g., "NC101A").
    /// </summary>
    public string? NistTestName { get; set; }

    public CompilationResult Compile(string sourcePath, string? outputPath = null)
    {
        var diagnostics = new DiagnosticBag();

        // Phase 0: Load and preprocess (expand COPY statements)
        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        // Detect and normalize reference format (fixed-form → free-form)
        string normalizedText = ReferenceFormatProcessor.NormalizeToFreeForm(rawText);

        // NIST preprocessing: replace XXXXX### site-specific placeholders
        if (NistTestName != null)
            normalizedText = NistPreprocessor.Process(normalizedText, NistTestName);

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

        // Phase 2b: Validate grammar invariants (debug only)
        Semantics.GrammarInvariants.ValidateSentenceAndStatementBoundaries(tree);

        // Phase 3: Semantic analysis — Pass 1: declaration collection
        string programId = ExtractProgramId(tree) ?? Path.GetFileNameWithoutExtension(sourcePath);
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1);
        semanticBuilder.Visit(tree);

        // Phase 3a: Resolve deferred REDEFINES (requires all data items registered)
        semanticBuilder.ResolveRedefines();
        semanticBuilder.PropagateGroupSignClauses();

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

        // Wire SPECIAL-NAMES PIC environment into semantic model
        semanticModel.SetPicEnvironment(semanticBuilder.CurrencySign, semanticBuilder.DecimalPointIsComma);

        // Populate paragraphs, sections, and data records from symbol table
        foreach (var sym in semanticBuilder.Symbols.Program.ProcedureDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.ParagraphSymbol para)
                semanticModel.AddParagraph(para);
            else if (sym is Semantics.SectionSymbol sect)
                semanticModel.AddSection(sect);
        }
        // Register section-paragraph membership
        foreach (var (sectionName, paragraphNames) in semanticBuilder.SectionParagraphs)
        {
            foreach (var paraName in paragraphNames)
                semanticModel.RegisterSectionParagraph(sectionName, paraName);
        }

        // Expose data items in declaration order (from SemanticBuilder's tree)
        foreach (var data in semanticBuilder.DataItemsInOrder)
        {
            if (data.LevelNumber == 1 || data.LevelNumber == 77)
                semanticModel.AddDataRecord(data);
        }
        semanticModel.SetDataItemsInOrder(semanticBuilder.DataItemsInOrder);

        // Phase 4a: Validate paragraphs — detect phantom paragraphs
        ValidateParagraphs(semanticModel, diagnostics);

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
            var assembly = CodeGen.CilEmitter.EmitAssembly(irModule, programId, semanticModel);

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
            diagnostics.ReportError("CIL", $"CIL emission failed: {ex.Message}\n{ex.StackTrace}",
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
    /// <summary>
    /// Recursive storage layout: groups share bytes with their children.
    /// Only elementary items (with PIC) consume bytes.
    /// </summary>
    /// <summary>
    /// Detect phantom paragraphs — paragraphs with no statements that may indicate
    /// a parsing error (e.g., stray identifier like 'LINES' from WRITE ADVANCING).
    /// </summary>
    private static void ValidateParagraphs(
        Semantics.SemanticModel model,
        Diagnostics.DiagnosticBag diagnostics)
    {
        // Check for paragraphs with suspicious names that are known COBOL keywords
        // These should never be paragraph names in well-formed programs
        var suspiciousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LINE", "LINES", "PAGE", "ADVANCING", "GIVING", "ROUNDED",
            "TALLYING", "REPLACING", "UNTIL", "VARYING", "TIMES",
        };

        foreach (var para in model.ParagraphsInOrder)
        {
            if (suspiciousNames.Contains(para.Name))
            {
                diagnostics.Add(new Diagnostics.Diagnostic(
                    "SEM",
                    Diagnostics.DiagnosticSeverity.Warning,
                    $"Paragraph '{para.Name}' has a name that matches a COBOL keyword — " +
                    "this may indicate a parsing error (e.g., unconsumed keyword from a statement clause).",
                    new Common.SourceLocation("<source>", 0, para.Line, 0),
                    new Common.TextSpan(0, 0)));
            }
        }
    }

    private static void ComputeStorageLayout(Semantics.SemanticModel model)
    {
        int wsOffset = 0;
        int fsOffset = 0;

        // Layout working storage roots
        foreach (var data in model.DataItemsInOrder)
        {
            if ((data.LevelNumber == 1 || data.LevelNumber == 77) &&
                data.Area == CodeGen.StorageAreaKind.WorkingStorage)
            {
                LayoutItem(data, CodeGen.StorageAreaKind.WorkingStorage, ref wsOffset, model);
            }
        }

        // Layout file section roots: all 01-level records under the same FD
        // share the same record buffer (implicit REDEFINES). Layout each at
        // offset 0 and take the max size as the file section size.
        int maxRecordSize = 0;
        foreach (var data in model.DataItemsInOrder)
        {
            if (data.LevelNumber == 1 && data.Area == CodeGen.StorageAreaKind.FileSection)
            {
                int recOffset = 0;
                LayoutItem(data, CodeGen.StorageAreaKind.FileSection, ref recOffset, model);
                if (recOffset > maxRecordSize) maxRecordSize = recOffset;
            }
        }
        fsOffset = maxRecordSize;

        model.WorkingStorageSize = wsOffset > 0 ? wsOffset : 256;
        model.FileSectionSize = fsOffset > 0 ? fsOffset : 256;
    }

    /// <summary>
    /// Recursively layout a data item and its children.
    /// </summary>
    private static void LayoutItem(
        Semantics.DataSymbol item,
        CodeGen.StorageAreaKind area,
        ref int offset,
        Semantics.SemanticModel model)
    {
        // REDEFINES: share bytes with target, do NOT advance offset
        if (item.Redefines != null)
        {
            var targetLoc = model.GetStorageLocation(item.Redefines);
            if (targetLoc.HasValue)
            {
                // REDEFINES shares the target's offset and area, but uses its own PIC
                int size = item.IsElementary ? ComputeFieldSize(item) : targetLoc.Value.Length;
                var pic = CodeGen.CompilerPicDescriptorFactory.FromDataSymbol(item, size, model.PicEnvironment);
                var loc = new CodeGen.StorageLocation(targetLoc.Value.Area, targetLoc.Value.Offset, size, pic);
                model.RegisterStorageLocation(item, loc);
                RegisterValue(model, item);

                // For group REDEFINES, recurse into children so nested items
                // get storage locations (e.g., CM-18V0 REDEFINES COMPUTED-A
                // contains COMPUTED-18V0 which needs its own location)
                if (item.Children.Count > 0)
                {
                    int childOffset = targetLoc.Value.Offset;
                    foreach (var child in item.Children)
                        LayoutItem(child, area, ref childOffset, model);
                }
            }
            return;
        }

        if (item.IsElementary)
        {
            // Elementary: allocate bytes from PIC
            int elementSize = ComputeFieldSize(item);
            item.ElementSize = elementSize;
            int totalSize = elementSize * item.OccursCount;
            var pic = CodeGen.CompilerPicDescriptorFactory.FromDataSymbol(item, totalSize, model.PicEnvironment);
            var loc = new CodeGen.StorageLocation(area, offset, totalSize, pic);
            model.RegisterStorageLocation(item, loc);
            RegisterValue(model, item);
            offset += totalSize;
        }
        else
        {
            // Group: recurse into children, then span from first child to last
            int groupStart = offset;

            if (item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                    LayoutItem(child, area, ref offset, model);

                // Group spans all children; OCCURS multiplies the total
                int childrenSize = offset - groupStart;
                if (childrenSize <= 0) childrenSize = 1;
                item.ElementSize = childrenSize;
                int groupSize = childrenSize * item.OccursCount;
                if (item.OccursCount > 1)
                    offset = groupStart + groupSize; // Advance past all occurrences
                var pic = CodeGen.CompilerPicDescriptorFactory.FromDataSymbol(item, groupSize, model.PicEnvironment);
                var loc = new CodeGen.StorageLocation(area, groupStart, groupSize, pic);
                model.RegisterStorageLocation(item, loc);
            }
            else
            {
                // Empty group (no children found) — allocate minimum
                var pic = CodeGen.CompilerPicDescriptorFactory.FromDataSymbol(item, 1, model.PicEnvironment);
                var loc = new CodeGen.StorageLocation(area, offset, 1, pic);
                model.RegisterStorageLocation(item, loc);
                offset += 1;
            }

            RegisterValue(model, item);
        }
    }

    private static void RegisterValue(Semantics.SemanticModel model, Semantics.DataSymbol data)
    {
        // Figurative constant VALUE (SPACE, HIGH-VALUE, etc.): register for field-filling init
        if (data.FigurativeInit.HasValue)
        {
            model.RegisterFigurativeInit(data, data.FigurativeInit.Value);
            // For ZERO and SPACE, also register the normal InitialValue path
            // so numeric fields get correct numeric initialization
            if (data.InitialValue == null) return;
        }

        if (data.InitialValue == null) return;

        if (decimal.TryParse(data.InitialValue,
            System.Globalization.CultureInfo.InvariantCulture, out var numVal)
            && data.ResolvedType?.IsNumeric == true)
        {
            model.RegisterInitialValue(data, numVal, Runtime.CobolCategory.Numeric);
        }
        else
        {
            model.RegisterInitialValue(data, data.InitialValue, Runtime.CobolCategory.Alphanumeric);
        }
    }

    private static int ComputeFieldSize(Semantics.DataSymbol data)
    {
        var pic = data.ResolvedType?.Pic;
        if (pic == null || pic.Length <= 0) return 1;

        int totalDigits = pic.IntegerDigits + pic.FractionDigits;

        // COMP/BINARY: binary storage size based on digit count
        if (data.Usage is Runtime.UsageKind.Comp or Runtime.UsageKind.Binary)
            return totalDigits switch { <= 4 => 2, <= 9 => 4, _ => 8 };

        // COMP-3/PACKED-DECIMAL: BCD packed (2 digits per byte + sign nibble)
        if (data.Usage is Runtime.UsageKind.Comp3 or Runtime.UsageKind.PackedDecimal)
            return (totalDigits + 2) / 2;

        // COMP-1 (float) / COMP-2 (double)
        if (data.Usage == Runtime.UsageKind.Comp1) return 4;
        if (data.Usage == Runtime.UsageKind.Comp2) return 8;

        // DISPLAY: digit length + sign byte (only for SEPARATE)
        bool separateSign = data.ExplicitSignStorage is Runtime.SignStorageKind.LeadingSeparate
            or Runtime.SignStorageKind.TrailingSeparate;
        int signBytes = separateSign ? 1 : 0;
        return pic.Length + signBytes;
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
