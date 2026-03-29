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
/// Supports multiple programs in a single source file (batch compilation)
/// and nested/contained programs per the COBOL-85 spec.
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

    /// <summary>Compilation options controlling dialect, warnings, and feature gating.</summary>
    public Semantics.CompilationOptions Options { get; set; } = new();

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

        // Phase 3: Collect all program contexts (top-level and nested)
        var programContexts = CollectProgramContexts(tree);
        if (programContexts.Count == 0)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 4: Process each program through semantic analysis, binding, and IR generation
        var compiledPrograms = new List<CompiledProgram>();
        foreach (var progCtx in programContexts)
        {
            string programId = ExtractProgramIdFromContext(progCtx)
                ?? Path.GetFileNameWithoutExtension(sourcePath);
            bool isInitial = ExtractIsInitialFromContext(progCtx);

            var semanticModel = BuildSemanticModel(progCtx, programId, diagnostics);
            semanticModel.Program.IsInitial = isInitial;

            // Validate and compute layout
            Semantics.ParagraphValidator.Validate(semanticModel, diagnostics);
            Semantics.StorageLayoutComputer.ComputeLayout(semanticModel);
            Semantics.DataItemClassifier.Validate(semanticModel, diagnostics);
            Semantics.FileStatusValidator.Validate(semanticModel, diagnostics);
            Semantics.SymbolValidator.Validate(semanticModel, diagnostics);

            // Bind -> IR
            var binder = new CodeGen.Binder(semanticModel, diagnostics, Options);
            var irModule = binder.Bind(progCtx);

            compiledPrograms.Add(new CompiledProgram(programId, irModule, semanticModel));
        }

        // Phase 5: CIL emission — all programs into a single assembly
        string mainProgramId = compiledPrograms[0].ProgramId;
        outputPath ??= Path.Combine(
            Path.GetDirectoryName(sourcePath) ?? ".",
            mainProgramId + ".dll");

        return EmitAssembly(compiledPrograms, outputPath, sourcePath, diagnostics);
    }

    /// <summary>
    /// Collect all program parse contexts from the compilation unit.
    /// Returns top-level programUnit contexts and recursively collects nested programs.
    /// Each context represents an independent COBOL program to compile.
    /// </summary>
    private static List<ParserRuleContext> CollectProgramContexts(
        CobolParserCore.CompilationUnitContext tree)
    {
        var result = new List<ParserRuleContext>();

        foreach (var group in tree.compilationGroup())
        {
            foreach (var programUnit in group.programUnit())
            {
                result.Add(programUnit);
                CollectNestedPrograms(programUnit.nestedProgram(), result);
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively collect nested program contexts.
    /// </summary>
    private static void CollectNestedPrograms(
        CobolParserCore.NestedProgramContext[] nestedPrograms,
        List<ParserRuleContext> result)
    {
        foreach (var nested in nestedPrograms)
        {
            result.Add(nested);
            CollectNestedPrograms(nested.nestedProgram(), result);
        }
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

        // Rewrite ZERO tokens to ZERO_ARITH in arithmetic contexts.
        // Must run after lexing, before parsing — avoids grammar ambiguity
        // that causes exponential ANTLR prediction time.
        ZeroTokenRewriter.Rewrite(tokenStream);

        var parser = new CobolParserCore(tokenStream);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CobolErrorListener(diagnostics, sourcePath));
        parser.ErrorHandler = new CobolErrorStrategy();

        // Two-stage parsing: try fast SLL mode first, fall back to full LL on error.
        // BailErrorStrategy forces SLL to throw on ambiguity instead of hanging.
        parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;
        parser.ErrorHandler = new Antlr4.Runtime.BailErrorStrategy();
        CobolParserCore.CompilationUnitContext tree;
        try
        {
            tree = parser.compilationUnit();
        }
        catch (Exception)
        {
            // SLL failed — retry with full LL prediction and normal error handling
            tokenStream.Seek(0);
            parser.Reset();
            parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.LL;
            parser.ErrorHandler = new CobolErrorStrategy();
            tree = parser.compilationUnit();
        }
        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }

    private static Semantics.SemanticModel BuildSemanticModel(
        ParserRuleContext programTree,
        string programId,
        DiagnosticBag diagnostics)
    {
        // Pass 1: Declaration collection
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1);
        semanticBuilder.Visit(programTree);
        semanticBuilder.ResolveRedefines();
        semanticBuilder.ResolveRenames();
        semanticBuilder.PropagateGroupSignClauses();

        // Pass 2: Reference resolution
        var semDiagnostics = new List<Diagnostic>(semanticBuilder.Diagnostics);
        var resolver = new Semantics.ReferenceResolver(semanticBuilder.Symbols, semDiagnostics);
        resolver.Visit(programTree);

        foreach (var d in semDiagnostics)
            diagnostics.Add(d);

        // Build model
        var model = new Semantics.SemanticModel(
            semanticBuilder.Symbols.Program,
            semanticBuilder.Symbols,
            diagnostics);

        model.SetPicEnvironment(semanticBuilder.CurrencySign, semanticBuilder.DecimalPointIsComma);

        foreach (var sw in semanticBuilder.ImplementorSwitches)
            model.RegisterImplementorSwitch(sw);

        foreach (var classDef in semanticBuilder.ClassDefinitions)
            model.RegisterClassDefinition(classDef);

        foreach (var (symName, symValue) in semanticBuilder.SymbolicCharacters)
            model.RegisterSymbolicCharacter(symName, symValue);

        foreach (var alphaDef in semanticBuilder.AlphabetDefinitions)
            model.RegisterAlphabetDefinition(alphaDef);

        // Resolve PROGRAM COLLATING SEQUENCE (needs alphabet definitions registered first)
        if (semanticBuilder.ProgramCollatingSequenceAlphabetName is { } pcsName)
        {
            var alphaDef = model.ResolveAlphabetDefinition(pcsName);
            if (alphaDef != null)
                model.SetProgramCollatingSequence(alphaDef.CollatingSequence);
        }

        foreach (var ext in semanticBuilder.ExtensionClauses)
            model.AddExtensionClause(ext);

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

        // Resolve PROCEDURE DIVISION USING parameters to LINKAGE SECTION DataSymbols
        var usingParams = new List<Semantics.DataSymbol>();
        foreach (var name in semanticBuilder.ProcedureUsingNames)
        {
            var sym = semanticBuilder.Symbols.Program.DataDivisionScope.Resolve<Semantics.DataSymbol>(name);
            if (sym != null)
                usingParams.Add(sym);
        }
        model.SetProcedureUsingParameters(usingParams);

        // Resolve PROCEDURE DIVISION RETURNING
        if (semanticBuilder.ProcedureReturningName != null)
        {
            var retSym = semanticBuilder.Symbols.Program.DataDivisionScope
                .Resolve<Semantics.DataSymbol>(semanticBuilder.ProcedureReturningName);
            model.SetProcedureReturningItem(retSym);
        }

        return model;
    }

    private static CompilationResult EmitAssembly(
        List<CompiledProgram> programs,
        string outputPath,
        string sourcePath,
        DiagnosticBag diagnostics)
    {
        try
        {
            var mainProgram = programs[0];
            var assembly = CodeGen.CilEmitter.EmitAssembly(
                programs.Select(p => (p.IrModule, (Semantics.SemanticModel?)p.SemanticModel)).ToList(),
                mainProgram.ProgramId);

            string dir = Path.GetDirectoryName(outputPath) ?? ".";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            assembly.Write(outputPath);

            EmitRuntimeConfig(outputPath);
            CopyRuntimeLibrary(outputPath);

            return new CompilationResult(!diagnostics.HasErrors, outputPath, diagnostics.Diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Report(DiagnosticDescriptors.COBOL0600,
                new SourceLocation(sourcePath, 0, 0, 0),
                TextSpan.Empty,
                programs[0].ProgramId, ex.Message);
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

    /// <summary>Extract PROGRAM-ID from a programUnit or nestedProgram context.</summary>
    private static string? ExtractProgramIdFromContext(ParserRuleContext ctx)
    {
        CobolParserCore.IdentificationDivisionContext? idDiv = ctx switch
        {
            CobolParserCore.ProgramUnitContext pu => pu.identificationDivision(),
            CobolParserCore.NestedProgramContext np => np.identificationDivision(),
            _ => null
        };

        return idDiv?.identificationBody()?.programIdParagraph()?.programName()?.IDENTIFIER()?.GetText();
    }

    /// <summary>Extract IS INITIAL attribute from a programUnit or nestedProgram context.</summary>
    private static bool ExtractIsInitialFromContext(ParserRuleContext ctx)
    {
        CobolParserCore.IdentificationDivisionContext? idDiv = ctx switch
        {
            CobolParserCore.ProgramUnitContext pu => pu.identificationDivision(),
            CobolParserCore.NestedProgramContext np => np.identificationDivision(),
            _ => null
        };

        var attrs = idDiv?.identificationBody()?.programIdParagraph()
            ?.programIdAttributes()?.programIdAttribute();
        if (attrs == null) return false;

        return attrs.Any(a => a.commonProgramAttribute()?.INITIAL_() != null);
    }

    /// <summary>Extract PROGRAM-ID from the first program in the tree (backward compat).</summary>
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

    /// <summary>Compiled program tuple: PROGRAM-ID, IR module, and semantic model.</summary>
    private sealed record CompiledProgram(
        string ProgramId,
        IR.IrModule IrModule,
        Semantics.SemanticModel SemanticModel);
}
