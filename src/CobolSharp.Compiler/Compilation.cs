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
        string processedText = Preprocess(sourcePath, diagnostics);

        // Phase 1: Lex + Parse
        int dialectLevel = Options.Dialect == Semantics.DialectMode.Default ? 85 : (int)Options.Dialect;
        var tree = LexAndParse(processedText, sourcePath, diagnostics, dialectLevel);
        if (tree == null)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 2: Validate grammar invariants (debug only)
        Semantics.GrammarInvariants.ValidateSentenceAndStatementBoundaries(tree);

        // Phase 3: Collect all program contexts (top-level and nested), tracking containment.
        var programParents = new Dictionary<ParserRuleContext, ParserRuleContext?>();
        var programContexts = CollectProgramContexts(tree, programParents);
        if (programContexts.Count == 0)
            return new CompilationResult(false, "", diagnostics.Diagnostics);

        // Phase 4: Process each program through semantic analysis, binding, and IR generation.
        // Containing programs precede their contained programs (collection order), so each program's
        // ancestors are already built and laid out when we reach it — needed for GLOBAL inheritance.
        var compiledPrograms = new List<CompiledProgram>();
        var modelByContext = new Dictionary<ParserRuleContext, Semantics.SemanticModel>();
        var idByContext = new Dictionary<ParserRuleContext, string>();
        foreach (var progCtx in programContexts)
        {
            string programId = ExtractProgramIdFromContext(progCtx)
                ?? Path.GetFileNameWithoutExtension(sourcePath);
            bool isInitial = ExtractIsInitialFromContext(progCtx);

            var semanticModel = BuildSemanticModel(progCtx, programId, sourcePath, diagnostics, Options);
            semanticModel.Program.IsInitial = isInitial;

            // Validate and compute layout
            Semantics.ParagraphValidator.Validate(semanticModel, diagnostics);
            Semantics.StorageLayoutComputer.ComputeLayout(semanticModel);
            Semantics.DataItemClassifier.Validate(semanticModel, diagnostics);
            Semantics.FileStatusValidator.Validate(semanticModel, diagnostics);
            Semantics.SymbolValidator.Validate(semanticModel, diagnostics);

            // Make IS GLOBAL items declared in containing programs visible here, sharing the
            // containing program's storage at runtime (ISO §8.4.5).
            InheritGlobalItems(progCtx, semanticModel, programParents, modelByContext, idByContext);

            modelByContext[progCtx] = semanticModel;
            idByContext[progCtx] = programId;

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
        CobolParserCore.CompilationUnitContext tree,
        Dictionary<ParserRuleContext, ParserRuleContext?> parents)
    {
        var result = new List<ParserRuleContext>();

        foreach (var group in tree.compilationGroup())
        {
            foreach (var programUnit in group.programUnit())
            {
                result.Add(programUnit);
                parents[programUnit] = null;
                CollectNestedPrograms(programUnit.nestedProgram(), programUnit, result, parents);
            }
        }

        return result;
    }

    /// <summary>
    /// Recursively collect nested (contained) program contexts, recording each one's containing
    /// program in <paramref name="parents"/> so GLOBAL inheritance can walk the containment chain.
    /// </summary>
    private static void CollectNestedPrograms(
        CobolParserCore.NestedProgramContext[] nestedPrograms,
        ParserRuleContext parent,
        List<ParserRuleContext> result,
        Dictionary<ParserRuleContext, ParserRuleContext?> parents)
    {
        foreach (var nested in nestedPrograms)
        {
            result.Add(nested);
            parents[nested] = parent;
            CollectNestedPrograms(nested.nestedProgram(), nested, result, parents);
        }
    }

    /// <summary>
    /// Make data items declared IS GLOBAL in containing programs visible in <paramref name="model"/>.
    /// ISO §8.4.5: a global name is available to the program that declares it and to every program
    /// contained within it (directly or indirectly), unless that contained program declares the same
    /// name itself (which shadows the global). The item keeps its storage in the declaring program's
    /// ProgramState; its StorageLocation is tagged with that program's id so the emitter reads the
    /// shared bytes (see <see cref="CodeGen.StorageLocation.OwnerProgramId"/>).
    /// </summary>
    private static void InheritGlobalItems(
        ParserRuleContext progCtx,
        Semantics.SemanticModel model,
        IReadOnlyDictionary<ParserRuleContext, ParserRuleContext?> parents,
        IReadOnlyDictionary<ParserRuleContext, Semantics.SemanticModel> modelByContext,
        IReadOnlyDictionary<ParserRuleContext, string> idByContext)
    {
        // Walk outward through containing programs, nearest first (nearest declaration wins).
        var ancestor = parents.TryGetValue(progCtx, out var p) ? p : null;
        while (ancestor != null)
        {
            if (modelByContext.TryGetValue(ancestor, out var ancestorModel)
                && idByContext.TryGetValue(ancestor, out var ownerId))
            {
                foreach (var item in ancestorModel.DataItemsInOrder)
                {
                    if (!item.IsGlobal) continue; // IS GLOBAL is set on the 01/77 declaring item
                    foreach (var member in EnumerateSelfAndDescendants(item))
                    {
                        if (member.IsFiller) continue; // FILLER is unreferenceable
                        if (ancestorModel.GetStorageLocation(member) is not { } loc) continue;
                        model.TryInheritGlobal(member, loc with { OwnerProgramId = ownerId });
                    }
                }
            }
            ancestor = parents.TryGetValue(ancestor, out var pp) ? pp : null;
        }
    }

    private static IEnumerable<Semantics.DataSymbol> EnumerateSelfAndDescendants(Semantics.DataSymbol item)
    {
        yield return item;
        foreach (var child in item.Children)
            foreach (var d in EnumerateSelfAndDescendants(child))
                yield return d;
    }

    private string Preprocess(string sourcePath, DiagnosticBag diagnostics)
    {
        string rawText = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        rawText = ReferenceFormatProcessor.StripNistArchiveMarkers(rawText);
        string normalizedText = ReferenceFormatProcessor.NormalizeToFreeForm(rawText);

        // COPY expansion runs BEFORE NIST placeholder substitution so that XXXXX###/XXXXP###/
        // XXXXD### placeholders inside copied library text are substituted too — e.g. a file's
        // ASSIGN target supplied by a COPY (K3FCB) must map to the same physical file as the
        // matching placeholder in the consumer's own source. COPY library-name qualifiers are
        // raw placeholders (XXXXX047) resolved against the copy library directory, so expanding
        // first does not disturb them.
        var copyProcessor = new CopyProcessor(_copySearchPaths, diagnostics, sourcePath,
            strict: Options.Dialect != Semantics.DialectMode.Default);
        string expandedText = copyProcessor.Process(normalizedText, sourceDir);

        if (NistTestName != null)
            expandedText = NistPreprocessor.Process(expandedText, NistTestName);

        return expandedText;
    }

    private static CobolParserCore.CompilationUnitContext? LexAndParse(
        string processedText,
        string sourcePath,
        DiagnosticBag diagnostics,
        int dialectLevel = 85)
    {
        var inputStream = new AntlrInputStream(processedText);
        var lexer = new CobolLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        // Rewrite ZERO tokens to ZERO_ARITH in arithmetic contexts.
        // Must run after lexing, before parsing — avoids grammar ambiguity
        // that causes exponential ANTLR prediction time.
        ZeroTokenRewriter.Rewrite(tokenStream);

        var parser = new CobolParserCore(tokenStream);
        parser.DialectLevel = dialectLevel;

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

    /// <summary>
    /// SPECIAL-NAMES-declared names that are valid references but live outside any Scope: mnemonic-names
    /// and their ON/OFF switch conditions, symbolic characters, user CLASS names, and ALPHABET names.
    /// <see cref="Semantics.ReferenceResolver"/> whitelists these so its undefined-data-name check
    /// (CBL3128) does not flag them.
    /// </summary>
    private static IEnumerable<string> CollectSpecialNames(Semantics.SemanticBuilder b)
    {
        foreach (var sw in b.ImplementorSwitches)
        {
            yield return sw.Name;
            if (sw.OnValueName != null) yield return sw.OnValueName;
            if (sw.OffValueName != null) yield return sw.OffValueName;
        }
        foreach (var (name, _) in b.SymbolicCharacters) yield return name;
        foreach (var c in b.ClassDefinitions) yield return c.Name;
        foreach (var a in b.AlphabetDefinitions) yield return a.Name;
    }

    private static Semantics.SemanticModel BuildSemanticModel(
        ParserRuleContext programTree,
        string programId,
        string sourcePath,
        DiagnosticBag diagnostics,
        Semantics.CompilationOptions options)
    {
        // Pass 1: Declaration collection
        var semanticBuilder = new Semantics.SemanticBuilder(programId, 1, options, sourcePath);
        semanticBuilder.Visit(programTree);
        semanticBuilder.ResolveRedefines();
        semanticBuilder.ResolveRenames();
        semanticBuilder.PropagateGroupSignClauses();

        // Pass 2: Reference resolution
        var semDiagnostics = new List<Diagnostic>(semanticBuilder.Diagnostics);
        var resolver = new Semantics.ReferenceResolver(
            semanticBuilder.Symbols, semDiagnostics, sourcePath, options, CollectSpecialNames(semanticBuilder));
        resolver.Visit(programTree);

        foreach (var d in semDiagnostics)
            diagnostics.Add(d);

        // Build model
        var model = new Semantics.SemanticModel(
            semanticBuilder.Symbols.Program,
            semanticBuilder.Symbols,
            diagnostics);
        model.SourceName = sourcePath;

        model.SetPicEnvironment(semanticBuilder.CurrencySign, semanticBuilder.CurrencyOutputChar, semanticBuilder.DecimalPointIsComma);

        foreach (var sw in semanticBuilder.ImplementorSwitches)
            model.RegisterImplementorSwitch(sw);

        foreach (var classDef in semanticBuilder.ClassDefinitions)
            model.RegisterClassDefinition(classDef);

        foreach (var (symName, symValue) in semanticBuilder.SymbolicCharacters)
            model.RegisterSymbolicCharacter(symName, symValue);

        foreach (var alphaDef in semanticBuilder.AlphabetDefinitions)
            model.RegisterAlphabetDefinition(alphaDef);

        // Resolve PROGRAM COLLATING SEQUENCE (needs alphabet definitions registered first).
        // An identity sequence (NATIVE / STANDARD-1 / STANDARD-2 on this ASCII host) is behaviorally
        // identical to having no program collating sequence, so normalize it to null: every consumer
        // then uses the proven native comparison path (which is trailing-space-insensitive like COBOL
        // alphanumeric comparison), instead of the weight-table path. Only a genuinely reordered
        // alphabet is stored, so it is still honored. This keeps STANDARD-* programs (e.g. the CCVS
        // boilerplate's `ALPHABET x IS STANDARD-2`) comparing exactly as native.
        if (semanticBuilder.ProgramCollatingSequenceAlphabetName is { } pcsName)
        {
            var alphaDef = model.ResolveAlphabetDefinition(pcsName);
            if (alphaDef != null && !IsIdentityCollation(alphaDef.CollatingSequence))
                model.SetProgramCollatingSequence(alphaDef.CollatingSequence);
        }

        foreach (var ext in semanticBuilder.ExtensionClauses)
            model.AddExtensionClause(ext);

        if (semanticBuilder.ScreenItems.Count > 0)
            model.RegisterScreenItems(semanticBuilder.ScreenItems);

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

    /// <summary>
    /// True when a 256-entry collating table is the identity mapping (weight[i] == i), i.e. it
    /// reorders nothing. NATIVE / STANDARD-1 / STANDARD-2 produce identity on this ASCII host, and
    /// an identity program collating sequence is indistinguishable from having none — so callers
    /// normalize it to null and use the native comparison path.
    /// </summary>
    private static bool IsIdentityCollation(byte[] sequence)
    {
        if (sequence.Length != 256) return false;
        for (int i = 0; i < 256; i++)
            if (sequence[i] != (byte)i) return false;
        return true;
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

        return idDiv?.identificationBody()?.programIdParagraph()?.programName()?.cobolWord()?.GetText();
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
        return progId?.programName()?.cobolWord()?.GetText();
    }

    /// <summary>Compiled program tuple: PROGRAM-ID, IR module, and semantic model.</summary>
    private sealed record CompiledProgram(
        string ProgramId,
        IR.IrModule IrModule,
        Semantics.SemanticModel SemanticModel);
}
