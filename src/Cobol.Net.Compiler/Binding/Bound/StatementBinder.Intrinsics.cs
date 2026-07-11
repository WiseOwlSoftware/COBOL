// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Common;
using CobolNet.Frontend.Generated;

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

    /// <summary>True when the unit being bound is a nested (contained) program — set from <c>CallUnit.Parent</c>
    /// at binder construction. Gates FUNCTION MODULE-NAME NESTED (§15.65.3 argument rule 1 — NESTED shall be
    /// specified only within a contained program).</summary>
    public bool InNestedProgram { get; init; }

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

        // SUBSTITUTE (§15.87) — argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …: per-pair phrase
        // keywords ahead of each replacement pair, bound apart from the generic argument path.
        if (sig.Name == "SUBSTITUTE") return BindSubstitute(sig, argTokens);

        // CONVERT (§15.19) — argument-1 source-format destination-format: two keyword groups after the operand,
        // bound apart from the generic argument path (the result category is computed per §15.19.1).
        if (sig.Name == "CONVERT") return BindConvert(sig, argTokens);

        // MODULE-NAME (§15.65) — one mandatory keyword (ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL), resolved
        // from the runtime module call-name stack; bound apart from the generic argument path.
        if (sig.Name == "MODULE-NAME") return BindModuleName(sig, argTokens);

        var args = BindIntrinsicArgs(argTokens);
        if (args.Count < sig.MinArgs || args.Count > sig.MaxArgs)
        {
            data.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes "
                + (sig.MinArgs == sig.MaxArgs ? $"{sig.MinArgs}" : sig.MaxArgs == int.MaxValue ? $"at least {sig.MinArgs}" : $"{sig.MinArgs}..{sig.MaxArgs}")
                + $" argument(s); {args.Count} given (ISO §15.3)");
            return new BoundExprError($"FUNCTION {sig.Name} arity");
        }

        // §15.38–15.41 / §15.48 / §15.79 / §15.92 rule 1: the date/time FORMAT (argument-1) shall be a LITERAL —
        // the format is analyzed/derived at compile time (SECONDS-FROM-FORMATTED-TIME needs the fraction scale).
        if (args.Count > 0 && args[0] is not BoundStringLiteral
            && sig.Name is "FORMATTED-CURRENT-DATE" or "FORMATTED-DATE" or "FORMATTED-DATETIME" or "FORMATTED-TIME"
                or "INTEGER-OF-FORMATTED-DATE" or "SECONDS-FROM-FORMATTED-TIME" or "TEST-FORMATTED-DATETIME")
            data.Edition.Error("COBOLNET1517", $"FUNCTION {sig.Name} argument-1 shall be a literal date/time format "
                + "(ISO §15 — the FORMATTED-*/INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/"
                + "TEST-FORMATTED-DATETIME format is a literal)");

        // FUNCTION LENGTH folds at compile time from PIC metadata (§15.50; deep-dive D7).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "LENGTH")
            return BindLengthFold(sig, args);

        // SMALLEST/HIGHEST/LOWEST-ALGEBRAIC fold at compile time from the argument item's PICTURE metadata
        // (§15.83/§15.43/§15.58; the same Fold discipline as LENGTH).
        if (sig.Bind == IntrinsicBind.Fold
                && sig.Name is "SMALLEST-ALGEBRAIC" or "HIGHEST-ALGEBRAIC" or "LOWEST-ALGEBRAIC")
            return BindAlgebraicFold(sig, args);

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

    /// <summary>SUBSTITUTE (§15.87) — <c>argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …</c>
    /// (§15.87.2). argument-1 is the source; each following group is a replacement PAIR (argument-2 matched,
    /// argument-3 substituted) preceded by its own optional phrase keywords: ANYCASE (case fold, rule 5),
    /// FIRST (first occurrence only, rule 3.a) or LAST (last only, rule 3.b) — default replaces ALL occurrences.
    /// The keyword words arrive as lone <c>SUB_IDENTIFIER</c> segments; each pair's mode (bits 0=FIRST/1=LAST/
    /// 2=ANYCASE) rides on <see cref="BoundIntrinsicCall.SubstituteModes"/>, and <see cref="BoundIntrinsicCall.Args"/>
    /// carries [source, from₁, to₁, from₂, to₂, …].</summary>
    private BoundExpr BindSubstitute(IntrinsicSig sig, List<IToken> argTokens)
    {
        var operands = new List<BoundOperand>();   // [source, from₁, to₁, from₂, to₂, …]
        var modes = new List<int>();               // one mode per completed pair
        int pending = 0;                           // phrase flags accumulating for the NEXT pair
        bool haveSource = false;
        int pairOperands = 0;                      // operands seen since the last completed pair
        BoundExpr Malformed() { data.Edition.Error("COBOLNET1504", "FUNCTION SUBSTITUTE takes argument-1 and one "
            + "or more [ANYCASE][FIRST|LAST] argument-2 argument-3 pairs (ISO §15.87.2)"); return new BoundExprError("FUNCTION SUBSTITUTE"); }

        foreach (var seg in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            if (seg.Where(t => t.Type != Core.SUB_WS).ToList() is [{ Type: Core.SUB_IDENTIFIER } kw])
            {
                if (kw.Text.Equals("ANYCASE", StringComparison.OrdinalIgnoreCase)) { pending |= 4; continue; }
                if (kw.Text.Equals("FIRST", StringComparison.OrdinalIgnoreCase)) { pending |= 1; continue; }
                if (kw.Text.Equals("LAST", StringComparison.OrdinalIgnoreCase)) { pending |= 2; continue; }
            }
            var op = ParseArgSegment(seg);
            if (!haveSource) { operands.Add(op); haveSource = true; continue; }
            operands.Add(op);
            if (++pairOperands == 2)   // argument-2 then argument-3 complete a pair
            {
                if ((pending & 3) == 3) return Malformed();   // FIRST and LAST are mutually exclusive (§15.87.2)
                modes.Add(pending);
                pending = 0;
                pairOperands = 0;
            }
        }
        // A well-formed call ends on a completed pair with no dangling keyword/operand (§15.87.3 rule requires
        // at least one pair; the source alone or a half pair is malformed).
        if (!haveSource || modes.Count == 0 || pairOperands != 0 || pending != 0) return Malformed();
        return new BoundIntrinsicCall(sig, operands, PicCategory.Alphanumeric) { SubstituteModes = modes };
    }

    /// <summary>CONVERT (§15.19) — data-representation conversion (2023). Argument-1 is followed by two phrase
    /// keyword groups: source-format (ANY | ANUM | HEX | NAT) and destination-format (ANUM | NAT [HEX] | BYTE),
    /// each a bare <c>SUB_IDENTIFIER</c> segment (§15.19.2). The operand binds ordinarily; the format words ride
    /// the node's <c>Convert*</c> init-properties. The argument/SR rules (§15.19.3) are enforced here
    /// (COBOLNET1514); the result category (§15.19.1) is National for a NAT destination, Alphanumeric otherwise.</summary>
    private BoundExpr BindConvert(IntrinsicSig sig, List<IToken> argTokens)
    {
        var kws = new List<string>();
        var operands = new List<BoundOperand>();
        foreach (var seg in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            if (seg.Where(t => t.Type != Core.SUB_WS).ToList() is [{ Type: Core.SUB_IDENTIFIER } w]
                && IsConvertFormatWord(w.Text))
            { kws.Add(w.Text.ToUpperInvariant()); continue; }
            operands.Add(ParseArgSegment(seg));
        }
        if (operands.Count != 1 || kws.Count < 2)
        {
            data.Edition.Error("COBOLNET1504", "FUNCTION CONVERT takes argument-1 source-format destination-format "
                + $"(ISO §15.19.2); {operands.Count} operand + {kws.Count} format keyword(s) given");
            return new BoundExprError("FUNCTION CONVERT");
        }

        int src = kws[0] switch { "ANY" => 0, "ALPHANUMERIC" or "ANUM" => 1, "HEX" => 2, "NATIONAL" or "NAT" => 3, _ => -1 };
        int i = 1, dst; bool hex = false;
        dst = kws[i] switch { "BYTE" => 4, "ALPHANUMERIC" or "ANUM" => 1, "NATIONAL" or "NAT" => 3, _ => -1 };
        if (dst >= 0) i++;
        if (dst is 1 or 3 && i < kws.Count && kws[i] == "HEX") { hex = true; i++; }

        if (src < 0 || dst < 0 || i != kws.Count)
        {
            data.Edition.Error("COBOLNET1514", $"FUNCTION CONVERT: '{string.Join(' ', kws)}' is not a valid "
                + "source-format destination-format pair (ISO §15.19.2 — ANY|ANUM|HEX|NAT then ANUM|NAT [HEX] | BYTE)");
            return new BoundExprError("FUNCTION CONVERT");
        }
        // SR3 — source shall differ from destination (only ANUM→ANUM / NAT→NAT with no HEX collide, §15.19.3).
        if ((src == 1 && dst == 1 && !hex) || (src == 3 && dst == 3 && !hex))
            data.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the source-format and destination-format are the "
                + "same (ISO §15.19.3 SR3)");
        // SR8 — an ANY source requires an ANUM HEX or NAT HEX destination.
        if (src == 0 && !(hex && dst is 1 or 3))
            data.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a source-format of ANY requires a destination of "
                + "ANUM HEX or NAT HEX (ISO §15.19.3 SR8)");
        // SR9 — a BYTE destination requires a HEX source.
        if (dst == 4 && src != 2)
            data.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a destination-format of BYTE requires a "
                + "source-format of HEX (ISO §15.19.3 SR9)");
        // SR1 — argument-1 shall not be zero length (compile-time catch for an empty literal).
        if (operands[0] is BoundStringLiteral { Value.Length: 0 })
            data.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: argument-1 is of zero length (ISO §15.19.3 SR1)");

        // §15.19.3 SR7 — a source-format of ANY takes the operand's RAW storage bits regardless of usage. Resolve
        // ANY to the operand's actual storage encoding at bind time (keeping the runtime free of PICTURE metadata):
        // a national operand's bits are UTF-16BE (the NAT reduction, 2 bytes/position); an alphanumeric operand's
        // are Latin-1 (the ANY default = 1 byte/char). ANY always pairs with a HEX destination (SR8).
        int runtimeSrc = src == 0 && IsNationalOperand(operands[0]) ? 3 : src;

        var category = dst == 3 ? PicCategory.National : PicCategory.Alphanumeric;   // §15.19.1 table
        return new BoundIntrinsicCall(sig, operands, category)
            { ConvertSource = runtimeSrc, ConvertDest = dst, ConvertDestHex = hex };
    }

    /// <summary>Is a bound operand a NATIONAL-category item or literal (its storage is UTF-16, per D-N1)?</summary>
    private static bool IsNationalOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral { Category: PicCategory.National } => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.National,
        _ => false,
    };

    /// <summary>The §15.19.2 CONVERT format words (reserved within the argument list, like TRIM's LEADING/TRAILING).</summary>
    private static bool IsConvertFormatWord(string w) =>
        w.ToUpperInvariant() is "ANY" or "ALPHANUMERIC" or "ANUM" or "HEX" or "NATIONAL" or "NAT" or "BYTE";

    /// <summary>MODULE-NAME (§15.65) — one mandatory phrase keyword selecting a runtime element of the running
    /// COBOL hierarchy (§15.65.2: ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL; <c>TOP-LEVEL</c> is one hyphenated
    /// <c>SUB_IDENTIFIER</c> token). The selector rides on <see cref="BoundIntrinsicCall.ModuleNameKind"/>; the
    /// value resolves at runtime from the module call-name stack (<c>CobolModule</c>). NESTED requires a contained
    /// program (§15.65.3 argument rule 1) — a compile-time conformance check.</summary>
    private BoundExpr BindModuleName(IntrinsicSig sig, List<IToken> argTokens)
    {
        int kind = -1;
        foreach (var seg in ReferenceResolver.SplitSubscriptTokens(argTokens))
        {
            if (seg.All(t => t.Type == Core.SUB_WS)) continue;
            int k = seg.Where(t => t.Type != Core.SUB_WS).ToList() is [{ Type: Core.SUB_IDENTIFIER } kw]
                ? kw.Text.ToUpperInvariant() switch
                    { "CURRENT" => 0, "ACTIVATING" => 1, "NESTED" => 2, "STACK" => 3, "TOP-LEVEL" => 4, _ => -1 }
                : -1;
            if (k < 0 || kind >= 0)
            {
                data.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME takes exactly one keyword argument "
                    + $"(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2), not '{SegText(seg)}'");
                return new BoundExprError("FUNCTION MODULE-NAME");
            }
            kind = k;
        }
        if (kind < 0)
        {
            data.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME requires one keyword argument "
                + "(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2)");
            return new BoundExprError("FUNCTION MODULE-NAME");
        }
        // §15.65.3 argument rule 1 — NESTED only within a nested (contained) program.
        if (kind == 2 && !InNestedProgram)
            data.Edition.Error("COBOLNET1515", "FUNCTION MODULE-NAME NESTED shall be specified only within a "
                + "nested program (ISO §15.65.3 argument rule 1) — this compilation unit is not contained");
        return new BoundIntrinsicCall(sig, [], PicCategory.Alphanumeric) { ModuleNameKind = kind };
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

    /// <summary>SMALLEST/HIGHEST/LOWEST-ALGEBRAIC (§15.83 / §15.43 / §15.58) — a compile-time PICTURE fold, like
    /// LENGTH. Argument-1 must be a category-numeric DATA ITEM (SMALLEST §15.83.3 r1) or numeric/numeric-edited
    /// DATA ITEM (HIGHEST/LOWEST §15.43.3/§15.58.3 r1) — never a literal, expression, group, ref-mod, or function.
    /// SMALLEST = 10^(−scale) (the smallest positive increment, r2). HIGHEST/LOWEST = the greatest/lowest value the
    /// PICTURE represents: all-nines (10^Digits−1) for a DigitCount item, the two's-complement container range for a
    /// BinaryCapacity item (COMP-5 / BINARY-CHAR family), the mask capacity for a numeric-edited item; LOWEST is the
    /// negated magnitude for a sign-representable item, else 0 (Annex D.32). The value returns as a numeric literal
    /// at the correct scale (BoundNumLiteral — the LENGTH-fold precedent).</summary>
    private BoundExpr BindAlgebraicFold(IntrinsicSig sig, List<BoundOperand> args)
    {
        if (args[0] is not BoundFieldOperand f || f.Place is RefModPlace || f.Place.Item.IsGroup
            || f.Place.Item.Pic is not { } pic)
            return AlgebraicArgError(sig);
        // §15.83.3 r1: SMALLEST admits ONLY category numeric; HIGHEST/LOWEST also admit numeric-edited.
        bool edited = pic.Category is PicCategory.NumericEdited;
        if (pic.Category is not PicCategory.Numeric && !(edited && sig.Name != "SMALLEST-ALGEBRAIC"))
            return AlgebraicArgError(sig);
        if (pic.Usage is Usage.Index)   // class index — not category numeric (§13.18.60)
            return AlgebraicArgError(sig);

        // Float usage (COMP-1/COMP-2): under native arithmetic the restriction is implementor-defined (§15.83.3 r4 /
        // §15.43.3 / §15.58.3), and COBOL.NET defines no PICTURE-based algebraic range for IEEE floats; under
        // standard-decimal these are barred by rule 2. Loud, complete — never a wrong value.
        if (pic.IsFloat)
        {
            data.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name} does not support a floating-point argument "
                + "(USAGE COMP-1/COMP-2): the native-arithmetic usage restriction is implementor-defined and "
                + "COBOL.NET does not define a PICTURE-based algebraic range for IEEE floats (ISO §15.83.3 r4 / "
                + "§15.43.3 / §15.58.3); under STANDARD-DECIMAL it is barred by rule 2");
            return new BoundExprError($"FUNCTION {sig.Name} float argument");
        }

        // SMALLEST — the smallest positive increment 10^(−scale), independent of digit count / sign / container.
        if (sig.Name == "SMALLEST-ALGEBRAIC")
            return new BoundNumLiteral(Decimalize(System.Numerics.BigInteger.One, pic.Scale, negative: false));

        int scale; System.Numerics.BigInteger unscaled; bool signable;
        if (edited)
        {
            var (cap, frac) = CobolNet.Runtime.CobolEdit.MaskCapacity(pic.EditMask!, data.CurrencyPicSymbol, data.DecimalPointIsComma);
            scale = frac;
            unscaled = Pow10(cap) - 1;   // all-nines over the mask's digit positions (§13.18.40.4)
            signable = pic.EditMask!.IndexOf('+') >= 0 || pic.EditMask!.IndexOf('-') >= 0
                       || pic.EditMask!.Contains("CR") || pic.EditMask!.Contains("DB");
        }
        else if (pic.Usage is Usage.Comp5 or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble)
        {
            scale = pic.Scale;
            int bits = 8 * pic.StorageWidth;   // container width (§13.18.60.4) — COMP-5 / BINARY-CHAR own the full range
            unscaled = sig.Name == "HIGHEST-ALGEBRAIC"
                ? (pic.Signed ? (System.Numerics.BigInteger.One << (bits - 1)) - 1 : (System.Numerics.BigInteger.One << bits) - 1)
                : (pic.Signed ? -(System.Numerics.BigInteger.One << (bits - 1)) : System.Numerics.BigInteger.Zero);
            return new BoundNumLiteral(Decimalize(unscaled, scale, unscaled.Sign < 0));
        }
        else
        {
            scale = pic.Scale;
            unscaled = Pow10(pic.Digits) - 1;   // all-nines (DigitCount discipline)
            signable = pic.Signed;
        }

        if (sig.Name == "HIGHEST-ALGEBRAIC")
            return new BoundNumLiteral(Decimalize(unscaled, scale, negative: false));
        // LOWEST: sign-representable → −magnitude; else 0 (§15.58.4 / Annex D.32).
        return signable
            ? new BoundNumLiteral(Decimalize(unscaled, scale, negative: true))
            : new BoundNumLiteral("0");
    }

    private BoundExpr AlgebraicArgError(IntrinsicSig sig)
    {
        string cat = sig.Name == "SMALLEST-ALGEBRAIC" ? "category numeric" : "category numeric or numeric-edited";
        string sec = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3" : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
        data.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name} argument-1 shall be a {cat} DATA ITEM — not a "
            + "literal, an arithmetic expression, a group item, a reference-modified item, an index, or another "
            + $"function (ISO §{sec} rule 1)");
        return new BoundExprError($"FUNCTION {sig.Name} argument");
    }

    private static System.Numerics.BigInteger Pow10(int n) => System.Numerics.BigInteger.Pow(10, Math.Max(0, n));

    /// <summary>Render an unscaled BigInteger at <paramref name="scale"/> fractional digits as a decimal literal
    /// string ('.' radix always — an internal C#-facing literal, never COBOL source, so DECIMAL-POINT IS COMMA
    /// does not apply). A negative scale (trailing P) appends |scale| zeros; a positive scale inserts the point.</summary>
    private static string Decimalize(System.Numerics.BigInteger unscaled, int scale, bool negative)
    {
        if (unscaled == 0) return "0";
        string s = System.Numerics.BigInteger.Abs(unscaled).ToString();
        string sign = negative ? "-" : "";
        if (scale <= 0) return sign + s + new string('0', -scale);          // S9PP: 99 @ −2 → "9900"; 1 @ −2 → "100"
        if (s.Length <= scale) s = s.PadLeft(scale + 1, '0');               // 1 @ 3 → "0001" → "0.001"
        return sign + s[..^scale] + "." + s[^scale..];                      // 99999 @ 3 → "99.999"
    }

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
    private static readonly ArgExprVisitor _argExprVisitor = new();
    private static BoundExpr ArgExpr(BoundOperand op) => op.Accept(_argExprVisitor);

    private sealed class ArgExprVisitor : IBoundOperandVisitor<BoundExpr>
    {
        // The former `_ =>` — a figurative / ALL-literal / boolean operand is not an arithmetic-intrinsic argument.
        private static BoundExpr Loud(BoundOperand n) => new BoundExprError($"intrinsic argument '{n.GetType().Name}'");
        public BoundExpr Visit(BoundComputedOperand n) => n.Expr;
        public BoundExpr Visit(BoundFieldOperand n) => new BoundNumRef(n.Place);
        public BoundExpr Visit(BoundNumericLiteral n) => new BoundNumLiteral(n.Text);
        public BoundExpr Visit(BoundStringLiteral n) => new BoundExprError("alphanumeric literal in an arithmetic intrinsic argument");
        public BoundExpr Visit(BoundOperandError n) => new BoundExprError(n.Feature);
        public BoundExpr Visit(BoundFigurative n) => Loud(n);
        public BoundExpr Visit(BoundAllLiteral n) => Loud(n);
        public BoundExpr Visit(BoundBoolOperand n) => Loud(n);
    }

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
    /// doubled quotes collapse — ISO §8.3.3.4). The one codec (the frozen-legacy SUBSCRIPT token path itself is
    /// deleted at PHASE 15; the decode delegates now so there is a SINGLE decoder — feedback_singular_pattern).</summary>
    private static string DecodeSubString(string raw) => CobolLiteral.Decode(raw);

    private static string SegText(List<IToken> toks) => string.Concat(toks.Select(t => t.Text)).Trim();
}
