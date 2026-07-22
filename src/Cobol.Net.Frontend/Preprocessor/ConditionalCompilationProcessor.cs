// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// COBOL conditional compilation (ISO/IEC 1989:2023 §7.3.11 DEFINE, §7.3.16 IF, §7.3.13 EVALUATE) — a
/// text-manipulation stage (§7.2) that selectively includes source lines based on compilation variables. It runs on
/// free-form normalized text, before COPY expansion, so an <c>&gt;&gt;IF</c> may gate COPY statements in its
/// branches.
///
/// Two cleanly separated jobs (DESIGN-compile-time-expressions.md §2): (1) LINE SELECTION — walking the
/// <c>&gt;&gt;IF/&gt;&gt;ELSE/&gt;&gt;END-IF/&gt;&gt;EVALUATE/&gt;&gt;WHEN</c> nesting to decide which physical lines
/// survive; this stays a small line-inclusion state machine here because "text-1/text-2" may be any source lines,
/// including un-expanded COPY and (in omitted branches) non-COBOL, so it MUST precede the main parse. (2) EXPRESSION /
/// CONDITION EVALUATION — every <c>&gt;&gt;DEFINE</c> operand, <c>&gt;&gt;IF</c> cce, and <c>&gt;&gt;EVALUATE</c>/
/// <c>&gt;&gt;WHEN</c> operand is fragment-parsed by ANTLR (<see cref="DirectiveExpressionFragment"/>) and evaluated
/// by the ONE shared <see cref="CompileTimeExpressionEvaluator"/> — there is no hand-rolled tokenizer or condition
/// parser; the ANTLR grammar is the single source of truth for directive-expression syntax (§7.3.6 arithmetic,
/// §7.3.7 boolean, §7.3.8 constant-conditional-expression). A formation violation is a loud <b>COBOLNET1619</b>,
/// never a silently mis-bound value.
///
/// Blast radius is essentially nil: a source with no <c>&gt;&gt;</c> lines is reproduced byte-for-byte.
/// </summary>
public static class ConditionalCompilationProcessor
{
    /// <summary>
    /// Standard ISO §7.3 compiler directives that CobolSharp recognizes but does not yet act on. They are
    /// consumed (the program compiles with default behavior) rather than reaching the lexer as stray tokens.
    /// Conditional-compilation directives (DEFINE/IF/ELSE/END-IF/EVALUATE/WHEN/END-EVALUATE) and SOURCE FORMAT
    /// (handled earlier by ReferenceFormatProcessor) are NOT in this set — they have real behavior.
    /// </summary>
    private static readonly HashSet<string> KnownIgnoredDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "CALL-CONVENTION", "LISTING", "PAGE", "LEAP-SECOND", "PROPAGATE",
        "FLAG-85", "FLAG-NATIVE-ARITHMETIC", "REF-MOD-ZERO-LENGTH", "TURN", "COBOL-WORDS",
        "PUSH", "POP",   // §7.3.20/§7.3.22 directive-state save/restore — recognized so they don't stray-token; no
                         // compiler-directive state we currently vary needs saving, so a no-op disposition is faithful.
        "DISPLAY",       // §7.3.12 — transfers text to the source listing / a compile-time device; no listing is
                         // produced (like LISTING/PAGE), so the directive is recognized and consumed.
        "FLAG-02", "FLAG-14",   // §7.3.14/§7.3.15 flagging directives — RECOGNIZED (a conforming compiler must not
                         // error on a standard directive); the migration/obsolescence diagnostics themselves are a
                         // remaining Wave-D item (the flags are not yet emitted).
    };

    /// <param name="text">The free-form-normalized source text.</param>
    /// <param name="leaveTurnDirectives">When true, an emitting-branch <c>&gt;&gt;TURN</c> line is LEFT IN the
    /// text for the downstream <see cref="TurnDirectiveProcessor"/> (the COBOL.NET EC model, ISO §7.3.25); an
    /// omitted-branch one still drops with its branch. The default (false) is the exact legacy behavior —
    /// TURN consumed here, the legacy caller untouched.</param>
    public static string Process(string text, bool leaveTurnDirectives = false, bool leavePropagateDirectives = false,
        bool leaveRefModZeroLengthDirectives = false, bool leaveFlagDirectives = false,
        bool leaveCobolWordsDirectives = false,
        DiagnosticBag? diagnostics = null, string? sourcePath = null)
    {
        var defines = new Dictionary<string, CtValue>(StringComparer.OrdinalIgnoreCase);
        // The running >>FLAG-14 state for the frontend-inline options b / c — mutated as the scan proceeds; `diag`
        // holds a reference to it so operand evaluation can flag a compile-time arithmetic expression (b) in place.
        var flagScan = new FlagScanState();
        var diag = new DirectiveDiag(diagnostics, sourcePath, flagScan);
        // The ONE shared compile-time expression evaluator (ledger C2). Its name resolution reads the CURRENT
        // `defines` (a directive may reference a variable an earlier directive set); the frontend routes every
        // formation diagnostic to COBOLNET1619; a directive operand is dot-decimal (§5.3 — processed before
        // SPECIAL-NAMES, so DECIMAL-POINT IS COMMA is not yet in effect).
        var evaluator = new CompileTimeExpressionEvaluator(
            resolveName: w => defines.TryGetValue(w, out var v) ? v : null,
            diag: diag,
            vocab: new CtOperandVocabulary("previously defined numeric compilation variables", "ISO §7.3.6.2 SR1b"),
            decimalPointIsComma: false);

        var stack = new Stack<Frame>();
        var lines = text.Split('\n');
        var output = new string[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimEnd('\r').TrimStart();
            bool emitting = stack.Count == 0 || stack.Peek().Emitting;
            diag.Line = i;

            if (!trimmed.StartsWith(">>", StringComparison.Ordinal))
            {
                output[i] = emitting ? line : "";   // ordinary line: include only when the active branch emits
                continue;
            }

            var (keyword, rest) = SplitDirective(trimmed);
            switch (keyword)
            {
                case "IF":
                {
                    bool parentActive = stack.Count == 0 || stack.Peek().Emitting;
                    bool cond = parentActive && EvaluateCceText(rest, evaluator, diag, ">>IF");
                    stack.Push(new Frame { Kind = FrameKind.If, ParentActive = parentActive, Emitting = cond, BranchTaken = cond });
                    output[i] = "";
                    break;
                }
                case "ELSE":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.If)
                    {
                        var f = stack.Peek();
                        f.Emitting = f.ParentActive && !f.BranchTaken;   // the ELSE body emits only if no prior branch did
                    }
                    output[i] = "";
                    break;
                case "END-IF":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.If) stack.Pop();
                    output[i] = "";
                    break;
                case "EVALUATE":
                {
                    // Format 1: >>EVALUATE selection-subject   Format 2: >>EVALUATE TRUE
                    bool parentActive = stack.Count == 0 || stack.Peek().Emitting;
                    var f = new Frame { Kind = FrameKind.Evaluate, ParentActive = parentActive, Emitting = false, BranchTaken = false,
                        StartLine = i, EvaluateFlagOn = flagScan.IsOn(FlagOption.Flag14Evaluate) };   // c anchor (§7.3.15.4 GR4 c)
                    string subj = rest.Trim();
                    if (subj.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) f.TruthForm = true;
                    else if (parentActive) f.Subject = EvaluateOperandText(subj, evaluator, diag, ">>EVALUATE");
                    stack.Push(f);
                    output[i] = "";
                    break;
                }
                case "WHEN":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.Evaluate)
                    {
                        var f = stack.Peek();
                        string obj = rest.Trim();
                        // c EVALUATE: record the syntactic presence of a >>WHEN / >>WHEN OTHER (independent of which
                        // branch emits) — GR4 c flags a directive containing BOTH.
                        if (obj.Equals("OTHER", StringComparison.OrdinalIgnoreCase)) f.SawWhenOther = true; else f.SawWhen = true;
                        if (obj.Equals("OTHER", StringComparison.OrdinalIgnoreCase))
                            f.Emitting = f.ParentActive && !f.BranchTaken;     // OTHER fires only if nothing matched
                        else if (!f.ParentActive || f.BranchTaken)
                            f.Emitting = false;                                // enclosing omitted, or a prior WHEN already matched
                        else
                        {
                            bool match = f.TruthForm
                                ? EvaluateCceText(obj, evaluator, diag, ">>WHEN")               // Format 2: constant-conditional-expression
                                : MatchWhen(f.Subject, obj, evaluator, diag);                   // Format 1: subject = object [THRU object3]
                            f.Emitting = match;
                            if (match) f.BranchTaken = true;
                        }
                    }
                    output[i] = "";
                    break;
                case "END-EVALUATE":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.Evaluate)
                    {
                        var f = stack.Peek();
                        // c EVALUATE (§7.3.15.4 GR4 c; E.2 item 8) — flag the directive when it carried both a >>WHEN
                        // and a >>WHEN OTHER and FLAG-14 EVALUATE was ON at the >>EVALUATE line.
                        if (f.SawWhen && f.SawWhenOther && f.EvaluateFlagOn)
                            diag.FlagWarn(FlagOption.Flag14Evaluate, f.StartLine);
                        stack.Pop();
                    }
                    output[i] = "";
                    break;
                case "DEFINE":
                    if (emitting) ApplyDefine(rest, defines, evaluator, diag);   // a DEFINE in an omitted branch has no effect
                    output[i] = "";
                    break;
                default:
                    // A >> directive other than the conditional-compilation set handled above. If it is a
                    // recognized ISO §7.3 directive that we do not yet act on (CALL-CONVENTION, LISTING, TURN, …),
                    // consume it so the program still compiles with default behavior. A directive in an omitted
                    // branch is dropped regardless. An UNRECOGNIZED >> word is left in place when emitting so it
                    // surfaces downstream (catching typos like >>IFF) rather than being silently swallowed.
                    // With leaveTurnDirectives (the COBOL.NET caller), an emitting-branch >>TURN survives for the
                    // TurnDirectiveProcessor stage (ISO §7.3.25) — legacy callers keep consuming it.
                    if (!emitting) output[i] = "";
                    else if (leaveTurnDirectives && keyword == "TURN") output[i] = line;
                    // With leavePropagateDirectives (the COBOL.NET caller), an emitting-branch >>PROPAGATE survives
                    // for the PropagateDirectiveProcessor stage (ISO §7.3.21 — the edition gate). Checked BEFORE the
                    // KnownIgnoredDirectives fallthrough, so PROPAGATE stays in that set and a legacy caller (no
                    // flag) keeps consuming it — the greenfield stage adds the edition gate, legacy behavior intact.
                    else if (leavePropagateDirectives && keyword == "PROPAGATE") output[i] = line;
                    // With leaveRefModZeroLengthDirectives (the COBOL.NET caller), an emitting-branch
                    // >>REF-MOD-ZERO-LENGTH survives for the RefModZeroLengthDirectiveProcessor stage (ISO §7.3.23 —
                    // the introduction gate + the per-line zero-length fold). It stays in KnownIgnoredDirectives so a
                    // legacy caller (no flag) keeps consuming it; the greenfield stage adds the real behavior.
                    else if (leaveRefModZeroLengthDirectives && keyword == "REF-MOD-ZERO-LENGTH") output[i] = line;
                    // With leaveFlagDirectives (the COBOL.NET caller), an emitting-branch >>FLAG-02 / >>FLAG-14
                    // survives for the FlagDirectiveProcessor stage (ISO §7.3.14 / §7.3.15 — the per-line per-option
                    // migration-flag fold). Both stay in KnownIgnoredDirectives so a legacy caller (no flag) keeps
                    // consuming them; the greenfield stage adds the real behavior.
                    else if (leaveFlagDirectives && (keyword == "FLAG-02" || keyword == "FLAG-14"))
                    {
                        // Track the running FLAG state for the frontend-inline options (this arm is reached only when
                        // emitting). The line still SURVIVES for the post-COPY FlagDirectiveProcessor (the bound-option
                        // FlagState). A malformed operand is reported there, not here — track only a well-formed toggle.
                        var which = keyword == "FLAG-02" ? FlagDirective.Flag02 : FlagDirective.Flag14;
                        if (FlagDirectiveLine.TryParse(which, rest, out var flagOpts, out bool flagOn, out _))
                            flagScan.Apply(which, flagOpts, flagOn);
                        output[i] = line;
                    }
                    // With leaveCobolWordsDirectives (the COBOL.NET caller), an emitting-branch >>COBOL-WORDS
                    // survives for the CobolWordsDirectiveProcessor stage (ISO §7.3.10 — the introduction gate + the
                    // per-group reserved/context/function word-table override). It stays in KnownIgnoredDirectives so
                    // a legacy caller (no flag) keeps consuming it; the greenfield stage adds the real behavior.
                    else if (leaveCobolWordsDirectives && keyword == "COBOL-WORDS") output[i] = line;
                    else output[i] = KnownIgnoredDirectives.Contains(keyword) ? "" : line;
                    break;
            }
        }

        return string.Join('\n', output);
    }

    private enum FrameKind { If, Evaluate }

    /// <summary>One <c>&gt;&gt;IF…&gt;&gt;END-IF</c> or <c>&gt;&gt;EVALUATE…&gt;&gt;END-EVALUATE</c> nesting level.</summary>
    private sealed class Frame
    {
        public FrameKind Kind;
        public bool ParentActive;   // is the enclosing context emitting? (a nested directive inside an omitted branch stays omitted)
        public bool Emitting;       // is THIS branch's text currently being included?
        public bool BranchTaken;    // IF: the IF arm was taken (drives ELSE); EVALUATE: some WHEN already matched (drives later WHEN/OTHER)
        public bool TruthForm;      // EVALUATE: true for Format 2 (>>EVALUATE TRUE), where each WHEN carries a constant-conditional-expression
        public CtValue? Subject;    // EVALUATE Format 1: the selection-subject value
        // FLAG-14 c EVALUATE (§7.3.15.4 GR4 c): flag a >>EVALUATE directive that carries BOTH a >>WHEN and a
        // >>WHEN OTHER — syntactic presence (independent of which branch emits). Captured at the >>EVALUATE line.
        public bool SawWhen;
        public bool SawWhenOther;
        public int StartLine;
        public bool EvaluateFlagOn;   // FLAG-14 EVALUATE was ON at the >>EVALUATE directive's line (the GR2 anchor)
    }

    /// <summary>The running per-option <c>&gt;&gt;FLAG-02</c>/<c>&gt;&gt;FLAG-14</c> ON/OFF state, updated as the CC
    /// stage scans directive lines in order — the frontend-inline options (b COMPILE-TIME-ARITHMETIC-EXPRESSIONS,
    /// c EVALUATE) query it at their construct's line. Mirrors the compile-time <see cref="Binding.FlagState"/> fold
    /// ("last toggle strictly before the site wins, default OFF") but built incrementally in source order, because
    /// these constructs are consumed at THIS stage and never reach the bound tree.</summary>
    private sealed class FlagScanState
    {
        private readonly Dictionary<FlagOption, bool> _on = [];
        public void Apply(FlagDirective which, IReadOnlyList<FlagOption> options, bool on)
        {
            foreach (var opt in options.Count == 0 ? FlagOptions.OptionsOf(which) : options) _on[opt] = on;
        }
        public bool IsOn(FlagOption opt) => _on.TryGetValue(opt, out bool v) && v;
    }

    // ── DEFINE (§7.3.11) ──────────────────────────────────────────────────────────────────────────────────────

    private enum DefineKind { Value, Off, Parameter }

    /// <summary>Split a <c>&gt;&gt;DEFINE compilation-variable-name [AS] { operand | PARAMETER | OFF } [OVERRIDE]</c>
    /// directive at the DIRECTIVE-SYNTAX level (name / AS / OFF / PARAMETER / OVERRIDE are directive keywords, not
    /// expression syntax); the OPERAND text is handed to the ANTLR fragment parse. §7.3.11.2 makes AS optional and
    /// OVERRIDE a trailing phrase; OFF and PARAMETER are the two operand-less alternatives.</summary>
    private static (string Name, DefineKind Kind, string Operand, bool Override) SplitDefine(string rest)
    {
        string s = rest.Trim();
        int sp = 0;
        while (sp < s.Length && !char.IsWhiteSpace(s[sp])) sp++;
        string name = s[..sp];
        string body = sp < s.Length ? s[sp..].Trim() : "";
        if (StartsWithWord(body, "AS")) body = body["AS".Length..].TrimStart();
        bool over = EndsWithWord(body, "OVERRIDE");
        if (over) body = body[..^"OVERRIDE".Length].TrimEnd();
        body = body.Trim();
        if (body.Equals("OFF", StringComparison.OrdinalIgnoreCase)) return (name, DefineKind.Off, "", over);
        if (body.Equals("PARAMETER", StringComparison.OrdinalIgnoreCase)) return (name, DefineKind.Parameter, "", over);
        return (name, DefineKind.Value, body, over);
    }

    private static void ApplyDefine(string rest, Dictionary<string, CtValue> defines,
        CompileTimeExpressionEvaluator evaluator, DirectiveDiag diag)
    {
        var (name, kind, operand, over) = SplitDefine(rest);
        if (name.Length == 0) return;
        switch (kind)
        {
            case DefineKind.Off:
                defines.Remove(name);   // GR2 — undefine
                return;
            case DefineKind.Parameter:
            {
                // GR4 — the value is obtained from the operating environment; unavailable ⇒ NOT defined. A value
                // that parses as a numeric literal is numeric, else alphanumeric.
                string? env = Environment.GetEnvironmentVariable(name);
                if (env is null) { defines.Remove(name); return; }
                var pv = decimal.TryParse(env, NumberStyles.Number, CultureInfo.InvariantCulture, out var num)
                    ? CtValue.Numeric(num, env) : CtValue.Alphanumeric(env);
                AssignDefine(name, pv, over, defines, diag);
                return;
            }
            default:
                if (EvaluateOperandText(operand, evaluator, diag, $">>DEFINE {name}") is { } v)
                    AssignDefine(name, v, over, defines, diag);
                return;
        }
    }

    /// <summary>Bind <paramref name="name"/> to <paramref name="newVal"/>, enforcing §7.3.11.3 SR2: without the
    /// OVERRIDE phrase a compilation variable already defined (and not OFF'd) may be redefined only to the SAME
    /// value (category-aware value equality — <c>AS 1</c> / <c>AS 01</c> / <c>AS 1.0</c> are the same). A differing
    /// no-OVERRIDE redefinition is COBOLNET1618 (superset-continue: the new value still binds).</summary>
    private static void AssignDefine(string name, CtValue newVal, bool over, Dictionary<string, CtValue> defines,
        DirectiveDiag diag)
    {
        if (!over && defines.TryGetValue(name, out var existing) && !existing.Equals(newVal))
            diag.Report1618(name);
        defines[name] = newVal;
    }

    // ── operand / cce evaluation via the ANTLR fragment parse + the shared evaluator ──────────────────────────

    /// <summary>Fragment-parse and evaluate one compile-time operand to a <see cref="CtValue"/>, or null (a
    /// syntax error is reported as COBOLNET1619; a formation error is reported by the evaluator).</summary>
    private static CtValue? EvaluateOperandText(string text, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag, string where)
    {
        if (DirectiveExpressionFragment.ParseOperand(text) is not { } frag) { diag.Malformed(where, text); return null; }
        var operand = frag.compileTimeOperand();
        diag.FlagArithmetic(operand);   // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) — evaluated context
        return evaluator.EvaluateOperand(operand, where);
    }

    /// <summary>Fragment-parse and evaluate a constant-conditional-expression; a malformed cce / formation error
    /// yields false for line selection (and is reported).</summary>
    private static bool EvaluateCceText(string text, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag, string where)
    {
        if (DirectiveExpressionFragment.ParseCce(text) is not { } frag) { diag.Malformed(where, text); return false; }
        var cce = frag.constantConditionalExpression();
        diag.FlagArithmetic(cce);   // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) — evaluated context
        return evaluator.EvaluateCce(cce, where) ?? false;
    }

    /// <summary>The first descendant of type <typeparamref name="T"/> in <paramref name="node"/>'s subtree — used
    /// to detect an arithmetic OPERATOR (<c>addOp</c>/<c>mulOp</c>) inside a directive operand for FLAG-14 b.</summary>
    private static bool HasDescendant<T>(Antlr4.Runtime.Tree.IParseTree node) where T : class
    {
        for (int k = 0; k < node.ChildCount; k++)
        {
            var child = node.GetChild(k);
            if (child is T || HasDescendant<T>(child)) return true;
        }
        return false;
    }

    /// <summary>Format-1 WHEN match (§7.3.13.4 GR4): subject == object, or (with THROUGH/THRU) the subject in the
    /// inclusive NUMERIC range [object, object3] (SR12 — a range requires numeric operands). Non-numeric equality
    /// is category-aware and length-sensitive (GR7).</summary>
    private static bool MatchWhen(CtValue? subject, string whenText, CompileTimeExpressionEvaluator evaluator,
        DirectiveDiag diag)
    {
        if (subject is null) return false;
        var (loText, hiText) = SplitRange(whenText);
        if (EvaluateOperandText(loText, evaluator, diag, ">>WHEN") is not { } lo) return false;
        // §7.3.13.3 SR11 — all selection subjects and objects shall be of the same category.
        if (subject.Category != lo.Category)
        {
            diag.Report(CtDiagCode.DirectiveRule,
                ">>WHEN: a selection object shall be of the same category as the selection subject (ISO §7.3.13.3 SR11)");
            return false;
        }
        if (hiText is null) return subject.RelationalEquals(lo);   // GR4a — subject == object (boolean right-extends, §8.8.4.2.8)
        if (EvaluateOperandText(hiText, evaluator, diag, ">>WHEN") is not { } hi) return false;
        // GR4b / SR12 — an inclusive NUMERIC range.
        if (subject.Category != CtCategory.Numeric || lo.Category != CtCategory.Numeric || hi.Category != CtCategory.Numeric)
        {
            diag.Report(CtDiagCode.DirectiveRule, ">>WHEN: a THROUGH range requires numeric operands (ISO §7.3.13.3 SR12)");
            return false;
        }
        return subject.Number >= lo.Number && subject.Number <= hi.Number;
    }

    /// <summary>Split a WHEN object at a top-level <c>THROUGH</c>/<c>THRU</c> word (the §7.3.13 range separator),
    /// ignoring any occurrence inside a string literal. Returns (object, null) when no range is present.</summary>
    private static (string Lo, string? Hi) SplitRange(string text)
    {
        int idx = FindRangeWord(text);
        if (idx < 0) return (text.Trim(), null);
        int end = idx;
        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;
        return (text[..idx].Trim(), text[end..].Trim());
    }

    private static int FindRangeWord(string text)
    {
        bool inStr = false;
        char q = '\0';
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inStr) { if (c == q) inStr = false; continue; }
            if (c is '"' or '\'') { inStr = true; q = c; continue; }
            bool wordStart = i == 0 || char.IsWhiteSpace(text[i - 1]);
            if (wordStart && (MatchesWordAt(text, i, "THROUGH") || MatchesWordAt(text, i, "THRU"))) return i;
        }
        return -1;
    }

    private static bool MatchesWordAt(string text, int i, string word)
    {
        if (i + word.Length > text.Length) return false;
        if (string.Compare(text, i, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0) return false;
        int after = i + word.Length;
        return after == text.Length || char.IsWhiteSpace(text[after]);
    }

    // ── small directive-syntax helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>Strip the leading <c>&gt;&gt;</c>, return the upper-cased directive keyword and the remainder.</summary>
    private static (string keyword, string rest) SplitDirective(string trimmed)
    {
        string s = trimmed[2..].TrimStart();
        int sp = 0;
        while (sp < s.Length && !char.IsWhiteSpace(s[sp])) sp++;
        return (s[..sp].ToUpperInvariant(), sp < s.Length ? s[sp..].Trim() : "");
    }

    private static bool StartsWithWord(string s, string word) =>
        s.StartsWith(word, StringComparison.OrdinalIgnoreCase)
        && (s.Length == word.Length || char.IsWhiteSpace(s[word.Length]));

    private static bool EndsWithWord(string s, string word) =>
        s.EndsWith(word, StringComparison.OrdinalIgnoreCase)
        && (s.Length == word.Length || char.IsWhiteSpace(s[s.Length - word.Length - 1]));

    /// <summary>The frontend diagnostic gateway: the shared evaluator's code-preserving reports (any
    /// <see cref="CtDiagCode"/>) and a fragment syntax error both route to COBOLNET1619 (the directive-expression
    /// violation); the §7.3.11.3 SR2 redefinition is COBOLNET1618. <see cref="Line"/> is set before each directive.</summary>
    private sealed class DirectiveDiag(DiagnosticBag? bag, string? sourcePath, FlagScanState flagScan) : ICtDiagnostics
    {
        private readonly string _path = sourcePath ?? "";
        private readonly FlagScanState _flagScan = flagScan;
        public int Line;

        /// <summary>FLAG-14 b (§7.3.15.4 GR4 b; E.2 item 6) — flag a compile-time arithmetic EXPRESSION (one with a
        /// real <c>addOp</c>/<c>mulOp</c>, not a bare literal) when COMPILE-TIME-ARITHMETIC-EXPRESSIONS is ON at the
        /// current directive line. Called on the already-parsed operand/cce fragment, in the evaluated context.</summary>
        public void FlagArithmetic(Antlr4.Runtime.Tree.IParseTree tree)
        {
            if (_flagScan.IsOn(FlagOption.Flag14CompileTimeArithmeticExpressions)
                && (HasDescendant<CobolParserCore.AddOpContext>(tree) || HasDescendant<CobolParserCore.MulOpContext>(tree)))
                FlagWarn(FlagOption.Flag14CompileTimeArithmeticExpressions, Line);
        }

        public void Report(CtDiagCode code, string message) => Emit("COBOLNET1619", message);

        public void Report1618(string name) => Emit("COBOLNET1618",
            $">>DEFINE: compilation variable '{name}' is redefined to a different value without the OVERRIDE "
            + "phrase (ISO §7.3.11.3 SR2)");

        public void Malformed(string where, string text) => Emit("COBOLNET1619",
            $"{where}: malformed compile-time expression '{text}' (ISO §7.3.6 / §7.3.7 / §7.3.8)");

        /// <summary>Emit a migration-flag WARNING for a frontend-inline FLAG option (b / c) at
        /// <paramref name="line"/> — the same code/message shape as <c>FlagConformancePass</c> for the bound
        /// options, so the two collection sites are indistinguishable to the user.</summary>
        public void FlagWarn(FlagOption option, int line)
        {
            var info = FlagOptions.Info(option);
            string code = info.Directive == FlagDirective.Flag14
                ? Editions.Diagnostics.DiagnosticCatalog.Flag14Warning.Code
                : Editions.Diagnostics.DiagnosticCatalog.Flag02Warning.Code;
            bag?.ReportWarning(code,
                $"{info.Change} — flagged by >>{FlagDirectiveLine.DirectiveWord(info.Directive)} {info.Word}",
                new SourceLocation(_path, 0, line, 0), default);
        }

        private void Emit(string code, string message) =>
            bag?.ReportError(code, message, new SourceLocation(_path, 0, Line, 0), default);
    }
}
