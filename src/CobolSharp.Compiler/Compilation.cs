// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Preprocessor;

namespace CobolSharp.Compiler;

/// <summary>
/// Top-level compilation facade. Orchestrates all compiler phases:
/// Source -> Preprocess -> ANTLR4 Lex -> ANTLR4 Parse -> Semantic -> Emit.
/// Each phase delegates to a focused component — this class only wires the pipeline.
/// </summary>
public sealed class Compilation
{
    private readonly List<string> _copySearchPaths = [];

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

        // Phase 0: Preprocess
        string processedText = Preprocess(sourcePath);

        // Phase 1: Lex + Parse
        var tree = LexAndParse(processedText, sourcePath, diagnostics);
        if (tree == null)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 2: Validate grammar invariants (debug only)
        Semantics.GrammarInvariants.ValidateSentenceAndStatementBoundaries(tree);

        // Phase 3: Semantic analysis
        string programId = ExtractProgramId(tree) ?? Path.GetFileNameWithoutExtension(sourcePath);
        var semanticModel = BuildSemanticModel(tree, programId, diagnostics);

        // Phase 4: Validate and compute layout
        Semantics.ParagraphValidator.Validate(semanticModel, diagnostics);
        Semantics.StorageLayoutComputer.ComputeLayout(semanticModel);

        // Phase 5: Bind -> IR
        var binder = new CodeGen.Binder(semanticModel, diagnostics);
        var irModule = binder.Bind(tree);

        // Phase 6: CIL emission
        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            programId + ".dll");

        return EmitAssembly(irModule, programId, semanticModel, outputPath, sourcePath, diagnostics);
    }

    private string Preprocess(string sourcePath)
    {
        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        string normalizedText = ReferenceFormatProcessor.NormalizeToFreeForm(rawText);

        if (NistTestName != null)
            normalizedText = NistPreprocessor.Process(normalizedText, NistTestName);

        var copyProcessor = new CopyProcessor(_copySearchPaths);
        return copyProcessor.Process(normalizedText, sourceDir);
    }

    private static CobolParserCore.CompilationUnitContext? LexAndParse(
        string processedText,
        string sourcePath,
        DiagnosticBag diagnostics)
    {
        var inputStream = new AntlrInputStream(processedText);
        var lexer = new CobolLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new CobolParserCore(tokenStream);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CobolErrorListener(diagnostics, sourcePath));

        var tree = parser.compilationUnit();
        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }

    private static Semantics.SemanticModel BuildSemanticModel(
        CobolParserCore.CompilationUnitContext tree,
        string programId,
        DiagnosticBag diagnostics)
    {
        // Pass 1: Declaration collection
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1);
        semanticBuilder.Visit(tree);
        semanticBuilder.ResolveRedefines();
        semanticBuilder.PropagateGroupSignClauses();

        // Pass 2: Reference resolution
        var semDiagnostics = new List<Diagnostic>(semanticBuilder.Diagnostics);
        var resolver = new Semantics.ReferenceResolver(semanticBuilder.Symbols, semDiagnostics);
        resolver.Visit(tree);

        foreach (var d in semDiagnostics)
            diagnostics.Add(d);

        // Build model
        var model = new Semantics.SemanticModel(
            semanticBuilder.Symbols.Program,
            semanticBuilder.Symbols,
            diagnostics);

        model.SetPicEnvironment(semanticBuilder.CurrencySign, semanticBuilder.DecimalPointIsComma);

        // Populate procedure symbols
        foreach (var sym in semanticBuilder.Symbols.Program.ProcedureDivisionScope.Symbols.Values)
        {
            if (sym is Semantics.ParagraphSymbol para)
                model.AddParagraph(para);
            else if (sym is Semantics.SectionSymbol sect)
                model.AddSection(sect);
        }

        foreach (var (sectionName, paragraphNames) in semanticBuilder.SectionParagraphs)
        {
            foreach (var paraName in paragraphNames)
                model.RegisterSectionParagraph(sectionName, paraName);
        }

        // Populate data items
        foreach (var data in semanticBuilder.DataItemsInOrder)
        {
            if (data.LevelNumber is 1 or 77)
                model.AddDataRecord(data);
        }
        model.SetDataItemsInOrder(semanticBuilder.DataItemsInOrder);

        return model;
    }

    private static CompilationResult EmitAssembly(
        IR.IrModule irModule,
        string programId,
        Semantics.SemanticModel semanticModel,
        string outputPath,
        string sourcePath,
        DiagnosticBag diagnostics)
    {
        try
        {
            var assembly = CodeGen.CilEmitter.EmitAssembly(irModule, programId, semanticModel);

            string dir = Path.GetDirectoryName(outputPath) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            assembly.Write(outputPath);

            EmitRuntimeConfig(outputPath);
            CopyRuntimeLibrary(outputPath);

            return new CompilationResult(!diagnostics.HasErrors, outputPath, diagnostics.Diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.ReportError("CIL", $"CIL emission failed: {ex.Message}\n{ex.StackTrace}",
                new SourceLocation(sourcePath, 0, 0, 0),
                new TextSpan(0, 0));
            return new CompilationResult(false, outputPath, diagnostics.Diagnostics);
        }
    }

    private static void EmitRuntimeConfig(string outputPath)
    {
        string configPath = Path.ChangeExtension(outputPath, ".runtimeconfig.json");
        var version = Environment.Version;
        string tfm = $"net{version.Major}.{version.Minor}";
        string frameworkVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        File.WriteAllText(configPath, $$"""
            {
              "runtimeOptions": {
                "tfm": "{{tfm}}",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "{{frameworkVersion}}"
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

    private static string? ExtractProgramId(CobolParserCore.CompilationUnitContext tree)
    {
        var compilationGroups = tree.compilationGroup();
        if (compilationGroups.Length == 0) return null;

        var programUnit = compilationGroups[0].programUnit();
        if (programUnit.Length == 0) return null;

        var idDiv = programUnit[0].identificationDivision();
        var body = idDiv?.identificationBody();
        var progId = body?.programIdParagraph();
        return progId?.programName()?.IDENTIFIER()?.GetText();
    }
}
