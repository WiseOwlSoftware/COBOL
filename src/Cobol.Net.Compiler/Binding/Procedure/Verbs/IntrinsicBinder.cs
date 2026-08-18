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
/// are REAL parse trees — <c>functionCall : FUNCTION functionName (FNARG_LPAREN functionArgList? FNARG_RPAREN)?</c>
/// (the argument-list parens carry their own token type since PB48 — ISO §8.4.3.2.3 SR6), each
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

    /// <summary>FUNCTION call in an expression position (the <c>BindPrimary</c> hook). A trailing
    /// <c>refModPart</c> reference-modifies the RESULT (ISO §8.4.3.3.3 SR2 — fix-queue PB8).</summary>
    public BoundExpr BindIntrinsic(Core.FunctionCallContext fc)
    {
        // The KEYWORD-OMITTED alternative for a reserved intrinsic name (fix-queue PB9: RANDOM / SIGN / SUM —
        // §8.9-reserved words that are also §8.11 intrinsic function names). §8.4.3.2.3 SR2 permits the omission
        // ONLY when the REPOSITORY declares the function, which the grammar cannot know; enforce it here.
        // Rejecting is unambiguous: a reserved word cannot be a data name (§8.3.2.4.1), so there is no other
        // reading to fall back to and a plain, cited error is the whole answer.
        string? reservedName = fc.reservedIntrinsicArgFn()?.GetText() ?? fc.RANDOM()?.GetText();
        if (reservedName is { } fn)
        {
            if (!ctx.Data.RepositoryAllIntrinsic && !ctx.Data.RepositoryIntrinsics.Contains(fn))
            {
                ctx.Edition.Error("COBOLNET1543",
                    $"'{fn}' is written without the word FUNCTION, but the REPOSITORY paragraph does not declare "
                    + $"it. ISO §8.4.3.2.3 SR2 allows the omission only for an intrinsic named in REPOSITORY — "
                    + $"write 'FUNCTION {fn}', or add 'REPOSITORY. FUNCTION {fn} INTRINSIC.' (or FUNCTION ALL "
                    + "INTRINSIC).");
                return new BoundExprError($"FUNCTION {fn}");
            }
            // The captured group is the ARGUMENT LIST unless it holds a depth-0 colon, in which case it is a
            // reference modification of a zero-argument result — the same two shapes, decided the same way, as
            // KeywordOmittedFunction. §8.4.3.2.3 SR6 is then applied by ResultRefMod exactly as for the
            // FUNCTION-keyword form, so `RANDOM (1:4)` is rejected as an argument list on both routes.
            var sp = fc.subscriptPart();
            if (sp?.subscriptOrRefMod() is { } grp && ReferenceResolver.HasDepth0Colon(grp))
                return ResultRefMod(BindIntrinsicCore(fn, []), ctx.Refs.ReadRefMod(grp),
                    argListWritten: false, fn);
            var args = sp is null ? [] : ReparseArgs(sp);
            return args is null
                ? new BoundExprError($"FUNCTION {fn} arguments")
                : FinishIntrinsic(fc, BindIntrinsicCore(fn, args), fn);
        }
        string name = fc.functionName().GetText();
        return FinishIntrinsic(fc, BindIntrinsicCore(name, ArgsOf(fc.functionArgList())), $"FUNCTION {name}");
    }

    /// <summary>The tail both <c>functionCall</c> alternatives share: the §8.4.3.3.3 ref-mod on the RESULT
    /// (fix-queue PB8). Shared so the FUNCTION-keyword form and the reserved-name keyword-omitted form cannot
    /// drift apart on SR2/SR3/SR6 — the drift PB8 itself was.</summary>
    private BoundExpr FinishIntrinsic(Core.FunctionCallContext fc, BoundExpr call, string display)
    {
        var refMods = fc.refModPart();
        if (refMods.Length == 0) return call;
        // §8.4.3.3.3 SR3, enforced HERE rather than by the grammar's arity so that a function result and a data
        // reference report the SAME diagnostic (the data side must count anyway — dataReferenceSuffix* cannot
        // express the limit either).
        if (refMods.Length > 1)
        {
            ctx.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                $"'{display}' carries {refMods.Length} reference modifications; the result of a function cannot "
                + "be reference-modified twice (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier.");
            return new BoundExprError($"reference modification of {display}");
        }
        // FNARG_LPAREN, not LPAREN: the argument-list paren now carries its own token type (PB48), which is the
        // §8.4.3.2.3 SR6 question this argument is asking, answered by the lexer instead of inferred here.
        return ResultRefMod(call, ctx.Refs.ReadRefMod(refMods[0]), fc.FNARG_LPAREN() is not null, display);
    }

    /// <summary>
    /// Apply a reference modification to a FUNCTION RESULT (fix-queue PB8) — the ONE applier, shared by the
    /// FUNCTION-keyword form and both keyword-omitted shapes, so the two syntax rules below are enforced once.
    /// <para><b>ISO §8.4.3.2.3 SR6</b> — "If a function's definition permits arguments and a left parenthesis
    /// immediately follows … the left parenthesis is ALWAYS treated as the left parenthesis of that function's
    /// arguments." So a ref-mod written directly after the NAME of a function that takes arguments is not a
    /// ref-mod at all: that <c>(</c> opened an argument list, and <c>1:4</c> is not an argument (§8.4.3.2.3 SR8).
    /// <c>FUNCTION RANDOM (1:4)</c> is the standard's own cautionary shape (the SR6 NOTE) and is REJECTED —
    /// reported as the argument-list error it is, not as a class error about a ref-mod that was never written.
    /// <paramref name="argListWritten"/> is what distinguishes it from the legal <c>FUNCTION UPPER-CASE(x) (1:2)</c>.</para>
    /// <para><b>ISO §8.4.3.3.3 SR2</b> — "If identifier-1 is a function-identifier, it shall reference an
    /// alphanumeric, boolean, or national function." A numeric/integer function has no character positions for
    /// §8.4.3.3.4 GR4 to number, so it is rejected with <c>COBOLNET1629</c>.</para>
    /// <para>A USER-DEFINED function's result is already a real <see cref="Place"/> (the §8.4.3.2.4 GR1 caller
    /// temp cloned from the RETURNING item), so it reference-modifies through the SAME
    /// <see cref="RefModPlace"/> a data item does — no second slicer, and the temp's own category answers SR2.</para>
    /// </summary>
    private BoundExpr ResultRefMod(BoundExpr call, RefModSpec? spec, bool argListWritten, string display)
    {
        if (call is BoundExprError) return call;                    // already loud — do not stack a second report
        if (call is BoundIntrinsicCall ic && !argListWritten && ic.Sig.MaxArgs > 0)
        {
            ctx.Edition.Error("COBOLNET1543", $"'{display} (…)' — the '(' after the name of a function that "
                + "takes arguments is ALWAYS its argument list (ISO §8.4.3.2 SR6), so this is an argument list "
                + "and 'start:length' is not a valid argument (SR8). Reference-modify the RESULT by writing the "
                + $"argument list first: {display}(<arguments>) (start:length).");
            return new BoundExprError($"{display} arguments");
        }
        PicCategory? category = call switch
        {
            BoundIntrinsicCall c => c.ResultCategory,
            BoundNumRef { Place.Item.Pic: { } pic } => pic.Category,
            _ => null,
        };
        // §8.4.3.3.3 SR2. NumericEdited is NOT admitted: SR2 names the three FUNCTION types (§15.2), and no
        // intrinsic is numeric-edited — the category exists here only for a user-function RETURNING item.
        if (category is not (PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean))
        {
            ctx.Edition.Error(DiagnosticCatalog.RefModFunctionResultClass,
                $"'{display}' is not an alphanumeric, boolean, or national function, so its result cannot be "
                + "reference-modified (ISO §8.4.3.3.3 SR2).");
            return new BoundExprError($"reference modification of {display}");
        }
        if (spec is not { } rm) return new BoundExprError($"reference modification of {display}");
        return call switch
        {
            BoundIntrinsicCall c => c with { RefMod = rm },
            BoundNumRef r => new BoundNumRef(new RefModPlace(r.Place, rm.Start, rm.Length)
                { AllowZeroLength = rm.AllowZeroLength }),
            _ => new BoundExprError($"reference modification of {display}"),
        };
    }

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
        // ⛔ ZERO SUFFIXES IS A LEGAL FORM, and requiring exactly one made every ZERO-ARGUMENT intrinsic
        // unreachable in the keyword-omitted form (fix-queue PB7). §15.21.2's general format is
        // `FUNCTION CURRENT-DATE` with no parentheses at all, so with the keyword omitted it is a BARE NAME —
        // zero suffixes, not one. `REPOSITORY. FUNCTION ALL INTRINSIC.` + `MOVE CURRENT-DATE TO X` therefore
        // fell through to a data reference, resolved to nothing, COMPILED CLEAN and threw
        // NotImplementedCobolFeatureException at RUN TIME. PI, E, WHEN-COMPILED and the rest of the family
        // failed identically; the standard writes the form itself at §D.14.3.6.
        // Admitted ONLY when the catalog says the function can take zero arguments, so a bare word can never be
        // re-routed away from a data reference on the strength of merely sharing a name with some function.
        // ── The suffix shapes a keyword-omitted function reference can wear ──────────────────────────────────
        // With FUNCTION omitted the reference lexes as a dataReference, so its parenthesised groups arrive as
        // dataReferenceSuffixes and the ARGUMENT list is indistinguishable from a subscript until this point.
        // Reference modification of the RESULT (§8.4.3.3.3 SR2, fix-queue PB8) adds two more shapes, and the
        // standard writes one of them itself at §D.14.3.6: `FUNCTION LOCALE-DATE (CURRENT-DATE (1:8))`.
        //   ()                       zero suffixes  — a bare zero-argument reference           (PB7)
        //   (args)                   subscriptPart, no depth-0 colon — the argument list
        //   (start:len)              subscriptPart WITH a depth-0 colon — a ref-mod on a ZERO-ARGUMENT result
        //   (args) (start:len)       the argument list, then a ref-mod
        // The ref-mod tail is a refModPart, not a subscriptPart: after the argument list's ')' the previous
        // token is no longer a data-name, so the lexer's SUBSCRIPT trigger does not fire and the group stays in
        // DEFAULT mode. Both carriers are read by the ONE ReferenceResolver.ReadRefMod.
        Core.SubscriptPartContext? sp = null;
        Core.SubscriptOrRefModContext? capturedRefMod = null;   // `name (start:len)` — a zero-argument result
        Core.RefModPartContext? tailRefMod = null;              // `name (args) (start:len)`
        if (suffixes.Length == 1)
        {
            if (suffixes[0].subscriptPart() is not { } only) return null;   // a qualification tail is not an argument list
            if (only.subscriptOrRefMod() is { } g && ReferenceResolver.HasDepth0Colon(g)) capturedRefMod = g;
            else sp = only;
        }
        else if (suffixes.Length == 2)
        {
            // Exactly `(args) (ref-mod)`. Anything else — two argument lists, a qualification, a second ref-mod
            // (§8.4.3.3.3 SR3) — is not a function reference and stays a data reference, which reports through
            // the ordinary unresolved-name path rather than being turned into a function-arity error here.
            if (suffixes[0].subscriptPart() is not { } argsPart
                || argsPart.subscriptOrRefMod() is { } ag && ReferenceResolver.HasDepth0Colon(ag)
                || suffixes[1].refModPart() is not { } tail) return null;
            sp = argsPart;
            tailRefMod = tail;
        }
        else if (suffixes.Length != 0)
        {
            return null;
        }
        string name = cw.GetText();
        bool catalogued = IntrinsicCatalog.TryGet(name, out var sig);
        bool declaredFn = ctx.Data.UserFunctionNames.Contains(name)
            || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase)
            || (catalogued && (ctx.Data.RepositoryAllIntrinsic || ctx.Data.RepositoryIntrinsics.Contains(name)));
        if (!declaredFn && !catalogued) return null;
        if (ctx.Symbols.TryResolve(name, ctx.ActiveScope, out _)) return null;   // a declared data item wins — never a mis-routed subscript
        if (!declaredFn)
        {
            // The OTHER arm of the §8.4.3.2.3 SR2 discrimination (kb/Work R22): the name is a catalogued
            // intrinsic, no data item shadows it, and the REPOSITORY does not declare it — so the reference
            // is not a legal function-identifier (SR2: without the declaration "the word FUNCTION" is
            // required) and nothing else resolves it. Until now only the RESERVED names (SIGN/SUM/RANDOM,
            // the grammar alternative in BindIntrinsic) drew COBOLNET1543; every other catalogued name fell
            // through to generic unresolved-reference staging and died at RUN time. A bare name (no argument
            // list) reads as a function only when the function admits zero arguments — mirroring the declared
            // arm below — so a mere name-collision with, say, SQRT still reports as the ordinary unresolved
            // name it is.
            if (sp is null && sig.MinArgs != 0) return null;
            ctx.Edition.Error("COBOLNET1543",
                $"'{name}' is written without the word FUNCTION, but the REPOSITORY paragraph does not declare "
                + $"it. ISO §8.4.3.2.3 SR2 allows the omission only for an intrinsic named in REPOSITORY — "
                + $"write 'FUNCTION {name}', or add 'REPOSITORY. FUNCTION {name} INTRINSIC.' (or FUNCTION ALL "
                + "INTRINSIC).");
            return new BoundExprError($"FUNCTION {name}");
        }
        if (sp is null)
        {
            // A REPOSITORY-DECLARED USER function referenced bare is §8.4.3.2.3 SR2's own case too (kb/Work
            // R35 — the two-arm-dispatch shape a SIXTH time: PB7 fixed the intrinsic arm of the bare-name
            // form and never asked the UDF arm, so `MOVE WITHOUTPAR TO X` over a declared zero-argument
            // function fell to the data path and died as "undefined"). Route it to the UDF bind, whose
            // prototype machinery owns the zero-vs-N arity verdict — a bare reference to a declared
            // 2-argument function is an ARITY error about a function, never an undefined name: the user
            // DECLARED it a function, so there is no coincidental-collision reading to protect.
            if (ctx.Data.UserFunctionNames.Contains(name)
                || name.Equals(host.UdfSelfName, StringComparison.OrdinalIgnoreCase))
            {
                var udfBare = host.Udf.UdfBindCall(name, []);
                return capturedRefMod is null
                    ? udfBare
                    : ResultRefMod(udfBare, ctx.Refs.ReadRefMod(capturedRefMod), argListWritten: false, name);
            }
            // A bare CATALOGUED name only becomes a function reference when the function genuinely admits
            // ZERO arguments (MinArgs 0 — CURRENT-DATE, PI, E, WHEN-COMPILED, and RANDOM's no-argument form,
            // whose §15.75.2 format brackets the whole parenthesised part). Anything else stays a data
            // reference: for a name that merely COLLIDES with the catalog, the ordinary unresolved-name path
            // is the honest verdict, not an arity error about a function never intended.
            if (!catalogued || sig.MinArgs != 0) return null;
            var bare = BindIntrinsicCore(name, []);
            // `CURRENT-DATE (1:8)` — the captured group carries a depth-0 colon, so it is a reference
            // modification of the RESULT, not an argument list. ResultRefMod still applies §8.4.3.2.3 SR6 with
            // argListWritten: false, which is what rejects the same shape on RANDOM (MinArgs 0 but MaxArgs 1) —
            // exactly as the FUNCTION-keyword form does, so the two reference forms cannot drift apart.
            return capturedRefMod is null
                ? bare
                : ResultRefMod(bare, ctx.Refs.ReadRefMod(capturedRefMod), argListWritten: false, name);
        }
        var call = ReparseArgs(sp) is { } args
            ? BindIntrinsicCore(name, args)
            : new BoundExprError($"FUNCTION {name} arguments");
        return tailRefMod is null
            ? call
            : ResultRefMod(call, ctx.Refs.ReadRefMod(tailRefMod), argListWritten: true, name);
    }


    private static string Name(DateTimeFormatKind k) => k switch
    {
        DateTimeFormatKind.Date => "DATE",
        DateTimeFormatKind.Time => "TIME",
        _ => "COMBINED date-and-time",
    };

    /// <summary>The §15.3 format kinds each format-taking function ADMITS — a SET, not one kind, because four
    /// of the seven rules name more than one.
    /// <para>⛔ THIS WAS A SINGLE VALUE AND THE CORPUS CAUGHT IT. Reading the three "sibling" functions by name
    /// analogy gave TEST-FORMATTED-DATETIME "combined", and §15.92.3 r2 actually says "either a date format, a
    /// time format, or a combined date and time format" — so an existing, legal corpus program
    /// (<c>2014/formatted_datetime</c>, <c>TEST-FORMATTED-DATETIME("YYYYMMDD" …)</c>) was rejected. Each row
    /// below is now the function's OWN rule, quoted:</para>
    /// <list type="bullet">
    ///   <item>§15.38.3 r2 FORMATTED-CURRENT-DATE — "shall be a combined date and time format"</item>
    ///   <item>§15.39.3 r2 FORMATTED-DATE — "shall be a date format"</item>
    ///   <item>§15.40.3 r2 FORMATTED-DATETIME — "shall be a combined date and time format"</item>
    ///   <item>§15.41.3 r2 FORMATTED-TIME — "shall be a time format"</item>
    ///   <item>§15.48.3 r2 INTEGER-OF-FORMATTED-DATE — "either a date format or a combined date and time format"</item>
    ///   <item>§15.79.3 r2 SECONDS-FROM-FORMATTED-TIME — "either a time format or a combined date and time format"</item>
    ///   <item>§15.92.3 r2 TEST-FORMATTED-DATETIME — "either a date format, a time format, or a combined …"</item>
    /// </list>
    /// Null for a function that takes no format argument.</summary>
    private static DateTimeFormatKind[]? FormatKindsAdmittedBy(string name) => name switch
    {
        "FORMATTED-DATE" => [DateTimeFormatKind.Date],
        "FORMATTED-TIME" => [DateTimeFormatKind.Time],
        "FORMATTED-CURRENT-DATE" or "FORMATTED-DATETIME" => [DateTimeFormatKind.Combined],
        "INTEGER-OF-FORMATTED-DATE" => [DateTimeFormatKind.Date, DateTimeFormatKind.Combined],
        "SECONDS-FROM-FORMATTED-TIME" => [DateTimeFormatKind.Time, DateTimeFormatKind.Combined],
        "TEST-FORMATTED-DATETIME" =>
            [DateTimeFormatKind.Date, DateTimeFormatKind.Time, DateTimeFormatKind.Combined],
        _ => null,
    };

    /// <summary>The ZERO-BASED position of the optional offset-from-UTC argument, for the two functions that take
    /// one, else null. FORMATTED-DATETIME's is argument-4 (§15.40.3 r5/r6) and FORMATTED-TIME's is argument-3
    /// (§15.41.3 r4/r5) — the same rule at different ordinals, which is exactly why the ordinal is a lookup and
    /// not a literal at the screen. ⚠ FORMATTED-CURRENT-DATE takes no offset argument at all (§15.38.2 gives it
    /// one operand), so it is absent here rather than mapped to a position it does not have.</summary>
    private static int? OffsetArgumentIndex(string name) => name switch
    {
        "FORMATTED-DATETIME" => 3,
        "FORMATTED-TIME" => 2,
        _ => null,
    };

    /// <summary>The clause each function's format-content rule lives in, for the diagnostic text.</summary>
    private static string FormatRuleCitation(string name) => name switch
    {
        "FORMATTED-CURRENT-DATE" => "ISO §15.38.3 r2",
        "FORMATTED-DATE" => "ISO §15.39.3 r2",
        "FORMATTED-DATETIME" => "ISO §15.40.3 r2",
        "FORMATTED-TIME" => "ISO §15.41.3 r2",
        "INTEGER-OF-FORMATTED-DATE" => "ISO §15.48.3 r2",
        "SECONDS-FROM-FORMATTED-TIME" => "ISO §15.79.3 r2",
        "TEST-FORMATTED-DATETIME" => "ISO §15.92.3 r2",
        _ => "ISO §15.3",
    };

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
        // ⛔ A DOCUMENTED-NON-SUPPORT FUNCTION DOES NOT ASSERT AN INTRODUCTION EDITION (fix-queue PB27 §③).
        // COBOLNET1502 states a FACT about the standard — "was introduced by ISO/IEC 1989:{year}" — and for the
        // §A.4.9 locale family that year is HAND-ASSIGNED AND UNVERIFIABLE: the 2023 standard carries no
        // introduction record (§8.11 lists the intrinsic NAMES with no edition data; Annex E covers only
        // 2014→2023), the repo holds no 2002 or 2014 text, the reserved-word tables do not carry intrinsic
        // names, and GnuCOBOL's testsuite pins no `-std=` on these functions. Every source was checked.
        // ⚠ AND THE DIAGNOSTIC IS REDUNDANT HERE ANYWAY: `Bind = Unsupported` means COBOLNET1518 rejects the
        // reference at EVERY edition, 2023 included (measured), so the edition claim adds no actionable
        // information — no `--std` makes the program compile — while asserting something we cannot substantiate.
        // ⚠ IT SELF-LIFTS: the suppression keys on Bind, so implementing the locale module restores the gate —
        // at which point IntroducedIn would have to be verified anyway, which is the right forcing function.
        if (sig.Bind is IntrinsicBind.Unsupported) { /* §A.4.9 non-support says it all; see above */ }
        else if (sig.IntroducedIn > ctx.Edition.DialectLevel)
            ctx.Edition.Error("COBOLNET1502", $"FUNCTION {sig.Name} was introduced by ISO/IEC 1989:{sig.IntroducedIn} "
                + $"(§15) — it requires --std {sig.IntroducedIn} or later (targeting COBOL-{ctx.Edition.DialectLevel})");
        else if (sig.RemovedIn is { } gone && ctx.Edition.DialectLevel >= gone)
            ctx.Edition.Error("COBOLNET1503", $"FUNCTION {sig.Name} was removed by ISO/IEC 1989:{gone} — "
                + $"it is not available when targeting COBOL-{ctx.Edition.DialectLevel}");

        // The former standard-arithmetic staging (P10 Step 12 → COBOLNET0899) is GONE (fix-queue PB56):
        // ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION now evaluate their §15.4.1 r1 equivalent
        // arithmetic expressions on the SDIDI carrier (CobolIntrinsics.Dec.cs, dispatched by
        // IntrinsicRenderer.RenderDec), so a standard-arithmetic program gets the standard-decimal values the
        // rule requires — the "until CobolDec evaluations land" condition the stage recorded is satisfied.

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

        // FUNCTION LENGTH's optional PHYSICAL keyword (§15.50.2's general format:
        // `FUNCTION LENGTH ( argument-1 [ PHYSICAL ] )`). Bound apart from the generic argument path for the same
        // reason ANYCASE is: it is a KEYWORD, not an operand, and the generic path counts it as one — which is
        // exactly what it did, rejecting the conforming `FUNCTION LENGTH(WS-G PHYSICAL)` with
        // "COBOLNET1504: takes 1 argument(s); 2 given" (fix-queue PB24).
        if (sig.Name == "LENGTH") return BindLengthFamily(sig, argCtxs);

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

        // ISO §15.3 ARGUMENT-CLASS screen (fix-queue PB1). THE catalog-driven enforcement of every catalogued
        // function's argument rule — see IntrinsicArgumentRules for why this had to be written: sig.ArgKinds
        // declared the required class on all 79 rows and sig.ArgKind had ZERO callers, so `FUNCTION REVERSE` over
        // a numeric item and `FUNCTION ABS` over an alphanumeric one both compiled clean and produced garbage.
        // It sits HERE — after arity, before every per-function arm — so a new catalog row is screened the day it
        // is added rather than the day someone remembers to write its arm.
        CheckArgumentClasses(sig, args);

        // §15.38–15.41 / §15.48 / §15.79 / §15.92 rule 1: the date/time FORMAT (argument-1) shall be a LITERAL —
        // the format is analyzed/derived at compile time (SECONDS-FROM-FORMATTED-TIME needs the fraction scale).
        if (args.Count > 0 && args[0] is not BoundStringLiteral
            && sig.Name is "FORMATTED-CURRENT-DATE" or "FORMATTED-DATE" or "FORMATTED-DATETIME" or "FORMATTED-TIME"
                or "INTEGER-OF-FORMATTED-DATE" or "SECONDS-FROM-FORMATTED-TIME" or "TEST-FORMATTED-DATETIME")
            ctx.Edition.Error("COBOLNET1517", $"FUNCTION {sig.Name} argument-1 shall be a literal date/time format "
                + "(ISO §15 — the FORMATTED-*/INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/"
                + "TEST-FORMATTED-DATETIME format is a literal)");

        // §15.38.3/§15.39.3/§15.40.3/§15.41.3/§15.48.3/§15.79.3/§15.92.3 rule 2 — the format's CONTENT
        // (fix-queue PB11). The literal screen above established only that argument-1 IS a literal; this asks
        // the question nothing asked before: is it one of the §15.3.1–§15.3.4 formats, and is it a kind THIS
        // function admits? Character-wise validation cannot answer that — every counter-example is assembled
        // from individually legal subfields.
        // §15.40.3 r6 / §15.41.3 r5 — the OFFSET ARGUMENT vs the format's ZONE (fix-queue PB11's value half).
        // "Argument-4 [argument-3] shall not be specified if the time portion of the format in argument-1 is
        // neither a UTC format nor an offset format." Decidable HERE because rule 1 above already established
        // argument-1 is a literal, so `Describe` knows the zone, and the argument's presence is syntactic.
        // ⚠ ONE-SIDED, deliberately: the CONVERSE is explicitly legal — omitting the argument for a UTC/offset
        // format "shall be evaluated as though 0 were specified" (§15.40.3 r7 / §15.41.3 r6), which the emitter
        // already does by passing hasOffset:false. Screening that too would reject conforming source.
        // Before this the argument bound cleanly and was then SILENTLY DISCARDED by a local format.
        if (args.Count > 0 && args[0] is BoundStringLiteral zoneFmt
            && OffsetArgumentIndex(sig.Name) is { } offIx && args.Count > offIx
            && DateTimeFormatGrammar.Describe(zoneFmt.Value, ctx.Data.DecimalPointIsComma) is { } zoneInfo
            && zoneInfo.Zone is DateTimeZone.Local)
            ctx.Edition.Error(DiagnosticCatalog.DateTimeOffsetArgumentNotPermitted,
                $"FUNCTION {sig.Name} is given an offset-from-UTC argument (argument-{offIx + 1}), but its format "
                + $"'{zoneFmt.Value}' has a LOCAL time portion — neither a UTC format (a trailing 'Z') nor an "
                + "offset format (an explicit '+hhmm' / '+hh:mm' subformat), ISO §15.3.3.4–§15.3.3.6. "
                + (sig.Name == "FORMATTED-DATETIME" ? "§15.40.3 r6" : "§15.41.3 r5")
                + " bars the argument there. Use a UTC or offset format, or drop the argument");

        if (args.Count > 0 && args[0] is BoundStringLiteral fmt
            && FormatKindsAdmittedBy(sig.Name) is { } admitted)
        {
            var actual = DateTimeFormatGrammar.Classify(fmt.Value, ctx.Data.DecimalPointIsComma);
            if (actual is null || Array.IndexOf(admitted, actual.Value) < 0)
                ctx.Edition.Error(DiagnosticCatalog.DateTimeFormatKindMismatch,
                    $"FUNCTION {sig.Name} argument-1 is '{fmt.Value}', which is "
                    + (actual is null
                        ? "not a date, time or combined date-and-time format at all (ISO §15.3.1.1 / §15.3.2 / "
                          + "§15.3.4 — note that basic and extended forms never mix)"
                        : $"a {Name(actual.Value)} format; this function admits "
                          + string.Join(" or ", admitted.Select(Name)))
                    + $" ({FormatRuleCitation(sig.Name)}).");
        }

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

        // ⛔ THE ONE RESULT-TYPE RESOLUTION (§15.x.1 result-type tables; fix-queue PB15). Twenty functions have a
        // type that DEPENDS ON THEIR ARGUMENTS, and this used to be two hand-written name lists here — a
        // `RuntimeMethod is "UpperCase" or "LowerCase" or "Reverse"` chain (CA25) and a `Name is "MAX" or "MIN"`
        // arm (V54) — so the other TEN functions silently kept the catalog's hardcoded Alphanumeric. The rule now
        // lives on the catalog ROW (IntrinsicSig.Result) and is read HERE and nowhere else; adding a function is
        // a column, not an edit to this method. IntrinsicResultTypeDriftTests re-derives the population from
        // specs/ISO_COBOL.md so a new §15.x.1 table cannot be missed.
        var category = IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, args));

        // ⚠ SEPARATE CONCERN, DELIBERATELY NOT MERGED ABOVE: which runtime BODY compares the arguments.
        // MAX/MIN/ORD-MAX/ORD-MIN over string arguments dispatch to the string comparison body; ORD-MAX/ORD-MIN
        // still return an ordinal (§15.71.1/§15.72.1 carry NO result-type table — only MAX/MIN do), which is why
        // this is a RuntimeMethod choice and not a type one, and why the two must not be folded together.
        // ⛔ A CLASS-NEUTRAL ARGUMENT MUST NOT VOTE (fix-queue PB48). This read `args.All(IsStringOperand)`, which
        // silently counted the figurative ZERO as a NON-string argument and so forced the numeric body on a list
        // that was alphanumeric: `FUNCTION MAX(ZERO "A")` returned "0" — a WRONG ANSWER, since §15.59.4 r1 takes
        // the greatest by §8.8.4.2 relation rules and "A" (65) exceeds "0" (48) — and `FUNCTION MAX(SPACE "A")`,
        // two plainly alphanumeric arguments, drove SPACE into the numeric channel and aborted at RUN TIME.
        // §15.59.3 r2 requires all arguments to be of the SAME class, and §8.3.3.6.4 GR4 says ZERO takes its class
        // from the context — so the non-neutral arguments are the context, and they are what chooses the body.
        // With every argument neutral (`FUNCTION MAX(ZERO ZERO)`) there is no such context and the numeric reading
        // stands, which is both GR4's first-listed value and §8.8.1.1's.
        var resolved = sig;
        if (sig.ArgKinds == "p" && args.Count > 0
            && args.All(a => IsStringOperand(a) || IsClassNeutralOperand(a))
            && args.Any(IsStringOperand))
        {
            resolved = sig with
            {
                RuntimeMethod = sig.Name switch
                {
                    "MAX" => "MaxString", "MIN" => "MinString",
                    "ORD-MAX" => "OrdMaxString", _ => "OrdMinString",
                },
            };
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
        // BASECONVERT (§15.12.3 r1) — the bind-time screen family (PB59 / AR-15.12.3-1 legs (b)–(f)).
        if (sig.Name is "BASECONVERT") CheckBaseConvertArgs(args);

        // A FUNCTION EXCEPTION-* reference reads the runtime last-exception register (§15.28–15.33) — flag the
        // program's EC usage so the generated source carries the Exceptions using (the group EC gate).
        if (resolved.RuntimeMethod.StartsWith("Ec", StringComparison.Ordinal)) host.Ec.EcNoteFunction();

        // §15.18.4 r3 — CONCAT returns an ALPHABETIC value when argument-1 is usage display and argument-1 and
        // all argument-2 are of class alphabetic (PB59 family 7 / RV-15.18.4-3). Class alphabetic is a PIC A
        // DATA ITEM — §8.3.1.2 gives no literal that class, a ref-mod view is class alphanumeric (§8.4.3.3.4
        // GR2/GR6), and a nested CONCAT contributes through its own rider — and every PIC A item is usage
        // display, so all-alphabetic subsumes r3's usage-display precondition. Fail-soft: any shape not
        // provably alphabetic keeps r3's "otherwise" arm (alphanumeric).
        bool alphabetic = resolved.Name == "CONCAT" && args.Count > 0 && args.All(IsAlphabeticArg);

        return new BoundIntrinsicCall(resolved, args, category, collate)
            { CollateNat = collateNat, ResultIsAlphabetic = alphabetic };
    }

    /// <summary>Is this argument provably CLASS ALPHABETIC (§8.5.2.1 Table 2 — category alphabetic, the PIC A
    /// shape)? A ref-mod view never is (§8.4.3.3.4 GR2/GR6 make every view plain alphanumeric); a nested
    /// CONCAT is exactly when its own §15.18.4 r3 rider says so.</summary>
    private static bool IsAlphabeticArg(BoundOperand op) => op switch
    {
        BoundFieldOperand { Place: RefModPlace } => false,
        BoundFieldOperand f => f.Place.Item.Pic is { IsAlphabetic: true },
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultIsAlphabetic: true } } => true,
        _ => false,
    };

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
        // The §15.3 screen — see the PB12 note on the FIND-STRING arm: this binder returns before the
        // generic path reaches CheckArgumentClasses, so it screens its own operand list.
        CheckArgumentClasses(sig, operands);
        // ⛔ THE §15.96.1 RESULT-TYPE TABLE APPLIES HERE TOO, AND THIS SITE IS WHY PB15 NEEDED FINDING TWICE.
        // TRIM has a BESPOKE bind (the LEADING/TRAILING phrase), so it builds its own node and NEVER reaches the
        // generic path's resolution — hardcoding Alphanumeric here left `MOVE FUNCTION TRIM(national) TO PIC X`
        // compiling clean even with the catalog row correct. The two-arm dispatch, in its silent form: the fix
        // present, the defect intact, every test green. Both arms now read the ONE rule.
        return new BoundIntrinsicCall(
            sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands)))
            { TrimMode = mode };
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
        BoundExpr Malformed(string why)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 [LAST] "
                + $"[[START AFTER] argument-3] [ANYCASE], in that order (ISO §15.37.2) — {why}");
            return new BoundExprError("FUNCTION FIND-STRING");
        }
        // ⛔ A POSITIONAL WALK OVER THE §15.37.2 FORMAT, not an order-free switch (kb/Work R20 — ledger F28).
        // The old walk accepted the phrase words ANYWHERE, REPEATED, and — the dangerous case — a dangling
        // `START AFTER` with no argument-3, where the two written words were silently DISCARDED and the call
        // degraded to the plain two-argument form. The general format fixes order and multiplicity; each slot
        // below is one bracket of it. (BindSubstitute/BindConvert already walk positionally — this was the
        // last order-free arm.)
        //   slot 0/1 = argument-1/-2 · 2 = [LAST] open · 3 = LAST taken · 4 = START seen · 5 = AFTER seen ·
        //   6 = argument-3 taken · 7 = ANYCASE taken (nothing may follow)
        int slot = 0;
        foreach (var a in argCtxs)
        {
            switch (KeywordWordOf(a))
            {
                case "LAST":
                    if (slot < 2) return Malformed("LAST precedes the two operand arguments");
                    if (slot > 2) return Malformed(last ? "LAST is repeated" : "LAST follows a later phrase");
                    last = true; slot = 3; continue;
                case "START":
                    if (slot is not (2 or 3)) return Malformed(slot < 2
                        ? "START precedes the two operand arguments" : "START follows a later phrase");
                    slot = 4; continue;
                case "AFTER":
                    if (slot != 4) return Malformed("AFTER without an immediately preceding START");
                    slot = 5; continue;
                case "ANYCASE":
                    if (slot < 2) return Malformed("ANYCASE precedes the two operand arguments");
                    if (slot == 7) return Malformed("ANYCASE is repeated");
                    if (slot is 4 or 5) return Malformed("START AFTER is not followed by argument-3");
                    anycase = true; slot = 7; continue;
            }
            if (slot == 4) return Malformed("START is not followed by AFTER");
            if (slot == 7) return Malformed("an argument follows ANYCASE, which the format places last");
            operands.Add(BindArgOperand(a));
            slot = slot switch { 0 => 1, 1 => 2, _ => 6 };   // arg-1 → arg-2 → argument-3 (bare or after START AFTER)
        }
        if (slot is 4 or 5)
            return Malformed(slot == 4 ? "START is not followed by AFTER"
                : "START AFTER is not followed by argument-3 — the two written words would be discarded");
        if (operands.Count is < 2 or > 3)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION FIND-STRING takes argument-1 argument-2 "
                + $"[[START AFTER] argument-3] (ISO §15.37.2); {operands.Count} operand argument(s) given");
            return new BoundExprError("FUNCTION FIND-STRING");
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
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
        // §15.87.1's result-type table follows argument-1, and this bespoke bind is the second site that bypassed
        // it (see the TRIM note above — the same two-arm shape, the same silent under-rejection).
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        return new BoundIntrinsicCall(
            sig, operands, IntrinsicSig.CategoryOf(IntrinsicResultType.Resolve(sig, operands)))
            { SubstituteModes = modes };
    }

    /// <summary>CONVERT (§15.19) — data-representation conversion (2023). Argument-1 is followed BY POSITION by
    /// two keyword groups: source-format (ANY | ANUM | HEX | NAT) and destination-format (ANUM | NAT [HEX] |
    /// BYTE), each a bare-word argument (§15.19.2; <see cref="KeywordWordOf"/> — ANY/ALPHANUMERIC/NATIONAL are
    /// reserved words, the rest §8.10 context-sensitive words that arrive as bare names and stay legal as
    /// argument-1's own data-name). The operand binds ordinarily; the format words ride the node's
    /// <c>Convert*</c> init-properties. The argument/SR rules (§15.19.3) are enforced here (COBOLNET1514); the
    /// result category (§15.19.1) is National for a NAT destination, Alphanumeric otherwise.</summary>
    private BoundExpr BindConvert(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        // §15.19.2: ( argument-1 source-format destination-format ) — a POSITIONAL walk, not an order-free
        // harvest (fix-queue PB59 / FMT-15.19.2). Slot 0 is ALWAYS argument-1 and binds as an OPERAND: NAT /
        // ANUM / HEX / BYTE are §8.10 CONTEXT-SENSITIVE words, reserved only inside CONVERT's own format
        // ("otherwise it is treated as a user-defined word"), so a data item NAMED one of them is legal as
        // argument-1 — the old harvest swallowed it as a keyword (measured: 1504 "0 operand + 3 format
        // keyword(s) given") and, symmetrically, accepted CONVERT(ANUM WS-A ANUM HEX) with the operand
        // mid-list. ANY/ALPHANUMERIC/NATIONAL stay §8.9 reserved and can never be data-names, so only the
        // four context-sensitive words change behavior.
        var operands = new List<BoundOperand>();
        var kws = new List<string>();
        foreach (var (a, at) in argCtxs.Select((a, at) => (a, at)))
        {
            if (at == 0) { operands.Add(BindArgOperand(a)); continue; }
            if (KeywordWordOf(a) is { } w && IsConvertFormatWord(w)) { kws.Add(w); continue; }
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: the arguments after argument-1 shall be the "
                + "source-format and destination-format keywords, in that order (ISO §15.19.2)");
            return new BoundExprError("FUNCTION CONVERT format");
        }
        if (operands.Count != 1 || kws.Count is < 2 or > 3)
        {
            ctx.Edition.Error("COBOLNET1504", "FUNCTION CONVERT takes ( argument-1 source-format "
                + $"destination-format ) (ISO §15.19.2); {operands.Count} operand + {kws.Count} format keyword(s) given");
            return new BoundExprError("FUNCTION CONVERT arity");
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
        // The §15.3 screen — see the PB12 note on the FIND-STRING arm: this binder returns before the
        // generic path reaches CheckArgumentClasses, so it screens its own operand list.
        CheckArgumentClasses(sig, operands);
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
        // SR1 — argument-1 shall not be zero length. This arm is the COMPILE-TIME catch for the one shape
        // knowable here (an empty literal — ISO 8.5.4's shape 8); the RUNTIME twin at the top of
        // CobolIntrinsics.Convert screens the other 8.5.4 zero-length shapes (a DYNAMIC LENGTH item at
        // length 0, a zero-occurrence ODO group, …) with EC-ARGUMENT-FUNCTION (fix-queue PB59 / AR-15.19.3-1).
        if (operands[0] is BoundStringLiteral { Value.Length: 0 })
            ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: argument-1 is of zero length (ISO §15.19.3 SR1)");

        // §15.19.3 r4/r5/r6/r7 — the source-format keys what argument-1's STORAGE must hold, and the static
        // half of each rule is its REPRESENTATION (IntrinsicArgumentRules.StaticUsageOf — the keyword-dependent
        // axis the ordinal ArgSchema cannot express; DeliberatelyUnscreened["CONVERT"] records the disposition).
        // r4 names the axis outright ("a valid string of hexadecimal digits of display or national usage");
        // r5 wants "a valid string of characters from the program's alphanumeric coded character set", and its
        // NOTE ("distinct from simply requiring the string to be of class alphanumeric") cuts by representation,
        // not class — a numeric or edited DISPLAY item qualifies, a COMP or national item does not; r6 wants
        // national characters. The VALUE halves (r4's digit validity, r5/r6 membership of the coded set) are
        // the runtime screens' territory (the hex-digit screen, the Annex A.1 item-33 total correspondence), so
        // nothing further is screenable at bind; shapes with no static representation pass to runtime.
        if (IntrinsicArgumentRules.StaticUsageOf(operands[0]) is { } u)
        {
            if (src == 1 && u is not Usage.Display)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANUM source-format reads argument-1 as "
                    + $"a string of characters from the alphanumeric coded character set, which usage {u} does "
                    + "not hold (ISO §15.19.3 rule 5)");
            if (src == 3 && u is not Usage.National)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a NAT source-format reads argument-1 as a "
                    + $"string of national characters, which usage {u} does not hold (ISO §15.19.3 rule 6)");
            if (src == 2 && u is not (Usage.Display or Usage.National))
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: a HEX source-format takes argument-1 of "
                    + $"display or national usage, not {u} (ISO §15.19.3 rule 4)");
            // r7 — ANY takes any usage EXCEPT the address-holding ones; "it is not necessary for the contents
            // to be valid according to the usage", so this exclusion is the ONLY screen on an ANY source.
            // ClassOfCategory cannot express it (the pointer/program-pointer collapse), which is why the
            // predicate reads Usage. MESSAGE-TAG has no Usage member — the usage-inventory drift test forces
            // that decision when the usage lands.
            if (src == 0 && u is Usage.Index or Usage.ObjectReference or Usage.Pointer or Usage.ProgramPointer
                    or Usage.FunctionPointer)
                ctx.Edition.Error("COBOLNET1514", "FUNCTION CONVERT: an ANY source-format argument-1 shall not "
                    + "be of usage index, message-tag, object reference, pointer, function-pointer or "
                    + "program-pointer (ISO §15.19.3 rule 7)");
        }

        // §15.19.3 r7 — a source-format of ANY takes the operand's RAW storage bits regardless of usage. The
        // node carries the WRITTEN source; the RENDERER's storage channel (IntrinsicRenderer.StorageArg →
        // OperandText.AsStorageImage) delivers the bytes per the operand's own representation (PB59 family 5b —
        // the former bind-time ANY→NAT remap was a second mechanism for the same job and is deleted).
        var category = dst == 3 ? PicCategory.National : PicCategory.Alphanumeric;   // §15.19.1 table
        return new BoundIntrinsicCall(sig, operands, category)
            { ConvertSource = src, ConvertDest = dst, ConvertDestHex = hex };
    }

    /// <summary>The statically knowable data category of a function argument (drives the §15.3 per-function
    /// argument class/category rules): a categorized literal, a non-group field reference (a reference-modified
    /// operand keeps its item's category — a national item's ref-mod is category national, §8.4.4.4), a nested
    /// intrinsic's result category, or numeric for a computed arithmetic expression. Null = no fixed static
    /// class (groups, figuratives, ALL literals, error operands) — the caller skips its check.
    /// <para>⚠ THE DEFINITION LIVES IN <see cref="IntrinsicResultType"/>: the §15.x.1 RESULT-type rules need the
    /// same §8.5.2.1 answer these §15.3 ARGUMENT rules do, and two copies of one rule is how they drift apart
    /// (<c>feedback_one_rule_one_place</c>). This is a local alias, not a second implementation.</para></summary>
    private static PicCategory? OperandCategory(BoundOperand op) => IntrinsicResultType.OperandCategory(op);

    /// <summary>The statically knowable width (character positions) of a function argument — TOTAL over the
    /// static shapes (fix-queue PB59 / AR-15.26.3-2, AR-15.66.3-2): null ONLY when the length genuinely exists
    /// at run time. The previous three-arm partial returned null for a GROUP, a REF-MOD view, an ALL literal
    /// and a figurative, and its one call site's <c>is {{ }}</c> guard read every null as "skip the screen" —
    /// so a six-position group or <c>ALL "QQ"</c> as a substitution character sailed past the §15.26.3 r2 /
    /// §15.66.3 r2 one-position rule with no diagnostic, while the equivalent plain literal was rejected: a
    /// partial function where the rule needs a total one.</summary>
    /// <remarks>The CATEGORY twin (<c>OperandCategory</c>) is now TOTAL as well — landed with the family-7b
    /// measurement (2026-08-09): groups answer their §8.5.2.1 class, ALL literals their literal's category,
    /// and a ref-mod view the §8.4.3.3.4 GR6 rewrite, with MAX/MIN's type resolution and body choice aligned
    /// on the one classifier.</remarks>
    private static int? KnownWidth(BoundOperand op) => op switch
    {
        BoundStringLiteral sl => sl.Value.Length,
        // §8.4.3.3 — a ref-mod view with a LITERAL length has that static width; a computed or
        // omitted-length form is genuinely runtime.
        BoundFieldOperand { Place: RefModPlace rm } => int.TryParse(rm.Length, out int n) ? n : null,
        BoundFieldOperand { Place.Item: { IsAnyLength: true } or { IsDynamicLength: true } } => null,
        // A group's width is static exactly when nothing beneath it varies at run time — the §15.50.4 r7
        // dynamic guards plus an ODO subordinate's varying current length (§8.5.1.8 GR7/GR8).
        BoundFieldOperand { Place.Item: { IsGroup: true } g } =>
            HasDynamicLengthLeaf(g) || HasDynamicCapacityTable(g) || HasOdoBeneath(g) ? null : g.ImageWidth,
        BoundFieldOperand { Place.Item: { } item } => item.ImageWidth,
        BoundAllLiteral a => a.Literal.Length,   // §8.3.3.6.4 GR3c — a length-unspecified context takes the literal once
        BoundFigurative => 1,                    // §8.3.3.6.4 GR3b — a bare figurative is ONE character
        _ => null,   // computed results / error operands — genuinely runtime
    };

    /// <summary>Does this group have an OCCURS DEPENDING ON table beneath it? Its CURRENT extent varies at run
    /// time (§8.5.1.8), so the group's width is not statically known. The recursive REDEFINES-excluding walk
    /// mirrors <see cref="HasDynamicLengthLeaf"/>.</summary>
    private static bool HasOdoBeneath(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.OccursSpec?.DependingName is not null || (c.IsGroup && HasOdoBeneath(c))));

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

    /// <summary>BASECONVERT argument rules (§15.12.3 r1 — fix-queue PB59 / AR-15.12.3-1 legs (b)–(f)), the
    /// COMPILE-TIME halves; the runtime twins (the r2 digit screen, the dynamic base range and the equal-bases
    /// EC) live in <c>CobolIntrinsics.BaseConvert</c>. Argument-1 "shall be a usage display or national data
    /// item or literal" — the <see cref="IntrinsicArgumentRules.StaticUsageOf"/> axis (a COMP/PACKED item's
    /// storage is not a digit string; the same shared reader the CONVERT r4–r7 screens ride). When the base in
    /// argument-2 is a STATIC literal below 11, argument-1 "shall also be an unsigned integer data item or
    /// literal" — which rejects a SIGNED or SCALED numeric argument-1 and a numeric-edited one (not an
    /// unsigned integer data item). ⚠ An ALPHANUMERIC string under a sub-11 base is deliberately ADMITTED:
    /// "unsigned integer … literal" is readable as the literal's KIND or its CONTENT, the corpus and GnuCOBOL
    /// both accept string arguments at every base (BASECONVERT("1010", 2, 16) is a pinned-green golden), and
    /// the runtime r2 digit screen owns the content — the narrower reading would reject pinned-legal source
    /// (follow-GnuCOBOL-on-split-latitude). Argument-2/-3 "shall be positive nonzero numeric integer literals
    /// or data items with unequal values in the range 2 to 16" — every STATIC-literal violation is flagged at
    /// compile time per §4.2.2 ¶3 (a non-integer literal base, a literal base outside 2..16, two EQUAL literal
    /// bases); data-item bases stay the runtime guards' territory.</summary>
    private void CheckBaseConvertArgs(List<BoundOperand> args)
    {
        if (args.Count > 0 && IntrinsicArgumentRules.StaticUsageOf(args[0]) is { } u
            && u is not (Usage.Display or Usage.National))
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-1 shall be a usage display or "
                + $"national data item or literal, not usage {u} (ISO §15.12.3 rule 1)");

        long? b2 = StaticIntLiteral(args, 1), b3 = StaticIntLiteral(args, 2);
        if (b2 is { } r2v && r2v is < 2 or > 16)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-2 shall be in the range 2 to 16, "
                + $"not {r2v} (ISO §15.12.3 rule 1; §4.2.2 — a literal violation is flagged at compile time)");
        if (b3 is { } r3v && r3v is < 2 or > 16)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-3 shall be in the range 2 to 16, "
                + $"not {r3v} (ISO §15.12.3 rule 1; §4.2.2 — a literal violation is flagged at compile time)");
        if (b2 is { } e2 && b3 is { } e3 && e2 == e3)
            ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: argument-2 and argument-3 shall have "
                + $"unequal values — both are {e2} (ISO §15.12.3 rule 1)");
        for (int i = 1; i <= 2 && i < args.Count; i++)
            if (args[i] is BoundNumericLiteral bl && (bl.Text.Contains('.') || bl.Text.Contains(',')))
                ctx.Edition.Error("COBOLNET1642", $"FUNCTION BASECONVERT: argument-{i + 1} shall be a positive "
                    + $"nonzero numeric INTEGER literal or data item, not '{bl.Text}' (ISO §15.12.3 rule 1)");

        // r1's sub-11 half. OperandCategory is the totalized §8.5.2.1 reader, so a ref-mod view (class
        // alphanumeric, GR6c) correctly skips this arm and reads as a string.
        if (b2 is >= 2 and < 11 && args.Count > 0
            && OperandCategory(args[0]) is PicCategory.Numeric or PicCategory.NumericEdited)
        {
            bool bad = args[0] switch
            {
                BoundFieldOperand f0 => f0.Place.Item.Pic is { } p0
                    && (p0.Category is PicCategory.NumericEdited || p0.Signed || p0.Scale > 0 || p0.IsFloat),
                BoundNumericLiteral nl0 => nl0.Text.Contains('.') || nl0.Text.Contains(',')
                    || nl0.Text.StartsWith('-'),
                _ => false,   // computed results — the runtime digit screen owns their content
            };
            if (bad)
                ctx.Edition.Error("COBOLNET1642", "FUNCTION BASECONVERT: when the base in argument-2 is below "
                    + "11, argument-1 shall be an unsigned integer data item or literal (ISO §15.12.3 rule 1)");
        }
    }

    /// <summary>The statically known integer value of argument <paramref name="i"/> — a plain numeric literal
    /// only (a data item or expression is runtime territory); null when absent or not an integer literal.</summary>
    private static long? StaticIntLiteral(IReadOnlyList<BoundOperand> args, int i) =>
        i < args.Count && args[i] is BoundNumericLiteral nl && long.TryParse(nl.Text, out long v) ? v : null;

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
    /// [locale-name-1])</c> (§15.68.2). LOCALE is not a LEXER TOKEN here, so the phrase parses as extra space- or
    /// comma-separated arguments and is recognised by NAME.
    /// <para>⚠ It IS a reserved word from 2002 (§8.9; <c>reserved-words.json</c> r2002/r2014/r2023) — this comment
    /// used to say otherwise, which is wrong by the repo's own data. Not tokenizing it is a deliberate choice, not
    /// a consequence of the reservation: this detection depends on the word arriving as an ordinary argument, so a
    /// token would silently break the very diagnostic below. (fix-queue PB25.)</para>
    /// The argument-1 exclusion avoids a false positive on a data item happening to
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
        string fmt = sig.Name == "NUMVAL-C" ? "15.68.2" : "15.94.2";
        // §15.68.2/§15.94.2: ( argument-1 [argument-2] [ANYCASE] ) — a POSITIONAL walk, not an order-free
        // keyword sweep (fix-queue PB60 / FMT-15.68.2). Slot 0 is ALWAYS argument-1 and binds as an operand
        // (ANYCASE is §8.10 context-sensitive — a data item so named stays legal there); ANYCASE is admitted
        // ONCE, after the operands, and nothing may follow it. The old sweep accepted
        // NUMVAL-C(ANYCASE WS-A "USD") and a doubled trailing ANYCASE (both measured).
        bool anycase = false;
        var operands = new List<BoundOperand>();
        foreach (var (a, at) in argCtxs.Select((a, at) => (a, at)))
        {
            if (at > 0 && KeywordWordOf(a) == "ANYCASE")
            {
                if (anycase)
                {
                    ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name}: the ANYCASE keyword is repeated "
                        + $"(ISO §{fmt} — argument-1 [argument-2] [ANYCASE])");
                    return new BoundExprError($"FUNCTION {sig.Name} format");
                }
                anycase = true;
                continue;
            }
            if (anycase)
            {
                ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name}: an argument follows ANYCASE, which "
                    + $"the format places last (ISO §{fmt})");
                return new BoundExprError($"FUNCTION {sig.Name} format");
            }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count is < 1 or > 2)
        {
            ctx.Edition.Error("COBOLNET1504", $"FUNCTION {sig.Name} takes argument-1 [argument-2] [ANYCASE] "
                + $"(ISO §{fmt}); {operands.Count} operand argument(s) given");
            return new BoundExprError($"FUNCTION {sig.Name} arity");
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        // §15.68.3 r1 (mirrored onto TEST-NUMVAL-C by §15.94.3 r1) — argument-1 shall be of CATEGORY
        // alphanumeric or national. The §15.3 class row above admits the EDITED categories (class alphanumeric
        // spans alphanumeric-edited and numeric-edited, Table 2) that r1's CATEGORY wording excludes — the
        // finer axis screens here (fix-queue PB60 / AR-15.68.3-1). A ref-mod view is plain category
        // alphanumeric (§8.4.3.3.4 GR6) and passes; shapes with no static category pass to the runtime scan.
        if (operands.Count > 0
            && (OperandCategory(operands[0]) is PicCategory.NumericEdited
                || operands[0] is BoundFieldOperand { Place: not RefModPlace } f1
                   && f1.Place.Item.Pic is { Category: PicCategory.Alphanumeric, EditMask: not null }))
            ctx.Edition.Error("COBOLNET1627", $"FUNCTION {sig.Name} argument-1 is of an EDITED category; "
                + "ISO §15.68.3 rule 1 admits category alphanumeric or national only");
        // §15.68.3 r2's CONTENT halves for a LITERAL argument-2 (the same-class half rides the schema row;
        // a data item's content is the runtime guard's): at least one non-space character; none of the digits
        // 0-9 or the characters * + - , . ; no CR/DB pair in any case. The characters are named OUTRIGHT —
        // the comma/period ban does not flex with DECIMAL-POINT IS COMMA.
        if (operands.Count == 2 && operands[1] is BoundStringLiteral curLit)
        {
            string cur = curLit.Value.Trim();
            if (cur.Length == 0
                || cur.Any(c => char.IsAsciiDigit(c) || c is '*' or '+' or '-' or ',' or '.')
                || cur.Contains("CR", StringComparison.OrdinalIgnoreCase)
                || cur.Contains("DB", StringComparison.OrdinalIgnoreCase))
                ctx.Edition.Error("COBOLNET1627", $"FUNCTION {sig.Name} argument-2 shall contain at least one "
                    + "non-space character and none of the digits 0-9, the characters '*' '+' '-' ',' '.', or "
                    + "the letter pair CR/DB in any case (ISO §15.68.3 rule 2)");
        }
        if (operands.Count == 1)
        {
            // §15.68.3 r3: "If neither argument-2 nor the LOCALE keyword is specified, there shall be only one
            // currency string for the compilation unit, either the default currency sign or a currency string
            // specified in the SPECIAL-NAMES paragraph" — the unit's SoleCurrencyString ("$" with no clause, the
            // one explicitly specified string, NULL past that). Two or more distinct strings make the reference
            // illegal (kb/Work PB60 / AR-15.68.3-3 — the former scalar injected whichever clause bound last, so
            // NUMVAL-C("#1,234.56") silently returned 0 in a '#'-then-'@' unit).
            if (ctx.Data.SoleCurrencyString is { } sole)
                operands.Add(new BoundStringLiteral(sole));
            else
                ctx.Edition.Error(DiagnosticCatalog.NumvalCAmbiguousCurrency, $"FUNCTION {sig.Name} without "
                    + $"argument-2: the compilation unit specifies {ctx.Data.ExplicitCurrencyStringCount} distinct "
                    + "currency strings in its SPECIAL-NAMES paragraph; when neither argument-2 nor LOCALE is "
                    + "specified there shall be only one currency string for the compilation unit (ISO §15.68.3 "
                    + "rule 3) — name the currency string as argument-2");
        }
        return new BoundIntrinsicCall(sig, operands, PicCategory.Numeric) { Anycase = anycase };
    }

    /// <summary>An operand whose comparison/result category is alphanumeric (drives MAX/MIN resolution): a string
    /// literal, an alphanumeric/edited/group item, or a nested alphanumeric-result intrinsic.</summary>
    /// <summary>
    /// Screen every argument's CLASS against the catalog row's declared <c>ArgKinds</c> (ISO §15.3 argument
    /// types; fix-queue PB1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rejects under strict conformance and warns under <c>--permissive</c>, matching DA6/<c>COBOLNET0844</c>,
    /// which settled the same question for §8.8.1.1 arithmetic operands one wave earlier — the leniency is
    /// dialect-gated, never silent.
    /// </para>
    /// <para>
    /// ⚠ Binding CONTINUES after a violation rather than returning a <c>BoundExprError</c>. The argument is
    /// well-formed, its class is merely wrong, so the rest of the call still binds and later rules still report —
    /// which is what lets one compile name every bad argument in a statement instead of the first. Under
    /// <c>--permissive</c> the existing coercion then runs unchanged, so the leniency is genuinely the old
    /// behaviour and not a second code path.
    /// </para>
    /// </remarks>
    private void CheckArgumentClasses(IntrinsicSig sig, IReadOnlyList<BoundOperand> args)
    {
        // ⛔ Driven by the SPEC-VERIFIED table, not by sig.ArgKinds. The catalog's hint column is unaudited —
        // BYTE-LENGTH is declared "s" while §15.14.3 admits any class, and an empty ArgKinds defaults to 'n',
        // which would screen LENGTH as numeric-only. Screening from it rejected 12 legal corpus programs.
        // A function absent from Verified is not screened, so this can only ever ADD rejections that a cited
        // §15 rule demands. sig.ArgKind(i) still supplies the POSITION when the rule is per-argument.
        if (!IntrinsicArgumentRules.Verified.TryGetValue(sig.Name, out var schema)) return;

        // PER-POSITION (fix-queue PB12). `schema.At(i)` is null for a position the clause does not describe —
        // UNSCREENED, never screened by whatever the previous position declared, which is exactly the mistake a
        // one-kind-per-function table forced on FIND-STRING and the FORMATTED-* family.
        for (int i = 0; i < args.Count; i++)
        {
            if (schema.At(i) is not { } rule) continue;
            // "Argument-N shall not be a zero-length literal" (fix-queue PB35) — a LENGTH rule, not a class one,
            // so it is tested beside the class rule rather than through it. SIX §15 clauses state it and exactly
            // ONE was enforced: NATIONAL-OF's, hand-written in CheckRepertoireArgs, while MAX, MIN, ORD-MAX,
            // ORD-MIN and STANDARD-COMPARE accepted `FUNCTION MAX("" "AB")` silently at every edition.
            // ⚠ THE TEST IS ON THE LITERAL, NOT ON THE LENGTH. A zero-length ITEM is legal in every one of these
            // positions — the clauses each say "zero-length LITERAL" — so screening on width would reject legal
            // source, which is how PB1 turned 12 conforming corpus programs away.
            if (rule.NoZeroLengthClause is { } zlClause && args[i] is BoundStringLiteral { Value.Length: 0 })
                Report($"FUNCTION {sig.Name} argument-{i + 1} is a zero-length literal, which {zlClause} "
                    + "does not admit");
            if (IntrinsicArgumentRules.Violation(rule.Kind, args[i]) is not { } why) continue;
            Report($"FUNCTION {sig.Name} argument-{i + 1} {why} ({rule.Clause})");
        }

        // CROSS-ARGUMENT (fix-queue PB31) — §15.59.3 r2 and its siblings, which no per-position check can see.
        if (IntrinsicArgumentRules.CrossViolation(schema, args) is { } cross)
            Report($"FUNCTION {sig.Name} {cross}");

        void Report(string where)
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning(DiagnosticCatalog.IntrinsicArgumentClass,
                    $"{where}; accepted under --permissive with the existing coercion");
            else
                ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
                    $"{where}. --permissive accepts it as a coercion extension");
        }
    }

    private static bool IsStringOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral => true,
        // National/boolean operands participate through the char pipeline (MAX/MIN over national compares
        // ordinal per D-N3); a nested intrinsic with a string-class result (alphanumeric OR national —
        // NATIONAL-OF/CONVERT-to-NAT) is a string operand likewise. ⛔ The FIELD arm reads the ONE classifier
        // (PB59 family 7b) so the BODY choice and UniformArgumentType's TYPE resolution can never split on a
        // shape again: a group is category alphanumeric (§8.5.2.1), and a ref-mod view takes GR6's rewrites —
        // the pre-7b split crashed MAX(G1 G2) at run time ("no numeric render recipe").
        BoundFieldOperand => OperandCategory(op) is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean,
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } } => true,
        // A figurative whose ONLY reading is a character value — SPACE, QUOTE, HIGH-VALUE, LOW-VALUE (§8.3.3.6.4
        // GR5–GR8) — is a string operand exactly as a one-character alphanumeric literal is. ZERO is excluded by
        // the neutrality test rather than by name, so the §8.3.3.6.4 table stays written down once (PB48).
        BoundFigurative => !IsClassNeutralOperand(op),
        // ALL "literal" is an alphanumeric literal repeated (§8.3.3.6.4 GR9); never numeric (§8.3.3.6.3 SR1a).
        BoundAllLiteral => true,
        _ => false,
    };

    /// <summary>Can this operand present a NUMERIC class and a character one both — i.e. does it take its class
    /// from the context rather than bringing one (§8.3.3.6.4 GR4)? True for the figurative ZERO and nothing else
    /// the binder can build today.</summary>
    /// <remarks>Derived from <see cref="IntrinsicArgumentRules.CandidateClasses"/> rather than testing
    /// <c>Kind == 'Z'</c>, so the §8.3.3.6.4 GR1/GR4–GR8 reading lives in ONE table and a change there cannot
    /// leave this dispatch behind (<c>feedback_one_rule_one_place</c>).</remarks>
    private static bool IsClassNeutralOperand(BoundOperand op) =>
        IntrinsicArgumentRules.CandidateClasses(op) is { Length: > 1 } candidates
        && candidates.Contains(CobolClass.Numeric);

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
        // ⛔ A REFERENCE-MODIFIED ARGUMENT IS LEGAL AND ITS LENGTH IS THE RUNTIME CHANNEL'S ANSWER (fix-queue
        // PB24). §15.50.3 r1 admits "a data item of any class or category", and §8.4.3.3.3 SR5 makes a
        // reference-modified item a data item — so `FUNCTION LENGTH(WS-NAME(1:5))` is conforming source. It used
        // to bind to a BoundExprError, which COMPILES CLEAN and throws NotImplementedCobolFeatureException at RUN
        // TIME (the PB7/DA7 wrong-stage family): measured exit 127 on a five-line program.
        // No new machinery is needed. `IntrinsicRenderer`'s Length arm renders its argument through the ONE
        // string channel, and a ref-modified place renders as the SUBSTRING — so the runtime `.Length` over that
        // image IS §15.50.4's character-position count, for the literal `(1:5)` form and the runtime `(I:L)` form
        // alike, and the omitted-length `(I:)` form comes out right because the substring already ends where the
        // item does.
        BoundFieldOperand { Place: RefModPlace } => new BoundIntrinsicCall(sig, args, PicCategory.Numeric),
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
        // ⛔ A VARIABLE-LENGTH GROUP IS §15.50.4 r7's SUM, NOT A FIXED WIDTH (fix-queue PB24). This arm did not
        // exist, so such a group fell through to the fixed fold below — and `DataItem.ImageWidth` contributes
        // ZERO for a dynamic-length child (its width is a runtime fact, §8.5.1.10). MEASURED: a group of
        // `PIC X(4)` plus a `PIC X DYNAMIC LENGTH` child holding "XYZ" returned **4** where r7 requires **7**,
        // with no diagnostic. A missing arm in a dispatch, which is this compiler's most reproducible defect
        // shape — and the silent kind, which is the worst.
        // ⚠ THE CHEAP FIX DOES NOT WORK AND WAS TRIED: routing the group through the runtime string channel
        // fails, because the whole-group IMAGE of a group with a dynamic child is itself staged loud (the Tier-C
        // byte island). So r7 is built from the structure, which is known at COMPILE time:
        //   r7(a) the fixed subordinates — exactly what `ImageWidth` already returns, since it zeroes the
        //         dynamic ones; and
        //   r7(b) the CURRENT length of each dynamic-length subordinate — one runtime LENGTH per leaf, summed.
        BoundFieldOperand g when g.Place.Item.IsGroup && HasDynamicLengthLeaf(g.Place.Item)
                                 && !HasDynamicCapacityTable(g.Place.Item)
            => VariableLengthGroupSum(sig, g.Place.Item),
        // ⚠ r7(c) — a subordinate DYNAMIC-CAPACITY table — is NOT implemented, and is staged LOUD rather than
        // summed wrong. `ImageWidth` counts such a table as ONE occurrence (`c.Occurs ?? 1`), so folding it would
        // return a plausible number that is wrong by (capacity − 1) elements. A loud stage is a worse experience
        // and a better answer than a silent miscount (§1.4).
        BoundFieldOperand g when g.Place.Item.IsGroup && HasDynamicCapacityTable(g.Place.Item) =>
            new BoundExprError("FUNCTION LENGTH of a group with a subordinate DYNAMIC-CAPACITY table "
                               + "(current capacity is a runtime value, ISO §15.50.4 rule 7c)"),
        // ⛔ §15.50.4 r1 — AN ELEMENTARY BOOLEAN ITEM'S LENGTH IS IN BOOLEAN POSITIONS, NOT IN THE CHARACTER
        // POSITIONS IT OCCUPIES: "If argument-1 is a bit group item, an elementary boolean data item, a boolean
        // literal, or a type declaration for a boolean item, the returned value is an integer equal to the length
        // of argument-1 in boolean positions." The generic fold below reads ImageWidth, and for a USAGE BIT item
        // that is now ceil(n/8) — its OCCUPANCY (design D19, fix-queue PB43) — so `PIC 1(8) USAGE BIT` would fold
        // to 1 instead of 8.
        // ⚠ THIS ARM WAS NOT MISSING BEFORE; IT WAS UNNECESSARY, and that is the more useful way to say it. While
        // USAGE BIT was stored one character per bit, boolean positions and occupied positions were the same
        // number, so r1 and r3 could not be told apart and the generic arm answered both. Giving bit items their
        // real occupancy separates the two rules, and the separation is what makes r1 need its own arm.
        // Both usages come here: a DISPLAY-form boolean item (§13.18.60.3 SR13(b)) is equally "an elementary
        // boolean data item", and for it the answer is unchanged because occupancy IS the boolean-position count.
        BoundFieldOperand { Place.Item: { IsElementary: true, Pic.Category: PicCategory.Boolean } b } =>
            new BoundNumLiteral(Math.Max(1, b.Pic!.Length).ToString()),
        BoundFieldOperand f => new BoundNumLiteral(Math.Max(1, f.Place.Item.ImageWidth).ToString()),
        // A nested string-result intrinsic (alphanumeric OR national — one UTF-16 char per national position,
        // D-N1, so .Length IS the §15.50.4 character-position count for both) keeps a runtime .Length.
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National } } =>
            new BoundIntrinsicCall(sig, args, PicCategory.Numeric),   // runtime .Length over the nested result image
        // ⛔ A FIGURATIVE CONSTANT IS A LEGAL LENGTH ARGUMENT, AND THE ARM BELOW USED TO REFUSE IT ALONGSIDE THE
        // NUMERIC LITERAL IT CORRECTLY REFUSES (fix-queue PB25). One arm standing for TWO rules enforced neither:
        // §15.50.3 r1 restricts a LITERAL argument to "an alphanumeric, national, or boolean literal", which
        // excludes a NUMERIC literal — but says nothing about figurative constants, and §8.3.3.6.3 SR1 admits one
        // "whenever 'literal' appears in a format" except where the literal is restricted to NUMERIC (a) or a
        // syntax rule prohibits it (b). Neither exception applies, and §8.3.3.6.4 GR1 makes SPACE/QUOTE an
        // ALPHANUMERIC character value here — precisely what r1 permits. So `FUNCTION LENGTH(SPACE)` is legal.
        // The length is §8.3.3.6.4 GR3's, the "length not specified by the context" case that a bare argument is:
        //   (b) a figurative other than ALL literal-1 is ONE character;
        //   (c) otherwise, the length of literal-1.
        // Both are compile-time constants, so they fold exactly like the literal arm above rather than reaching
        // the runtime channel.
        BoundFigurative => new BoundNumLiteral("1"),                                     // §8.3.3.6.4 GR3b
        BoundAllLiteral a => new BoundNumLiteral(Math.Max(1, a.Literal.Length).ToString()),  // §8.3.3.6.4 GR3c
        // A NUMERIC literal is still not a valid LENGTH argument — §15.50.3 r1 admits only "an alphanumeric,
        // national, or boolean literal" (a numeric *data item* is allowed as "a data item of any class or
        // category", handled by the BoundFieldOperand arm above). That half of the old arm was always correct.
        _ => InadmissibleArgument(sig,
            "is a numeric literal, which §15.50.3 r1 does not admit — it takes an alphanumeric, national or "
            + "boolean literal, a based entry, a type-name, or a DATA ITEM of any class (a numeric ITEM is fine)",
            "§15.50.3 r1"),
    };

    /// <summary>
    /// FUNCTION LENGTH (§15.50.2) — argument-1 plus the optional PHYSICAL keyword.
    /// </summary>
    /// <remarks>
    /// <para>⛔ <b>PHYSICAL IS A KEYWORD, NOT AN ARGUMENT</b>, and the generic argument path counted it as one:
    /// `FUNCTION LENGTH(WS-G PHYSICAL)` — conforming source — was rejected with "COBOLNET1504: FUNCTION LENGTH
    /// takes 1 argument(s); 2 given" (fix-queue PB24). Consumed here exactly as ANYCASE is for the NUMVAL-C
    /// family, so no grammar change is needed: a bare word already parses as an argument context.</para>
    /// <para>⚖ <b>WHAT IT RETURNS IS AN IMPLEMENTOR DETERMINATION, AND §15.50.4 r8 IS THE ONE THAT MAKES IT
    /// SMALL.</b> r8's closing sentence: <i>"If argument-1 is physically located where it is defined, LENGTH
    /// returns the same value that would be returned had the PHYSICAL argument not been specified."</i> COBOL.NET
    /// determines that a variable-length group IS physically located where it is defined — the program has no
    /// addressable out-of-line pointer to observe, and the group presents as a contiguous character image at its
    /// defined position — so PHYSICAL returns the r7 value. The alternative reading (r8's middle sentence: "the
    /// returned value includes only the length of the implementor-defined pointer") would require inventing a
    /// user-visible pointer width that nothing in this implementation exposes. Recorded in
    /// <c>docs/CONFORMANCE.md</c> per §4.2.16.</para>
    /// <para>The keyword is therefore ACCEPTED and semantically transparent — which is the whole defect: the
    /// prior behaviour was not a different answer, it was a REJECTION of legal source.</para>
    /// </remarks>
    private BoundExpr BindLengthFamily(IntrinsicSig sig, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        bool physical = false;
        var operands = new List<BoundOperand>();
        foreach (var a in argCtxs)
        {
            if (KeywordWordOf(a) == "PHYSICAL") { physical = true; continue; }
            operands.Add(BindArgOperand(a));
        }
        if (operands.Count != 1)
        {
            ctx.Edition.Error("COBOLNET1504",
                $"FUNCTION LENGTH takes argument-1 [PHYSICAL] (ISO §15.50.2); {operands.Count} operand argument(s) given");
            return new BoundExprError("FUNCTION LENGTH arity");
        }
        // ⛔ THE §15.3 SCREEN IS CALLED HERE BECAUSE THIS BINDER RETURNS BEFORE THE GENERIC ONE REACHES IT
        // (fix-queue PB12). CheckArgumentClasses sits after arity on the generic path and its comment
        // claimed it ran "before every per-function arm" — FALSE for the eight bespoke binders that
        // `return` above it, so no Verified row could ever screen them. Screened here, after this
        // binder's own arity check, exactly as the generic path orders it.
        CheckArgumentClasses(sig, operands);
        _ = physical;   // §15.50.4 r8 — see the remarks: transparent under this implementation's determination.
        return BindLengthFold(sig, operands);
    }

    /// <summary>Does this group have a DYNAMIC LENGTH elementary item somewhere beneath it? (§15.50.4 r7b.)
    /// Recursive, because r7 says "all subordinate", not "all immediate children".</summary>
    private static bool HasDynamicLengthLeaf(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.IsDynamicLength || (c.IsGroup && HasDynamicLengthLeaf(c))));

    /// <summary>Does this group have a DYNAMIC-CAPACITY table beneath it? (§15.50.4 r7c — not implemented, staged
    /// loud rather than miscounted; see the arm that uses this.)</summary>
    private static bool HasDynamicCapacityTable(DataItem g) =>
        g.Children.Any(c => c.RedefinesTargetName is null
                            && (c.IsDynamicTable || (c.IsGroup && HasDynamicCapacityTable(c))));

    /// <summary>
    /// §15.50.4 r7 as an expression: the fixed subordinates (r7a) plus the CURRENT length of every dynamic-length
    /// subordinate (r7b).
    /// </summary>
    /// <remarks>
    /// <para>r7a is <see cref="DataItem.ImageWidth"/> unchanged — it already sums the fixed subordinates and
    /// contributes zero for each dynamic-length one, which is precisely the split r7 asks for. That is why this
    /// adds to it rather than recomputing it: two independent width walks would be two things to keep in
    /// agreement, and the fixed-layout math elsewhere already depends on this one.</para>
    /// <para>Each dynamic leaf contributes one runtime <c>FUNCTION LENGTH</c> over the leaf itself — the same
    /// single-item path measured correct before this change (a `DYNAMIC LENGTH` item holding "ABCDEFG" returns 7).
    /// A leaf whose place cannot be resolved is not silently dropped: the whole fold degrades to the loud stage,
    /// because an under-counted length is a wrong answer and a missing one is a visible failure.</para>
    /// </remarks>
    private BoundExpr VariableLengthGroupSum(IntrinsicSig sig, DataItem group)
    {
        var leaves = new List<DataItem>();
        void Walk(DataItem g)
        {
            foreach (var c in g.Children.Where(x => x.RedefinesTargetName is null))
            {
                if (c.IsDynamicLength) leaves.Add(c);
                else if (c.IsGroup) Walk(c);
            }
        }
        Walk(group);

        BoundExpr sum = new BoundNumLiteral(group.ImageWidth.ToString());          // r7a
        foreach (DataItem leaf in leaves)
        {
            if (ctx.Refs.ResolveItem(leaf) is not { } place)
                return new BoundExprError($"FUNCTION LENGTH of a variable-length group: the dynamic-length "
                                          + $"subordinate '{leaf.CobolName ?? "(unnamed)"}' could not be addressed (ISO §15.50.4 rule 7b)");
            sum = new BoundBinary(sum, '+',
                new BoundIntrinsicCall(sig, [new BoundFieldOperand(place)], PicCategory.Numeric));   // r7b
        }
        return sum;
    }

    /// <summary>FUNCTION BYTE-LENGTH (§15.14.4 r1): the argument's length in BYTES — the compile-time twin of the
    /// LENGTH fold, counting bytes instead of character positions (D7). §15.14.3 r1 restricts a LITERAL argument
    /// to an ALPHANUMERIC or NATIONAL literal (unlike LENGTH, a boolean/numeric literal is NOT valid): an
    /// alphanumeric literal is 1 byte/char, a national literal 2 bytes/char (D-N1). A fixed data item folds to
    /// <see cref="DataItem.ByteWidth"/> (the pinned per-usage widths). Runtime-length shapes — a reference-modified
    /// view, a variable-length (OCCURS DEPENDING) group, or an ANY LENGTH item (§15.14.4 r2/r5) — have a byte
    /// length known only at runtime; with no runtime BYTE-LENGTH body (the §15.14 CONSTANT-entry path aside) they
    /// stage LOUD by name, the LENGTH discipline (§1.4).</summary>
    /// <summary>An argument the §15 rules make INADMISSIBLE, reported where it is decidable — at BIND (fix-queue
    /// PB52 cause 3). Returns a <see cref="BoundExprError"/> so the caller's expression shape is unchanged.</summary>
    /// <remarks>
    /// ⛔ THE DISTINCTION THIS DRAWS IS THE POINT, because the LENGTH/BYTE-LENGTH folds return
    /// <c>BoundExprError</c> for TWO different reasons and only one of them is a defect. A ref-modified, ODO,
    /// ANY LENGTH or DYNAMIC LENGTH argument has a RUNTIME length these compile-time folds genuinely cannot
    /// produce — a staged gap, correctly loud where it is discovered. **An inadmissible LITERAL is not that**:
    /// §15.14.3 r1 and §15.50.3 r1 decide it from the source text alone, so reporting it at run time is the
    /// wrong-STAGE family ([[PB47]]'s shape), not a missing capability.
    /// <para>
    /// ⚠ IT IS A HARD ERROR AND DOES NOT TAKE THE <c>--permissive</c> DOWNGRADE the class screen's
    /// <c>Report</c> helper applies. That helper's warning says "accepted … with the existing coercion", and
    /// here there is no coercion to accept: the fold has no value it could produce for a numeric literal, so a
    /// warning would be followed by the identical run-time abort. Permissive is the migration mode for
    /// constructs an EDITION removed, and this is illegal in every edition.
    /// </para>
    /// </remarks>
    private BoundExpr InadmissibleArgument(IntrinsicSig sig, string why, string clause)
    {
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
            $"FUNCTION {sig.Name} argument-1 {why} (ISO {clause})");
        return new BoundExprError($"FUNCTION {sig.Name} argument ({why}, ISO {clause})");
    }

    private BoundExpr BindByteLengthFold(IntrinsicSig sig, List<BoundOperand> args) => args[0] switch
    {
        BoundStringLiteral { Category: PicCategory.National } s => new BoundNumLiteral((2 * Math.Max(1, s.Value.Length)).ToString()),
        BoundStringLiteral { Category: PicCategory.Alphanumeric } s => new BoundNumLiteral(Math.Max(1, s.Value.Length).ToString()),
        // A literal of any OTHER class — in practice a BOOLEAN literal, since the alphanumeric and national
        // arms matched above. §15.14.3 r1 admits "an alphanumeric or national literal" and stops there.
        // ⚠ THE SIBLING CLAUSE DIFFERS AND THE TWO MUST NOT BE UNIFIED: §15.50.3 r1 admits "an alphanumeric,
        // national, or BOOLEAN literal", so `FUNCTION LENGTH(B"101")` is legal where BYTE-LENGTH's is not.
        BoundStringLiteral => InadmissibleArgument(sig,
            "is a literal of a class §15.14.3 r1 does not admit — it takes an alphanumeric or national literal "
            + "(a boolean literal is admissible to FUNCTION LENGTH, §15.50.3 r1, but not here)", "§15.14.3 r1"),
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
        // ⛔ THE FIGURATIVE HALF OF THE ARM BELOW WAS FALSE, AND IT IS PB25's OWN DEFECT IN THE ADJACENT METHOD
        // (fix-queue PB48 sweep). PB25 gave BindLengthFold its §8.3.3.6.4 GR3 arms and cited the reasoning in
        // full; BYTE-LENGTH — the neighbouring fold, with the same rule shape — kept a default arm that named
        // "a numeric/FIGURATIVE literal" as invalid on the authority of §15.14.3. §15.14.3 r1 says the opposite:
        // "Argument-1 shall be an alphanumeric or national literal, a based entry, a type-name, or a data item of
        // any class or category", and §8.3.3.6.4 GR1 makes a figurative in a character context an ALPHANUMERIC
        // character value — exactly what r1 admits. §8.3.3.6.3 SR1 bars a figurative only where the literal is
        // restricted to NUMERIC (a) or a syntax rule prohibits it (b); neither applies. So
        // `FUNCTION BYTE-LENGTH(SPACE)` is legal source that aborted at RUN TIME, and it did so before PB48 as
        // well — ZERO merely joined it once the figurative stopped being rewritten (feedback_scan_all_similar).
        // The byte count follows §8.3.3.6.4 GR3: (b) one character for a figurative other than ALL literal-1,
        // (c) the length of literal-1 otherwise; both are alphanumeric here, so one byte per character (D-N1).
        BoundFigurative => new BoundNumLiteral("1"),                                        // §8.3.3.6.4 GR3b
        BoundAllLiteral a => new BoundNumLiteral(Math.Max(1, a.Literal.Length).ToString()), // §8.3.3.6.4 GR3c
        // A NUMERIC literal remains invalid — §15.14.3 r1 admits only "an alphanumeric or national literal"
        // (a numeric DATA ITEM is "a data item of any class or category" and folds on the arm above).
        _ => InadmissibleArgument(sig,
            "is a numeric literal, which §15.14.3 r1 does not admit — it takes an alphanumeric or national "
            + "literal, a based entry, a type-name, or a DATA ITEM of any class (a numeric ITEM is fine)",
            "§15.14.3 r1"),
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
        // §8.5.2.12 items 3/4/5 make the LINAGE-/LINE-/PAGE-COUNTER registers category-numeric DATA ITEMS, so
        // §15.43.3/§15.58.3/§15.83.3 r1 ADMITS them (kb/Work R26 — they used to draw the r1 rejection). Their
        // capacity: LINAGE-COUNTER's "size is equal to the page size specified in the LINAGE clause"
        // (§8.4.3.14.4 GR1) — a literal operand's value directly, a data-name operand's all-nines (the maximum
        // page size it can specify); the report counters carry NO size in the standard (§8.4.3.15.4 GR1 —
        // "temporary unsigned integer data items") and take the documented implementor shape PIC 9(18), the
        // all-nines of the runtime's long carrier (CONFORMANCE.md §3, "the counter registers' declared
        // capacity" — NOT an Annex A.1 item, so it lives with the pinned behavior determinations). All three
        // registers are UNSIGNED:
        // LOWEST-ALGEBRAIC = 0, SMALLEST-ALGEBRAIC = 1 (scale 0).
        if (args[0] is BoundComputedOperand { Expr: BoundLinageCounterRef lc })
            return CounterRegisterFold(sig, LinagePageCapacity(lc.File));
        if (args[0] is BoundComputedOperand { Expr: BoundReportCounterRef })
            return CounterRegisterFold(sig, "999999999999999999");
        if (args[0] is not BoundFieldOperand f || f.Place is RefModPlace || f.Place.Item.IsGroup
            || f.Place.Item.Pic is not { } pic)
            return AlgebraicArgError(sig);
        // r1 admits "a data item … and shall not be an integer function or numeric function" — an exclusion
        // that must be WRITTEN because §8.5.2.12 items 6/7 make functions category numeric too. A USER-DEFINED
        // function's result binds to a synthesized caller temp that wears BoundFieldOperand exactly like
        // declared data, so `FUNCTION HIGHEST-ALGEBRAIC(FUNCTION MY-FN)` folded a constant from the TEMP's
        // PICTURE (kb/Work R26, ledger F76). The temp flag is the positive is-a-declared-item test — placed
        // HERE, not in OperandOf: every other verb legitimately needs the temp→operand mapping.
        if (f.Place.Item.IsCompilerTemp)
        {
            string fsec = sig.Name == "SMALLEST-ALGEBRAIC" ? "15.83.3"
                : sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3";
            ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: argument-1 is a FUNCTION RESULT, and "
                + $"ISO §{fsec} rule 1 admits a data item and \"shall not be an integer function or numeric "
                + "function\" — name the data item whose description defines the range instead");
            return new BoundExprError($"FUNCTION {sig.Name} function argument");
        }
        // §15.83.3 r1: SMALLEST admits ONLY category numeric; HIGHEST/LOWEST also admit numeric-edited.
        bool edited = pic.Category is PicCategory.NumericEdited;
        if (pic.Category is not PicCategory.Numeric && !(edited && sig.Name != "SMALLEST-ALGEBRAIC"))
            return AlgebraicArgError(sig);
        if (pic.Usage is Usage.Index)   // class index — not category numeric (§13.18.60)
            return AlgebraicArgError(sig);

        // Float usage — kb/Work R10 (Phase-B F72): the implementor-defined usage latitude is SMALLEST-ALGEBRAIC's
        // ALONE (§15.83.3 r4; Annex A.1 item 180 documents it for SMALLEST only). HIGHEST/LOWEST (§15.43.3 /
        // §15.58.3) carry exactly the two mode-aware bars below — under NATIVE arithmetic (the default) nothing
        // bars any float, §8.5.2.12 item 2 makes every float usage category numeric, and §15.43.4 r2 /
        // §15.58.4 r2 define the value as the greatest finite magnitude representable in argument-1 (negated for
        // LOWEST — every float usage is signed). The old guard rejected ALL floats for ALL THREE functions, and
        // its text asserted COMP-1/COMP-2 are barred by rule 2 under STANDARD-DECIMAL — false: rule 2 bars only
        // the §3.166 STANDARD BINARY usages (FLOAT-BINARY-*), never the native family.
        if (pic.IsFloat)
        {
            if (sig.Name == "SMALLEST-ALGEBRAIC")
            {
                ctx.Edition.Error("COBOLNET1516", "FUNCTION SMALLEST-ALGEBRAIC does not support a floating-point "
                    + "argument: the usage restriction under native arithmetic is implementor-defined "
                    + "(ISO §15.83.3 r4 / Annex A.1 item 180), and COBOL.NET defines no smallest positive "
                    + "increment for IEEE floats (it is exponent-dependent, not a PICTURE property)");
                return new BoundExprError($"FUNCTION {sig.Name} float argument");
            }
            bool stdBinaryUsage = pic.Usage is Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128;
            bool stdDecimalUsage = pic.Usage is Usage.FloatDecimal16 or Usage.FloatDecimal34;
            if (ctx.Data.Options.Arithmetic == ArithmeticMode.StandardDecimal && stdBinaryUsage)
            {
                ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: under STANDARD-DECIMAL arithmetic "
                    + "argument-1 shall not specify a standard binary floating-point usage "
                    + $"(ISO §{(sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3")} rule 2)");
                return new BoundExprError($"FUNCTION {sig.Name} float argument");
            }
            if (ctx.Data.Options.Arithmetic == ArithmeticMode.StandardBinary && stdDecimalUsage)
            {
                ctx.Edition.Error("COBOLNET1516", $"FUNCTION {sig.Name}: under STANDARD-BINARY arithmetic "
                    + "argument-1 shall not specify a standard decimal floating-point usage "
                    + $"(ISO §{(sig.Name == "HIGHEST-ALGEBRAIC" ? "15.43.3" : "15.58.3")} rule 3)");
                return new BoundExprError($"FUNCTION {sig.Name} float argument");
            }
            // The greatest finite magnitude of the item's CARRIER (PicInfo.ClrType's mapping — §15.43.4 r2's
            // "represented in argument-1" is a property of this implementation's representation).
            string max = pic.Usage switch
            {
                Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => "3.4028235E+38",
                Usage.FloatDecimal16 or Usage.FloatDecimal34 => "79228162514264337593543950335",
                _ => "1.7976931348623157E+308",   // binary64 carrier: COMP-2 / FLOAT-LONG / -EXTENDED / -BINARY-64/128
            };
            return new BoundNumLiteral(sig.Name == "LOWEST-ALGEBRAIC" ? "-" + max : max);
        }

        // SMALLEST — the smallest positive increment 10^(−scale), independent of digit count / sign / container.
        if (sig.Name == "SMALLEST-ALGEBRAIC")
            return new BoundNumLiteral(Decimalize(System.Numerics.BigInteger.One, pic.Scale, negative: false));

        int scale; System.Numerics.BigInteger unscaled; bool signable;
        if (edited)
        {
            var (cap, frac) = CobolNet.Runtime.CobolEdit.MaskCapacity(pic.EditMask!, '$', ctx.Data.DecimalPointIsComma);
            scale = frac;
            unscaled = Pow10(cap) - 1;   // all-nines over the mask's digit positions (§13.18.40.4)
            signable = pic.EditMask!.IndexOf('+') >= 0 || pic.EditMask!.IndexOf('-') >= 0
                       || pic.EditMask!.Contains("CR") || pic.EditMask!.Contains("DB");
        }
        else if (pic.Truncation == CobolNet.Runtime.NumericTruncation.BinaryCapacity)
        {
            // ⛔ Keyed on the ONE capacity-discipline table (PicInfo.Truncation) — this branch previously carried
            // its own usage list (Comp5/BinaryChar/BinaryShort/BinaryLong/BinaryDouble), the same rule written
            // twice (kb/Work R10, F74's drift-test demand); AlgebraicFoldContainerAgreementTests pins the fold's
            // bound against the runtime capacity for every BinaryCapacity profile. The UNSIGNED container bound
            // 2^128−1 (a 16-byte container, §13.18.60.4 GR12) exceeds Int128 — Decimalize's BigInteger arm
            // renders it and EmitCore.IntLiteralX carries it as a UInt128 literal (F73's crash was this value
            // forced through Int128.Parse).
            scale = pic.Scale;
            int bits = 8 * pic.StorageWidth;   // container width (§13.18.60.4 GR12) — the item owns the full range
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

    /// <summary>The HIGHEST/LOWEST/SMALLEST fold for a counter register (kb/Work R26): the registers are
    /// UNSIGNED integers (§8.4.3.14.4 GR1 / §8.4.3.15.4 GR1), so LOWEST is 0 and SMALLEST is 1 (scale 0);
    /// HIGHEST is the register's capacity, computed by the caller.</summary>
    private static BoundExpr CounterRegisterFold(IntrinsicSig sig, string highest) => sig.Name switch
    {
        "SMALLEST-ALGEBRAIC" => new BoundNumLiteral("1"),
        "LOWEST-ALGEBRAIC" => new BoundNumLiteral("0"),
        _ => new BoundNumLiteral(highest),
    };

    /// <summary>§8.4.3.14.4 GR1 — LINAGE-COUNTER's size "is equal to the page size specified in the LINAGE
    /// clause": a literal operand's value directly; for a data-name operand the page size is set at run time,
    /// so the capacity is the MAXIMUM the operand item can specify — its all-nines (§15.43.4 r2's "may be
    /// represented in argument-1" read against the register's largest possible size).</summary>
    private string LinagePageCapacity(Model.FileModel file)
    {
        var body = file.Linage?.Body;
        if (body?.Literal is { } lit) return lit.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (body?.DataName is { } dn && ctx.Symbols.TryResolve(dn, ctx.ActiveScope, out var items)
            && items[0].Pic is { } p && p.Digits > 0)
            return new string('9', p.Digits);
        return "999999999999999999";   // no resolvable operand shape — the long carrier's own bound
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
    /// <para>The BigInteger arm is LIVE, not defensive: the HIGHEST-ALGEBRAIC fold of a 16-byte UNSIGNED
    /// container is 2^128−1 (39 digits, beyond <see cref="Int128"/>), and this arm renders it. ⚠ Its previous
    /// text asserted "nothing here currently produces one" — an unverified claim that hid exactly this value
    /// (kb/Work R10, F73): the rendered literal was then forced through <c>Int128.Parse</c> and threw at run
    /// time. <c>EmitCore.IntLiteralX</c> now carries a positive magnitude beyond Int128 as a <c>UInt128</c>
    /// literal on the unsigned-wide lane.</para></summary>
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
        {
            // A phrase word this function does not take — REPORTED, not silently staged (kb/Work R19): the
            // OMITTED arm above reports and this arm did not, so `FUNCTION EXP10(LEADING)` compiled with zero
            // diagnostics and threw at run time (§4.2.2 ¶3 obliges the indication).
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentNotAValue,
                $"the reserved phrase word '{kw.GetText()}' is written as an intrinsic-function argument, but "
                + "this function's §15.x.2 general format takes no phrase — a phrase word is not an identifier, "
                + "literal, or expression (ISO §15.3)");
            return new BoundOperandError($"intrinsic argument '{kw.GetText()}'");
        }
        if (a.nonNumericLiteral() is { } nn) return NonNumericOperand(nn);
        // ⛔ A CONSTANT-NAME SUBSTITUTES ITS LITERAL, OF ITS OWN CLASS — HERE, BEFORE THE NUMERIC PATH BELOW
        // (fix-queue R01). §13.10.4 GR1: "the effect of specifying constant-name-1 in other than this entry is as
        // if literal-1 … were written where constant-name-1 is written", and §13.10.3 SR2 admits it "anywhere
        // that a format specifies a literal of the class and category of constant-name-1". An intrinsic argument
        // is such a position for whatever class the function's own §15.x argument rule admits.
        // Without this arm the reference fell to `BindFunctionArgumentExpr` — the §8.8.1.1 NUMERIC-expression
        // bind — and an alphanumeric or national constant was rejected as "not a numeric operand", in EVERY
        // intrinsic argument position. `FUNCTION UPPER-CASE(K-TEXT)` did not compile while
        // `FUNCTION UPPER-CASE("abcdef")` did, for source §13.10.4 GR1 makes identical.
        // ⚠ A NUMERIC constant deliberately falls through: the expression path already substitutes it correctly
        // (ExpressionBinder.RefExpr → BoundNumLiteral) and it must keep participating in arithmetic — an argument
        // like `FUNCTION MAX(K-NUM + 1)` is an expression, not a bare literal, and only that path can bind it.
        if (SoleDataReference(a) is { } cref
            && ctx.Data.ConstantOf(cref) is { Category: not PicCategory.Numeric } k)
            return new BoundStringLiteral(k.Text) { Category = k.Category };
        // An argument is NOT an §8.8.1.1 arithmetic expression: its legality comes from this function's own §15.x
        // ARGUMENT RULE, and the string functions admit alphanumeric data. The named entry says so at the call
        // site — TRIM / SUBSTITUTE / FIND-STRING / CONVERT over a PIC X item are legal (DA6).
        return OperandOf(host.Expr.BindFunctionArgumentExpr(a.arithmeticExpression()));
    }

    /// <summary>A non-numeric-literal argument as a categorized operand (§8.3.3.4/.5/.6.4 — the same decode +
    /// introduction-gate helpers every literal channel uses). HEXLIT decodes as the alphanumeric literal it is
    /// (§8.3.3.2 Format 2) — DA3.</summary>
    private BoundOperand NonNumericOperand(Core.NonNumericLiteralContext nn)
    {
        // Through the ONE literal mapping (ExpressionBinder.NonNumericLiteralOperand) — this used to be a second
        // hand-maintained copy of the same chain, which is how the hexadecimal form came to be supported in some
        // literal positions and not others (DA3). §8.8.3.3 GR3 concatenation folding and the §8.3.3.4/.5/.6.4
        // decode + introduction gates all live there now.
        if (host.Expr.NonNumericLiteralOperand(nn) is { } op) return op;
        // The F18 sibling one shape over (kb/Work R19): the null fallback was a silent runtime stage too.
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentNotAValue,
            $"the literal '{nn.GetText()}' is not a form this intrinsic-argument position admits (ISO §15.3)");
        return new BoundOperandError($"literal argument '{nn.GetText()}'");
    }

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
