// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>Which migration-flagging directive an option belongs to — <c>&gt;&gt;FLAG-02</c> (§2023:7.3.14, the
/// 2002↔2014 incompatibilities), <c>&gt;&gt;FLAG-14</c> (§2023:7.3.15, the 2014↔2023 incompatibilities),
/// <c>&gt;&gt;FLAG-85</c> (§2002:7.2.11, the 1985↔2002 incompatibilities) or <c>&gt;&gt;FLAG-NATIVE-ARITHMETIC</c>
/// (§2002:7.2.12, native arithmetic under standard arithmetic). The last two exist in the COBOL-2002 and COBOL-2014
/// directive sets only (Annex E.2 item 21 removed both at 2023).</summary>
public enum FlagDirective { Flag02, Flag14, Flag85, FlagNativeArithmetic }

/// <summary>
/// Every flaggable option of <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> (ISO §2023:7.3.14.4 / §2023:7.3.15.4 GR4). The
/// same option WORD may appear in both directives with a DIFFERENT meaning (<c>I-O-STATUS-07</c> is FLAG-02 c =
/// CLOSE NO REWIND/UNIT, but FLAG-14 f = a condition testing FILE STATUS for '07'), so every member is
/// directive-qualified. <c>ALL</c> is not a member: it is a parse-time fan-out to every option of its directive.
/// The canonical spelling is the GENERAL-FORMAT word (not the two GR4 typos — g's <c>NUM-ED-ZERO-FIG-CONSTANT</c>
/// and k's <c>VALUE-FIG-CON-NO-LENTH</c>). Design SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
public enum FlagOption
{
    // ── FLAG-02 (§2023:7.3.14.4 GR4 b–f) ──
    Flag02EcProgramExceptions,
    Flag02IoStatus07,
    Flag02MoveToSameName,
    Flag02RangeExceptionForIndex,
    Flag02TerminateWithVarying,

    // ── FLAG-14 (§2023:7.3.15.4 GR4 b–m) ──
    Flag14CompileTimeArithmeticExpressions,
    Flag14Evaluate,
    Flag14IoDeclarative,
    Flag14IoStatus04,
    Flag14IoStatus07,
    Flag14NumEdZeroFigconst,
    Flag14ReadPrevious,
    Flag14RefModZeroLength,
    Flag14ValueEditing,
    Flag14ValueFigConLength,
    Flag14ValueZero,
    Flag14WriteEndOfPage,

    // ── FLAG-85 (§2002:7.2.11.3 GR4 b–k) ──
    Flag85Corresponding,
    Flag85DeEditing,
    Flag85Divide,
    Flag85FunctionArgument,
    Flag85Move,
    Flag85Numval,
    Flag85Set,
    Flag85Standard1,
    Flag85Standard2,
    Flag85ZeroLength,

    // ── FLAG-NATIVE-ARITHMETIC (§2002:7.2.12.3 GR1–GR4): the directive selects no option, so it has the one
    //    option its ON/OFF toggles ──
    FlagNativeArithmeticOperation,
}

/// <summary>One flaggable option's static identity: which directive owns it, the general-format WORD that selects
/// it, the exact GR4 sub-rule + Annex-E citation, and the one-line behaviour-change summary the warning message
/// carries. The ONE catalog (§2023:7.3.14.4 / §2023:7.3.15.4) — the emitter reads <see cref="Citation"/> / <see
/// cref="Change"/> for a per-option, spec-faithful warning.</summary>
public sealed record FlagOptionInfo(FlagOption Option, FlagDirective Directive, string Word, string Citation, string Change);

/// <summary>The single source of truth for the option catalog + the word→option parse: enumerates every option of
/// both directives once. Reused by BOTH collection sites (the post-COPY <see cref="FlagDirectiveProcessor"/> and
/// the in-scan tracker in <see cref="ConditionalCompilationProcessor"/>) so directive-line parsing is
/// single-sourced.</summary>
public static class FlagOptions
{
    /// <summary>The complete option catalog (FLAG-02 then FLAG-14, in general-format order).</summary>
    public static readonly IReadOnlyList<FlagOptionInfo> All =
    [
        new(FlagOption.Flag02EcProgramExceptions, FlagDirective.Flag02, "EC-PROGRAM-EXCEPTIONS",
            "ISO §2023:7.3.14.4 GR4 b",
            "a >>TURN for an EC-PROGRAM-family exception in a source element that calls a function or invokes a method (2002↔2014)"),
        new(FlagOption.Flag02IoStatus07, FlagDirective.Flag02, "I-O-STATUS-07",
            "ISO §2023:7.3.14.4 GR4 c; Annex E.2 item 16",
            "a CLOSE with the WITH NO REWIND or UNIT phrase (I-O status '07' behaviour, 2002↔2014)"),
        new(FlagOption.Flag02MoveToSameName, FlagDirective.Flag02, "MOVE-TO-SAME-NAME",
            "ISO §2023:7.3.14.4 GR4 d",
            "a MOVE whose sending and receiving operands are the same alphanumeric-edited (or subordinate-ODO) data description entry (2002↔2014)"),
        new(FlagOption.Flag02RangeExceptionForIndex, FlagDirective.Flag02, "RANGE-EXCEPTION-FOR-INDEX",
            "ISO §2023:7.3.14.4 GR4 e",
            "an index-assignment or index-arithmetic SET into an index while EC-RANGE-INDEX checking is enabled (2002↔2014)"),
        new(FlagOption.Flag02TerminateWithVarying, FlagDirective.Flag02, "TERMINATE-WITH-VARYING",
            "ISO §2023:7.3.14.4 GR4 f",
            "a TERMINATE of a report whose description contains a VARYING clause (2002↔2014)"),

        new(FlagOption.Flag14CompileTimeArithmeticExpressions, FlagDirective.Flag14, "COMPILE-TIME-ARITHMETIC-EXPRESSIONS",
            "ISO §2023:7.3.15.4 GR4 b; Annex E.2 item 6",
            "a compile-time arithmetic expression whose result could differ now that the arithmetic mode is implementor-defined (2014↔2023)"),
        new(FlagOption.Flag14Evaluate, FlagDirective.Flag14, "EVALUATE",
            "ISO §2023:7.3.15.4 GR4 c; Annex E.2 item 8",
            "a >>EVALUATE directive containing both a WHEN and a WHEN OTHER phrase (end-of-EVALUATE omission rules changed 2014↔2023)"),
        new(FlagOption.Flag14IoDeclarative, FlagDirective.Flag14, "I-O-DECLARATIVE",
            "ISO §2023:7.3.15.4 GR4 d; Annex E.2 item 19",
            "an I-O statement without its INVALID KEY / AT END phrase while an INPUT/OUTPUT/I-O/EXTEND declarative is in effect (now executed on the exception, 2014↔2023)"),
        new(FlagOption.Flag14IoStatus04, FlagDirective.Flag14, "I-O-STATUS-04",
            "ISO §2023:7.3.15.4 GR4 e; Annex E.2 item 15",
            "a reference to a FILE STATUS item that tests for '04' (its setting is now clarified, 2014↔2023)"),
        new(FlagOption.Flag14IoStatus07, FlagDirective.Flag14, "I-O-STATUS-07",
            "ISO §2023:7.3.15.4 GR4 f; Annex E.2 item 16",
            "a reference to a FILE STATUS item that tests for '07' (its setting is now restricted to OPEN/CLOSE, 2014↔2023)"),
        new(FlagOption.Flag14NumEdZeroFigconst, FlagDirective.Flag14, "NUM-ED-ZERO-FIGCONST",
            "ISO §2023:7.3.15.4 GR4 g; Annex E.2 item 28",
            "the figurative constant ZERO in the VALUE clause of a numeric-edited item (now the numeric literal zero, 2014↔2023)"),
        new(FlagOption.Flag14ReadPrevious, FlagDirective.Flag14, "READ-PREVIOUS",
            "ISO §2023:7.3.15.4 GR4 h; Annex E.2 item 22",
            "a READ PREVIOUS statement (an at-end condition now occurs on a READ PREVIOUS following OPEN, 2014↔2023)"),
        new(FlagOption.Flag14RefModZeroLength, FlagDirective.Flag14, "REF-MOD-ZERO-LENGTH",
            "ISO §2023:7.3.15.4 GR4 i; Annex E.2 item 23",
            "a reference modification where the REF-MOD-ZERO-LENGTH directive is unset and EC-BOUND-REF-MOD is on (a zero-length result now raises the exception, 2014↔2023)"),
        new(FlagOption.Flag14ValueEditing, FlagDirective.Flag14, "VALUE-EDITING",
            "ISO §2023:7.3.15.4 GR4 j; Annex E.2 item 29",
            "a numeric-edited item whose VALUE is a literal without editing symbols (editing symbols are now compulsory, 2014↔2023)"),
        new(FlagOption.Flag14ValueFigConLength, FlagDirective.Flag14, "VALUE-FIG-CON-LENGTH",
            "ISO §2023:7.3.15.4 GR4 k; Annex E.2 item 11",
            "a figurative constant in the VALUE clause of an item with no specified length (the length is now defined, 2014↔2023)"),
        new(FlagOption.Flag14ValueZero, FlagDirective.Flag14, "VALUE-ZERO",
            "ISO §2023:7.3.15.4 GR4 l; Annex E.2 item 28",
            "a numeric-edited item whose VALUE specifies the figurative constant ZERO (now the numeric literal zero, 2014↔2023)"),
        new(FlagOption.Flag14WriteEndOfPage, FlagDirective.Flag14, "WRITE-END-OF-PAGE",
            "ISO §2023:7.3.15.4 GR4 m",
            "a WRITE that allows an END-OF-PAGE phrase (the file has a LINAGE clause) but omits it (2014↔2023)"),

        new(FlagOption.Flag85Corresponding, FlagDirective.Flag85, "CORRESPONDING",
            "ISO §2002:7.2.11.3 GR4 b",
            "an ADD, MOVE or SUBTRACT CORRESPONDING statement with a subscript other than a constant on an operand (1985↔2002)"),
        new(FlagOption.Flag85DeEditing, FlagDirective.Flag85, "DE-EDITING",
            "ISO §2002:7.2.11.3 GR4 c; §2002:14.8.24.3 GR3",
            "a de-editing MOVE: a numeric-edited sending operand and a numeric or numeric-edited receiving item (1985↔2002)"),
        new(FlagOption.Flag85Divide, FlagDirective.Flag85, "DIVIDE",
            "ISO §2002:7.2.11.3 GR4 d",
            "a DIVIDE … REMAINDER whose quotient has no sign while the divisor or the dividend has one (1985↔2002)"),
        new(FlagOption.Flag85FunctionArgument, FlagDirective.Flag85, "FUNCTION-ARGUMENT",
            "ISO §2002:7.2.11.3 GR4 e",
            "FUNCTION RANDOM followed immediately by a parenthesized arithmetic expression, as an argument of a function that allows several arithmetic expressions (1985↔2002)"),
        new(FlagOption.Flag85Move, FlagDirective.Flag85, "MOVE",
            "ISO §2002:7.2.11.3 GR4 f",
            "a MOVE of an alphanumeric literal or data item of more than 31 characters to a numeric data item (1985↔2002)"),
        new(FlagOption.Flag85Numval, FlagDirective.Flag85, "NUMVAL",
            "ISO §2002:7.2.11.3 GR4 g",
            "a reference to the NUMVAL or NUMVAL-C intrinsic function (1985↔2002)"),
        new(FlagOption.Flag85Set, FlagDirective.Flag85, "SET",
            "ISO §2002:7.2.11.3 GR4 h",
            "a condition-setting SET statement that references a variable-length group item (1985↔2002)"),
        new(FlagOption.Flag85Standard1, FlagDirective.Flag85, "STANDARD-1",
            "ISO §2002:7.2.11.3 GR4 i",
            "an ALPHABET clause that specifies STANDARD-1 (the character set is the current issue of the standard, 1985↔2002)"),
        new(FlagOption.Flag85Standard2, FlagDirective.Flag85, "STANDARD-2",
            "ISO §2002:7.2.11.3 GR4 j",
            "an ALPHABET clause that specifies STANDARD-2 (the character set is the current issue of the standard, 1985↔2002)"),
        new(FlagOption.Flag85ZeroLength, FlagDirective.Flag85, "ZERO-LENGTH",
            "ISO §2002:7.2.11.3 GR4 k",
            "a READ that could return a zero-length item, or a statement that references a data item that could be a zero-length item (1985↔2002)"),

        new(FlagOption.FlagNativeArithmeticOperation, FlagDirective.FlagNativeArithmetic, "FLAG-NATIVE-ARITHMETIC",
            "ISO §2002:7.2.12.3 GR2; §2002:8.8.1.3",
            "an arithmetic operation that uses native arithmetic although standard arithmetic is in effect for the source unit (an operand of usage binary-char, binary-short, binary-long, binary-double, float-short, float-long or float-extended)"),
    ];

    private static readonly Dictionary<(FlagDirective, string), FlagOptionInfo> ByWord =
        All.ToDictionary(o => (o.Directive, o.Word), new DirWordComparer());

    private static readonly Dictionary<FlagOption, FlagOptionInfo> ByOption =
        All.ToDictionary(o => o.Option);

    /// <summary>The metadata for one option (citation, change summary, owning directive).</summary>
    public static FlagOptionInfo Info(FlagOption option) => ByOption[option];

    /// <summary>Every option of <paramref name="directive"/> — the <c>ALL</c> fan-out set.</summary>
    public static IReadOnlyList<FlagOption> OptionsOf(FlagDirective directive) =>
        [.. All.Where(o => o.Directive == directive).Select(o => o.Option)];

    /// <summary>Resolve a general-format option WORD to its option within <paramref name="directive"/>; null when the
    /// word is not a valid option of that directive. The word is a COBOL word, so it is keyed by the Annex C fold
    /// (<see cref="DirWordComparer"/>, ISO §2023:8.1.3.2 GR3 b) — never by the host's case mapping, which would find an
    /// option under a spelling Annex C does not give it (kb/Work PB2845).</summary>
    public static FlagOption? TryOption(FlagDirective directive, string word) =>
        ByWord.TryGetValue((directive, word), out var info) ? info.Option : null;

    /// <summary>FLAG-14 b's construct test (ISO §2023:7.3.15.4 GR4 b; Annex E.2 item 6): "a compile-time arithmetic
    /// expression that could give a different result" — an expression that EVALUATES something, i.e. carries a binary
    /// <c>addOp</c> or <c>mulOp</c> (a bare literal or a lone unary sign has one value in every arithmetic mode, so
    /// nothing could differ). The ONE predicate every site of a compile-time arithmetic expression asks (§2023:7.3.6.1: the
    /// DEFINE, EVALUATE and DISPLAY directive operands and the constant conditional expression, read by the
    /// conditional-compilation stage, and the CONSTANT entry's arithmetic-expression-1, read by
    /// <c>FlagConformancePass</c>), so the sites cannot disagree about what a flaggable expression is.</summary>
    public static bool IsFlaggableCompileTimeArithmetic(Antlr4.Runtime.Tree.IParseTree expression)
    {
        for (int k = 0; k < expression.ChildCount; k++)
        {
            var child = expression.GetChild(k);
            if (child is Generated.CobolParserCore.AddOpContext or Generated.CobolParserCore.MulOpContext
                || IsFlaggableCompileTimeArithmetic(child)) return true;
        }
        return false;
    }

    private sealed class DirWordComparer : IEqualityComparer<(FlagDirective, string)>
    {
        public bool Equals((FlagDirective, string) x, (FlagDirective, string) y) =>
            x.Item1 == y.Item1 && CobolKeywords.Same(x.Item2, y.Item2);
        public int GetHashCode((FlagDirective, string) o) =>
            HashCode.Combine(o.Item1, CobolKeywords.Comparer.GetHashCode(o.Item2));
    }
}

/// <summary>One <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> toggle, anchored to its 1-based line in the FINAL
/// preprocessed text (directly comparable to an ANTLR token's <c>Start.Line</c>). A directive applies to the text
/// that FOLLOWS it (the <c>&gt;&gt;TURN</c> GR2 discipline). An empty <see cref="Options"/> list means the
/// <c>ALL</c> selection — every option of <see cref="Which"/>. The compile-time <see cref="Binding.FlagState"/>
/// folds these per option per source line.</summary>
/// <param name="Line">1-based line of the directive in the final preprocessed text.</param>
/// <param name="Which">Which directive (FLAG-02 / FLAG-14).</param>
/// <param name="On">ON (enable flagging) vs OFF (disable) for the selected option(s).</param>
/// <param name="Options">The selected options, or empty ⇒ ALL of <see cref="Which"/>.</param>
public sealed record FlagEvent(int Line, FlagDirective Which, bool On, IReadOnlyList<FlagOption> Options);

/// <summary>Parses the operand of a <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> directive line —
/// <c>{ ALL | |option…| } { ON | OFF }</c> (ISO §2023:7.3.14.2 / §2023:7.3.15.2). Both directives print the SAME shape, and in
/// both <c>ALL</c> and <c>ON</c> are NOT underlined — optional words (§2023:5.2.3) — so by §2023:5.2.6.3 each is the DEFAULT
/// of its braces and is selected when no other alternative is written: a bare directive is <c>ALL ON</c>, a lone
/// <c>OFF</c> is <c>ALL OFF</c>, and a lone option list is <c>ON</c>. The option list is one or more words in any
/// order, each at most once (§2023:5.2.6.4 choice indicators within braces). The single source of directive-line
/// syntax, reused by both collection sites.</summary>
public static class FlagDirectiveLine
{
    /// <summary>Parse the operand text that FOLLOWS the <c>FLAG-02</c>/<c>FLAG-14</c> keyword. On success yields the
    /// selected options (empty ⇒ ALL) and the ON/OFF flag; on a malformed operand yields <paramref name="error"/>
    /// (an unknown option word, a word written twice, <c>ALL</c> combined with options, or an ON/OFF that is not the
    /// last word).</summary>
    public static bool TryParse(FlagDirective directive, string operand,
        out IReadOnlyList<FlagOption> options, out bool on, out string? error)
    {
        options = [];
        on = true;   // §2023:5.2.6.3: ON is the un-underlined alternative of its braces, hence the default; §2023:7.3.15.4 2): "explicitly or implicitly specified"
        error = null;

        var tokens = operand.SplitSpaces();

        // The second pair of braces: a trailing ON/OFF, written or not, identically for every directive.
        int end = tokens.Length;
        if (end > 0 && CobolKeywords.Spelling(tokens[^1]) is var last && last is "ON" or "OFF")
        {
            on = last == "ON";
            end--;
        }

        // FLAG-NATIVE-ARITHMETIC prints no first pair of braces (§2002:7.2.12.1): ON or OFF and nothing else.
        if (directive == FlagDirective.FlagNativeArithmetic)
        {
            if (end == 0) return true;
            error = $"'{tokens[0]}' is not admissible: >>{DirectiveWord(directive)} takes only ON or OFF";
            return false;
        }

        // The first pair of braces: ALL (un-underlined, so the default when no option is written) or the option list.
        bool allWritten = false;
        var picked = new List<FlagOption>();
        for (int i = 0; i < end; i++)
        {
            string w = CobolKeywords.Spelling(tokens[i]);   // the directive's words are keywords: the basic letters alone, never the host's case mapping (kb/Work PB2845, PB4240)
            if (w == "ALL")
            {
                if (allWritten) { error = Repeated(tokens[i]); return false; }
                allWritten = true;
            }
            else if (w is "ON" or "OFF")
            {
                error = $"'{tokens[i]}' may be written only once, as the last word of the directive";
                return false;
            }
            else if (FlagOptions.TryOption(directive, w) is { } opt)
            {
                if (picked.Contains(opt)) { error = Repeated(tokens[i]); return false; }
                picked.Add(opt);
            }
            else
            {
                error = $"'{tokens[i]}' is not a valid option of >>{DirectiveWord(directive)}";
                return false;
            }
        }

        if (allWritten && picked.Count > 0) { error = "ALL may not be combined with individual options"; return false; }
        options = allWritten || picked.Count == 0 ? [] : picked;   // empty ⇒ ALL fan-out at fold time
        return true;
    }

    /// <summary>§2023:5.2.6.4: within choice indicators enclosed by braces "any single alternative shall be specified only
    /// once" — a repeated option word (or a repeated ALL) is a syntax error, never a silent merge.</summary>
    private static string Repeated(string word) =>
        $"'{word}' is specified more than once (ISO §2023:5.2.6.4: any single alternative shall be specified only once)";

    /// <summary>The directive's general-format word — the ONE spelling per <see cref="FlagDirective"/>.</summary>
    public static string DirectiveWord(FlagDirective directive) => directive switch
    {
        FlagDirective.Flag02 => "FLAG-02",
        FlagDirective.Flag14 => "FLAG-14",
        FlagDirective.Flag85 => "FLAG-85",
        FlagDirective.FlagNativeArithmetic => "FLAG-NATIVE-ARITHMETIC",
        _ => throw new ArgumentOutOfRangeException(nameof(directive), directive, null),
    };

    /// <summary>The directive a compiler-directive word names, or null for a word that is none of the four. Keyed by
    /// the Annex C fold (<see cref="CobolKeywords.Same"/>) over <see cref="DirectiveWord"/>, so the spelling lives once.</summary>
    public static FlagDirective? OfWord(string word)
    {
        foreach (var d in Enum.GetValues<FlagDirective>())
            if (CobolKeywords.Same(word, DirectiveWord(d))) return d;
        return null;
    }

    /// <summary>The <c>constructs/</c> row id of the directive — the row whose edition window gates its word and whose
    /// state <see cref="DirectiveStateRegistry"/> accounts for.</summary>
    public static string Row(FlagDirective directive) => directive switch
    {
        FlagDirective.Flag02 => Constructs.Flag02Directive2014,
        FlagDirective.Flag14 => Constructs.Flag14Directive2023,
        FlagDirective.Flag85 => Constructs.Flag85DirectiveWindow,
        FlagDirective.FlagNativeArithmetic => Constructs.FlagNativeArithmeticDirectiveWindow,
        _ => throw new ArgumentOutOfRangeException(nameof(directive), directive, null),
    };

    /// <summary>The Warning code a construct flagged by an option of <paramref name="directive"/> carries — one code per
    /// directive, the option's identity riding in the message, citation and <see cref="ConstructId"/>.</summary>
    public static string WarningCode(FlagDirective directive) => directive switch
    {
        FlagDirective.Flag02 => Editions.Diagnostics.DiagnosticCatalog.Flag02Warning.Code,
        FlagDirective.Flag14 => Editions.Diagnostics.DiagnosticCatalog.Flag14Warning.Code,
        FlagDirective.Flag85 => Editions.Diagnostics.DiagnosticCatalog.Flag85Warning.Code,
        FlagDirective.FlagNativeArithmetic => Editions.Diagnostics.DiagnosticCatalog.FlagNativeArithmeticWarning.Code,
        _ => throw new ArgumentOutOfRangeException(nameof(directive), directive, null),
    };

    /// <summary>The suppress-key of an option's warning: <c>flag-02-move-to-same-name</c>, <c>flag-85-numval</c>, …
    /// (the directive's word, lower-cased, then the option's word; FLAG-NATIVE-ARITHMETIC has the one key of its word).</summary>
    public static string ConstructId(FlagOptionInfo info) =>
        info.Directive == FlagDirective.FlagNativeArithmetic
            ? DirectiveWord(info.Directive).ToLowerInvariant()
            : $"{DirectiveWord(info.Directive).ToLowerInvariant()}-{info.Word.ToLowerInvariant()}";

    /// <summary>The citation of the directive's general format — the clause a malformed operand violates, in the
    /// edition that prints it (FLAG-85 and FLAG-NATIVE-ARITHMETIC are printed by the 2002 and 2014 texts only).</summary>
    public static string FormatCitation(FlagDirective directive) => directive switch
    {
        FlagDirective.Flag02 => "ISO §2023:7.3.14.2",
        FlagDirective.Flag14 => "ISO §2023:7.3.15.2",
        FlagDirective.Flag85 => "ISO §2002:7.2.11.1",
        FlagDirective.FlagNativeArithmetic => "ISO §2002:7.2.12.1",
        _ => throw new ArgumentOutOfRangeException(nameof(directive), directive, null),
    };
}
