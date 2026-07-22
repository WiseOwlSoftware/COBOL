// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;   // ParseCanceledException (thrown by BailErrorStrategy on the SLL pass)
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Frontend;

/// <summary>
/// The COBOL.NET front-end: source text → preprocessed free-form text → ANTLR parse tree.
/// </summary>
/// <remarks>
/// This is the COBOL.NET front-end (assembly <c>Cobol.Net.Frontend</c>): the source preprocessor
/// (reference-format normalization, conditional compilation, COPY expansion, NIST placeholder substitution) and
/// the ANTLR lexer/parser. The parse tree it returns (<see cref="CobolParserCore.CompilationUnitContext"/>) is a
/// pure syntactic artifact — no semantic analysis, storage layout, or emission is involved. It is shared,
/// unchanged, by both the greenfield COBOL.NET pipeline and (until the G8 cut-over) the legacy differential
/// oracle, which references this same assembly.
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

    /// <summary>The frontend's <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive events (ISO §7.3.23) — they build the
    /// group's compile-time <see cref="Binding.RefModZeroLengthState"/> (the per-line zero-length allowance fold).</summary>
    public IReadOnlyList<RefModZeroLengthEvent> RefModZeroLengthEvents { get; private set; } = [];

    /// <summary>The frontend's <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> directive events (ISO §7.3.14 /
    /// §7.3.15) — they build the group's compile-time <see cref="Binding.FlagState"/> (the per-line per-option
    /// migration-flag fold that <c>FlagConformancePass</c> queries). Empty when the source has no FLAG directives.</summary>
    public IReadOnlyList<FlagEvent> FlagEvents { get; private set; } = [];

    /// <summary>The frontend's <c>&gt;&gt;COBOL-WORDS</c> override layer (ISO §7.3.10) — the per-group
    /// reserved/context-sensitive/intrinsic word-table modification the post-lex <c>CobolWordsRewriter</c>
    /// applies to the token stream and the compiler's composed <c>ReservedWordSet</c> / intrinsic resolution
    /// consult. <see cref="CobolWordsMap.Empty"/> when the source has no COBOL-WORDS directive.</summary>
    public CobolWordsMap CobolWordsMap { get; private set; } = CobolWordsMap.Empty;

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

        // The MERGED text-manipulation driver (ISO §7.2.1) — conditional compilation INTERLEAVED with COPY, so a
        // >>DEFINE/>>IF/>>EVALUATE INSIDE a copybook is processed (the CC-before-COPY split could not see them), while
        // a main-source >>IF still gates a COPY (omitted-branch COPY is never expanded) and REPLACE (Step 3) runs over
        // the expanded group. leave* keep the post-85 directive families flowing to their dedicated stages below.
        // COPY runs BEFORE NIST substitution so placeholders inside copied library text are substituted.
        var copy = new CopyProcessor(_copySearchPaths, diagnostics, sourcePath, strict: false,
            dialectLevel: DialectLevel, permissive: Permissive);
        text = ConditionalCompilationProcessor.ProcessWithCopy(text, sourceDir, copy,
            leaveTurnDirectives: true, leavePropagateDirectives: true, leaveRefModZeroLengthDirectives: true,
            leaveFlagDirectives: true, leaveCobolWordsDirectives: true, diagnostics: diagnostics, sourcePath: sourcePath);

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

        // >>PROPAGATE (ISO §7.3.21): recognize + edition-gate (introduction gate; runtime semantics are PHASE-13).
        // Line-count preserving like the >>TURN stage.
        text = PropagateDirectiveProcessor.Process(text, DialectLevel, diagnostics, sourcePath);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "PropagateDirectiveProcessor changed the line count (hazard H3)");

        // >>REF-MOD-ZERO-LENGTH (ISO §7.3.23): recognize + edition-gate + collect the per-line zero-length toggle
        // events on the FINAL text (each event line is directly comparable to a ref-mod token's Start.Line — the
        // >>TURN anchoring discipline). Line-count preserving like the two stages above.
        (text, RefModZeroLengthEvents) =
            RefModZeroLengthDirectiveProcessor.Process(text, DialectLevel, Permissive, diagnostics, sourcePath);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "RefModZeroLengthDirectiveProcessor changed the line count (hazard H3)");

        // >>FLAG-02 / >>FLAG-14 (ISO §7.3.14 / §7.3.15): collect the per-option ON/OFF toggle events on the FINAL
        // text (each event line is directly comparable to a flagged construct's token Start.Line — the >>TURN
        // anchoring discipline). Line-count preserving like the stages above.
        (text, FlagEvents) = FlagDirectiveProcessor.Process(text, DialectLevel, Permissive, diagnostics, sourcePath);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "FlagDirectiveProcessor changed the line count (hazard H3)");

        // >>COBOL-WORDS (ISO §7.3.10): parse the per-group reserved/context/intrinsic word-table modification into
        // the CobolWordsMap (the post-lex rewriter + composed ReservedWordSet consume it), edition-gate the
        // directive word, and enforce SR1/SR2/SR5. Line-count preserving like the stages above.
        (text, CobolWordsMap) = CobolWordsDirectiveProcessor.Process(text, DialectLevel, Permissive, diagnostics, sourcePath);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "CobolWordsDirectiveProcessor changed the line count (hazard H3)");

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
        // >>COBOL-WORDS (ISO §7.3.10.4 GR3/GR4): a de-reserved word (UNDEFINE/SUBSTITUTE) may be used as a
        // SUBSCRIPTED data name; the lexer must open SUBSCRIPT mode at its following '(' even though the word is
        // still lexed as its keyword token (the retype below runs post-lex, after the '(' decision is frozen).
        // Set BEFORE any tokenization (ZeroTokenRewriter.Fill). A no-op when no de-reserved word is a keyword token.
        if (!CobolWordsMap.IsEmpty)
            lexer.SetCobolWordsDataNames(CobolWordsRewriter.DeReservedTokenTypes(CobolWordsMap));
        var tokens = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(tokens);
        // >>COBOL-WORDS (ISO §7.3.10.4) — retype tokens per the per-group override: synonyms (EQUATE/SUBSTITUTE)
        // become their canonical keyword, de-reserved words (UNDEFINE/SUBSTITUTE) become IDENTIFIERs. A no-op when
        // the source has no directive (byte-identical).
        CobolWordsRewriter.Rewrite(tokens, CobolWordsMap);

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
        catch (Exception e) when (e is ParseCanceledException or RecognitionException)
        {
            // SLL bailed on a genuine parse ambiguity/mismatch (BailErrorStrategy throws ParseCanceledException,
            // wrapping a RecognitionException) — retry with full LL prediction and the diagnostic-collecting error
            // strategy. A NON-parse exception (a predicate/lexer-action bug) now propagates instead of being
            // silently retried under LL, where it would surface as a misleading generic syntax error. (Rearch P1.)
            tokens.Seek(0);
            parser.Reset();
            parser.Interpreter.PredictionMode = PredictionMode.LL;
            parser.ErrorHandler = new CobolErrorStrategy();
            tree = parser.compilationUnit();
        }

        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }
}
