// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// Intrinsic-function binding (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1). P7 Step 12: FUNCTION arguments
/// are REAL parse trees — <c>functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?</c>, each
/// <c>functionArgument</c> one of the §8.4.3.2 SR8 shapes (phrase word / OMITTED / non-numeric literal /
/// arithmetic expression) — and every arithmetic argument binds through the ONE
/// <see cref="ExpressionBinder.BindExpr"/> (nested <c>FUNCTION</c> calls recurse naturally through
/// <c>BindPrimary</c>). The keyword-omitted form <c>name(args)</c> (§8.4.3.2 SR2) still arrives as a
/// SUBSCRIPT-mode capture on a <c>dataReference</c> (D2 — a grammar alternative is an irreducible ambiguity);
/// <see cref="KeywordOmittedFunction"/> re-parses that captured text through the SAME <c>functionArgList</c>
/// rule (<c>FunctionArgFragment</c>), so both reference forms bind through one argument pipeline and the former
/// per-segment recursive-descent parser is DELETED. <c>table(ALL)</c> arguments expand at bind time to one
/// operand per occurrence (§15.3), detected from the argument's sole data reference whose subscript capture
/// (still SUBSCRIPT-mode — the D10/PHASE-15 deferral) holds a depth-0 ALL. ALL semantics — D8 edition gating,
/// §15.3 arity, MAX/MIN category resolution, the §15.68.3 r3 default-currency injection, the D7 LENGTH fold —
/// happen HERE; backends only render the resulting <see cref="BoundIntrinsicCall"/>.
/// P7 Step 10k: a real collaborator over <see cref="BinderContext"/>, landed TOGETHER with
/// <see cref="UdfBinder"/> (the bidirectional §12.3.8.2 GR12 pair: the user-function dispatch here PRECEDES
/// the catalog lookup and reaches <c>host.Udf</c>; UdfBinder's argument bind reaches BACK into
/// <see cref="BindArgOperand"/>). The D8 IntroducedIn/RemovedIn windows, the &lt;2002 keyword-omitted
/// routing gate, and TRIM-arg2 moved VERBATIM (Exec Step E folds the diagnostics; the &lt;2002 routing gate
/// at the top of <see cref="KeywordOmittedFunction"/> is PARSE-ROUTING behavior and stays a binder switch by
/// design — the premise audit's finding). <c>CompileClock</c> lives HERE (IntrinsicRenderer reads it).</summary>
internal sealed class IntrinsicBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The injectable compile-time clock for WHEN-COMPILED (§15.99.3 r2 — the COMPILATION timestamp;
    /// deep-dive D6). One capture per process: every unit compiled in this run shares the stamp, which the
    /// backend bakes into the generated source as a string constant.</summary>
    internal static Func<DateTimeOffset> CompileClock { get; set; } = () => DateTimeOffset.Now;

    /// <summary>FUNCTION call in an expression position (the <c>BindPrimary</c> hook).</summary>
    public BoundExpr BindIntrinsic(Core.FunctionCallContext fc) =>
        BindIntrinsicCore(fc.functionName().GetText(), ArgsOf(fc.functionArgList()));

    private static IReadOnlyList<Core.FunctionArgumentContext> ArgsOf(Core.FunctionArgListContext? list) =>
        list?.functionArgument() ?? [];

    /// <summary>FUNCTION call as a MOVE sending operand (the <c>BindMove</c> hook) — and the operand shape every
    /// general-operand channel shares: the bound expression wrapped as a <see cref="BoundComputedOperand"/> (a
    /// LENGTH fold surfaces as its literal; an error stays a loud named operand).</summary>
    public BoundOperand IntrinsicOperand(Core.FunctionCallContext fc) => OperandOf(BindIntrinsic(fc));

    public static BoundOperand OperandOf(BoundExpr e) => e switch
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
    public BoundExpr? KeywordOmittedFunction(Core.DataReferenceContext dref)
    {
        // Keyword omission via the REPOSITORY FUNCTION specifier is a COBOL-2002 introduction (§12.3.8) — below
        // 2002 the routing is inert, so the 85/NIST surface is byte-invariant (a lone name(args) stays a data
        // reference exactly as before). The FUNCTION-keyword form is unaffected at every edition.
        if (ctx.Edition.DialectLevel < 2002) return null;
        if (dref.cobolWord() is not { } cw) return null;                    // special registers (LINAGE/LINE/PAGE-COUNTER) are never functions
        var suffixes = dref.dataReferenceSuffix();
        if (suffixes.Length != 1 || suffixes[0].subscriptPart() is not { } sp) return null;   // exactly `name(args)` — no qualification / refmod tail
        string name = cw.GetText();
        bool isFn = ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase)
            || ((ctx.Data.RepositoryAllIntrinsic || ctx.Data.RepositoryIntrinsics.Contains(name))
                && IntrinsicCatalog.TryGet(name, out _));
        if (!isFn) return null;
        if (ctx.Symbols.TryResolve(name, ctx.ActiveScope, out _)) return null;   // a declared data item wins — never a mis-routed subscript
        return ReparseArgs(sp) is { } args
            ? BindIntrinsicCore(name, args)
            : new BoundExprError($"FUNCTION {name} arguments");
    }

    /// <summary>The D2 keyword-omitted argument re-parse: the SUBSCRIPT-mode captured argument text (verbatim
    /// from the source char stream, spacing intact) re-parses through the ONE <c>functionArgList</c> grammar
    /// rule via <see cref="FunctionArgFragment"/> — the same tokens, rule, and binding path as the
    /// FUNCTION-keyword form. Null (after a COBOLNET1543 diagnostic) on a malformed argument list.</summary>
    private IReadOnlyList<Core.FunctionArgumentContext>? ReparseArgs(Core.SubscriptPartContext sp)
    {
        if (sp.subscriptOrRefMod() is not { } som) return [];
        string text = som.Start.InputStream.GetText(
            new Antlr4.Runtime.Misc.Interval(som.Start.StartIndex, som.Stop.StopIndex));
        if (Frontend.Parsing.FunctionArgFragment.Parse(text, ctx.Edition.Edition) is { } frag)
            return ArgsOf(frag.functionArgList());
        ctx.Edition.Error("COBOLNET1543", $"malformed function-argument list '({text})' — an argument is an "
            + "identifier, a literal, a boolean expression, or an arithmetic expression (ISO §8.4.3.2 SR8)");
        return null;
    }

    /// <summary>Bind one FUNCTION reference from its name + argument parse trees (shared by the FUNCTION-keyword
    /// entry, the keyword-omitted re-parse, and — through <c>BindExpr</c>'s <c>BindPrimary</c> — every nested
    /// FUNCTION recursion).</summary>
    private BoundExpr BindIntrinsicCore(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4): an intrinsic-function-name synonym (EQUATE literal-2 /
        // SUBSTITUTE literal-5 whose canonical is an intrinsic) resolves to the canonical name; an intrinsic that
        // was UNDEFINE'd (literal-3) or SUBSTITUTE'd away (literal-4) is no longer a function. Only PURE
        // intrinsic-name synonyms reach here — reserved/context synonyms were already retyped by CobolWordsRewriter.
        // `cobolWordsRemoved` tests the ORIGINAL written name, so a SUBSTITUTE (literal-4 in DeReserved AND
        // literal-5→literal-4 in Synonyms) still resolves literal-5 to the intrinsic while literal-4 is removed.
        bool cobolWordsRemoved = !ctx.CobolWords.IsEmpty && ctx.CobolWords.DeReserved.Contains(name);
        if (!ctx.CobolWords.IsEmpty && ctx.CobolWords.Synonyms.TryGetValue(name, out var cwCanonical))
            name = cwCanonical;

        // §12.3.8.2 GR12 (:14885): within the environment division's scope, a REPOSITORY-declared
        // function-prototype-name refers to the USER-DEFINED function "and not to an intrinsic function of
        // the same name" (the spec's own factorial-override example, :43651) — so the user-function
        // dispatch PRECEDES the catalog. §8.4.6.6 adds the CONTAINING function definition's own name with
        // no repository declaration (self-recursion; a present self-entry is ignored, §12.3.8 GR11).
        if (ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase))
            return host.Udf.UdfBindCall(name, argCtxs);

        if (cobolWordsRemoved || !IntrinsicCatalog.TryGet(name, out var sig))
        {
            bool definedInGroup = host.UserFunctions?.ContainsKey(name) == true;
            ctx.Edition.Error("COBOLNET1501", $"FUNCTION {name.ToUpperInvariant()} is not an intrinsic function "
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
        if (sig.IntroducedIn > ctx.Edition.DialectLevel)
            ctx.Edition.Error("COBOLNET1502", $"FUNCTION {sig.Name} was introduced by ISO/IEC 1989:{sig.IntroducedIn} "
                + $"(§15) — it requires --std {sig.IntroducedIn} or later (targeting COBOL-{ctx.Edition.DialectLevel})");
        else if (sig.RemovedIn is { } gone && ctx.Edition.DialectLevel >= gone)
            ctx.Edition.Error("COBOLNET1503", $"FUNCTION {sig.Name} was removed by ISO/IEC 1989:{gone} — "
                + $"it is not available when targeting COBOL-{ctx.Edition.DialectLevel}");

        // Standard-arithmetic staging (P10 Step 12): under STANDARD / STANDARD-DECIMAL, §15.4.1 r1 makes a
        // function's returned value EQUAL its equivalent arithmetic expression evaluated in SDIDI form. The
        // exact-engine functions already satisfy that (every EAE step is exact in both engines — the family
        // header of CobolIntrinsics.Exact.cs carries the equivalence derivation), MEAN evaluates its one
        // division in SDIDI form (IntrinsicRenderer), and the prose-"approximation" functions (SQRT, the
        // trig/log family) have NO equivalent arithmetic expression — implementor-defined in every mode
        // (§15.4.1 last paragraph). The four double-engine functions whose EAEs carry inexact divisions are
        // the residue — staged LOUD so a standard-arithmetic program never silently gets native results.
        if (ctx.Data.Options.Arithmetic is ArithmeticMode.Standard or ArithmeticMode.StandardDecimal
            && sig.Name is "ANNUITY" or "PRESENT-VALUE" or "VARIANCE" or "STANDARD-DEVIATION")
            ctx.Edition.Error(DiagnosticCatalog.ArithmeticStandardIntrinsic,
                $"FUNCTION {sig.Name} under ARITHMETIC IS {(ctx.Data.Options.Arithmetic == ArithmeticMode.Standard ? "STANDARD" : "STANDARD-DECIMAL")} "
                + "is recognized but not yet implemented: §15.4.1 r1 requires the returned value to equal the "
                + "equivalent arithmetic expression evaluated in the standard-decimal intermediate (§8.8.1.5), "
                + "and this function's native IEEE-double evaluation cannot honor that equality");

        // A.4.9 LOCALE MODULE — DOCUMENTED NON-SUPPORT (ratified decision 3; conformant per ISO §4.2.7 +
        // Annex A §A.4.1: an implementation accepts an optional element's syntax ONLY when support is claimed).
        // The five locale FUNCTIONS bind Unsupported → a bind-time reject BY NAME (never the renderer's loud
        // backstop). STANDARD-COMPARE additionally rides §A.3 item 25 (the implementor need not accept the
        // syntax absent an ISO/IEC 14651:2020 implementation) — it is ordering-table-, not locale-, dependent.
        if (sig.Bind == IntrinsicBind.Unsupported)
            return LocaleUnsupported($"FUNCTION {sig.Name}", alsoA3: sig.Name == "STANDARD-COMPARE");

        // The LOCALE keyword variant of the otherwise-supported case/numeric functions — LOWER-CASE (§15.57),
        // UPPER-CASE (§15.97), NUMVAL-C (§15.68), TEST-NUMVAL-C (§15.94): the LOCALE phrase itself is A.4.9
        // (items 6/13/12; NUMVAL-C's LOCALE keyword is a spec Annex-A LIST OMISSION, disposed identically —
        // §15.94.3 r1 imports every §15.68.3 rule, and the phrase is inoperable without the A.4.9 SPECIAL-NAMES
        // LOCALE machinery). The function WITHOUT a LOCALE phrase binds exactly as before (zero regression).
        if (sig.Name is "LOWER-CASE" or "UPPER-CASE" or "NUMVAL-C" or "TEST-NUMVAL-C" && HasLocalePhrase(argCtxs))
            return LocaleUnsupported($"the LOCALE phrase of FUNCTION {sig.Name}");

        // TRIM (§15.96) — the one §15 function whose argument list carries a phrase keyword (LEADING/TRAILING);
        // its bespoke shape is bound apart from the generic comma/space-split argument path.
        if (sig.Name == "TRIM") return BindTrim(sig, argCtxs);

        // FIND-STRING (§15.37) — argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]: phrase
        // keywords interleaved with operands, bound apart from the generic argument path.
        if (sig.Name == "FIND-STRING") return BindFindString(sig, argCtxs);

        // SUBSTITUTE (§15.87) — argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …: per-pair phrase
        // keywords ahead of each replacement pair, bound apart from the generic argument path.
        if (sig.Name == "SUBSTITUTE") return BindSubstitute(sig, argCtxs);

        // CONVERT (§15.19) — argument-1 source-format destination-format: two keyword groups after the operand,
        // bound apart from the generic argument path (the result category is computed per §15.19.1).
        if (sig.Name == "CONVERT") return BindConvert(sig, argCtxs);

        // MODULE-NAME (§15.65) — one mandatory keyword (ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL), resolved
        // from the runtime module call-name stack; bound apart from the generic argument path.
        if (sig.Name == "MODULE-NAME") return BindModuleName(sig, argCtxs);

        // NUMVAL-C / TEST-NUMVAL-C (§15.68.2 / §15.94.2) — the optional ANYCASE keyword (orthogonal to the
        // argument-2 currency; §15.94.3 r1 imports every §15.68.3 argument rule) + the §15.68.3 r3
        // compilation-unit currency injection; bound apart from the generic argument path. The LOCALE phrase
        // is the A.4.9 documented non-support (decision 3 — the P11 Step-8 disposition).
        if (sig.Name is "NUMVAL-C" or "TEST-NUMVAL-C") return BindNumvalCFamily(sig, argCtxs);

        // EXCEPTION-FILE / EXCEPTION-FILE-N with a file-connector-name argument (§15.28.4 r2 / §15.29.4 r2,
        // COBOL-2023 — E.3.3 items 25/26): the argument is an FD file-name (a file connector, NOT a data
        // reference — it reports the NAMED connector's I-O status); bound apart from the generic operand path.
        if (sig.Name is "EXCEPTION-FILE" or "EXCEPTION-FILE-N" && argCtxs.Count == 1)
            return BindExceptionFileArg(sig, argCtxs[0]);

        var args = BindIntrinsicArgs(argCtxs);
        if (args.Count < sig.MinArgs || args.Count > sig.MaxArgs)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes "
                + (sig.MinArgs == sig.MaxArgs ? $"{sig.MinArgs}" : sig.MaxArgs == int.MaxValue ? $"at least {sig.MinArgs}" : $"{sig.MinArgs}..{sig.MaxArgs}")
                + $" argument(s); {args.Count} given (ISO §15.3)");
            return new BoundExprError($"FUNCTION {sig.Name} arity");
        }

        // §15.38–15.41 / §15.48 / §15.79 / §15.92 rule 1: the date/time FORMAT (argument-1) shall be a LITERAL —
        // the format is analyzed/derived at compile time (SECONDS-FROM-FORMATTED-TIME needs the fraction scale).
        if (args.Count > 0 && args[0] is not BoundStringLiteral
            && sig.Name is "FORMATTED-CURRENT-DATE" or "FORMATTED-DATE" or "FORMATTED-DATETIME" or "FORMATTED-TIME"
                or "INTEGER-OF-FORMATTED-DATE" or "SECONDS-FROM-FORMATTED-TIME" or "TEST-FORMATTED-DATETIME")
            ctx.Edition.Error("COBOLNET1517", $"FUNCTION {sig.Name} argument-1 shall be a literal date/time format "
                + "(ISO §15 — the FORMATTED-*/INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/"
                + "TEST-FORMATTED-DATETIME format is a literal)");

        // FUNCTION LENGTH folds at compile time from PIC metadata (§15.50; deep-dive D7).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "LENGTH")
            return BindLengthFold(sig, args);

        // FUNCTION BYTE-LENGTH folds at compile time from the item's declared BYTE geometry (§15.14; the D7
        // byte-vs-position twin of LENGTH).
        if (sig.Bind == IntrinsicBind.Fold && sig.Name == "BYTE-LENGTH")
            return BindByteLengthFold(sig, args);

        // SMALLEST/HIGHEST/LOWEST-ALGEBRAIC fold at compile time from the argument item's PICTURE metadata
        // (§15.83/§15.43/§15.58; the same Fold discipline as LENGTH).
        if (sig.Bind == IntrinsicBind.Fold
                && sig.Name is "SMALLEST-ALGEBRAIC" or "HIGHEST-ALGEBRAIC" or "LOWEST-ALGEBRAIC")
            return BindAlgebraicFold(sig, args);

        // MAX/MIN are category-polymorphic (§15.59/§15.63: the result follows the arguments — all-alphanumeric
        // arguments return the SELECTED STRING); ORD-MAX/ORD-MIN always return an ordinal but dispatch their
        // comparison by the same argument category. The legacy rule: all-non-numeric ⇒ the string family.
        var resolved = sig;
        var category = sig.ResultCategory;
        // §15.97.1 (UPPER-CASE) / §15.57.1 (LOWER-CASE) / §15.78.1 (REVERSE) result-type tables: the result category
        // FOLLOWS the argument — a National argument yields a National result (the transform is a code-unit op on the
        // national UTF-16 string, so the same RuntimeMethod body applies). Hardcoding Alphanumeric in the catalog
        // mis-labelled it, bypassing the §14.9.25.4 Table-16 National→Alphanumeric MOVE guard and feeding the wrong
        // class to comparison collation. (CA25 — mirrors the V54 MAX/MIN category resolution.)
        if (sig.RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"
                && args.Count > 0 && OperandCategory(args[0]) is PicCategory.National)
            category = PicCategory.National;
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
            // §15.59.1 (MAX) / §15.63.1 (MIN) result-type table: the result category FOLLOWS the arguments — an
            // all-national argument list yields a NATIONAL result (the selected national UTF-16 string). Hardcoding
            // Alphanumeric mis-labelled it, bypassing the §14.9.25.4 Table-16 National→Alphanumeric MOVE guard and
            // feeding the wrong class to comparison collation. (V54.)
            if (sig.Name is "MAX" or "MIN")
                category = args.All(a => OperandCategory(a) is PicCategory.National)
                    ? PicCategory.National : PicCategory.Alphanumeric;
        }

        // CHAR/ORD are PCS-relative (§15.15.4 r2 / §15.70.4 r1): flag the call when a NON-identity ALPHANUMERIC
        // program collating sequence is in effect so the backend passes its weights table — and only then
        // (hazard H5: the emitted __COLLATE field exists only under a non-identity PCS; STANDARD-1/2/NATIVE
        // normalize to identity). A NATIONAL ORD argument reads the NATIONAL program collating sequence instead
        // (§15.70.4 r2), and CHAR-NATIONAL always does (§15.16.4) — so the alphanumeric weights table must NOT
        // be passed for those (its 256-entry domain would alias national characters): they take the H5-twin
        // CollateNat flag, set only under a NON-native national sequence (an ALPHABET … FOR NATIONAL literal
        // phrase — NATIVE/UCS-4 are the D-N3 code-unit identity the parameterless runtime bodies realize).
        bool nationalArg = args.Any(a => OperandCategory(a) is PicCategory.National);
        // MAX/MIN/ORD-MAX/ORD-MIN over string arguments compare by the SAME PCS as CHAR/ORD (§15.59.4 r1 → §8.8.4.2.7
        // alphanumeric / §8.8.4.2.9 national) — flag the STRING form (resolved above) so the backend passes __COLLATE /
        // __COLLATE_NAT, else it silently uses the native ordinal and disagrees with the program's relation conditions. (CA23.)
        bool maxMinStr = resolved.RuntimeMethod is "MaxString" or "MinString" or "OrdMaxString" or "OrdMinString";
        bool collate = (sig.Name is "CHAR" or "ORD" || maxMinStr) && ctx.Data.Collating is not null && !nationalArg;
        bool collateNat = ctx.Data.NationalCollating is not null
            && (sig.Name is "CHAR-NATIONAL" || ((sig.Name is "ORD" || maxMinStr) && nationalArg));

        // CHAR takes an INTEGER argument (§15.15.3 r1) — a national operand is a category violation; the
        // national ordinal→character direction is FUNCTION CHAR-NATIONAL (§15.16, implemented — the P10
        // Step-11 EC-N wave). Rejected BY NAME at bind — never a wrong ordinal through the numeric channel.
        if (sig.Name is "CHAR" && nationalArg)
            ctx.Edition.Error("COBOLNET0844", "FUNCTION CHAR takes an integer operand (§15.15.3 rule 1) — "
                + "FUNCTION CHAR-NATIONAL (§15.16) is the national program-collating-sequence form");

        // DISPLAY-OF / NATIONAL-OF (§15.26.3 / §15.66.3) — the argument class/category rules of the sanctioned
        // national↔alphanumeric repertoire pair.
        if (sig.Name is "DISPLAY-OF" or "NATIONAL-OF") CheckRepertoireArgs(sig, args);

        // A FUNCTION EXCEPTION-* reference reads the runtime last-exception register (§15.28–15.33) — flag the
        // program's EC usage so the generated source carries the Exceptions using (the group EC gate).
        if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) host.Ec.EcNoteFunction();

        return new BoundIntrinsicCall(resolved, args, category, collate) { CollateNat = collateNat };
    }

    /// <summary>Bind FUNCTION EXCEPTION-FILE(file-connector-name) / EXCEPTION-FILE-N(...) (§15.28.4 r2 / §15.29.4 r2,
    /// COBOL-2023, E.3.3 items 25/26): argument-1 is an FD file-name (a file connector, §15.28.3 rule 1) resolved to
    /// its <see cref="FileModel"/>; the runtime reports that connector's I-O status + SELECT-spelled name (r2b), or
    /// two spaces when it was never opened/attempted/accessed (r2a). Introduction-gated at 2023 (COBOLNET0900).</summary>
    private BoundExpr BindExceptionFileArg(IntrinsicSig sig, Core.FunctionArgumentContext argCtx)
    {
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink,
            sig.Name == "EXCEPTION-FILE" ? Constructs.ExceptionFileArgument2023 : Constructs.ExceptionFileNArgument2023,
            $"FUNCTION {sig.Name}(file-connector-name)");
        string name = argCtx.GetText().Trim();
        var file = ctx.Data.Files.FirstOrDefault(f => f.CobolName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (file is null)
        {
            ctx.Edition.Error("COBOLNET1574", $"FUNCTION {sig.Name} argument '{name}' is not the name of a file "
                + "connector specified in an FD statement (ISO §15.28.3 rule 1)");
            return new BoundExprError($"FUNCTION {sig.Name} argument");
        }
        host.Ec.EcNoteFunction();
        return new BoundIntrinsicCall(sig, [], sig.ResultCategory) { FileArg = file };
    }

    /// <summary>TRIM (§15.96) — a phrase keyword in the argument list (<c>[LEADING|TRAILING]</c>, §15.96.2),
    /// extracted here (the <see cref="KeywordWordOf"/> bare-word view) so the remaining arguments bind as
    /// ordinary operands: argument-1 (the string, required) followed by zero-or-more single-character
    /// argument-2 trim characters (§15.96.3 rule 2 — the default when none is given is a space, rule 3.a).
    /// The LEADING/TRAILING mode rides on the bound node's <see cref="BoundIntrinsicCall.TrimMode"/>
    /// (0 = both, rule 3).</summary>
    private BoundExpr BindTrim(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        int mode = 0;   // 0 = both leading+trailing (r3), 1 = LEADING (r1), 2 = TRAILING (r2)
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                case "LEADING": mode = 1; continue;
                case "TRAILING": mode = 2; continue;
            }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count == 0)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION TRIM takes at least a string argument-1 (ISO §15.96.2/.3)");
            return new BoundExprError("FUNCTION TRIM");
        }
        // The argument-2 form (delete characters OTHER than space) is a 2023 enhancement — TRIM removed only
        // spaces through 2014 (Annex E.3.3 item 31; VERSION_CHANGE_REFERENCE row 74). Its introduction is gated
        // by name+edition, like the whole-function 1502 gate above.
        if (operands.Count > 1 && ctx.Edition.DialectLevel < 2023)
            ctx.Edition.Error("COBOLNET1502", "the FUNCTION TRIM argument-2 form (removing characters other than "
                + "space) was introduced by ISO/IEC 1989:2023 (§15.96; Annex E.3.3 item 31) — it requires "
                + $"--std 2023 or later (targeting COBOL-{ctx.Edition.DialectLevel}); TRIM removed only spaces through 2014");
        return new BoundIntrinsicCall(sig, operands, PicCategory.Alphanumeric) { TrimMode = mode };
    }

    /// <summary>FIND-STRING (§15.37) — <c>argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE]</c>
    /// (§15.37.2). The phrase words arrive as bare-word arguments (<see cref="KeywordWordOf"/>): LAST selects the
    /// last occurrence (rule 1); ANYCASE folds case (rule 4); START/AFTER are the optional argument-3 introducer
    /// (noise — argument-3's mere presence selects the skip form, rule 2). The operand arguments are argument-1
    /// (the haystack), argument-2 (the needle), and the optional integer argument-3 (the number of matches to
    /// ignore). The two flags ride on <see cref="BoundIntrinsicCall.FindLast"/> /
    /// <see cref="BoundIntrinsicCall.Anycase"/>; argument-3 (if given) is the third operand.</summary>
    private BoundExpr BindFindString(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        bool last = false, anycase = false;
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                case "LAST": last = true; continue;
                case "ANYCASE": anycase = true; continue;
                // START AFTER — the argument-3 introducer words (§15.37.2); the integer that follows is argument-3.
                case "START" or "AFTER": continue;
            }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count is < 2 or > 3)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 "
                + $"[[START AFTER] argument-3] (ISO §15.37.2); {operands.Count} operand argument(s) given");
            return new BoundExprError("FUNCTION FIND-STRING");
        }
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric) { FindLast = last, Anycase = anycase };
    }

    /// <summary>SUBSTITUTE (§15.87) — <c>argument-1 { [ANYCASE] [FIRST|LAST] argument-2 argument-3 } …</c>
    /// (§15.87.2). argument-1 is the source; each following group is a replacement PAIR (argument-2 matched,
    /// argument-3 substituted) preceded by its own optional phrase keywords: ANYCASE (case fold, rule 5),
    /// FIRST (first occurrence only, rule 3.a) or LAST (last only, rule 3.b) — default replaces ALL occurrences.
    /// The keyword words arrive as bare-word arguments (<see cref="KeywordWordOf"/>); each pair's mode (bits
    /// 0=FIRST/1=LAST/2=ANYCASE) rides on <see cref="BoundIntrinsicCall.SubstituteModes"/>, and
    /// <see cref="BoundIntrinsicCall.Args"/> carries [source, from₁, to₁, from₂, to₂, …].</summary>
    private BoundExpr BindSubstitute(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        var operands = new List<BoundOperand>();   // [source, from₁, to₁, from₂, to₂, …]
        var modes = new List<int>();               // one mode per completed pair
        int pending = 0;                           // phrase flags accumulating for the NEXT pair
        bool haveSource = false;
        int pairOperands = 0;                      // operands seen since the last completed pair
        BoundExpr Malformed() { ctx.Edition.Error("COBOLNET1504", "FUNCTION SUBSTITUTE takes argument-1 and one "
            + "or more [ANYCASE][FIRST|LAST] argument-2 argument-3 pairs (ISO §15.87.2)"); return new BoundExprError("FUNCTION SUBSTITUTE"); }

        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                case "ANYCASE": pending |= 4; continue;
                case "FIRST": pending |= 1; continue;
                case "LAST": pending |= 2; continue;
            }
            var op = BindArgOperand(a);
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
    /// each a bare-word argument (§15.19.2; <see cref="KeywordWordOf"/> — ANY/ALPHANUMERIC/NATIONAL are reserved
    /// words, the rest arrive as bare names). The operand binds ordinarily; the format words ride the node's
    /// <c>Convert*</c> init-properties. The argument/SR rules (§15.19.3) are enforced here (COBOLNET1514); the
    /// result category (§15.19.1) is National for a NAT destination, Alphanumeric otherwise.</summary>
    private BoundExpr BindConvert(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        var kws = new List<string>();
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (KeywordWordOf(a) is { } w && IsConvertFormatWord(w)) { kws.Add(w); continue; }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count != 1 || kws.Count < 2)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION CONVERT takes argument-1 source-format destination-format "
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
            ctx.Edition.Error("COBOLNET1514", $"FUNCTION CONVERT: '{string.Join(' ', kws)}' is not a valid "
                + "source-format destination-format pair (ISO §15.19.2 — ANY|ANUM|HEX|NAT then ANUM|NAT [HEX] | BYTE)");
            return new BoundExprError("FUNCTION CONVERT");
        }
        // SR3 — source shall differ from destination (only ANUM→ANUM / NAT→NAT with no HEX collide, §15.19.3).
        if ((src == 1 && dst == 1 && !hex) || (src == 3 && dst == 3 && !hex))
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the source-format and destination-format are the "
                + "same (ISO §15.19.3 SR3)");
        // SR8 — an ANY source requires an ANUM HEX or NAT HEX destination.
        if (src == 0 && !(hex && dst is 1 or 3))
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a source-format of ANY requires a destination of "
                + "ANUM HEX or NAT HEX (ISO §15.19.3 SR8)");
        // SR9 — a BYTE destination requires a HEX source.
        if (dst == 4 && src != 2)
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a destination-format of BYTE requires a "
                + "source-format of HEX (ISO §15.19.3 SR9)");
        // SR1 — argument-1 shall not be zero length (compile-time catch for an empty literal).
        if (operands[0] is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: argument-1 is of zero length (ISO §15.19.3 SR1)");

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
    private static bool IsNationalOperand(BoundOperand op) => OperandCategory(op) is PicCategory.National;

    /// <summary>The statically knowable data category of a function argument (drives the §15.3 per-function
    /// argument class/category rules): a categorized literal, a non-group field reference (a reference-modified
    /// operand keeps its item's category — a national item's ref-mod is category national, §8.4.4.4), a nested
    /// intrinsic's result category, or numeric for a computed arithmetic expression. Null = no fixed static
    /// class (groups, figuratives, ALL literals, error operands) — the caller skips its check.</summary>
    private static PicCategory? OperandCategory(BoundOperand op) => op switch
    {
        BoundStringLiteral sl => sl.Category,
        BoundNumericLiteral => PicCategory.Numeric,
        BoundFieldOperand f => f.Place.Item.IsGroup ? null : f.Place.Item.Pic?.Category,
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory,
        BoundComputedOperand => PicCategory.Numeric,
        _ => null,
    };

    /// <summary>The statically knowable width (character positions) of a function argument, or null when the
    /// length exists only at runtime (ref-mod views, ANY LENGTH items, computed results).</summary>
    private static int? KnownWidth(BoundOperand op) => op switch
    {
        BoundStringLiteral sl => sl.Value.Length,
        BoundFieldOperand { Place: RefModPlace } => null,
        BoundFieldOperand { Place.Item: { IsGroup: false, IsAnyLength: false } item } => item.ImageWidth,
        _ => null,
    };

    /// <summary>DISPLAY-OF / NATIONAL-OF argument rules — DISPLAY-OF (§15.26.3): argument-1 shall be of class
    /// national (r1); argument-2 shall be of class alphabetic or alphanumeric and one character position in
    /// length (r2 — the alphanumeric SUBSTITUTION character). NATIONAL-OF (§15.66.3): argument-1 shall be of
    /// class alphabetic or alphanumeric (r1) and shall not be a zero-length literal (r3); argument-2 shall be
    /// of category national and one character in length (r2 — the national substitution character). Class
    /// alphanumeric spans the alphanumeric, alphanumeric-edited, and numeric-edited categories (§8.4.2 table 2);
    /// the checks fire on the statically classifiable shapes and skip shapes with no fixed class.</summary>
    private void CheckRepertoireArgs(IntrinsicSig sig, List<BoundOperand> args)
    {
        bool toNat = sig.Name == "NATIONAL-OF";

        if (toNat && args[0] is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-1 shall not be a zero-length "
                + "literal (ISO §15.66.3 rule 3)");

        if (OperandCategory(args[0]) is { } c1)
        {
            if (!toNat && c1 is not PicCategory.National)
                ctx.Edition.Error("COBOLNET1546", "FUNCTION DISPLAY-OF: argument-1 shall be of class national "
                    + "(ISO §15.26.3 rule 1) — FUNCTION NATIONAL-OF is the alphanumeric→national conversion");
            else if (toNat && c1 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited))
                ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-1 shall be of class alphabetic "
                    + "or alphanumeric (ISO §15.66.3 rule 1) — FUNCTION DISPLAY-OF is the national→alphanumeric "
                    + "conversion");
        }

        if (args.Count < 2) return;
        if (OperandCategory(args[1]) is { } c2)
        {
            if (!toNat && c2 is not (PicCategory.Alphanumeric or PicCategory.NumericEdited))
                ctx.Edition.Error("COBOLNET1546", "FUNCTION DISPLAY-OF: argument-2 (the substitution character) "
                    + "shall be of class alphabetic or alphanumeric (ISO §15.26.3 rule 2)");
            else if (toNat && c2 is not PicCategory.National)
                ctx.Edition.Error("COBOLNET1546", "FUNCTION NATIONAL-OF: argument-2 (the substitution character) "
                    + "shall be of category national (ISO §15.66.3 rule 2)");
        }
        if (KnownWidth(args[1]) is { } w && w != 1)
            ctx.Edition.Error("COBOLNET1546", $"FUNCTION {sig.Name}: argument-2 (the substitution character) "
                + $"shall be one character position in length, not {w} "
                + $"(ISO {(toNat ? "§15.66.3" : "§15.26.3")} rule 2)");
    }

    /// <summary>The §15.19.2 CONVERT format words (reserved within the argument list, like TRIM's LEADING/TRAILING).</summary>
    private static bool IsConvertFormatWord(string w) =>
        w.ToUpperInvariant() is "ANY" or "ALPHANUMERIC" or "ANUM" or "HEX" or "NATIONAL" or "NAT" or "BYTE";

    /// <summary>MODULE-NAME (§15.65) — one mandatory phrase keyword selecting a runtime element of the running
    /// COBOL hierarchy (§15.65.2: ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL; <c>TOP-LEVEL</c> is one hyphenated
    /// word). The selector rides on <see cref="BoundIntrinsicCall.ModuleNameKind"/>; the value resolves at
    /// runtime from the module call-name stack (<c>CobolModule</c>). NESTED requires a contained program
    /// (§15.65.3 argument rule 1) — a compile-time conformance check.</summary>
    private BoundExpr BindModuleName(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        int kind = -1;
        foreach (var a in argCtxs)
        {
            int k = KeywordWordOf(a) switch
                { "CURRENT" => 0, "ACTIVATING" => 1, "NESTED" => 2, "STACK" => 3, "TOP-LEVEL" => 4, _ => -1 };
            if (k < 0 || kind >= 0)
            {
                ctx.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME takes exactly one keyword argument "
                    + $"(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2), not '{a.GetText()}'");
                return new BoundExprError("FUNCTION MODULE-NAME");
            }
            kind = k;
        }
        if (kind < 0)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION MODULE-NAME requires one keyword argument "
                + "(ACTIVATING/CURRENT/NESTED/STACK/TOP-LEVEL) (ISO §15.65.2)");
            return new BoundExprError("FUNCTION MODULE-NAME");
        }
        // §15.65.3 argument rule 1 — NESTED only within a nested (contained) program.
        if (kind == 2 && !host.InNestedProgram)
            ctx.Edition.Error("COBOLNET1515", "FUNCTION MODULE-NAME NESTED shall be specified only within a "
                + "nested program (ISO §15.65.3 argument rule 1) — this compilation unit is not contained");
        return new BoundIntrinsicCall(sig, [], PicCategory.Alphanumeric) { ModuleNameKind = kind };
    }

    /// <summary>The ONE A.4.9 locale-module documented-non-support diagnostic (ratified decision 3; conformant
    /// per ISO §4.2.7 + Annex A §A.4.1 — an implementation accepts an optional element's syntax only when
    /// support is claimed). Shared by the five locale FUNCTIONS (<c>Bind == Unsupported</c>) and by the LOCALE
    /// phrase of the otherwise-supported LOWER-CASE/UPPER-CASE/NUMVAL-C/TEST-NUMVAL-C. <paramref name="element"/>
    /// names the specific element; <paramref name="alsoA3"/> adds the §A.3 item 25 citation (STANDARD-COMPARE,
    /// which is ISO/IEC 14651:2020-ordering-dependent, not locale-dependent).</summary>
    private BoundExpr LocaleUnsupported(string element, bool alsoA3 = false)
    {
        string extra = alsoA3 ? " and §A.3 item 25 (dependent on an ISO/IEC 14651:2020 implementation)" : "";
        ctx.Edition.Error("COBOLNET1518", $"{element} is in the optional locale module (ISO/IEC 1989:2023 "
            + $"Annex A §A.4.9{extra}), which COBOL.NET does not support — documented non-support, conformant "
            + "per ISO §4.2.7 / §A.4.1. Use a supported alternative (e.g. STANDARD-1/STANDARD-2 collating, "
            + "FORMATTED-DATE/-TIME, or NUMVAL-C without the LOCALE phrase).");
        return new BoundExprError($"{element} (A.4.9 locale, not supported)");
    }

    /// <summary>Detect an A.4.9 <c>LOCALE</c> phrase in a function's argument list — the keyword appears as a
    /// bare-word argument (via <see cref="KeywordWordOf"/>) at argument position 2 or later (never argument-1,
    /// the operand): <c>LOWER-CASE(arg-1 LOCALE locale-name-1)</c> (§15.57.2), <c>NUMVAL-C(arg-1 LOCALE
    /// [locale-name-1])</c> (§15.68.2). LOCALE is not a reserved word, so the phrase parses as extra space- or
    /// comma-separated arguments; the argument-1 exclusion avoids a false positive on a data item happening to
    /// be named LOCALE.</summary>
    private static bool HasLocalePhrase(IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        for (int i = 1; i < argCtxs.Count; i++)
            if (KeywordWordOf(argCtxs[i]) == "LOCALE") return true;
        return false;
    }

    /// <summary>NUMVAL-C / TEST-NUMVAL-C (§15.68 / §15.94) — argument-1, then EITHER the argument-2 currency
    /// string OR the LOCALE phrase (stacked alternatives, §15.68.2/§15.94.2), plus the orthogonal optional
    /// ANYCASE keyword (§15.68.3 r4f — the currency match performed as if both sides were lowercased; rides
    /// the ONE <see cref="BoundIntrinsicCall.Anycase"/> flag). With neither argument-2 nor LOCALE there is
    /// exactly ONE currency string for the compilation unit — the SPECIAL-NAMES CURRENCY string or the default
    /// sign (§15.68.3 r3) — injected HERE at bind time so the SPECIAL-NAMES config stays out of the backend
    /// (bound nodes carry complete semantics). A LOCALE bare word is not consumed here — it falls to the
    /// ordinary operand bind (an unresolved name) until the P11 Step-8 A.4.9 disposition claims it.</summary>
    private BoundExpr BindNumvalCFamily(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        bool anycase = false;
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (KeywordWordOf(a) == "ANYCASE") { anycase = true; continue; }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count is < 1 or > 2)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes argument-1 [argument-2] [ANYCASE] "
                + $"(ISO §{(sig.Name == "NUMVAL-C" ? "15.68.2" : "15.94.2")}); {operands.Count} operand argument(s) given");
            return new BoundExprError($"FUNCTION {sig.Name} arity");
        }
        if (operands.Count == 1)
            operands.Add(new BoundStringLiteral(ctx.Data.CurrencyString));   // §15.68.3 r3
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric) { Anycase = anycase };
    }

    /// <summary>An operand whose comparison/result category is alphanumeric (drives MAX/MIN resolution): a string
    /// literal, an alphanumeric/edited/group item, or a nested alphanumeric-result intrinsic.</summary>
    private static bool IsStringOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        // National/boolean operands participate through the char pipeline (MAX/MIN over national compares
        // ordinal per D-N3); a nested intrinsic with a string-class result (alphanumeric OR national —
        // NATIONAL-OF/CONVERT-to-NAT) is a string operand likewise.
        BoundFieldOperand f => f.Place.Item.IsGroup
            || f.Place.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean,
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } } => true,
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
        // An ANY LENGTH item's length exists only at RUNTIME (ISO §13.18.2 GR1 — n = the length of the
        // corresponding argument): never the compile-time fold, always the runtime .Length over the carrier
        // (the same BoundIntrinsicCall channel a nested string-function argument uses).
        BoundFieldOperand { Place.Item.IsAnyLength: true } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        // A DYNAMIC LENGTH item's LENGTH is its CURRENT length in BYTES (ISO §15.50.4 rule 6 — unlike the
        // character-position count of rules 2/3), never a compile-time fold. For a PIC X item one byte per
        // character position, so the runtime .Length over the native string IS the answer (the ANY LENGTH
        // channel). A PIC N (national) item is 2 bytes per position (D-N1) — the byte count is 2 × positions,
        // and the plain .Length is the character count (half): staged loud by name until the doubling is wired
        // (no national dynamic-length consumer in the corpus; §1.4).
        BoundFieldOperand { Place.Item: { IsDynamicLength: true, Pic.Category: PicCategory.National } } =>
            new BoundExprError("FUNCTION LENGTH of a NATIONAL DYNAMIC LENGTH item (current length in bytes = 2× character positions, ISO §15.50.4 rule 6)"),
        BoundFieldOperand { Place.Item.IsDynamicLength: true } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
        BoundFieldOperand f => new BoundNumLiteral(Math.Max(1, f.Place.Item.ImageWidth).ToString()),
        // A nested string-result intrinsic (alphanumeric OR national — one UTF-16 char per national position,
        // D-N1, so .Length IS the §15.50.4 character-position count for both) keeps a runtime .Length.
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),   // runtime .Length over the nested result image
        // A numeric / figurative / ALL literal is NOT a valid LENGTH argument — §15.50.3 item 1 restricts a LITERAL
        // argument to "an alphanumeric, national, or boolean literal" (a numeric *data item* is allowed as "a data
        // item of any class or category", handled by the BoundFieldOperand arm above). So this error is spec-correct.
        _ => new BoundExprError("FUNCTION LENGTH argument (a numeric/figurative literal is not a valid argument, ISO §15.50.3)"),
    };

    /// <summary>FUNCTION BYTE-LENGTH (§15.14.4 r1): the argument's length in BYTES — the compile-time twin of the
    /// LENGTH fold, counting bytes instead of character positions (D7). §15.14.3 r1 restricts a LITERAL argument
    /// to an ALPHANUMERIC or NATIONAL literal (unlike LENGTH, a boolean/numeric literal is NOT valid): an
    /// alphanumeric literal is 1 byte/char, a national literal 2 bytes/char (D-N1). A fixed data item folds to
    /// <see cref="DataItem.ByteWidth"/> (the pinned per-usage widths). Runtime-length shapes — a reference-modified
    /// view, a variable-length (OCCURS DEPENDING) group, or an ANY LENGTH item (§15.14.4 r2/r5) — have a byte
    /// length known only at runtime; with no runtime BYTE-LENGTH body (the §15.14 CONSTANT-entry path aside) they
    /// stage LOUD by name, the LENGTH discipline (§1.4).</summary>
    private BoundExpr BindByteLengthFold(IntrinsicSig sig, List<BoundOperand> args) => args[0] switch
    {
        BoundStringLiteral { Category: PicCategory.National } s => new BoundNumLiteral((2 * Math.Max(1, s.Value.Length)).ToString()),
        BoundStringLiteral { Category: PicCategory.Alphanumeric } s => new BoundNumLiteral(Math.Max(1, s.Value.Length).ToString()),
        BoundStringLiteral =>
            new BoundExprError("FUNCTION BYTE-LENGTH literal argument (only an alphanumeric or national literal is a valid argument, ISO §15.14.3)"),
        BoundFieldOperand { Place: RefModPlace } =>
            new BoundExprError("FUNCTION BYTE-LENGTH of a reference-modified argument (runtime length, §15.14.4)"),
        BoundFieldOperand f when f.Place.Item.IsGroup
                && OdoModel.TableUnder(f.Place.Item) is { OccursSpec.Depending: not null } =>
            new BoundExprError("FUNCTION BYTE-LENGTH of a variable-length (OCCURS DEPENDING) group (runtime length, §15.14.4 r6)"),
        BoundFieldOperand { Place.Item.IsAnyLength: true } =>
            new BoundExprError("FUNCTION BYTE-LENGTH of an ANY LENGTH item (runtime length, ISO §13.18.2)"),
        // A DYNAMIC LENGTH item's BYTE-LENGTH is its CURRENT byte length at RUNTIME (ISO §15.14.4 rule 5), not the
        // compile-time ByteWidth (0 for a variable-length item) — staged loud by name until a runtime BYTE-LENGTH
        // body exists (the ANY LENGTH discipline; §1.4).
        BoundFieldOperand { Place.Item.IsDynamicLength: true } =>
            new BoundExprError("FUNCTION BYTE-LENGTH of a DYNAMIC LENGTH item (current length in bytes at runtime, ISO §15.14.4 rule 5)"),
        BoundFieldOperand f => new BoundNumLiteral(Math.Max(1, f.Place.Item.ByteWidth).ToString()),
        _ => new BoundExprError("FUNCTION BYTE-LENGTH argument (a numeric/figurative literal is not a valid argument, ISO §15.14.3)"),
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
            ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name} does not support a floating-point argument "
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
            var (cap, frac) = CobolNet.Runtime.CobolEdit.MaskCapacity(pic.EditMask!, ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma);
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
            // Zero, but AT THE ITEM'S SCALE — routed through Decimalize rather than a bare "0" literal so the
            // folded text carries the scale (a 9V99 item's lowest value is "0.00", not "0"), matching the runtime
            // rule and keeping the literal's precision for any arithmetic it feeds.
            : new BoundNumLiteral(Decimalize(System.Numerics.BigInteger.Zero, scale, negative: false));
    }

    private BoundExpr AlgebraicArgError(IntrinsicSig sig)
    {
        string cat = sig.Name == "SMALLEST-ALGEBRAIC" ? "category numeric" : "category numeric or numeric-edited";
        string sec = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3" : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
        ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name} argument-1 shall be a {cat} DATA ITEM — not a "
            + "literal, an arithmetic expression, a group item, a reference-modified item, an index, or another "
            + $"function (ISO §{sec} rule 1)");
        return new BoundExprError($"FUNCTION {sig.Name} argument");
    }

    private static System.Numerics.BigInteger Pow10(int n) => System.Numerics.BigInteger.Pow(10, Math.Max(0, n));

    /// <summary>Render an unscaled BigInteger at <paramref name="scale"/> fractional digits as a decimal literal
    /// string ('.' radix always — an internal C#-facing literal, never COBOL source, so DECIMAL-POINT IS COMMA
    /// does not apply). A negative scale (trailing P) appends |scale| zeros; a positive scale inserts the point.
    /// <para>
    /// ⛔ DELEGATES to <see cref="CobolNet.Runtime.CobolNum.FormatFunctionText"/> — the SAME rule the RUNTIME uses
    /// to render a computed intrinsic's value as text (DA2). These are the two halves of one job: this one folds a
    /// constant-argument intrinsic to a literal at COMPILE time, that one renders a runtime-computed result, and a
    /// COBOL programmer cannot tell which fired. Two hand-written copies of the rule is precisely the
    /// two-mechanisms anti-pattern, and they HAD already drifted: this method early-returned <c>"0"</c> for a zero
    /// magnitude and so DROPPED the scale, making <c>LOWEST-ALGEBRAIC</c> of an unsigned scaled item fold to
    /// <c>"0"</c> where the runtime rule gives <c>"0.00"</c> — a literal at the wrong scale, which then feeds
    /// subsequent arithmetic. Delegation removes the copy rather than syncing it.
    /// </para>
    /// <para>The BigInteger fallback survives only for a magnitude beyond <see cref="Int128"/>. Nothing here
    /// currently produces one — the widest value is all-nines over 38 digit positions (10^38−1 &lt;
    /// Int128.MaxValue) or a 128-bit COMP-5 container bound — but the parameter type permits it, so the path stays
    /// rather than becoming an overflow waiting for a wider PICTURE.</para></summary>
    private static string Decimalize(System.Numerics.BigInteger unscaled, int scale, bool negative)
    {
        var mag = System.Numerics.BigInteger.Abs(unscaled);
        if (mag <= (System.Numerics.BigInteger)Int128.MaxValue)
        {
            Int128 v = (Int128)mag;
            return CobolNet.Runtime.CobolNum.FormatFunctionText(negative ? -v : v, scale);
        }
        string s = mag.ToString();
        string sign = negative ? "-" : "";
        if (scale <= 0) return sign + s + new string('0', -scale);          // S9PP: 99 @ −2 → "9900"; 1 @ −2 → "100"
        if (s.Length <= scale) s = s.PadLeft(scale + 1, '0');               // 1 @ 3 → "0001" → "0.001"
        return sign + s[..^scale] + "." + s[^scale..];                      // 99999 @ 3 → "99.999"
    }

    // ── Argument-list binding: split → ALL expansion → per-segment parse ───────────────────────────────────

    /// <summary>The generic (non-phrase-keyword) argument bind: table(ALL) expansion first (§15.3), then one
    /// typed operand per argument through <see cref="BindArgOperand"/>.</summary>
    private List<BoundOperand> BindIntrinsicArgs(IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        var args = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (TryExpandAll(a, args)) continue;
            args.Add(BindArgOperand(a));
        }
        return args;
    }

    /// <summary>Bind one function argument to a typed operand (the §8.4.3.2 SR8 shapes): a non-numeric literal
    /// stays a categorized literal operand; a sole data reference stays a field operand through the ONE
    /// <c>BindExpr</c>→<c>RefExpr</c> mapping (its category decides string-vs-numeric rendering; a bare
    /// index-name reads its occurrence number, §13.18.38); anything arithmetic binds through <c>BindExpr</c>
    /// and wraps as a computed operand (<see cref="OperandOf"/>). OMITTED is barred for an intrinsic argument
    /// (§8.4.3.2 SR7); an unconsumed phrase word is a loud named operand — never a silent skip (§1.4).</summary>
    internal BoundOperand BindArgOperand(Core.FunctionArgumentContext a)
    {
        if (a.OMITTED() is not null)
        {
            ctx.Edition.Error("COBOLNET1544", "OMITTED shall not be specified as an intrinsic-function argument "
                + "(ISO §8.4.3.2 SR7 — OMITTED applies to user-defined function parameters declared OPTIONAL)");
            return new BoundOperandError("OMITTED intrinsic argument");
        }
        if (a.fnArgPhraseWord() is { } kw)
            return new BoundOperandError($"intrinsic argument '{kw.GetText()}'");   // a phrase word this function does not take
        if (a.nonNumericLiteral() is { } nn) return NonNumericOperand(nn);
        // An argument is NOT an §8.8.1.1 arithmetic expression: its legality comes from this function's own §15.x
        // ARGUMENT RULE, and the string functions admit alphanumeric data. The named entry says so at the call
        // site — TRIM / SUBSTITUTE / FIND-STRING / CONVERT over a PIC X item are legal (DA6).
        return OperandOf(host.Expr.BindFunctionArgumentExpr(a.arithmeticExpression()));
    }

    /// <summary>A non-numeric-literal argument as a categorized operand (§8.3.3.4/.5/.6.4 — the same decode +
    /// introduction-gate helpers every literal channel uses). HEXLIT decodes as the alphanumeric literal it is
    /// (§8.3.3.2 Format 2) — DA3.</summary>
    private BoundOperand NonNumericOperand(Core.NonNumericLiteralContext nn) =>
        // Through the ONE literal mapping (ExpressionBinder.NonNumericLiteralOperand) — this used to be a second
        // hand-maintained copy of the same chain, which is how the hexadecimal form came to be supported in some
        // literal positions and not others (DA3). §8.8.3.3 GR3 concatenation folding and the §8.3.3.4/.5/.6.4
        // decode + introduction gates all live there now.
        host.Expr.NonNumericLiteralOperand(nn) ?? new BoundOperandError($"literal argument '{nn.GetText()}'");

    /// <summary>The bare-word view of an argument for the §15 phrase-keyword functions: a reserved phrase word
    /// (<c>fnArgPhraseWord</c>) or a bare unqualified, unsubscripted name (the IDENTIFIER-shaped phrase words —
    /// ANYCASE, HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL). Uppercase; null when the
    /// argument is not a bare word. A word that matches no phrase keyword falls back to the ordinary operand
    /// bind, so a data-name argument is never swallowed.</summary>
    private static string? KeywordWordOf(Core.FunctionArgumentContext a)
    {
        if (a.fnArgPhraseWord() is { } kw) return kw.GetText().ToUpperInvariant();
        return SoleDataReference(a) is { } d && d.dataReferenceSuffix().Length == 0
            && d.cobolWord() is { } cw ? cw.GetText().ToUpperInvariant() : null;
    }

    /// <summary>The argument's sole data reference — non-null when the arithmetic-expression alternative is
    /// exactly one <c>dataReference</c> primary with no operators, unary signs, or parentheses around it.</summary>
    private static Core.DataReferenceContext? SoleDataReference(Core.FunctionArgumentContext a)
    {
        if (a.arithmeticExpression()?.additiveExpression() is not { } add) return null;
        if (add.multiplicativeExpression() is not [{ } mul]) return null;
        if (mul.powerExpression() is not [{ } pow]) return null;
        if (pow.unaryExpression() is not [{ } un]) return null;
        return un.primaryExpression()?.dataReference();
    }

    /// <summary>
    /// <c>table(… ALL …)</c> argument expansion (ISO §15.3): when a variadic function references a table with the
    /// ALL subscript, the effect is as if each table element were specified — left to right, the RIGHTMOST ALL
    /// subscript varying fastest, each through 1..its OCCURS count. Detected from an argument that is a sole data
    /// reference whose one subscript capture (SUBSCRIPT-mode tokens — the D10/PHASE-15 deferral) holds a depth-0
    /// ALL. Returns true when the argument IS such a reference (consuming it — including loud error operands for
    /// unresolvable shapes); false hands the argument to the ordinary operand bind. An ALL subscript over an
    /// OCCURS DEPENDING table takes the CURRENT count (§15.3) — a runtime quantity this bind-time expansion
    /// cannot produce; staged loud by name (§1.4).
    /// </summary>
    private bool TryExpandAll(Core.FunctionArgumentContext a, List<BoundOperand> args)
    {
        if (SoleDataReference(a) is not { } dref || dref.cobolWord() is not { } cw) return false;
        string name = cw.GetText();

        // Collect OF/IN qualifiers and the ONE trailing subscript group — which the grammar hangs off the LAST
        // qualification when qualifiers are present (qualification : (OF|IN) cobolWord (subscriptPart|refModPart)*).
        var quals = new List<string>();
        Core.SubscriptPartContext? sp = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (sp is not null) return false;                    // anything after the subscript group — not the shape
            if (suffix.subscriptPart() is { } direct) { sp = direct; continue; }
            if (suffix.qualification() is not { } q || q.cobolWord() is not { } qw) return false;   // ref-mod tail
            quals.Add(qw.GetText());
            if (q.refModPart().Length > 0 || q.subscriptPart().Length > 1) return false;
            if (q.subscriptPart() is [{ } trailing]) sp = trailing;
        }
        if (sp?.subscriptOrRefMod() is not { } som) return false;

        var inner = new List<IToken>();
        ReferenceResolver.CollectLeafTokens(som, inner);
        var innerSegs = ReferenceResolver.SplitSubscriptTokens(inner);
        if (!innerSegs.Any(IsAllSegment)) return false;

        if (ctx.Refs.FindItem(name, quals) is not { } item)
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
            else if (ctx.Refs.RenderIndexSegment(innerSegs[i]) is { } rendered)
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
            args.Add(ctx.Refs.ResolveByName(name, quals, exprs) is { } place
                ? new BoundFieldOperand(place)
                : new BoundOperandError($"table(ALL) occurrence of '{name}'"));
        }
        return true;
    }

    private static bool IsAllSegment(List<IToken> seg) =>
        seg.Count(t => t.Type != Core.SUB_WS) == 1 && seg.Any(t => t.Type == Core.SUB_ALL);

}
