// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolSharp.Compiler.Preprocessor;

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
/// Deferred (documented WS-2002-FORMAT follow-ups): <c>&gt;&gt;EVALUATE</c>/<c>&gt;&gt;WHEN</c>, arithmetic /
/// boolean expression operands in DEFINE, the PARAMETER (operating-environment) source of a define, and
/// conditional-compilation directives located inside copied library text.
///
/// Blast radius is essentially nil: a source with no <c>&gt;&gt;</c> lines is reproduced byte-for-byte.
/// </summary>
public static class ConditionalCompilationProcessor
{
    public static string Process(string text)
    {
        var defines = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<IfFrame>();
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
                    stack.Push(new IfFrame { ParentActive = parentActive, Emitting = cond, BranchTaken = cond });
                    output[i] = "";
                    break;
                }
                case "ELSE":
                    if (stack.Count > 0)
                    {
                        var f = stack.Peek();
                        f.Emitting = f.ParentActive && !f.BranchTaken;   // the ELSE body emits only if no prior branch did
                    }
                    output[i] = "";
                    break;
                case "END-IF":
                    if (stack.Count > 0) stack.Pop();
                    output[i] = "";
                    break;
                case "DEFINE":
                    if (emitting) ApplyDefine(rest, defines);   // a DEFINE inside an omitted branch has no effect
                    output[i] = "";
                    break;
                default:
                    // An unrecognized >> directive (e.g. EVALUATE / CALL-CONVENTION — not yet handled). Leave it
                    // in place when emitting so later stages see it; drop it when the branch is omitted.
                    output[i] = emitting ? line : "";
                    break;
            }
        }

        return string.Join('\n', output);
    }

    /// <summary>One <c>&gt;&gt;IF…[&gt;&gt;ELSE]…&gt;&gt;END-IF</c> nesting level.</summary>
    private sealed class IfFrame
    {
        public bool ParentActive;   // is the enclosing context emitting? (a nested IF inside an omitted branch stays omitted)
        public bool Emitting;       // is THIS branch's text currently being included?
        public bool BranchTaken;    // has a branch (the IF arm) already been taken? (drives ELSE)
    }

    /// <summary>Strip the leading <c>&gt;&gt;</c>, return the upper-cased directive keyword and the remainder.</summary>
    private static (string keyword, string rest) SplitDirective(string trimmed)
    {
        string s = trimmed[2..].TrimStart();
        int sp = 0;
        while (sp < s.Length && !char.IsWhiteSpace(s[sp])) sp++;
        return (s[..sp].ToUpperInvariant(), sp < s.Length ? s[sp..].Trim() : "");
    }

    private static void ApplyDefine(string rest, Dictionary<string, Value> defines)
    {
        var toks = Tokenize(rest);
        if (toks.Count == 0 || toks[0].Kind != TokKind.Word) return;
        string name = toks[0].Text;
        int idx = 1;
        if (idx < toks.Count && toks[idx].Kind == TokKind.Word &&
            toks[idx].Text.Equals("AS", StringComparison.OrdinalIgnoreCase)) idx++;
        if (idx >= toks.Count) return;
        var v = toks[idx];
        if (v.Kind == TokKind.Word && v.Text.Equals("OFF", StringComparison.OrdinalIgnoreCase))
        {
            defines.Remove(name);
            return;
        }
        defines[name] = Value.FromToken(v);   // trailing OVERRIDE (if any) is ignored — we always override
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
            bool r = Compare(Resolve(left), op, Resolve(right));
            return negate ^ r;
        }

        private string? ReadRelop()
        {
            if (Peek is not { Kind: TokKind.Op } t) return null;
            _p++;
            return t.Text;
        }

        /// <summary>Resolve a token to a comparable value: a literal is itself; a name is its define (null if undefined).</summary>
        private Value? Resolve(Tok t) => t.Kind switch
        {
            TokKind.Number => new Value(true, decimal.Parse(t.Text, CultureInfo.InvariantCulture), t.Text),
            TokKind.String => new Value(false, 0m, t.Value),
            TokKind.Word => _defines.TryGetValue(t.Text, out var v) ? v : null,
            _ => null,
        };

        private static bool Compare(Value? a, string op, Value? b)
        {
            if (a is null || b is null)
                return op == "<>" ? !(a is null && b is null) : (op == "=" && a is null && b is null);

            int cmp;
            if (a.IsNumeric && b.IsNumeric) cmp = decimal.Compare(a.Number, b.Number);
            else cmp = string.CompareOrdinal(a.Str, b.Str);

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
