// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// Intrinsic-function binding (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1). The grammar captures FUNCTION
/// arguments as a flat SUBSCRIPT-mode token stream (<c>functionCall : FUNCTION functionName subscriptPart?</c> —
/// the COBOL comma/space argument separators survive only as tokens), so binding is: flatten → split on depth-0
/// separators (the ONE splitter <see cref="ReferenceResolver.SplitSubscriptTokens"/> shares with subscripts) →
/// a small recursive-descent parse of each segment to a typed <see cref="BoundOperand"/> (the proven legacy
/// <c>ParseSubPrimary</c> shape): literals, OF/IN-qualified identifiers with nested subscripts, the COBOL
/// operators with §8.8.1 precedence, parentheses, and nested <c>FUNCTION</c> calls. <c>table(ALL)</c> arguments
/// expand at bind time to one operand per occurrence (§15.3). ALL semantics — D8 edition gating, §15.3 arity,
/// MAX/MIN category resolution, the §15.68.3 r3 default-currency injection, the D7 LENGTH fold — happen HERE;
/// backends only render the resulting <see cref="BoundIntrinsicCall"/>.
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>The injectable compile-time clock for WHEN-COMPILED (§15.99.3 r2 — the COMPILATION timestamp;
    /// deep-dive D6). One capture per process: every unit compiled in this run shares the stamp, which the
    /// backend bakes into the generated source as a string constant.</summary>
    internal static Func<DateTimeOffset> CompileClock { get; set; } = () => DateTimeOffset.Now;

    /// <summary>FUNCTION call in an expression position (the <c>BindPrimary</c> hook).</summary>
    private BoundExpr BindIntrinsic(Core.FunctionCallContext fc)
    {
        string name = fc.functionName().GetText();
        var tokens = new List<IToken>();
        if (fc.subscriptPart()?.subscriptOrRefMod() is { } args)
            ReferenceResolver.CollectLeafTokens(args, tokens);
        return BindIntrinsicCore(name, tokens);
    }

    /// <summary>FUNCTION call as a MOVE sending operand (the <c>BindMove</c> hook) — and the operand shape every
    /// general-operand channel shares: the bound expression wrapped as a <see cref="BoundComputedOperand"/> (a
    /// LENGTH fold surfaces as its literal; an error stays a loud named operand).</summary>
    private BoundOperand IntrinsicOperand(Core.FunctionCallContext fc) => OperandOf(BindIntrinsic(fc));

    private static BoundOperand OperandOf(BoundExpr e) => e switch
    {
        BoundNumLiteral l => new BoundNumericLiteral(l.Text),       // a folded LENGTH
        BoundNumRef r => new BoundFieldOperand(r.Place),            // a user-function result temp (M2-UDF-1)
        BoundExprError err => new BoundOperandError(err.Feature),
        _ => new BoundComputedOperand(e),
    };

    /// <summary>The §8.4.3.2 SR2 FUNCTION-keyword-OMITTED reference form (M2-UDF-4): a data reference whose head
    /// is a REPOSITORY-declared user-function / function-prototype-name (or the containing function's own name),
    /// or — when <c>FUNCTION ALL INTRINSIC</c> / <c>FUNCTION name INTRINSIC</c> is in effect — an
    /// intrinsic-function-name, followed by a single subscript part, is a FUNCTION CALL, not a subscripted data
    /// item (SR6 :6918 — a <c>(</c> after such a name is ALWAYS its argument list). Bind-side, no grammar change
    /// (D2 — the discriminator is semantic, so a keyword-omitted <c>functionCall</c> grammar alternative would be
    /// an irreducible ambiguity with subscripted <c>dataReference</c>). Data-item-safe: a name that ALSO resolves
    /// to a declared data item stays a subscript (a data item wins — zero regression). Called at the two
    /// dataReference chokepoints (<see cref="RefExpr"/> / <see cref="FieldOperand"/>). Null = not one.</summary>
    private BoundExpr? KeywordOmittedFunction(Core.DataReferenceContext dref)
    {
        // Keyword omission via the REPOSITORY FUNCTION specifier is a COBOL-2002 introduction (§12.3.8) — below
        // 2002 the routing is inert, so the 85/NIST surface is byte-invariant (a lone name(args) stays a data
        // reference exactly as before). The FUNCTION-keyword form is unaffected at every edition.
        if (data.Edition.DialectLevel < 2002) return null;
        if (dref.cobolWord() is not { } cw) return null;                    // special registers (LINAGE/LINE/PAGE-COUNTER) are never functions
        var suffixes = dref.dataReferenceSuffix();
        if (suffixes.Length != 1 || suffixes[0].subscriptPart() is not { } sp) return null;   // exactly `name(args)` — no qualification / refmod tail
        string name = cw.GetText();
        bool isFn = data.UserFunctionNames.Contains(name)
            || name.Equals(UdfSelfName, StringComparison.OrdinalIgnoreCase)
            || ((data.RepositoryAllIntrinsic || data.RepositoryIntrinsics.Contains(name))
                && IntrinsicCatalog.TryGet(name, out _));
        if (!isFn) return null;
        if (data.LookupData(name) is { Count: > 0 }) return null;          // a declared data item wins — never a mis-routed subscript
        var tokens = new List<IToken>();
        if (sp.subscriptOrRefMod() is { } args) ReferenceResolver.CollectLeafTokens(args, tokens);
        return BindIntrinsicCore(name, tokens);
    }

    /// <summary>Bind one FUNCTION reference from its name + flat argument tokens (shared by the parse-context
    /// entries above and the nested-FUNCTION recursion inside argument segments).</summary>
    private BoundExpr BindIntrinsicCore(string name, List<IToken> argTokens)
    {
        // §12.3.8.2 GR12 (:14885): within the environment division's scope, a REPOSITORY-declared
        // function-prototype-name refers to the USER-DEFINED function "and not to an intrinsic function of
        // the same name" (the spec's own factorial-override example, :43651) — so the user-function
        // dispatch PRECEDES the catalog. §8.4.6.6 adds the CONTAINING function definition's own name with
        // no repository declaration (self-recursion; a present self-entry is ignored, §12.3.8 GR11).
        if (data.UserFunctionNames.Contains(name)
            || name.Equals(UdfSelfName, StringComparison.OrdinalIgnoreCase))
            return UdfBindCall(name, argTokens);

        if (!IntrinsicCatalog.TryGet(name, out var sig))
        {
            bool definedInGroup = UserFunctions?.ContainsKey(name) == true;
            data.Edition.Error("COBOLNET1501", $"FUNCTION {name.ToUpperInvariant()} is not an intrinsic function "
                + "of ISO/IEC 1989 (§15.6 summary of functions)"
                + (definedInGroup
                    ? $"; the compilation group defines FUNCTION-ID {name.ToUpperInvariant()} — declare "
                      + $"FUNCTION {name.ToUpperInvariant()} in this unit's REPOSITORY paragraph to reference "
                      + "it as a user-defined function (ISO §12.3.8.2 GR12)"
                    : ""));
            return new BoundExprError($"FUNCTION {name}");
        }

        // D8 edition window: a function is rejected, BY NAME AND EDITION, outside [IntroducedIn, RemovedIn).
        // The 42-function 1989 Intrinsic Function Module rows carry IntroducedIn 85 — the amendment is part of
        // the CCVS-85 corpus, so --std 85 accepts them.
        if (sig.IntroducedIn > data.Edition.DialectLevel)
            data.Edition.Error("COBOLNET1502", $"FUNCTION {sig.Name} was introduced by ISO/IEC 1989:{sig.IntroducedIn} "
                + $"(§15) — it requires --std {sig.IntroducedIn} or later (targeting COBOL-{data.Edition.DialectLevel})");
        else if (sig.RemovedIn is { } gone && data.Edition.DialectLevel >= gone)
            data.Edition.Error("COBOLNET1503", $"FUNCTION {sig.Name} was removed by ISO/IEC 1989:{gone} — "
                + $"it is not available when targeting COBOL-{data.Edition.DialectLevel}");

        // TRIM (§15.96) — the one §15 function whose argument list carries a phrase keyword (LEADING/TRAILING);
        // its bespoke shape is bound apart from the generic comma/space-split argument path.
        if (sig.Name == "TRIM") return BindTrim(sig, argTokens);

        // FIND-STRING (§15.37) — argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]: phrase
        // keywords interleaved with operands, bound apart from the generic argument path.
        if (sig.Name == "FIND-STRING") return BindFindString(sig, argTokens);

        var args = BindIntrinsicArgs(argTokens);
        if (args.Count < sig.MinArgs || args.Count > sig.MaxArgs)
        {
            data.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes "
                + (sig.MinArgs == sig.MaxArgs ? $"{sig.MinArgs}" : sig.MaxArgs == int.MaxValue ? $"at least {sig.MinArgs}" : $"{sig.MinArgs}..{sig.MaxArgs}")
                + $" argument(s); {args.Count} given (ISO §15.3)");
            return new BoundExprError($"FUNCTION {sig.Name} arity");
        }

        // FUNCTION LENGTH folds at compile time from PIC metadata (§15.50; deep-dive D7).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "LENGTH")
            return BindLengthFold(sig, args);

        // NUMVAL-C with argument-2 omitted: there is exactly ONE currency string for the compilation unit — the
        // SPECIAL-NAMES CURRENCY string or the default sign (§15.68.3 rule 3). Injecting it at bind time keeps
        // the SPECIAL-NAMES config out of the backend (bound nodes carry complete semantics).
        if (sig.Name == "NUMVAL-C" && args.Count == 1)
            args.Add(new BoundStringLiteral(data.CurrencyString));

        // MAX/MIN are category-polymorphic (§15.59/§15.63: the result follows the arguments — all-alphanumeric
        // arguments return the SELECTED STRING); ORD-MAX/ORD-MIN always return an ordinal but dispatch their
        // comparison by the same argument category. The legacy rule: all-non-numeric ⇒ the string family.
        var resolved = sig;
        var category = sig.ResultCategory;
        if (sig.ArgKinds == "p" && args.Count > 0 && args.All(IsStringOperand))
        {
            resolved = sig with
            {
                RuntimeMethod = sig.Name switch
                {
                    "MAX" => "MaxString", "MIN" => "MinString",
                    "ORD-MAX" => "OrdMaxString", _ => "OrdMinString",
                },
            };
            if (sig.Name is "MAX" or "MIN") category = PicCategory.Alphanumeric;
        }

        // CHAR/ORD are PCS-relative (§15.15.4 r2 / §15.70.4): flag the call when a NON-identity program collating
        // sequence is in effect so the backend passes its weights table — and only then (hazard H5: the emitted
        // __COLLATE field exists only under a non-identity PCS; STANDARD-1/2/NATIVE normalize to identity).
        bool collate = sig.Name is "CHAR" or "ORD" && data.Collating is not null;

        // CHAR/ORD take ALPHANUMERIC operands (§15.15/§15.70; CHAR-NATIONAL §15.16 is the national twin —
        // Phase-4a residue #11). Belt-and-braces beside the D-N2 guards: a national arg through the 256-entry
        // weight table would alias its characters via `& 0xFF` — reject at bind, never a wrong ordinal.
        if (sig.Name is "CHAR" or "ORD" && args.Any(a => a switch
            {
                BoundStringLiteral { Category: PicCategory.National } => true,
                BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.National,
                _ => false,
            }))
            data.Edition.Error("COBOLNET0844", $"FUNCTION {sig.Name} takes an alphanumeric operand — the "
                + "national forms (FUNCTION CHAR-NATIONAL §15.16 / ORD over national) are not yet implemented "
                + "(Phase 4a residue)");

        // A FUNCTION EXCEPTION-* reference reads the runtime last-exception register (§15.28–15.33) — flag the
        // program's EC usage so the generated source carries the Exceptions using (the group EC gate).
        if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) EcNoteFunction();

        return new BoundIntrinsicCall(resolved, args, category, collate);
    }

    /// <summary>TRIM (§15.96) — the ONE §15 function with a phrase keyword in its argument list
    /// (<c>[LEADING|TRAILING]</c>, §15.96.2). The keyword is a bare <c>SUB_IDENTIFIER</c> segment (space- or
    /// comma-separated), extracted here so the remaining segments bind as ordinary operands: argument-1 (the
    /// string, required) followed by zero-or-more single-character argument-2 trim characters (§15.96.3 rule 2 —
    /// the default when none is given is a space, rule 3.a). The LEADING/TRAILING mode rides on the bound node's
    /// <see cref="BoundIntrinsicCall.TrimMode"/> (0 = both, rule 3).</summary>
    private BoundExpr BindTrim(IntrinsicSig sig, List<IToken> argTokens)
    {
        int mode = 0;   // 0 = both leading+trailing (r3), 1 = LEADING (r1), 2 = TRAILING (r2)
        var operands = new List<BoundOperand>();
        foreach (var seg in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            // A lone LEADING / TRAILING word (a bare SUB_IDENTIFIER segment) is the phrase keyword, not an operand.
            if (seg.Where(t => t.Type != Core.SUB_WS).ToList() is [{ Type: Core.SUB_IDENTIFIER } kw])
            {
                if (kw.Text.Equals("LEADING", StringComparison.OrdinalIgnoreCase)) { mode = 1; continue; }
                if (kw.Text.Equals("TRAILING", StringComparison.OrdinalIgnoreCase)) { mode = 2; continue; }
            }
            operands.Add(ParseArgSegment(seg));
        }
        if (operands.Count == 0)
        {
            data.Edition.Error("COBOLNET1504", "FUNCTION TRIM takes at least a string argument-1 (ISO §15.96.2/.3)");
            return new BoundExprError("FUNCTION TRIM");
        }
        // The argument-2 form (delete characters OTHER than space) is a 2023 enhancement — TRIM removed only
        // spaces through 2014 (Annex E.3.3 item 31; VERSION_CHANGE_REFERENCE row 74). Its introduction is gated
        // by name+edition, like the whole-function 1502 gate above.
        if (operands.Count > 1 && data.Edition.DialectLevel < 2023)
            data.Edition.Error("COBOLNET1502", "the FUNCTION TRIM argument-2 form (removing characters other than "
                + "space) was introduced by ISO/IEC 1989:2023 (§15.96; Annex E.3.3 item 31) — it requires "
                + $"--std 2023 or later (targeting COBOL-{data.Edition.DialectLevel}); TRIM removed only spaces through 2014");
        return new BoundIntrinsicCall(sig, operands, PicCategory.Alphanumeric) { TrimMode = mode };
    }

    /// <summary>FIND-STRING (§15.37) — <c>argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]</c>
    /// (§15.37.2). The phrase words arrive as lone <c>SUB_IDENTIFIER</c> segments (the whitespace splitter isolates
    /// each): LAST selects the last occurrence (rule 1); ANYCASE folds case (rule 4); START/AFTER are the optional
    /// argument-3 introducer (noise — argument-3's mere presence selects the skip form, rule 2). The operand
    /// segments are argument-1 (the haystack), argument-2 (the needle), and the optional integer argument-3 (the
    /// number of matches to ignore). The two flags ride on <see cref="BoundIntrinsicCall.FindLast"/> /
    /// <see cref="BoundIntrinsicCall.FindAnycase"/>; argument-3 (if given) is the third operand.</summary>
    private BoundExpr BindFindString(IntrinsicSig sig, List<IToken> argTokens)
    {
        bool last = false, anycase = false;
        var operands = new List<BoundOperand>();
        foreach (var seg in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            if (seg.Where(t => t.Type != Core.SUB_WS).ToList() is [{ Type: Core.SUB_IDENTIFIER } kw])
            {
                if (kw.Text.Equals("LAST", StringComparison.OrdinalIgnoreCase)) { last = true; continue; }
                if (kw.Text.Equals("ANYCASE", StringComparison.OrdinalIgnoreCase)) { anycase = true; continue; }
                // START AFTER — the argument-3 introducer words (§15.37.2); the integer that follows is argument-3.
                if (kw.Text.Equals("START", StringComparison.OrdinalIgnoreCase)
                    || kw.Text.Equals("AFTER", StringComparison.OrdinalIgnoreCase)) continue;
            }
            operands.Add(ParseArgSegment(seg));
        }
        if (operands.Count is < 2 or > 3)
        {
            data.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 "
                + $"[[START AFTER] argument-3] (ISO §15.37.2); {operands.Count} operand argument(s) given");
            return new BoundExprError("FUNCTION FIND-STRING");
        }
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric) { FindLast = last, FindAnycase = anycase };
    }

    /// <summary>An operand whose comparison/result category is alphanumeric (drives MAX/MIN resolution): a string
    /// literal, an alphanumeric/edited/group item, or a nested alphanumeric-result intrinsic.</summary>
    private static bool IsStringOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        // National/boolean operands participate through the char pipeline (MAX/MIN over national compares
        // ordinal per D-N3; the category-national RESULT channel is the -N intrinsic residue, #11).
        BoundFieldOperand f => f.Place.Item.IsGroup
            || f.Place.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean,
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric } } => true,
        _ => false,
    };

    /// <summary>FUNCTION LENGTH (§15.50.4): the argument's length in character positions. A literal or a
    /// fixed-size item folds to its compile-time size — <c>DataItem.ImageWidth</c> IS the character-position
    /// count (digits + a separate-sign position for numerics, §13.18.52; PICTURE length otherwise; the leaf-sum
    /// for groups; P positions occupy none). A nested string-function argument keeps a RUNTIME <c>.Length</c>
    /// over its rendered fixed-width image (the image length IS the count — equivalent to the legacy's
    /// length-recursion). Reference-modified and occurs-depending arguments have RUNTIME lengths (§15.50.4
    /// rules 4/7) — staged loud by name until a consumer exists (none in the NIST corpus; loud-failure §1.4).</summary>
    private BoundExpr BindLengthFold(IntrinsicSig sig, List<BoundOperand> args) => args[0] switch
    {
        BoundStringLiteral s => new BoundNumLiteral(Math.Max(1, s.Value.Length).ToString()),
        BoundFieldOperand { Place: RefModPlace } =>
            new BoundExprError("FUNCTION LENGTH of a reference-modified argument (runtime length, §15.50.4)"),
        BoundFieldOperand f when f.Place.Item.IsGroup
                && OdoModel.TableUnder(f.Place.Item) is { OccursSpec.Depending: not null } =>
            new BoundExprError("FUNCTION LENGTH of a variable-length (OCCURS DEPENDING) group (runtime length, §15.50.4 r7)"),
        BoundFieldOperand f => new BoundNumLiteral(Math.Max(1, f.Place.Item.ImageWidth).ToString()),
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric } } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),   // runtime .Length over the nested result image
        _ => new BoundExprError("FUNCTION LENGTH argument"),
    };

    // ── Argument-list binding: split → ALL expansion → per-segment parse ───────────────────────────────────

    private List<BoundOperand> BindIntrinsicArgs(List<IToken> tokens)
    {
        var args = new List<BoundOperand>();
        if (tokens.Count == 0) return args;
        foreach (var segment in ReferenceResolver.SplitSubscriptTokens(tokens))
        {
            if (segment.All(t => t.Type == Core.SUB_WS)) continue;
            if (TryExpandAll(segment, args)) continue;
            args.Add(ParseArgSegment(segment));
        }
        return args;
    }

    /// <summary>
    /// <c>table(… ALL …)</c> argument expansion (ISO §15.3): when a variadic function references a table with the
    /// ALL subscript, the effect is as if each table element were specified — left to right, the RIGHTMOST ALL
    /// subscript varying fastest, each through 1..its OCCURS count. Returns true when the segment IS such a
    /// reference (consuming it — including loud error operands for unresolvable shapes); false hands the segment
    /// to the ordinary expression parse. An ALL subscript over an OCCURS DEPENDING table takes the CURRENT count
    /// (§15.3) — a runtime quantity this bind-time expansion cannot produce; staged loud by name (§1.4).
    /// </summary>
    private bool TryExpandAll(List<IToken> seg, List<BoundOperand> args)
    {
        int pos = SkipWs(seg, 0);
        if (pos >= seg.Count || seg[pos].Type != Core.SUB_IDENTIFIER) return false;
        string name = seg[pos].Text;
        if (name.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase)) return false;
        pos++;
        var quals = new List<string>();
        while (PeekType(seg, pos) is Core.SUB_OF or Core.SUB_IN)
        {
            pos = SkipWs(seg, pos) + 1;
            pos = SkipWs(seg, pos);
            if (pos >= seg.Count || seg[pos].Type != Core.SUB_IDENTIFIER) return false;
            quals.Add(seg[pos++].Text);
        }
        if (PeekType(seg, pos) != Core.SUB_LPAREN) return false;
        pos = SkipWs(seg, pos);
        var inner = CollectBalanced(seg, ref pos);
        if (SkipWs(seg, pos) < seg.Count) return false;   // trailing tokens — not a bare table(ALL) argument

        var innerSegs = ReferenceResolver.SplitSubscriptTokens(inner);
        if (!innerSegs.Any(IsAllSegment)) return false;

        if (refs.FindItem(name, quals) is not { } item)
        {
            args.Add(new BoundOperandError($"table(ALL) reference '{name}'"));
            return true;
        }
        // The OCCURS levels on the item's ancestor chain, outermost first — the AccessPath subscript order.
        var levels = new List<DataItem>();
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (n.Occurs is not null) levels.Add(n);
        levels.Reverse();
        if (levels.Count != innerSegs.Count)
        {
            args.Add(new BoundOperandError($"table(ALL) subscript count for '{name}'"));
            return true;
        }

        var fixedExprs = new string?[innerSegs.Count];
        var counts = new long[innerSegs.Count];
        for (int i = 0; i < innerSegs.Count; i++)
        {
            if (IsAllSegment(innerSegs[i]))
            {
                if (levels[i].OccursSpec?.Depending is not null)
                {
                    args.Add(new BoundOperandError($"table(ALL) over OCCURS DEPENDING table '{name}' (current-count expansion, §15.3)"));
                    return true;
                }
                counts[i] = levels[i].Occurs!.Value;
            }
            else if (refs.RenderIndexSegment(innerSegs[i]) is { } rendered)
                fixedExprs[i] = rendered;
            else
            {
                args.Add(new BoundOperandError($"table(ALL) subscript of '{name}'"));
                return true;
            }
        }

        long total = 1;
        foreach (long c in counts) if (c > 0) total *= c;
        for (long k = 0; k < total; k++)
        {
            var exprs = new string[innerSegs.Count];
            long rem = k;
            for (int i = innerSegs.Count - 1; i >= 0; i--)   // rightmost ALL varies fastest (§15.3)
                if (fixedExprs[i] is { } fx) exprs[i] = fx;
                else { exprs[i] = (rem % counts[i] + 1).ToString(); rem /= counts[i]; }
            args.Add(refs.ResolveByName(name, quals, exprs) is { } place
                ? new BoundFieldOperand(place)
                : new BoundOperandError($"table(ALL) occurrence of '{name}'"));
        }
        return true;
    }

    private static bool IsAllSegment(List<IToken> seg) =>
        seg.Count(t => t.Type != Core.SUB_WS) == 1 && seg.Any(t => t.Type == Core.SUB_ALL);

    // ── The per-segment recursive-descent parse (the legacy ParseSubAdditive…ParseSubPrimary shape) ─────────

    /// <summary>Parse one argument segment to a typed operand: a sole string literal stays alphanumeric; a sole
    /// data reference stays a field operand (its category decides string-vs-numeric rendering); anything with
    /// operators becomes a computed numeric expression. Unconsumed trailing tokens are a loud named operand —
    /// never a silent partial parse (§1.4).</summary>
    private BoundOperand ParseArgSegment(List<IToken> toks)
    {
        int pos = 0;
        BoundOperand op = ParseAdditive(toks, ref pos);
        if (SkipWs(toks, pos) < toks.Count)
            return new BoundOperandError($"intrinsic argument '{SegText(toks)}'");
        return op;
    }

    private BoundOperand ParseAdditive(List<IToken> toks, ref int pos)
    {
        BoundOperand left = ParseMultiplicative(toks, ref pos);
        while (PeekType(toks, pos) is Core.SUB_PLUS or Core.SUB_MINUS)
        {
            pos = SkipWs(toks, pos);
            char op = toks[pos].Type == Core.SUB_PLUS ? '+' : '-';
            pos++;
            BoundOperand right = ParseMultiplicative(toks, ref pos);
            left = new BoundComputedOperand(new BoundBinary(ArgExpr(left), op, ArgExpr(right)));
        }
        return left;
    }

    private BoundOperand ParseMultiplicative(List<IToken> toks, ref int pos)
    {
        BoundOperand left = ParsePower(toks, ref pos);
        while (PeekType(toks, pos) is Core.SUB_STAR or Core.SUB_SLASH)
        {
            pos = SkipWs(toks, pos);
            char op = toks[pos].Type == Core.SUB_STAR ? '*' : '/';
            pos++;
            BoundOperand right = ParsePower(toks, ref pos);
            left = new BoundComputedOperand(new BoundBinary(ArgExpr(left), op, ArgExpr(right)));
        }
        return left;
    }

    private BoundOperand ParsePower(List<IToken> toks, ref int pos)
    {
        BoundOperand left = ParseUnary(toks, ref pos);
        if (PeekType(toks, pos) == Core.SUB_POWER)
        {
            pos = SkipWs(toks, pos) + 1;
            BoundOperand right = ParsePower(toks, ref pos);   // ** is right-associative (ISO §8.8.1)
            return new BoundComputedOperand(new BoundPower(ArgExpr(left), ArgExpr(right)));
        }
        return left;
    }

    private BoundOperand ParseUnary(List<IToken> toks, ref int pos)
    {
        int t = PeekType(toks, pos);
        if (t is Core.SUB_PLUS or Core.SUB_MINUS)
        {
            pos = SkipWs(toks, pos) + 1;
            BoundOperand operand = ParseUnary(toks, ref pos);
            return t == Core.SUB_PLUS ? operand : new BoundComputedOperand(new BoundNegate(ArgExpr(operand)));
        }
        return ParseArgPrimary(toks, ref pos);
    }

    private BoundOperand ParseArgPrimary(List<IToken> toks, ref int pos)
    {
        pos = SkipWs(toks, pos);
        if (pos >= toks.Count) return new BoundOperandError("empty intrinsic argument");
        var tok = toks[pos];

        if (tok.Type == Core.SUB_LPAREN)   // parenthesized sub-expression
        {
            pos++;
            BoundOperand inner = ParseAdditive(toks, ref pos);
            pos = SkipWs(toks, pos);
            if (pos < toks.Count && toks[pos].Type == Core.SUB_RPAREN) pos++;
            return inner;
        }

        if (tok.Type is Core.SUB_INTEGERLIT or Core.SUB_DECIMALLIT
            or Core.SIGNED_INTEGERLIT or Core.SIGNED_DECIMALLIT)
        {
            pos++;
            return new BoundNumericLiteral(CheckLiteral(tok.Text));   // the one literal chokepoint (digit cap + comma mode)
        }

        if (tok.Type == Core.SUB_STRINGLIT)
        {
            pos++;
            return new BoundStringLiteral(DecodeSubString(tok.Text));
        }

        // National/boolean literal arguments decode char-correct with their category tags (the introduction
        // gates + 0814 guards ride the shared helpers); per-function class conformance is the intrinsic
        // catalog's binding job (the -N function family is Phase-4a residue #11).
        if (tok.Type == Core.SUB_NATLIT)
        {
            pos++;
            return NationalLiteralOperand(tok.Text);
        }
        if (tok.Type == Core.SUB_BOOLLIT)
        {
            pos++;
            return BooleanLiteralOperand(tok.Text);
        }

        if (tok.Type == Core.SUB_IDENTIFIER)
        {
            // Nested FUNCTION call: in SUBSCRIPT mode the keyword and the function name are plain identifiers —
            // recognize "FUNCTION name [( args )]" and recurse (e.g. ORD(FUNCTION CHAR(4)), MOD(C, FUNCTION MOD(C, B))).
            if (tok.Text.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase))
            {
                pos = SkipWs(toks, pos + 1);
                if (pos >= toks.Count || toks[pos].Type != Core.SUB_IDENTIFIER)
                    return new BoundOperandError("nested FUNCTION without a function name");
                string fname = toks[pos++].Text;
                List<IToken> inner = [];
                if (PeekType(toks, pos) == Core.SUB_LPAREN)
                {
                    pos = SkipWs(toks, pos);
                    inner = CollectBalanced(toks, ref pos);
                }
                return OperandOf(BindIntrinsicCore(fname, inner));
            }

            pos++;
            string name = tok.Text;
            var quals = new List<string>();
            while (PeekType(toks, pos) is Core.SUB_OF or Core.SUB_IN)   // OF/IN qualification (§8.4.2.3.2)
            {
                pos = SkipWs(toks, pos) + 1;
                pos = SkipWs(toks, pos);
                if (pos >= toks.Count || toks[pos].Type != Core.SUB_IDENTIFIER) break;
                quals.Add(toks[pos++].Text);
            }

            if (PeekType(toks, pos) == Core.SUB_LPAREN)   // nested subscripts (or ref-mod — staged loud)
            {
                pos = SkipWs(toks, pos);
                var inner = CollectBalanced(toks, ref pos);
                if (inner.Any(t => t.Type == Core.SUB_COLON))
                    return new BoundOperandError($"reference-modified intrinsic argument '{name}(…:…)'");
                var rendered = new List<string>();
                foreach (var s in ReferenceResolver.SplitSubscriptTokens(inner))
                {
                    if (refs.RenderIndexSegment(s) is not { } e)
                        return new BoundOperandError($"subscript of intrinsic argument '{name}'");
                    rendered.Add(e);
                }
                return refs.ResolveByName(name, quals, rendered) is { } sp
                    ? new BoundFieldOperand(sp)
                    : new BoundOperandError($"intrinsic argument '{name}'");
            }

            // A bare INDEXED BY index-name argument reads its occurrence number (ISO §13.18.38 / §3.5).
            if (quals.Count == 0 && data.TryGetVisibleIndexField(name, out var ix))
                return new BoundComputedOperand(new BoundIndexRef(ix));

            return refs.ResolveByName(name, quals, []) is { } place
                ? new BoundFieldOperand(place)
                : new BoundOperandError($"intrinsic argument '{name}'");
        }

        pos++;   // consume — the segment-level trailing-token check reports it
        return new BoundOperandError($"intrinsic argument token '{tok.Text}'");
    }

    /// <summary>A bound operand as a numeric expression node (the arithmetic combinators' conversion): a field
    /// reads its value, a literal its scaled text, a computed wrapper unwraps; an alphanumeric literal inside
    /// arithmetic is a loud named error (only NUMVAL converts strings to numbers, §15.67).</summary>
    private static BoundExpr ArgExpr(BoundOperand op) => op switch
    {
        BoundComputedOperand c => c.Expr,
        BoundFieldOperand f => new BoundNumRef(f.Place),
        BoundNumericLiteral n => new BoundNumLiteral(n.Text),
        BoundStringLiteral => new BoundExprError("alphanumeric literal in an arithmetic intrinsic argument"),
        BoundOperandError e => new BoundExprError(e.Feature),
        _ => new BoundExprError($"intrinsic argument '{op.GetType().Name}'"),
    };

    // ── Token utilities ──────────────────────────────────────────────────────────────────────────────────────

    private static int SkipWs(List<IToken> toks, int pos)
    {
        while (pos < toks.Count && toks[pos].Type == Core.SUB_WS) pos++;
        return pos;
    }

    private static int PeekType(List<IToken> toks, int pos)
    {
        pos = SkipWs(toks, pos);
        return pos < toks.Count ? toks[pos].Type : -1;
    }

    /// <summary>Collect the tokens inside a balanced <c>( … )</c> starting at <paramref name="pos"/> (which must
    /// sit on the SUB_LPAREN), excluding the outer parens; <paramref name="pos"/> lands past the close.</summary>
    private static List<IToken> CollectBalanced(List<IToken> toks, ref int pos)
    {
        var inner = new List<IToken>();
        if (pos >= toks.Count || toks[pos].Type != Core.SUB_LPAREN) return inner;
        pos++;
        int depth = 1;
        while (pos < toks.Count)
        {
            int t = toks[pos].Type;
            if (t == Core.SUB_LPAREN) depth++;
            else if (t == Core.SUB_RPAREN && --depth == 0) { pos++; break; }
            inner.Add(toks[pos++]);
        }
        return inner;
    }

    /// <summary>A COBOL string literal's character value from its SUB_STRINGLIT text (either quote character;
    /// doubled quotes collapse — ISO §8.3.3.4).</summary>
    private static string DecodeSubString(string raw) =>
        raw.Length >= 2 && (raw[0] == '"' || raw[0] == '\'') && raw[^1] == raw[0]
            ? raw[1..^1].Replace(new string(raw[0], 2), raw[0].ToString())
            : raw;

    private static string SegText(List<IToken> toks) => string.Concat(toks.Select(t => t.Text)).Trim();
}
