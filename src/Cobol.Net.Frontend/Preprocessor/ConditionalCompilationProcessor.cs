// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// COBOL-2002 conditional compilation (ISO §7.3.11 DEFINE directive, §7.3.16 IF directive) — a text-manipulation
/// stage pass that selectively includes source lines based on compilation variables. It runs on free-form
/// normalized text, before COPY expansion, so an <c>&gt;&gt;IF</c> may gate COPY statements in its branches.
///
/// Supported:
///   <c>&gt;&gt;DEFINE name [AS] {literal | OFF} [OVERRIDE]</c> — define / undefine a compilation variable to a
///       numeric or alphanumeric literal (GR15: a single numeric literal is a literal, not an expression).
///   <c>&gt;&gt;IF cce</c> / <c>&gt;&gt;ELSE</c> / <c>&gt;&gt;END-IF</c> with arbitrary nesting.
///   constant-conditional-expression = defined-condition (<c>name [IS] [NOT] DEFINED</c>),
///       relation (<c>operand [IS] [NOT] relop operand</c>, relop ∈ = &lt;&gt; &lt; &gt; &lt;= &gt;=),
///       combined with <c>AND</c> / <c>OR</c> / <c>NOT</c> and parentheses.
///
/// Implemented (Wave D): the §7.3.11.3 SR2 no-OVERRIDE redefinition check (COBOLNET1618), <c>AS PARAMETER</c>
/// (GR4 — the operating-environment source), and the OVERRIDE phrase (GR3). <c>&gt;&gt;EVALUATE</c>/<c>&gt;&gt;WHEN</c>
/// ARE implemented (the earlier deferral note was stale).
///
/// Deferred (tracked in the P13 review ledger + Wave D): arithmetic / boolean EXPRESSION operands in
/// DEFINE/EVALUATE (§7.3.6/§7.3.7 — today only single-token operands bind; a multi-token operand binds the FIRST
/// token, a known silent-wrong-value defect awaiting the compile-time expression evaluator), and
/// conditional-compilation directives located inside copied library text (§7.2.1 orders COPY before the CC scan).
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
    };

    /// <param name="text">The free-form-normalized source text.</param>
    /// <param name="leaveTurnDirectives">When true, an emitting-branch <c>&gt;&gt;TURN</c> line is LEFT IN the
    /// text for the downstream <see cref="TurnDirectiveProcessor"/> (the COBOL.NET EC model, ISO §7.3.25); an
    /// omitted-branch one still drops with its branch. The default (false) is the exact legacy behavior —
    /// TURN consumed here, the legacy caller untouched.</param>
    public static string Process(string text, bool leaveTurnDirectives = false, bool leavePropagateDirectives = false,
        bool leaveRefModZeroLengthDirectives = false, DiagnosticBag? diagnostics = null, string? sourcePath = null)
    {
        var defines = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<Frame>();
        var lines = text.Split('\n');
        var output = new string[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.TrimEnd('\r').TrimStart();
            bool emitting = stack.Count == 0 || stack.Peek().Emitting;

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
                    bool cond = parentActive && Evaluate(rest, defines);
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
                    var f = new Frame { Kind = FrameKind.Evaluate, ParentActive = parentActive, Emitting = false, BranchTaken = false };
                    string subj = rest.Trim();
                    if (subj.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) f.TruthForm = true;
                    else f.Subject = ResolveFirst(subj, defines);
                    stack.Push(f);
                    output[i] = "";
                    break;
                }
                case "WHEN":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.Evaluate)
                    {
                        var f = stack.Peek();
                        string obj = rest.Trim();
                        if (obj.Equals("OTHER", StringComparison.OrdinalIgnoreCase))
                            f.Emitting = f.ParentActive && !f.BranchTaken;     // OTHER fires only if nothing matched
                        else if (!f.ParentActive || f.BranchTaken)
                            f.Emitting = false;                                // enclosing omitted, or a prior WHEN already matched
                        else
                        {
                            bool match = f.TruthForm ? Evaluate(obj, defines)  // Format 2: constant-conditional-expression
                                                     : MatchWhen(f.Subject, obj, defines); // Format 1: subject = object [THRU object3]
                            f.Emitting = match;
                            if (match) f.BranchTaken = true;
                        }
                    }
                    output[i] = "";
                    break;
                case "END-EVALUATE":
                    if (stack.Count > 0 && stack.Peek().Kind == FrameKind.Evaluate) stack.Pop();
                    output[i] = "";
                    break;
                case "DEFINE":
                    if (emitting) ApplyDefine(rest, defines, diagnostics, sourcePath, i);   // a DEFINE in an omitted branch has no effect
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
        public Value? Subject;      // EVALUATE Format 1: the selection-subject value
    }

    /// <summary>Resolve the first token of a directive operand string to a value (an EVALUATE selection-subject).</summary>
    private static Value? ResolveFirst(string text, Dictionary<string, Value> defines)
    {
        var toks = Tokenize(text);
        return toks.Count == 0 ? null : ResolveToken(toks[0], defines);
    }

    /// <summary>Format-1 WHEN match: subject = object, or subject within [object .. object3] when THROUGH/THRU is given.</summary>
    private static bool MatchWhen(Value? subject, string whenText, Dictionary<string, Value> defines)
    {
        if (subject is null) return false;
        var toks = Tokenize(whenText);
        if (toks.Count == 0) return false;
        int thru = toks.FindIndex(t => t.Kind == TokKind.Word &&
            (t.Text.Equals("THROUGH", StringComparison.OrdinalIgnoreCase) || t.Text.Equals("THRU", StringComparison.OrdinalIgnoreCase)));
        if (thru > 0 && thru + 1 < toks.Count)
        {
            var lo = ResolveToken(toks[0], defines);
            var hi = ResolveToken(toks[thru + 1], defines);
            return Relate(subject, ">=", lo) && Relate(subject, "<=", hi);
        }
        return Relate(subject, "=", ResolveToken(toks[0], defines));
    }

    /// <summary>Resolve a single token to a comparable value: a literal is itself; a name is its define (null if undefined).</summary>
    private static Value? ResolveToken(Tok t, Dictionary<string, Value> defines) => t.Kind switch
    {
        TokKind.Number => new Value(true, decimal.Parse(t.Text, CultureInfo.InvariantCulture), t.Text),
        TokKind.String => new Value(false, 0m, t.Value),
        TokKind.Word => defines.TryGetValue(t.Text, out var v) ? v : null,
        _ => null,
    };

    /// <summary>Apply a relational operator to two values (numeric compare when both numeric, else ordinal string compare).</summary>
    private static bool Relate(Value? a, string op, Value? b)
    {
        if (a is null || b is null)
            return op == "<>" ? !(a is null && b is null) : (op == "=" && a is null && b is null);

        int cmp = a.IsNumeric && b.IsNumeric
            ? decimal.Compare(a.Number, b.Number)
            : string.CompareOrdinal(a.Str, b.Str);

        return op switch
        {
            "=" => cmp == 0,
            "<>" => cmp != 0,
            "<" => cmp < 0,
            ">" => cmp > 0,
            "<=" => cmp <= 0,
            ">=" => cmp >= 0,
            _ => false,
        };
    }

    /// <summary>Strip the leading <c>&gt;&gt;</c>, return the upper-cased directive keyword and the remainder.</summary>
    private static (string keyword, string rest) SplitDirective(string trimmed)
    {
        string s = trimmed[2..].TrimStart();
        int sp = 0;
        while (sp < s.Length && !char.IsWhiteSpace(s[sp])) sp++;
        return (s[..sp].ToUpperInvariant(), sp < s.Length ? s[sp..].Trim() : "");
    }

    /// <summary>Apply a <c>&gt;&gt;DEFINE compilation-variable-name AS { arithmetic-expression | boolean-expression |
    /// literal | PARAMETER } [OVERRIDE] | OFF</c> directive (ISO §7.3.11). Enforces SR2 (§7.3.11.3 #2 — a
    /// no-OVERRIDE redefinition to a DIFFERENT value is a violation), GR3 (OVERRIDE ⇒ unconditional set), GR4
    /// (PARAMETER ⇒ the value comes from the operating environment; undefined when unavailable). NOTE: an
    /// arithmetic/boolean EXPRESSION operand (§7.3.6/§7.3.7) still binds only its FIRST token today — the §7.3.6
    /// compile-time evaluator is a separate Wave-D item; single-token literals + PARAMETER are exact.</summary>
    private static void ApplyDefine(string rest, Dictionary<string, Value> defines,
        DiagnosticBag? diagnostics, string? sourcePath, int lineIndex)
    {
        var toks = Tokenize(rest);
        if (toks.Count == 0 || toks[0].Kind != TokKind.Word) return;
        string name = toks[0].Text;
        int idx = 1;
        if (idx < toks.Count && toks[idx].Kind == TokKind.Word &&
            toks[idx].Text.Equals("AS", StringComparison.OrdinalIgnoreCase)) idx++;
        if (idx >= toks.Count) return;

        // OVERRIDE is the optional trailing phrase (GR3) — its presence bypasses SR2 and unconditionally sets.
        bool over = toks[^1].Kind == TokKind.Word && toks[^1].Text.Equals("OVERRIDE", StringComparison.OrdinalIgnoreCase);
        var v = toks[idx];

        if (v.Kind == TokKind.Word && v.Text.Equals("OFF", StringComparison.OrdinalIgnoreCase))
        {
            defines.Remove(name);   // GR2 — undefine
            return;
        }

        // AS PARAMETER (GR4): the value is obtained from the operating environment; when no value is available the
        // compilation variable is NOT defined. A value that parses as a numeric literal is numeric, else alphanumeric.
        if (v.Kind == TokKind.Word && v.Text.Equals("PARAMETER", StringComparison.OrdinalIgnoreCase))
        {
            string? env = Environment.GetEnvironmentVariable(name);
            if (env is null) { defines.Remove(name); return; }
            var pv = decimal.TryParse(env, NumberStyles.Number, CultureInfo.InvariantCulture, out var num)
                ? new Value(true, num, env) : new Value(false, 0m, env);
            AssignDefine(name, pv, over, defines, diagnostics, sourcePath, lineIndex);
            return;
        }

        AssignDefine(name, Value.FromToken(v), over, defines, diagnostics, sourcePath, lineIndex);
    }

    /// <summary>Bind <paramref name="name"/> to <paramref name="newVal"/>, enforcing §7.3.11.3 SR2: without the
    /// OVERRIDE phrase, a compilation variable already defined (and not OFF'd) may be redefined only to the SAME
    /// value. A differing no-OVERRIDE redefinition is diagnosed (superset-continue: the new value still binds).</summary>
    private static void AssignDefine(string name, Value newVal, bool over, Dictionary<string, Value> defines,
        DiagnosticBag? diagnostics, string? sourcePath, int lineIndex)
    {
        if (!over && defines.TryGetValue(name, out var existing) && !existing.Equals(newVal))
            diagnostics?.ReportError("COBOLNET1618",
                $">>DEFINE: compilation variable '{name}' is redefined to a different value without the OVERRIDE "
                + "phrase (ISO §7.3.11.3 SR2)", new SourceLocation(sourcePath ?? "", 0, lineIndex, 0), default);
        defines[name] = newVal;
    }

    // ── constant-conditional-expression evaluation ────────────────────────────────────────────────────────────

    private static bool Evaluate(string expr, Dictionary<string, Value> defines)
    {
        var parser = new CondParser(Tokenize(expr), defines);
        bool result = parser.ParseOr();
        return result;
    }

    private sealed class CondParser
    {
        private readonly List<Tok> _t;
        private readonly Dictionary<string, Value> _defines;
        private int _p;

        public CondParser(List<Tok> tokens, Dictionary<string, Value> defines) { _t = tokens; _defines = defines; }

        private Tok? Peek => _p < _t.Count ? _t[_p] : null;
        private Tok Next() => _t[_p++];
        private bool IsWord(string w) => Peek is { Kind: TokKind.Word } t && t.Text.Equals(w, StringComparison.OrdinalIgnoreCase);
        private bool TakeWord(string w) { if (IsWord(w)) { _p++; return true; } return false; }

        public bool ParseOr()
        {
            bool v = ParseAnd();
            while (TakeWord("OR")) v = ParseAnd() || v;   // evaluate rhs unconditionally (no side effects); keep || for clarity
            return v;
        }

        private bool ParseAnd()
        {
            bool v = ParseNot();
            while (TakeWord("AND")) v = ParseNot() && v;
            return v;
        }

        private bool ParseNot()
        {
            if (TakeWord("NOT")) return !ParseNot();
            return ParsePrimary();
        }

        private bool ParsePrimary()
        {
            if (Peek is { Kind: TokKind.LParen })
            {
                _p++;
                bool v = ParseOr();
                if (Peek is { Kind: TokKind.RParen }) _p++;
                return v;
            }
            return ParseCondition();
        }

        /// <summary>defined-condition | relation.</summary>
        private bool ParseCondition()
        {
            if (Peek is null) return false;
            Tok left = Next();
            TakeWord("IS");                 // optional IS
            bool negate = TakeWord("NOT");  // optional NOT (applies to the whole condition)

            if (TakeWord("DEFINED"))
            {
                bool d = left.Kind == TokKind.Word && _defines.ContainsKey(left.Text);
                return negate ^ d;
            }

            // relation: left relop right
            string? op = ReadRelop();
            if (op is null) return false;   // malformed — treat as false
            if (Peek is null) return false;
            Tok right = Next();
            bool r = Relate(ResolveToken(left, _defines), op, ResolveToken(right, _defines));
            return negate ^ r;
        }

        private string? ReadRelop()
        {
            if (Peek is not { Kind: TokKind.Op } t) return null;
            _p++;
            return t.Text;
        }
    }

    /// <summary>A compilation-variable value (numeric or alphanumeric).</summary>
    private sealed record Value(bool IsNumeric, decimal Number, string Str)
    {
        public static Value FromToken(Tok t) => t.Kind switch
        {
            TokKind.Number => new Value(true, decimal.Parse(t.Text, CultureInfo.InvariantCulture), t.Text),
            TokKind.String => new Value(false, 0m, t.Value),
            _ => new Value(false, 0m, t.Text),
        };
    }

    // ── tokenizer ─────────────────────────────────────────────────────────────────────────────────────────────

    private enum TokKind { Word, Number, String, Op, LParen, RParen }

    private readonly record struct Tok(TokKind Kind, string Text, string Value);

    private static List<Tok> Tokenize(string s)
    {
        var toks = new List<Tok>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c is '"' or '\'')
            {
                char q = c; i++;
                var sb = new System.Text.StringBuilder();
                while (i < s.Length)
                {
                    if (s[i] == q)
                    {
                        if (i + 1 < s.Length && s[i + 1] == q) { sb.Append(q); i += 2; continue; } // doubled quote
                        i++; break;
                    }
                    sb.Append(s[i++]);
                }
                toks.Add(new Tok(TokKind.String, sb.ToString(), sb.ToString()));
                continue;
            }

            if (c == '(') { toks.Add(new Tok(TokKind.LParen, "(", "")); i++; continue; }
            if (c == ')') { toks.Add(new Tok(TokKind.RParen, ")", "")); i++; continue; }

            if (c is '=' or '<' or '>')
            {
                // two-char relops first: <=, >=, <>
                if (i + 1 < s.Length && (s[i + 1] == '=' || (c == '<' && s[i + 1] == '>')))
                {
                    toks.Add(new Tok(TokKind.Op, s.Substring(i, 2), "")); i += 2; continue;
                }
                toks.Add(new Tok(TokKind.Op, c.ToString(), "")); i++; continue;
            }

            // numeric literal: optional sign, digits, optional fractional part
            if (char.IsDigit(c) || ((c == '+' || c == '-' || c == '.') && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                int start = i;
                if (c is '+' or '-') i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                toks.Add(new Tok(TokKind.Number, s[start..i], ""));
                continue;
            }

            // word: letters, digits, hyphen, underscore (a COBOL user-defined word / compiler-directive word)
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '-' || s[i] == '_')) i++;
                toks.Add(new Tok(TokKind.Word, s[start..i], ""));
                continue;
            }

            i++; // skip any other character
        }
        return toks;
    }
}
