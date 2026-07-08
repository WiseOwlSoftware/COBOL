// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Frontend;

/// <summary>
/// The COBOL.NET front-end: source text → preprocessed free-form text → ANTLR parse tree.
/// </summary>
/// <remarks>
/// This is the ONE place COBOL.NET reuses the legacy <c>CobolSharp.Compiler</c> assembly, and only its
/// <i>front-end</i>: the source preprocessor (reference-format normalization, conditional compilation, COPY
/// expansion, NIST placeholder substitution) and the ANTLR lexer/parser. The parse tree it returns
/// (<see cref="CobolParserCore.CompilationUnitContext"/>) is a pure syntactic artifact — none of the legacy
/// semantic analysis, byte-offset storage layout, or CIL emission is involved. Everything from the parse tree
/// onward is COBOL.NET's own typed-native pipeline.
/// <para>
/// The pipeline mirrors the legacy <c>Compilation.Preprocess</c> + <c>Compilation.LexAndParse</c> exactly so
/// the proven preprocessing (incl. the SLL→LL two-stage parse and the <c>ZERO</c>→<c>ZERO_ARITH</c> rewrite)
/// is preserved bit-for-bit.
/// </para>
/// </remarks>
public sealed class Frontend
{
    /// <summary>Directories searched for COPY copybooks, in order.</summary>
    private readonly List<string> _copySearchPaths = [];

    /// <summary>
    /// When non-null, enables NIST CCVS preprocessing for the named test (e.g. <c>"NC101A"</c>): the
    /// <c>XXXXX###</c>/<c>XXXXP###</c>/<c>XXXXD###</c> placeholders are substituted with COBOL.NET-appropriate
    /// values so the CCVS conformance programs compile.
    /// </summary>
    public string? NistTestName { get; init; }

    /// <summary>
    /// The dialect level (ISO year) the parser admits: <c>85</c>, <c>2002</c>, <c>2014</c>, or <c>2023</c>.
    /// Higher levels enable the post-85 grammar (OO, JSON/XML, generics, …). Defaults to COBOL-85, the level
    /// the NIST CCVS corpus targets.
    /// </summary>
    public int DialectLevel { get; init; } = 85;

    /// <summary>The strict/permissive severity axis (the P2.7 flip) as it applies to PREPROCESSOR-level
    /// removal gates (the W3 VCR 2/4/94 threading, DEVLOG 598): removed = error strict / warning permissive.
    /// Defaults strict, matching <c>EditionContext</c>.</summary>
    public bool Permissive { get; init; }

    /// <summary>Add a directory to the COPY copybook search path.</summary>
    public void AddCopySearchPath(string path) => _copySearchPaths.Add(path);

    /// <summary>The <c>&gt;&gt;TURN</c> directive events of the LAST parsed source (ISO §7.3.25), anchored to
    /// 1-based lines of the final preprocessed text (so token <c>Start.Line</c> is directly comparable — the
    /// compile-time TurnState's basis, deep-dive D10). Empty when the source has no TURN directives.</summary>
    public IReadOnlyList<TurnEvent> TurnEvents { get; private set; } = [];

    /// <summary>
    /// Preprocess and parse a COBOL source file. Returns the parse tree, or <see langword="null"/> if a fatal
    /// syntax error was reported (collected into <paramref name="diagnostics"/>).
    /// </summary>
    public CobolParserCore.CompilationUnitContext? Parse(string sourcePath, DiagnosticBag diagnostics)
    {
        string processed = Preprocess(sourcePath, diagnostics);
        return LexAndParse(processed, sourcePath, diagnostics);
    }

    /// <summary>
    /// Phase 0 — source preprocessing: NIST archive-marker stripping → free-form normalization → conditional
    /// compilation (<c>&gt;&gt;DEFINE/IF/…</c>) → COPY expansion → NIST placeholder substitution. Each stage is a
    /// no-op on source that does not use it, so an ordinary program passes through essentially unchanged.
    /// </summary>
    private string Preprocess(string sourcePath, DiagnosticBag diagnostics)
    {
        string raw = File.ReadAllText(sourcePath);
        string sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? ".";

        raw = ReferenceFormatProcessor.StripNistArchiveMarkers(raw);
        // The edition-aware overload carries the fixed-form continuation gates (VCR rows 2/94, W3): only the
        // column-aware pass can see the col-7 indicator, so the per-edition obligations emit HERE.
        string text = ReferenceFormatProcessor.NormalizeToFreeForm(raw, DialectLevel, Permissive, diagnostics, sourcePath);

        // Conditional compilation runs on free-form text BEFORE COPY so an >>IF may include/omit COPY statements.
        // leaveTurnDirectives: an emitting-branch >>TURN survives for the TurnDirectiveProcessor stage below
        // (the COBOL.NET EC model, ISO §7.3.25) — the legacy pipeline still consumes TURN here.
        text = ConditionalCompilationProcessor.Process(text, leaveTurnDirectives: true);

        // COPY expansion runs BEFORE NIST substitution so placeholders inside copied library text are substituted.
        var copy = new CopyProcessor(_copySearchPaths, diagnostics, sourcePath, strict: false,
            dialectLevel: DialectLevel, permissive: Permissive);
        text = copy.Process(text, sourceDir);

        if (NistTestName is { } nist)
            text = NistPreprocessor.Process(text, nist);

        // >>TURN directive collection runs LAST — after COPY (so copybook TURNs are seen) and after the
        // line-count-neutral NIST substitution — on the FINAL text, so each TurnEvent.Line is directly
        // comparable to the parser tokens' Start.Line (the TurnState anchor, deep-dive D10 / hazard H3).
        int linesBefore = CountLines(text);
        (text, TurnEvents) = TurnDirectiveProcessor.Process(text, DialectLevel, diagnostics, sourcePath);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "TurnDirectiveProcessor changed the line count — TURN scoping would silently misanchor (hazard H3)");

        return text;
    }

    private static int CountLines(string s) => s.Count(c => c == '\n');

    /// <summary>
    /// Phase 1 — lex + parse. Uses the proven two-stage strategy: fast SLL prediction first (with a
    /// <see cref="BailErrorStrategy"/> so an ambiguity throws rather than hangs), falling back to full LL on
    /// failure. The <c>ZERO</c>→<c>ZERO_ARITH</c> token rewrite runs between lexing and parsing to avoid an
    /// exponential-prediction ambiguity. Returns <see langword="null"/> if any syntax error was reported.
    /// </summary>
    private CobolParserCore.CompilationUnitContext? LexAndParse(string text, string sourcePath, DiagnosticBag diagnostics)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        var tokens = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(tokens);

        var parser = new CobolParserCore(tokens) { DialectLevel = DialectLevel };
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new CobolErrorListener(diagnostics, sourcePath));

        CobolParserCore.CompilationUnitContext tree;
        try
        {
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            parser.ErrorHandler = new BailErrorStrategy();
            tree = parser.compilationUnit();
        }
        catch (Exception)
        {
            // SLL bailed — retry with full LL prediction and the diagnostic-collecting error strategy.
            tokens.Seek(0);
            parser.Reset();
            parser.Interpreter.PredictionMode = PredictionMode.LL;
            parser.ErrorHandler = new CobolErrorStrategy();
            tree = parser.compilationUnit();
        }

        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }
}
