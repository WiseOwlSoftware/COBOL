// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The CALL / inter-program slice of the binder (ISO §14.9.4 / §14.9.5 / §14.9.18; deep-dive design
/// COBOLNET_INTERPROGRAM_DESIGN). All semantic resolution happens here ONCE — target form, the GR5 transitive
/// pass-mode threading, the receiving-operand checks, and the per-edition gates (the G1 four-compilers rule:
/// the complete behavior where the edition HAS the construct, a targeted COBOLNET-08xx diagnostic where it
/// lacks it — see the deep-dive "Edition gating" section). P7 Step 10j: a real collaborator over
/// <see cref="BinderContext"/>; the two hooks that outlive the batch stay HOST edges — <c>InMethod</c>/
/// <c>OoBindMethodGoback</c> (the OO half converts LAST, 10s) and <c>EcBindRaising</c> (EcBinder lands 10r).
/// The 0884/0880 gates moved VERBATIM incl. the 0880 bare-GOBACK null-condition that encodes the
/// 0900-subsumption contract with the VersionConformancePass. The five bound types stayed in
/// <c>Binding/Bound/BoundCall.cs</c>.</summary>
internal sealed class CallBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>CALL</c> (ISO §14.9.4 Format 1; design D2 — the uniform opaque ABI call).</summary>
    public BoundStatement BindCall(Core.CallStatementContext call)
    {
        // ── §14.9.4.2 FORMAT 2 (fix-queue PB46 · kb/Work PB131 · PB237) ───────────────────────────────────────
        // The RENDERED general format (PDF page 619) is
        //     CALL ⎡{ identifier-1 | literal-1 } AS⎤ { NESTED | program-prototype-name-1 }
        // — the optional bracket encloses the target brace AND the word AS, and the outer brace is NOT optional.
        // So Format 2 has TWO spellings, and the compiler must recognize both:
        //   (a) `… AS NESTED` / `… AS program-prototype-name-1` — a SYNTACTIC discriminator (Format 1 has no AS
        //       phrase at all), which is what refuted PB46's "the formats are indistinguishable at parse time";
        //   (b) the bracket omitted whole: `CALL program-prototype-name-1`, spelled EXACTLY like Format 1's
        //       `CALL identifier-1`. That one IS semantic, and §14.9.4.4 GR3 b) is written for it ("If neither
        //       identifier-1 nor literal-1 is specified, program-prototype-name-1 determines the externalized
        //       program-name of the program being called").
        // Spelling (b) is resolved AFTER the data-reference attempt, below, so no program that binds today
        // changes meaning: a word that names a data item stays identifier-1.
        bool asNested = false;
        ProgramPrototype? prototype = null;
        if (call.callAsPhrase() is { } asPhrase)
        {
            string asWord = asPhrase.cobolWord().GetText();
            asNested = string.Equals(asWord, "NESTED", StringComparison.OrdinalIgnoreCase);
            if (!asNested)
            {
                // §14.9.4.3 SR16: "Program-prototype-name-1 shall be specified in a program-specifier in the
                // REPOSITORY paragraph." ONE lookup answers it, because ProgramPrototypesOf also registers
                // §8.4.6.8's no-specifier spelling (a containing program definition's program-name).
                if (ResolvePrototype(asWord, "CALL … AS") is not { } proto) return new BoundNop();
                prototype = proto;
            }
            else
            {
                // §14.9.4.3 SR13: "The NESTED phrase may be specified only in a program definition" — a
                // function or method definition contains no programs (kb/Work PB132; the capability was one
                // property access away and BindCall never read it).
                if (host.InMethod || host.UdfSelfName is not null)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallAsNestedContext,
                        $"CALL … AS NESTED inside a {(host.InMethod ? "method" : "function")} definition: the "
                        + "NESTED phrase may be specified only in a program definition (ISO §14.9.4.3 SR13)");
                    return new BoundNop();
                }
                // §14.9.4.3 SR15: literal-1 shall be specified, and shall name a COMMON program or a program
                // directly contained in the calling program.
                if (call.callTarget()?.literal() is null)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallAsNestedScope,
                        "CALL … AS NESTED: literal-1 shall be specified — the NESTED phrase names a contained or "
                        + "COMMON program by its PROGRAM-ID literal, not through an identifier (ISO §14.9.4.3 SR15)");
                    return new BoundNop();
                }
            }
        }
        // ⛔ ONE Format-2 callee signature, TWO producers (kb/Work PB237). Everything downstream — GR9's
        // formal-decides mode derivation, SR19/SR21's explicit-phrase agreement, SR24's OPTIONAL correspondence,
        // §14.8.2's argument conformance and SR25→§14.8.3's returning conformance — reads `calleeFormals` /
        // `callee`, never "was this AS NESTED". Wiring the prototype producer therefore delivered the whole
        // §14.8.2/§14.8.3 regime to prototype calls in one assignment rather than by copying seven checks.
        CalleeSignature? callee = null;
        // kb/Work PB131 — the AS NESTED callee's bound formals (§14.9.4.3 SR15 sentence 2 enforced here:
        // the name shall be a directly-contained or visible-COMMON program; the old binder bound the flag,
        // discarded it, and let ProgramTable resolve ANY outermost program at run time).
        if (asNested && call.callTarget()?.literal() is { } asLit)
        {
            string nestedName = CobolLiteral.Decode(asLit.GetText());
            if (host.NestedCallables is { } nc && nc.TryGetValue(nestedName, out var sig))
                callee = sig;
            else
            {
                ctx.Edition.Error(DiagnosticCatalog.CallAsNestedScope,
                    $"CALL … AS NESTED \"{nestedName}\": the name shall be a program contained directly within "
                    + "the calling program, or a visible common program (ISO §14.9.4.3 SR15)");
                return new BoundNop();
            }
        }

        // ── Target: literal (static) or identifier (dynamic, resolved at run time — GR3b) ──
        string? literalName = null;
        BoundOperand? dynamicName = null;
        bool isPointerTarget = false;   // identifier-1 is a PROGRAM-POINTER item (§14.9.4.3 SR1; P10 Step 7)
        var target = call.callTarget();
        if (target?.literal() is { } lit)
        {
            // kb/Work PB130: through the ONE program-name-literal reader — SR2 admits alphanumeric AND
            // national literals (a hexadecimal literal IS §8.3.3.2 Format 2 of an alphanumeric one), and the
            // old STRINGLIT-only read sent N"P" / X".." to a run-time loud on legal source.
            if (ProgramNameLiteral(lit, "CALL", "§14.9.4.3 SR2") is not { } pn) return new BoundNop();
            literalName = pn;
        }
        else if (target?.dataReference() is { } dref)
        {
            // §14.9.4.2 Format 2 spelling (b), kb/Work PB237: with the whole `⎡{identifier-1|literal-1} AS⎤`
            // bracket omitted, the operand IS program-prototype-name-1 and is spelled exactly like identifier-1.
            // The DATA REFERENCE is tried first, and through Probe rather than Resolve because an unresolved
            // Probe reports nothing — so a word that names a data item stays identifier-1 and no program that
            // binds today changes meaning. When the AS phrase already named a prototype, identifier-1 IS
            // specified and GR3 b)'s FIRST bullet governs: the item's content is the program-name and the
            // prototype only supplies the characteristics (GR7) — hence the `prototype is null` guard.
            if (prototype is null && ctx.Refs.Probe(dref) is null && BarePrototypeWord(dref) is { } bareWord
                && host.ProgramPrototypes?.GetValueOrDefault(bareWord) is { } bareProto)
                // GR3 b) third bullet: "If neither identifier-1 nor literal-1 is specified,
                // program-prototype-name-1 determines the externalized program-name of the program being
                // called, according to the rules specified in 12.3.8, REPOSITORY paragraph."
                prototype = bareProto;
            else if (ctx.Refs.Resolve(dref) is not { } place)
                return new BoundUnsupported($"CALL target '{dref.GetText()}'");
            else
            {
                dynamicName = new BoundFieldOperand(place);
                // §14.9.4.3 SR1: "Identifier-1 shall be defined as an alphanumeric, national, or program-pointer
                // data item" — a program-pointer target activates the HELD program (ProgramRegistry.CallPointer,
                // P10 Step 7); any OTHER class is rejected here (kb/Work PB132 — the old arm read the category
                // only to set the pointer flag, and a numeric or boolean target fell through to a garbage
                // name-string read at run time).
                if (place.Item.Pic?.Category is PicCategory.ProgramPointer) isPointerTarget = true;
                else if (IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place))
                         is { } tCls and not (CobolClass.Alphanumeric or CobolClass.National))
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallTargetCategory,
                        $"CALL target '{dref.GetText()}' is of class {tCls.ToString().ToLowerInvariant()}; ISO "
                        + "§14.9.4.3 SR1 admits an alphanumeric, national, or program-pointer data item");
                    return new BoundNop();
                }
            }
        }
        else if (prototype is null)
            // The bracket may be omitted only in Format 2, where the outer brace then supplies the operand. With
            // neither a target nor a prototype there is nothing to activate.
            return new BoundUnsupported("CALL target form");

        // §14.9.4.4 GR7: "If the NESTED phrase is not specified, program-prototype-name-1 is used to determine
        // the characteristics of the called program." Its externalized name is the program-name whenever
        // identifier-1/literal-1 was not written (GR3 b)), and its signature — when §12.3.8.4 GR10 a) found the
        // definition in this compilation group — is the ONE callee signature every §14.8.2/§14.8.3 check below
        // reads. A GR10 c) prototype (a separately-compiled program) legally carries none; the locate and the
        // EC-PROGRAM-ARG-MISMATCH conformance check then happen at activation, per GR3 b) and GR3 d).
        if (prototype is { } proto2)
        {
            if (literalName is null && dynamicName is null) literalName = proto2.ExternalizedName;
            callee ??= proto2.Signature;
        }
        // Format 2 is selected by the AS phrase OR by a bare program-prototype-name operand (the two spellings of
        // §14.9.4.2 Format 2). Every narrowing below that reads `formatTwo` — Format 1's ban on BY VALUE, on a
        // BY CONTENT literal and on bare non-identifier arguments — therefore relaxes for both.
        bool formatTwo = call.callAsPhrase() is not null || prototype is not null;
        IReadOnlyList<LinkageFormal>? calleeFormals = callee?.Formals;
        // How the shared §14.8.2/§14.8.3 messages below name the callee they resolved. One string, because the
        // CHECKS are one set: only the provenance of the signature differs.
        string calleeWhere = asNested
            ? "CALL … AS NESTED"
            : $"CALL … program-prototype '{prototype?.Name}'";

        // ── USING arguments — the §14.9.4.4 GR5 TRANSITIVE pass mode: BY REFERENCE is assumed before the
        //    first phrase; each explicit BY REFERENCE / BY CONTENT / BY VALUE phrase applies to every following
        //    bare argument until the next phrase (the legacy CallBinder mode-threading, re-derived from GR5). ──
        var args = new List<BoundCallArg>();
        var mode = CobolPassMode.Reference;
        foreach (var a in call.callUsingPhrase()?.callArgument() ?? [])
        {
            if (a.callByReference() is { } byRef)
            {
                mode = CobolPassMode.Reference;
                // §14.9.4.2 Format 2: `[BY REFERENCE] {identifier-2 | OMITTED}` (kb/Work PB130). The omitted-
                // argument carrier joins the OPTIONAL-formal landing (PB133); until then the recognized form
                // draws the same staged diagnostic family instead of a parse error on legal source.
                if (byRef.OMITTED() is not null)
                {
                    // §14.9.4.4 GR11 (kb/Work PB133 wave C): the omitted argument occupies its position with
                    // the NULL carrier — the callee's Present test, the §8.8.4.8 condition, and GR12's checked
                    // raise all read that one fact. SR24's OPTIONAL correspondence is checked after the loop.
                    args.Add(new BoundCallArg(CobolPassMode.Reference, null, null, Omitted: true));
                    continue;
                }
                if (byRef.dataReference() is not { } byRefDref)
                    return new BoundUnsupported("CALL USING BY REFERENCE form");
                // kb/Work PB128: a BY REFERENCE argument is a RECEIVING operand and rides the ONE receiving
                // chokepoint — the direct Refs.Resolve bypass skipped the CONSTANT RECORD (§13.18.15.3 SR2),
                // CAPACITY-register (§13.18.38 SR30–32), constant-name and LINE-COUNTER screens, letting a
                // structured constant be silently overwritten by the callee (and a CAPACITY register reach
                // PlaceRenderer.Write's internal throw — an unhandled compiler exception).
                if (host.Expr.ResolveReceiving(byRefDref) is not { } p)
                    return OperandUnresolved(byRefDref, "USING argument");
                ScreenCallOperand(p, CobolPassMode.Reference, formatTwo, isReturning: false);
                args.Add(new BoundCallArg(CobolPassMode.Reference, p, null));
            }
            else if (a.callByContent() is { } byContent)
            {
                mode = CobolPassMode.Content;
                // ── ONE OPERAND, THREE CHANNELS — normalized once, exactly as OoBindInvokeArg does ──
                // The grammar parses BY CONTENT wide (both formats share this rule). The reduction is
                // §14.9.4.4 GR8's and lives in ONE place (kb/Work PB238 — the BY VALUE arm below and both
                // keyword-less arms read the SAME helper; the reduction used to be written out here only, so
                // GR8 held for one spelling of an argument and not for the others).
                var (cLit, cBareLit, cDref, cBool, cArith) = Gr8Classify(
                    byContent.literal(), byContent.dataReference(),
                    byContent.booleanExpression(), byContent.arithmeticExpression());
                // §14.9.4.2 FORMAT 1's BY CONTENT IS `{ identifier-2 } …` AND NOTHING ELSE. An expression operand
                // is legal only under Format 2, which the AS phrase selects — so accepting one here without that
                // phrase would admit illegal source, the exact trade this item refused to make in the grammar.
                if (!formatTwo && (cBool is not null || cLit is not null || (cArith is not null && cDref is null)))
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                        $"CALL … USING BY CONTENT {byContent.GetText()}: an expression operand belongs to the "
                        + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects. "
                        + "Format 1's BY CONTENT admits `{ identifier-2 } …` only.");
                    return new BoundNop();
                }
                // Probe to DISCRIMINATE (the cArith arm below is the legal alternative and its bind demands —
                // R30), then RESOLVE to commit: a probe is unscreened, so its Place must never enter the bound
                // tree (kb/Work PB221 — this arm used to commit the probe's Place, so `BY CONTENT E(XE)` with
                // `XE PIC X(4)` compiled clean while the BY REFERENCE operand of the same statement drew
                // COBOLNET0844, and a function-bearing subscript bound occurrence 1).
                if (cDref is not null && ctx.Refs.Probe(cDref) is not null
                    && ctx.Refs.Resolve(cDref) is { } cp)
                {
                    ScreenCallOperand(cp, CobolPassMode.Content, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(CobolPassMode.Content, cp, null));
                }
                else if (cLit is { } clit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(clit)));
                else if (cBareLit is { } cbl && host.Expr.NonNumericLiteralOperand(cbl) is { } cblOp)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, cblOp));
                else if (cArith is { } cax)
                    // Format-2 arithmetic-expression-1: bind through the ONE expression path and pass its value.
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(cax))));
                else if (cBool is { } cbx)
                    args.Add(BooleanContentArg(cbx));
                else
                    return new BoundUnsupported($"CALL USING BY CONTENT argument '{byContent.GetText()}'");
            }
            else if (a.callByValue() is { } byValue)
            {
                // §14.9.4.2 FORMAT 1 HAS NO BY VALUE ARM (kb/Work PB130): its USING brace prints BY REFERENCE
                // and BY CONTENT only (the repaired figure notes’ required-word list has no VALUE), and
                // SR21–SR23 sit under Format 2. Accepting it here passed a GR5-impossible mode.
                if (!formatTwo)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                        "CALL … USING BY VALUE belongs to the program-prototype CALL (ISO §14.9.4.2 Format 2), "
                        + "which the AS phrase selects — Format 1’s USING admits BY REFERENCE and BY CONTENT only");
                    return new BoundNop();
                }
                // BY VALUE (§14.9.4) is a COBOL-2002 introduction; the edition gate moved to the post-bind
                // VersionConformancePass (Step 14c), firing on a BoundCallProgram whose args use value passing.
                mode = CobolPassMode.Value;
                // ── §14.9.4.4 GR8 APPLIES HERE TOO, AND THAT IS THE WHOLE OF kb/Work PB238 ────────────────
                // "An argument that consists merely of a single identifier or literal is regarded as an
                // identifier or literal rather than an arithmetic or boolean expression." The printed
                // §14.9.4.2 Format-2 BY VALUE brace is `arithmetic-expression-1 | identifier-4 | literal-2`,
                // and this arm used to bind ALL THREE as arithmetic-expression-1 — so identifier-4 lost its
                // own carrier and its own PICTURE meta on the way to CallEmitter.ArgText, which then spelled
                // the crossing `ManagedPointer<Int128>.Cell((Int128)(expr)), 38, scale`: the item's digits
                // replaced by a constant, and a FLOAT item's fractional part TRUNCATED AWAY (measured:
                // `01 F FLOAT-LONG VALUE 1.5` reached a `PIC S9(3)V99` BY VALUE formal as 001.00, where
                // §14.2.3 GR10 requires "a COMPUTE statement without the ROUNDED phrase" ⇒ 001.50). The
                // reduction is the SAME one the BY CONTENT arm runs, from the SAME helper.
                var (vLit, vBareLit, vDref, _, vArith) = Gr8Classify(
                    byValue.literal(), null, null, byValue.arithmeticExpression());
                if (vLit is { } vlit)
                {
                    // §14.9.4.3 SR23 — reported BY NAME. The `literal` alternative exists in `callByValue`
                    // solely so this verdict is the compiler's, with its citation, instead of a raw ANTLR
                    // "no viable alternative" (the numeric spelling never reaches here: `arithmeticExpression`
                    // is the earlier alternative and subsumes every numeric literal and the figurative ZERO).
                    Sr23LiteralIsNumeric(vlit, "BY VALUE");
                    args.Add(new BoundCallArg(CobolPassMode.Value, null, host.Expr.LiteralOperand(vlit)));
                }
                else if (vDref is { } vdref && ctx.Refs.Probe(vdref) is not null
                         && ctx.Refs.Resolve(vdref) is { } vp)
                {
                    // identifier-4 — a SENDING operand (SR17), crossing on ITS OWN carrier and its own
                    // Digits/Scale through the Place arm of CallEmitter.ArgText, exactly as the BY CONTENT
                    // identifier does. Probe-to-discriminate then Resolve-to-commit, the PB221 discipline.
                    // Resolve, never ResolveReceiving: SR17 makes it sending, so the receiving screens do
                    // not apply and their diagnostics would be false rejections.
                    ScreenCallOperand(vp, CobolPassMode.Value, formatTwo, isReturning: false);
                    // §14.9.4.3 SR22 over the RESOLVED item — the same screen the keyword-less BY VALUE
                    // argument runs. It no longer has to unwrap a BoundComputedOperand to recover the
                    // identifier-ness GR8 says was never lost.
                    ValueClassScreen(IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(vp)), vdref.GetText());
                    args.Add(new BoundCallArg(CobolPassMode.Value, vp, null));
                }
                else if (vBareLit is not null)
                {
                    // A non-numeric literal-2 that reached the boolean node on a LATER argument's B-operator
                    // is still literal-2, and §14.9.4.3 SR23 still governs it. (`callByValue` has no boolean
                    // alternative, so this arm is reachable only through a shared reduction — which is the
                    // argument for having one.)
                    ctx.Edition.Error(DiagnosticCatalog.CallByValueLiteralKind,
                        $"CALL … USING {vBareLit.GetText()} with BY VALUE: literal-2 shall be a NUMERIC "
                        + "literal (ISO §14.9.4.3 SR23)");
                    return new BoundNop();
                }
                else if (vArith is { } vax)
                {
                    // arithmetic-expression-1 — the genuine residue.
                    // ⛔ BindByValueExpr, NOT BindExpr — §14.9.4.3 SR22 governs a BY VALUE operand, not
                    // §8.8.1.1. The production is named arithmeticExpression and binding it as arithmetic put
                    // DA6's §8.8.1.1 screen on it, so an alphanumeric operand was refused with a message about
                    // arithmetic expressions and a "digit-decoding extension" — the right verdict quoting the
                    // wrong rule.
                    var byValueOperand = new BoundComputedOperand(host.Expr.BindByValueExpr(vax));
                    CheckByValueClass(vax.GetText(), byValueOperand);
                    args.Add(new BoundCallArg(CobolPassMode.Value, null, byValueOperand));
                }
                else
                    return new BoundUnsupported($"CALL USING BY VALUE argument '{byValue.GetText()}'");
            }
            else if (a.dataReference() is { } bare)
            {
                // Format 1: a bare argument takes the prevailing transitive mode — §14.9.4.4 GR5 names
                // BY REFERENCE and BY CONTENT only. Format 2 (kb/Work PB131): GR5 is a FORMAT 1 rule; GR9
                // takes the keyword-less identifier's mode from the CORRESPONDING FORMAL, resolved at bind
                // through the AS NESTED callee table (the old single transitive `mode` silently passed
                // `USING BY VALUE A B`'s B detached, losing the callee's writeback).
                // ⛔ §14.9.4.4 GR8 FIRST: a constant-name substitutes a LITERAL — §13.10.4 GR1 ("the effect of
                // specifying constant-name-1 in other than this entry is as if literal-1 … were written where
                // constant-name-1 is written") with §13.10.3 SR2 admitting it "anywhere that a format
                // specifies a literal", which the Format-2 figure's literal-2 slot does —
                // so `CALL … AS NESTED USING K` is a bare LITERAL-2 argument, never identifier-2 — and
                // literal-2 never meets Syntax rule 3, so GR9 a)2 assumes BY CONTENT (or GR9 b) BY VALUE).
                // The old arm resolve-RECEIVED it and drew COBOLNET1548 "constant-name shall not be specified
                // as a receiving operand", rejecting conforming Format-2 source (kb/Work PB238).
                if (formatTwo && host.Expr.ConstantOperand(bare) is { } bareConst)
                {
                    var kMode = Gr9BareLiteralMode(calleeFormals, args.Count);
                    if (kMode is CobolPassMode.Value) Sr23ConstantIsNumeric(bare, kMode);
                    args.Add(new BoundCallArg(kMode, null, bareConst));
                    continue;
                }
                CobolPassMode bareMode = mode;
                bool byContentAssumed = false;
                if (formatTwo && calleeFormals is not null)
                {
                    int pos = args.Count;
                    bareMode = pos < calleeFormals.Count && calleeFormals[pos].ByValue
                        ? CobolPassMode.Value : CobolPassMode.Reference;
                }
                // ── §14.9.4.4 GR9 a)2 + §14.9.4.3 SR20 — the leg with no arm at all (kb/Work PB238) ────────
                // GR9 a)'s test is SYNTAX RULE 3 ("an address-identifier or a data item defined in the file,
                // working-storage, local-storage, or linkage section"), NOT "the receiving chokepoint accepted
                // it": a)1 assumes BY REFERENCE only when the argument MEETS SR3, and a)2 assumes BY CONTENT
                // when it does not. An OBJECT PROPERTY is the standard's own worked example of the second leg —
                // it is not a data item of any of those four sections, and SR20 names it explicitly as the ONE
                // identifier permitted as a receiving operand for which "BY CONTENT may be omitted". Binding it
                // BY REFERENCE made §8.4.3.9.4's desugar classify the occurrence ReadWrite (BoundStores) and
                // invoke the SET accessor on an operand SR17 calls SENDING.
                if (formatTwo && bareMode is CobolPassMode.Reference && ctx.Refs.IsObjectPropertyReference(bare))
                {
                    bareMode = CobolPassMode.Content;
                    byContentAssumed = true;
                }
                // A sending operand resolves through the SENDING resolver: the receiving chokepoint's screens
                // (§13.18.15.3 SR2, the CAPACITY register, LINE-COUNTER) are receiving-operand rules and would
                // be false rejections here.
                if ((byContentAssumed || bareMode is CobolPassMode.Value
                        ? ctx.Refs.Resolve(bare) : host.Expr.ResolveReceiving(bare)) is not { } bp)
                    return OperandUnresolved(bare, "USING argument");
                ScreenCallOperand(bp, bareMode, formatTwo, isReturning: false);
                // §14.9.4.3 SR22's OTHER arm (kb/Work PB132): "identifier-4 OR ITS CORRESPONDING FORMAL
                // PARAMETER is specified with a BY VALUE phrase" — the formal-derived Value mode (GR9 b))
                // must meet the same class screen the explicit BY VALUE arm runs.
                if (bareMode is CobolPassMode.Value)
                    ValueClassScreen(IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(bp)), bare.GetText());
                args.Add(new BoundCallArg(bareMode, bp, null));
            }
            // §14.9.4.2 Format 2's keyword-less non-identifier arguments (kb/Work PB130): literal-2,
            // arithmetic-expression-1, boolean-expression-1 and OMITTED all print bare in Format 2 (its BY
            // phrases are plain brackets — GR9 a)2 exists precisely for the non-identifier bare argument);
            // Format 1's bare argument is identifier-2 only, so each arm narrows on formatTwo. A bare
            // non-identifier crosses BY CONTENT semantics (a value, never a writeback carrier).
            else if (a.OMITTED() is not null)
                // §14.9.4.4 GR11 — same carrier as the BY REFERENCE spelling (kb/Work PB133 wave C).
                args.Add(new BoundCallArg(CobolPassMode.Reference, null, null, Omitted: true));
            else if (a.literal() is { } bLit)
            {
                if (!formatTwo) { BareNeedsFormat2(bLit.GetText()); return new BoundNop(); }
                // §14.9.4.4 GR9 b) — BY VALUE is assumed when the corresponding formal is BY VALUE, and the
                // MODE is the mechanism, not a side effect of the copy-out (kb/Work PB238: this arm hard-coded
                // Content and the VALUE outcome happened only because a Content literal has no storage to copy
                // back to — right by accident, and it would not have survived a change to the copy-out).
                var bLitMode = Gr9BareLiteralMode(calleeFormals, args.Count);
                // §14.9.4.3 SR23's SECOND subject — "or its corresponding formal parameter is specified with
                // the BY VALUE phrase" — which had no reachable arm at all before the mode was derived here.
                if (bLitMode is CobolPassMode.Value) Sr23LiteralIsNumeric(bLit, "a BY VALUE formal parameter");
                args.Add(new BoundCallArg(bLitMode, null, host.Expr.LiteralOperand(bLit)));
            }
            else if (a.booleanExpression() is { } bBool)
            {
                if (!formatTwo) { BareNeedsFormat2(bBool.GetText()); return new BoundNop(); }
                // §14.9.4.4 GR8's reduction applies to the bare spelling too: an operator-free
                // booleanExpression is its bare valueOperand, and the identifier/literal it holds is
                // identifier-4 / literal-2 — the BY CONTENT arm's own discipline, from the same helper.
                var (nLit, nBareLit, nDref, nBool, nArith) = Gr8Classify(null, null, bBool, null);
                if (nBool is { } nbx)
                    // boolean-expression-1 prints ONLY in the BY CONTENT brace of §14.9.4.2 Format 2, so the
                    // omitted phrase can only be BY CONTENT; §14.9.4.3 SR19 is then checked against the
                    // corresponding formal by the loop below (a BY VALUE formal draws its own diagnostic).
                    args.Add(BooleanContentArg(nbx));
                else if (nLit is { } nlit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(nlit)));
                else if (nBareLit is { } nbl && host.Expr.NonNumericLiteralOperand(nbl) is { } nblOp)
                    args.Add(new BoundCallArg(Gr9BareLiteralMode(calleeFormals, args.Count), null, nblOp));
                else if (nDref is { } ndref && ctx.Refs.Resolve(ndref) is { } np)
                {
                    ScreenCallOperand(np, CobolPassMode.Content, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(CobolPassMode.Content, np, null));
                }
                else if (nArith is { } nax)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(nax))));
                else
                    return new BoundUnsupported($"CALL USING argument '{bBool.GetText()}'");
            }
            else if (a.arithmeticExpression() is { } bArith)
            {
                // A parenthesized sole reference reduces to its identifier (the callByContent discipline).
                if (ConditionBinder.SoleDataReference(bArith) is { } sd)
                {
                    if (host.Expr.ResolveReceiving(sd) is not { } sp)
                        return OperandUnresolved(sd, "USING argument");
                    ScreenCallOperand(sp, mode, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(mode, sp, null));
                }
                else if (!formatTwo) { BareNeedsFormat2(bArith.GetText()); return new BoundNop(); }
                else
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(bArith))));
            }
        }

        // ⛔ ONE BOOLEAN VALUE CHANNEL for the CALL side, mirroring BoundInvokeArg.ContentBool exactly
        // (kb/Work PB46 landed the INVOKE half and left this one; PB238 closes it). §14.9.4.3 SR17 makes every
        // identifier inside boolean-expression-1 a SENDING operand, and a boolean expression evaluates to a
        // '0'/'1' bit-string VALUE (§8.8.2) whose length is fixed by §8.8.2 rule 10 — it is neither an
        // algebraic value nor a storage reference, so it is its own channel rather than a spelling of the
        // arithmetic one. BY CONTENT by construction: an expression has no storage to write back to.
        BoundCallArg BooleanContentArg(Core.BooleanExpressionContext bx)
        {
            var bound = host.Cond.BindBoolExpr(bx);
            return new BoundCallArg(CobolPassMode.Content, null, null)
            {
                ContentBool = bound,
                ContentBoolWidth = ConditionBinder.Gr3Width(bound),
            };
        }

        // §14.9.4.4 GR9 for a keyword-less LITERAL-2 (or a constant-name, which substitutes one): a literal is
        // never "a data item defined in the file, working-storage, local-storage, or linkage section", so it
        // never meets Syntax rule 3 — GR9 a)2 gives BY CONTENT, and GR9 b) gives BY VALUE whenever the
        // CORRESPONDING FORMAL is BY VALUE. Without a known formal (no AS NESTED — the program-prototype
        // registry is P13) GR9 has nothing to read, and BY CONTENT is the format's own omitted-phrase default.
        static CobolPassMode Gr9BareLiteralMode(IReadOnlyList<LinkageFormal>? formals, int pos) =>
            formals is not null && pos < formals.Count && formals[pos].ByValue
                ? CobolPassMode.Value : CobolPassMode.Content;

        void BareNeedsFormat2(string text) =>
            ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat,
                $"CALL … USING {text}: a keyword-less literal or expression argument belongs to the "
                + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects — Format 1's "
                + "bare argument is identifier-2 only");

        // §14.8.2.1 + §14.9.4.3 SR24 (kb/Work PB133 wave C, generalized by PB237): with the Format-2 callee's
        // formals known at BIND time — from the AS NESTED containment table or from the program prototype's
        // §12.3.8.4 GR10 a) definition — the argument COUNT (equality, except trailing OPTIONAL formals omitted)
        // and each written OMITTED's OPTIONAL correspondence are compile-time checks — the design doc's "or
        // diagnostic" lane; the dynamic Format-1 count check (EC-PROGRAM-ARG-MISMATCH at activation) rides wave C2.
        if (calleeFormals is not null)
        {
            bool countBad = args.Count > calleeFormals.Count;
            for (int i = args.Count; !countBad && i < calleeFormals.Count; i++)
                if (!calleeFormals[i].Optional) countBad = true;
            if (countBad)
                ctx.Edition.Error(DiagnosticCatalog.CallArgumentCount,
                    $"{calleeWhere} supplies {args.Count} argument(s) where the program declares "
                    + $"{calleeFormals.Count} formal parameter(s); ISO §14.8.2.1 requires the counts to be "
                    + "equal, except for trailing formal parameters declared OPTIONAL and omitted");
            for (int i = 0; i < args.Count && i < calleeFormals.Count; i++)
            {
                var f = calleeFormals[i];
                var arg = args[i];
                if (arg.Omitted)
                {
                    if (!f.Optional)
                        ctx.Edition.Error(DiagnosticCatalog.CallOmittedNeedsOptional,
                            $"CALL … USING OMITTED at argument {i + 1}: the corresponding formal parameter "
                            + $"'{f.Item.CobolName}' is not declared OPTIONAL (ISO §14.9.4.3 SR24)");
                    continue;
                }
                // §14.9.4.3 SR19/SR21 (kb/Work PB133 wave C2): the EXPLICIT phrase's mode must match the
                // corresponding formal's (a keyword-less argument already DERIVED its mode from that formal
                // — GR9, PB131 — so only a written phrase can disagree).
                if (arg.Mode is CobolPassMode.Reference or CobolPassMode.Content && f.ByValue)
                    ctx.Edition.Error(DiagnosticCatalog.CallArgumentMode,
                        $"CALL … argument {i + 1} passes {(arg.Mode == CobolPassMode.Content ? "BY CONTENT" : "BY REFERENCE")} "
                        + $"where the corresponding formal parameter '{f.Item.CobolName}' is BY VALUE "
                        + "(ISO §14.9.4.3 SR19)");
                else if (arg.Mode is CobolPassMode.Value && !f.ByValue)
                    ctx.Edition.Error(DiagnosticCatalog.CallArgumentMode,
                        $"CALL … argument {i + 1} passes BY VALUE where the corresponding formal parameter "
                        + $"'{f.Item.CobolName}' is not BY VALUE (ISO §14.9.4.3 SR21)");
                // §14.8.2.3.2 / §14.8.2.2 (BY REFERENCE only — 14.8.2.3.3 puts BY CONTENT / BY VALUE in the
                // MOVE/SET-validity regime instead): the same-description check, through THE one comparator
                // (OoConformance.DescriptionMismatch — previously INVOKE-only; a NESTED call is the same
                // §14.8.2.3.2 rule-2 regime as a method). Run-time it would be EC-PROGRAM-ARG-MISMATCH;
                // at bind it is the design's diagnostic lane.
                else if (arg.Mode is CobolPassMode.Reference && arg.Place is { } ap
                         && CobolNet.Compiler.Oo.OoConformance.DescriptionMismatch(f.Item, ap.Item,
                                byRefGroupPrefix: true) is { } why)
                    ctx.Edition.Error(DiagnosticCatalog.CallArgumentConformance,
                        $"{calleeWhere} argument {i + 1} ('{ap.Item.CobolName}') does not conform to formal "
                        + $"parameter '{f.Item.CobolName}': {why} (ISO §14.8.2)");

                // §14.8.2.3.2, last sentence of the class-pointer rule: "If either is a restricted pointer, both
                // shall be restricted and of the same type." The file already consulted StrongTypeModel for
                // §14.9.4.3 SR10 above, so the model was in hand and only this clause of the same conformance
                // regime was missing (kb/Work PB153).
                // ⛔ SCOPED DELIBERATELY TO THE AS-NESTED LOOP, i.e. to operands WITHIN ONE SOURCE ELEMENT.
                // StrongTypeModel's type equivalence is name-based within an element and explicitly DEFERS
                // cross-program EXTERNAL equivalence, so applying this to a separately-compiled callee would
                // over-reject on the deferred axis — rejecting legal source, the worse failure.
                // ⚠ kb/Work PB237 kept the `asNested` guard when the enclosing loop was generalized to every
                // Format-2 callee: a program prototype's §12.3.8.4 GR10 a) definition is a SEPARATE outermost
                // source element (it does not inherit the caller's GLOBAL TYPEDEFs the way a contained program
                // does), so it is exactly the deferred axis this comment excludes.
                if (asNested && arg.Mode is CobolPassMode.Reference && arg.Place is { } restrictedArg)
                {
                    string? argR = StrongTypeModel.PointerRestriction(restrictedArg.Item);
                    string? formalR = StrongTypeModel.PointerRestriction(f.Item);
                    if ((argR is not null || formalR is not null) && !StrongTypeModel.SameRestriction(argR, formalR))
                        ctx.Edition.Error(DiagnosticCatalog.CallArgumentConformance,
                            $"CALL … AS NESTED argument {i + 1} ('{restrictedArg.Item.CobolName}') and formal parameter "
                            + $"'{f.Item.CobolName}': one is a RESTRICTED data-pointer and the other is not "
                            + $"restricted to the same type (argument: {argR ?? "unrestricted"}; formal: "
                            + $"{formalR ?? "unrestricted"}) — ISO §14.8.2.3.2 requires that if either is a "
                            + "restricted pointer, both shall be restricted and of the same type");
                }
            }
        }

        // ── RETURNING (CALL side) — COBOL-2002+ (deep-dive "Edition gating"); maps to the activation result
        //    delivered through the opaque ABI's returning carrier (§14.2.3 GR7). ──
        Place? returning = null;
        if (call.callReturningPhrase() is { } rp)
        {
            // call-returning-2002: the VersionConformancePass owns the edition gate (Exec Step E).
            // kb/Work PB128: identifier-3 is a pure receiver — the chokepoint's screens apply.
            if (host.Expr.ResolveReceiving(rp.dataReference()) is not { } rpl)
                return OperandUnresolved(rp.dataReference(), "RETURNING item");
            ScreenCallOperand(rpl, CobolPassMode.Reference, formatTwo, isReturning: true);
            returning = rpl;
        }
        // §14.9.4.3 SR25 → §14.8.3, RETURNING ITEMS — the half that had NO home at all (kb/Work PB204). SR25
        // makes §14.8.3's rules apply to a Format-2 CALL, and wherever the callee's PD header is bound — the
        // AS NESTED containment table, or a program prototype's §12.3.8.4 GR10 a) definition (PB237) —
        // §14.8.3.1's "if and only if" and §14.8.3.2/§14.8.3.3's description rules are compile-time facts
        // rather than the run-time EC-PROGRAM-ARG-MISMATCH a dynamically-resolved callee would need. THE SAME
        // comparator the USING loop and INVOKE use, so the three can never drift.
        // §14.8.3.1: "The returning item in the activated element is the sending operand, the corresponding
        // returning item in the activating element is the receiving operand" — hence the callee's item is
        // parameter 1, which is also what the §14.8.3.3 rule-4/5 ANY LENGTH relaxation keys on. No prefix
        // latitude: §14.8.3.2 asks for "the same length", not §14.8.2.2 rule 1's "same or smaller".
        if (callee is { } calleeSig)
        {
            if ((returning is null) != (calleeSig.Returning is null))
                ctx.Edition.Error(DiagnosticCatalog.CallReturningConformance,
                    returning is null
                        ? $"{calleeWhere}: the activated program's procedure division header declares "
                          + $"RETURNING '{calleeSig.Returning!.CobolName}' but the CALL statement specifies no "
                          + "RETURNING item (ISO §14.8.3.1 — one shall be specified if and only if the "
                          + "activated element specifies one)"
                        : $"{calleeWhere} specifies a RETURNING item but the activated program's procedure "
                          + "division header declares none (ISO §14.8.3.1)");
            else if (returning is { } rr && calleeSig.Returning is { } cr
                     && CobolNet.Compiler.Oo.OoConformance.DescriptionMismatch(cr, rr.Item,
                            anyLengthActivationRelax: true) is { } rwhy)
                ctx.Edition.Error(DiagnosticCatalog.CallReturningConformance,
                    $"{calleeWhere} RETURNING '{rr.Item.CobolName}' does not conform to the activated "
                    + $"program's returning item '{cr.CobolName}': {rwhy} (ISO §14.8.3 via §14.9.4.3 SR25)");
        }

        // ── Exception phrases — edition-gated spellings (deep-dive "Edition gating"; VERSION_CHANGE_REFERENCE
        //    row 3): [NOT] ON EXCEPTION is ANSI X3.23-1985 surface (CALL Format 2; CCVS-85 IC222A tests both
        //    phrases — "'ON OVERFLOW' CAN BE USED IN PLACE OF 'ON EXCEPTION'"); ON OVERFLOW is the 74-carried
        //    synonym, accepted 85–2014 and REMOVED at 2023 (Annex E.2 item 1c). ──
        List<BoundStatement>? onExc = null, notOnExc = null;
        bool usedOverflow = false;
        // The two phrases live under ONE container (callExceptionPhrases) so either order parses — ISO 5.2.6.4
        // choice indicators. Order of WRITING does not change binding: each phrase keeps its own role.
        var excPhrases = call.callExceptionPhrases();
        if (excPhrases?.callOnExceptionPhrase() is { } onp)
        {
            usedOverflow |= onp.OVERFLOW() is not null;
            onExc = host.BindBlocks([onp.statementBlock()]);
        }
        if (excPhrases?.callNotOnExceptionPhrase() is { } notp)
        {
            usedOverflow |= notp.OVERFLOW() is not null;
            notOnExc = host.BindBlocks([notp.statementBlock()]);
        }

        // ON OVERFLOW is the COBOL-74-carried synonym for ON EXCEPTION, REMOVED at ISO 2023; the edition gate
        // (CallOnOverflowRemoved2023) moved to the post-bind VersionConformancePass (Step 14d), reading the flag.
        return new BoundCallProgram(literalName, dynamicName, args, returning, onExc, notOnExc)
        {
            UsedOverflowSpelling = usedOverflow,
            IsPointerTarget = isPointerTarget,
        };
    }

    /// <summary>Bind <c>CANCEL {literal|identifier}…</c> (ISO §14.9.5 — targets resolved like CALL's, §8.4.6.3).</summary>
    /// <summary>The ONE program-name-literal reader (kb/Work PB130) — §14.9.4.3 SR2 / §14.9.5.3 SR2 admit
    /// an alphanumeric OR national literal (a hexadecimal literal is §8.3.3.2 Format 2 of the alphanumeric
    /// kind; a concatenation folds per §8.8.3.3 GR3; the D-N1 identity repertoire makes the national name
    /// the same string). Reports the SR violation itself and returns null — a boolean literal, a numeric
    /// literal, and a zero-length name each draw the cited diagnostic instead of a run-time loud.</summary>
    private string? ProgramNameLiteral(Core.LiteralContext lit, string verb, string clause)
    {
        if (host.Expr.NonNumericLiteralOperand(lit.nonNumericLiteral()) is BoundStringLiteral
            { Category: PicCategory.Alphanumeric or PicCategory.National } sl)
        {
            if (sl.Value.Length != 0) return sl.Value;
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
                $"{verb} with a zero-length literal program name (ISO {clause})");
            return null;
        }
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
            $"{verb} '{lit.GetText()}': the literal program name shall be an alphanumeric or national literal "
            + $"(ISO {clause})");
        return null;
    }

    /// <summary>THE program-prototype-name lookup (kb/Work PB237) — ONE place, because §14.9.4.3 syntax rule 16
    /// ("Program-prototype-name-1 shall be specified in a program-specifier in the REPOSITORY paragraph") and
    /// §14.9.5.3 syntax rule 3 ("Program-prototype-name-1 shall be a program prototype specified in the REPOSITORY
    /// paragraph") state the SAME obligation about the same subject, and §8.4.6.8 adds the one further spelling
    /// they both inherit — "the program-name of a containing program definition". The table
    /// (<c>BinderDriver.ProgramPrototypesOf</c>) carries all of it, so both verbs ask this one question. Reports
    /// COBOLNET1760 and returns null when the name is not a program prototype here.</summary>
    private ProgramPrototype? ResolvePrototype(string name, string verb, string alsoNot = "")
    {
        if (host.ProgramPrototypes?.GetValueOrDefault(name) is { } proto) return proto;
        ctx.Edition.Error(DiagnosticCatalog.ProgramPrototypeUndeclared,
            $"{verb} {name}: '{name}' is not a program prototype — it is neither declared by a PROGRAM specifier "
            + "in the REPOSITORY paragraph (ISO §12.3.8.2) nor the program-name of a containing program "
            + $"definition (§8.4.6.8){alsoNot}");
        return null;
    }

    /// <summary>The BARE user-defined word a data reference spells, or null when it is qualified, subscripted,
    /// reference-modified, or one of the special registers the rule lists first. A program-prototype-name is a
    /// plain word (§8.4.6.8 — it names a source unit, so it takes no qualification and no subscript), so this is
    /// the SHAPE test that decides whether an operand written like identifier-1 can be read as
    /// program-prototype-name-1 at all (kb/Work PB237). Used by both reference sites, so CALL and CANCEL cannot
    /// disagree about what a prototype operand looks like.</summary>
    private static string? BarePrototypeWord(Core.DataReferenceContext dref) =>
        dref.cobolWord() is { } w && dref.dataReferenceSuffix().Length == 0 ? w.GetText() : null;

    public BoundStatement BindCancel(Core.CancelStatementContext cancel)
    {
        var targets = new List<(string?, BoundOperand?)>();
        foreach (var t in cancel.cancelTarget())
        {
            if (t.literal() is { } lit)
            {
                // kb/Work PB130: the ONE reader (national + hex admitted per §14.9.5.3 SR2 — the old
                // rejection also miscited §14.9.5.2 SR1); a bad target reports and the LOOP CONTINUES, so a
                // statement with one illegal target no longer discards its legal ones.
                if (ProgramNameLiteral(lit, "CANCEL", "§14.9.5.3 SR2") is { } pn)
                    targets.Add((pn, null));
            }
            else if (t.dataReference() is { } dref)
            {
                // §14.9.5.2's THIRD brace alternative — program-prototype-name-1 (kb/Work PB237). It has no
                // production of its own because it is spelled exactly like identifier-1, so the discrimination is
                // semantic and takes the SAME order the CALL twin takes: Probe the data reference first (Probe,
                // not Resolve — an unresolved Probe reports nothing), and only a word that is NOT a data item is
                // read as a prototype. §14.9.5.4 GR1 c) then makes the prototype the identification of the program
                // to be canceled, and §12.3.8.4 GR10 NOTE 1 says which name that is — literal-3 if the specifier
                // wrote AS, otherwise the prototype name. A prototype cancel is therefore a STATIC-name cancel,
                // the same bound shape literal-1 produces (GR1 b)); the run-time locate is GR2's.
                if (ctx.Refs.Probe(dref) is null && BarePrototypeWord(dref) is { } cWord)
                {
                    if (host.ProgramPrototypes?.GetValueOrDefault(cWord) is { } cProto)
                    {
                        targets.Add((cProto.ExternalizedName, null));
                        continue;
                    }
                    // §14.9.5.3 SR1 and SR3 together: the word is neither an alphanumeric/national data item nor a
                    // program prototype, so NEITHER identifier-shaped alternative of §14.9.5.2's operand brace is
                    // available. The resolver's §8.4.2.1 "not defined" report names only the data reading, which
                    // would leave SR3's obligation invisible on the one operand that can violate it.
                    ResolvePrototype(cWord, "CANCEL",
                        ", and no data item of that name is defined either — §14.9.5.2's operand brace is "
                        + "identifier-1, literal-1 or program-prototype-name-1");
                    continue;
                }
                if (ctx.Refs.Resolve(dref) is { } p)
                {
                    // §14.9.5.3 SR1 (kb/Work PB154): "Identifier-1 shall be defined as an alphanumeric or
                    // national data item" — the CALL twin's class screen (PB132) that this arm never got:
                    // CANCEL WS-NUM compiled clean and the digit image resolved to no program, a silent
                    // no-op at both stages. Same rule family, but NOT the same rule — §14.9.4.3 SR1 also
                    // admits program-pointer, so CANCEL carries its own descriptor.
                    if (IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(p))
                        is { } cCls and not (CobolClass.Alphanumeric or CobolClass.National))
                    {
                        ctx.Edition.Error(DiagnosticCatalog.CancelTargetCategory,
                            $"CANCEL target '{dref.GetText()}' is of class {cCls.ToString().ToLowerInvariant()}; "
                            + "ISO §14.9.5.3 SR1 admits an alphanumeric or national data item");
                        continue;
                    }
                    targets.Add((null, new BoundFieldOperand(p)));
                }
                // an unresolved name was already diagnosed by the resolver; keep binding the rest
            }
        }
        return new BoundCancel(targets);
    }

    /// <summary>Bind <c>GOBACK [RETURNING x]</c> (ISO §14.9.18). GOBACK itself was introduced by ISO/IEC
    /// 1989:2002 — at <c>--std 85</c> it is rejected with a targeted diagnostic (the G1 lacks-it obligation;
    /// COBOL-85 programs use STOP RUN / EXIT PROGRAM). The 2023 WITH ERROR/NORMAL STATUS phrase (§14.9.18.2,
    /// VERSION_CHANGE_REFERENCE row 75) parses through the shared <c>statusPhrase</c> rule and is introduction-
    /// gated by the VersionConformancePass (GobackStatus2023); it binds presence-only here (the status VALUE →
    /// exit-code wiring is the shared STOP+GOBACK termination-status slice, matching the presence-only STOP sibling).</summary>
    public BoundStatement BindGoback(Core.GobackStatementContext g)
    {
        if (host.InMethod) return host.Oo.OoBindMethodGoback(g);   // §14.9.18.4 GR4 — a METHOD return, never an activation return (D8)
        // goback-bare-2002 / goback-returning-2002: the VersionConformancePass owns both edition gates
        // (Exec Step E folded the bare-GOBACK 0880; the RETURNING 0900 subsumption lives in its parse arm).
        Place? source = null;
        if (g.dataReference() is { } dref)
        {
            // GOBACK RETURNING x ≡ move x into the procedure-division RETURNING item, then return (§14.9.18 GR2
            // — the activation result; the grammar already 2002-gates the phrase).
            if (ctx.Refs.Resolve(dref) is not { } p)
                return new BoundUnsupported($"GOBACK RETURNING '{dref.GetText()}'");
            source = p;
        }
        if (g.raisingPhrase() is { } raising)
            return host.Ec.EcBindRaising(raising, g.Start.Line, "GOBACK") is { } r
                ? new BoundGoback(source, r)
                : new BoundUnsupported("GOBACK RAISING identifier (exception object — the OO wave; ISO §14.9.18.3 SR4)");
        // GOBACK … WITH {NORMAL|ERROR} STATUS [value] (§14.9.18.2, COBOL-2023, 2023-gated in the pass; mutually
        // exclusive with RAISING by the grammar). Decode the shared statusPhrase into the termination status; the
        // emit passes it to the OS only in a MAIN program (§14.9.18.4 GR3/GR10 — a called-program status is inert).
        return new BoundGoback(source, null, host.ControlFlow.BindTerminationStatus(g.statusPhrase()));
    }

    /// <summary>
    /// ISO §14.9.4.3 SR22 — a BY VALUE operand "shall be of class numeric, object, or pointer".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Strict-reject with the leniency dialect-gated, the disposition DA6 established for the sibling §8.8.1.1
    /// question. GnuCOBOL accepts an alphanumeric operand as an extension and silently assumes BY CONTENT — a
    /// DIFFERENT passing mode from the one written, which is why accepting it quietly is the wrong kindness: the
    /// callee would receive an address where the source said value.
    /// </para>
    /// <para>
    /// ⚠ Fail-open on an undecidable class, exactly as the intrinsic screen does: a false reject turns legal
    /// COBOL away, a missed one leaves a rule unenforced, and only the first is forbidden outright.
    /// </para>
    /// </remarks>
    private void CheckByValueClass(string what, BoundComputedOperand operand)
    {
        // ⛔ CLASSIFY THE UNDERLYING REFERENCE, NOT THE WRAPPER. IntrinsicArgumentRules.ClassOf maps any
        // BoundComputedOperand to NUMERIC — correct in its own context, where a computed operand really is an
        // arithmetic expression — so asking it about the wrapper made this check a silent no-op. The first
        // version did exactly that and turned a wrongly-worded REJECT into a clean ACCEPT, which looks like a fix
        // and is a regression: the rule stopped being enforced at all. A bare identifier binds to BoundNumRef, so
        // its Place is the thing SR22 is about.
        // kb/Work PB132: an index-NAME binds to BoundIndexRef, not BoundNumRef, and the old
        // `is not BoundNumRef … return` guard ACCEPTED it — class index (§8.5.2.1 Table 2's own row) is not
        // numeric, object, or pointer. Classified explicitly here; a genuine computed expression still
        // classifies numeric through the generic arm.
        CobolClass? actual = operand.Expr switch
        {
            BoundNumRef { Place: { } place } => IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place)),
            BoundIndexRef => CobolClass.Index,
            _ => IntrinsicArgumentRules.ClassOf(operand),
        };
        ValueClassScreen(actual, what);
    }

    /// <summary>ISO §14.9.4.4 GR8 — <b>the ONE reduction</b> that turns a Format-2 argument's PARSE NODE into
    /// what the standard says the argument IS: "An argument that consists merely of a single identifier or
    /// literal is regarded as an identifier or literal rather than an arithmetic or boolean expression."
    /// <para>Two facts about the grammar make it necessary, and they are the same two on both the CALL and
    /// INVOKE sides: <c>booleanExpression</c>'s leaf is <c>valueOperand</c>, so a B-operator-FREE boolean node
    /// holds an ordinary arithmetic operand; and <c>arithmeticExpression</c> SUBSUMES <c>dataReference</c> and
    /// every numeric literal, so a lone identifier reaches the binder inside an expression node. The predicate
    /// therefore decides only WHICH NODE an operand lands in, never what it means.</para>
    /// <para>⛔ IT IS SHARED BY EVERY SPELLING (kb/Work PB238). The reduction used to be written out inside the
    /// BY CONTENT arm alone, so GR8 held for <c>BY CONTENT N</c> and not for <c>BY VALUE N</c> or for the
    /// keyword-less <c>N</c> — one rule, four arms, one of them fixed. Pure: it reads parse contexts, resolves
    /// nothing and reports nothing.</para></summary>
    private static (Core.LiteralContext? Literal, Core.NonNumericLiteralContext? BareLiteral,
                    Core.DataReferenceContext? Reference, Core.BooleanExpressionContext? Bool,
                    Core.ArithmeticExpressionContext? Arith)
        Gr8Classify(Core.LiteralContext? lit, Core.DataReferenceContext? dref,
                    Core.BooleanExpressionContext? boolExpr, Core.ArithmeticExpressionContext? arith)
    {
        Core.NonNumericLiteralContext? bareLit = null;
        if (boolExpr is not null && ConditionBinder.UnwrapBareBool(boolExpr) is { } bare)
        {
            boolExpr = null;
            arith = bare.arithmeticExpression();
            // ⛔ AND ITS LITERAL LEG (kb/Work PB238). `valueOperand` is `arithmeticExpression |
            // nonNumericLiteral`, so an operand that reached the boolean node ON ANOTHER ARGUMENT'S
            // B-OPERATOR (the predicate's scan runs to the statement's period) can be a plain literal:
            // `CALL "S" AS NESTED USING "AB" B1 B-AND B2` put "AB" in a booleanExpression node. Recovering
            // only the arithmetic leg left it classified as nothing at all, and the argument staged a RUN-TIME
            // NotImplemented on conforming source. OoBindInvokeArg's reduction has always recovered both legs;
            // this one recovered one, which is the same rule written twice and only one of them complete.
            bareLit = bare.nonNumericLiteral();
        }
        // The grammar keeps a `dataReference` arm on callByContent (the legacy binder shares this rule), so a
        // bare identifier can land there directly; the sole-reference reduction covers the one that arrived
        // inside an expression node. The two paths must agree, which is why both are consulted.
        return (lit, bareLit, dref ?? ConditionBinder.SoleDataReference(arith), boolExpr, arith);
    }

    /// <summary>ISO §14.9.4.3 SR23 — "If literal-2 or its corresponding formal parameter is specified with the
    /// BY VALUE phrase, literal-2 shall be a numeric literal." Reported BY NAME (kb/Work PB238): the rule was
    /// previously enforced only as a side effect of the grammar's expression spine, which bottoms out at
    /// <c>numericLiteral | ZERO_ARITH | functionCall | dataReference | ( … )</c> — the right verdict, delivered
    /// as a raw ANTLR "no viable alternative" that names no rule, and unreachable altogether for the rule's
    /// second subject (a keyword-less literal-2 whose FORMAL carries the phrase).</summary>
    private void Sr23LiteralIsNumeric(Core.LiteralContext lit, string subject)
    {
        // The figurative ZERO is a numeric literal wherever a literal is restricted to numeric (§8.3.3.6.3
        // SR1a) — and it never reaches here under an explicit BY VALUE, where ZERO_ARITH takes the earlier
        // `arithmeticExpression` alternative; the keyword-less arm can still spell it, so admit it by rule.
        if (lit.numericLiteral() is not null
            || lit.nonNumericLiteral()?.figurativeConstant()?.ZERO() is not null)
            return;
        ctx.Edition.Error(DiagnosticCatalog.CallByValueLiteralKind,
            $"CALL … USING {lit.GetText()} with {subject}: literal-2 shall be a NUMERIC literal "
            + "(ISO §14.9.4.3 SR23)");
    }

    /// <summary>The <see cref="Sr23LiteralIsNumeric"/> twin for a constant-name argument. §13.10.4 GR1 makes
    /// it literal-2 by substitution and §13.10.4 GR2 gives it the SUBSTITUTED literal's class and category,
    /// so SR23 is a question about that literal and never about the name (§13.10.3 SR2 is the admission —
    /// a constant-name "may be used anywhere that a format specifies a literal of [its] class and
    /// category" — which is why the Format-2 literal-2 slot accepts one at all).</summary>
    private void Sr23ConstantIsNumeric(Core.DataReferenceContext dref, CobolPassMode mode)
    {
        if (ctx.Data.ConstantOf(dref) is not { } k || k.Category is PicCategory.Numeric) return;
        ctx.Edition.Error(DiagnosticCatalog.CallByValueLiteralKind,
            $"CALL … USING {dref.GetText()} with a {(mode is CobolPassMode.Value ? "BY VALUE" : "BY CONTENT")} "
            + $"formal parameter: the constant-name substitutes the {k.Category.ToString().ToLowerInvariant()} "
            + $"literal {k.RawText} (ISO §13.10.4 GR1/GR2), and §14.9.4.3 SR23 requires literal-2 to be a numeric "
            + "literal when the corresponding formal parameter is specified with the BY VALUE phrase");
    }

    /// <summary>The §14.9.4.3 SR22 class screen, shared by the EXPLICIT BY VALUE arm and the Format-2
    /// bare argument whose FORMAL is BY VALUE (kb/Work PB132 — the two arms each ran half the rule).</summary>
    private void ValueClassScreen(CobolClass? actual, string what)
    {
        if (actual is null or CobolClass.Numeric or CobolClass.Object or CobolClass.Pointer) return;
        string rule = $"CALL … USING BY VALUE operand '{what}' is of class {actual.Value.ToString().ToLowerInvariant()}; "
            + "ISO §14.9.4.3 SR22 admits only class numeric, object or pointer by value";
        if (ctx.Edition.Permissive)
            ctx.Edition.Warning(DiagnosticCatalog.CallByValueOperandClass, $"{rule}; accepted under --permissive");
        else
            ctx.Edition.Error(DiagnosticCatalog.CallByValueOperandClass, $"{rule}. --permissive accepts it as an extension");
    }

    /// <summary>An unresolved CALL operand (kb/Work PB132): a SCREEN SECTION name draws the CITED §14.9.4.3
    /// SR3/SR7 rejection at bind time — the R32 posture distinguishes "declared in an unsupported section"
    /// from "not defined", and the old BoundUnsupported staged the answer to run time (the PB88 wrong-stage
    /// shape). Everything else keeps the staged-loud posture.</summary>
    private BoundStatement OperandUnresolved(Core.DataReferenceContext dref, string role)
    {
        string text = dref.GetText();
        if (ctx.Data.ScreenNames.Contains(text))
        {
            ctx.Edition.Error(DiagnosticCatalog.CallOperandSection,
                $"CALL {role} '{text}' names a SCREEN SECTION entry; ISO §14.9.4.3 "
                + (role.StartsWith("RETURNING") ? "SR7" : "SR3")
                + " requires a data item defined in the file, working-storage, local-storage, or linkage section");
            return new BoundNop();
        }
        return new BoundUnsupported($"CALL {role} '{text}'");
    }

    /// <summary>The CALL operand chokepoint (kb/Work PB132) — ISO §14.9.4.3 SR3/SR6/SR8/SR10/SR11/SR12/SR18
    /// over the RESOLVED mode (after GR5's transitivity or GR9's formal derivation), for every Place-carrying
    /// USING argument and the RETURNING item, so both arms of every dispatch meet the same law.</summary>
    private void ScreenCallOperand(Place p, CobolPassMode mode, bool formatTwo, bool isReturning)
    {
        var item = p.Item;
        string? name = item.CobolName;
        string role = isReturning ? "RETURNING item" : "USING argument";

        // SR11 (Format 1: identifier-2 AND identifier-3) / SR18 (Format 2: identifier-4); Format 2's
        // RETURNING rides §14.8.3 via SR25 — a prototype-less callee's formal cannot be proven ANY LENGTH
        // (the §13.18.2.3 SR2 NOTE). INVOKE permits it (§14.8.2.3.2 rule e).
        if (item.IsAnyLength)
            ctx.Edition.Error("COBOLNET1542", $"CALL {role} '{name}' is described with the ANY LENGTH clause "
                + (formatTwo
                    ? (isReturning ? "(ISO §14.8.3 via §14.9.4.3 SR25)" : "(ISO §14.9.4.3 SR18)")
                    : "(ISO §14.9.4.3 SR11)"));

        // SR3 sentence 2: BY REFERENCE (specified or implied) shall not carry factory/instance object data.
        if (!isReturning && mode is CobolPassMode.Reference && ctx.Data.OoIsObjectData(item))
            ctx.Edition.Error(DiagnosticCatalog.CallByReferenceObjectData,
                $"CALL … USING BY REFERENCE '{name}': identifier-2 shall not be defined in the working-storage "
                + "or file section of a factory or an instance object (ISO §14.9.4.3 SR3)");

        // SR10 (Format 1, BY REFERENCE specified or implied).
        if (!isReturning && !formatTwo && mode is CobolPassMode.Reference)
        {
            string? kind =
                StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item"
                : p.Pic?.Category is PicCategory.ObjectReference ? "a data item of class object"
                : p.Pic?.Category is PicCategory.Pointer or PicCategory.ProgramPointer ? "a data item of class pointer"
                : null;
            if (kind is not null)
                ctx.Edition.Error(DiagnosticCatalog.CallByReferenceOperandKind,
                    $"CALL … USING BY REFERENCE '{name}' is {kind} (ISO §14.9.4.3 SR10 — Format 1 shall pass "
                    + "neither by reference; the program-prototype CALL admits them under §14.8.2)");
        }

        // SR12 (Format 1, any mode): no variable-length group (§8.5.1.12.1) — the ONE predicate.
        // ⛔ THE `!isReturning && !formatTwo` GATE IS SPEC-CORRECT ON BOTH CONJUNCTS. DO NOT "FIX" IT, and do
        // not clone this screen at INVOKE (kb/Work PB177 arm C — the derivation reversed the finding that
        // proposed exactly that). In the PRINTED standard SR12 sits under the **FORMAT 1** heading, which opens
        // at SR10 and closes at SR13's `FORMAT 2`, and its sentence names **identifier-2** only — never
        // identifier-3, the RETURNING item of SR7/SR9. Format 2's own law is SR25: "The rules for conformance
        // specified in 14.8.2, Parameters and 14.8.3, Returning items apply" — and §14.8.2.2 / §14.8.3.2 ADMIT
        // a variable-length group subject to the §8.5.1.12 COMPATIBILITY relation rather than forbidding it,
        // with §14.9.4.4 GR3d making the verdict a RUNTIME EC-PROGRAM-ARG-MISMATCH where the callee's
        // description is not statically visible. §14.9.23.3 SR1–SR17 (INVOKE) contains no such prohibition at
        // all; an SR12 clone there would REJECT LEGAL SOURCE. The compatibility relation itself now EXISTS —
        // VariableLengthCompatibility, applied by OoConformance.DescriptionMismatch at all three Format-2 /
        // INVOKE boundaries, with the crossing carried by CobolVarGroup (kb/Work PB204) — so this gate stays
        // exactly as narrow as SR12's own sentence.
        if (!isReturning && !formatTwo && item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item))
            ctx.Edition.Error(DiagnosticCatalog.CallVariableLengthGroup,
                $"CALL … USING argument '{name}' references a variable-length group (a DYNAMIC LENGTH item or "
                + "dynamic-capacity table is subordinate to it) — ISO §14.9.4.3 SR12");

        // SR6 (BY REFERENCE argument) / SR8 (RETURNING): a bit item must sit statically on a byte boundary.
        if ((isReturning || mode is CobolPassMode.Reference) && BitLayout.IsBitItem(item))
            ScreenBitAlignment(p, isReturning ? "§14.9.4.3 SR8" : "§14.9.4.3 SR6", role);
    }

    /// <summary>SR6/SR8's byte-boundary proof (kb/Work PB132): the referenced occurrence's start bit, computed
    /// from the §8.5.1.6.3 cursor walk (<see cref="BitLayout.StartBitWithin"/>) plus each table subscript
    /// times its element stride, plus a ref-mod's leftmost boolean position. The rules' second clause makes
    /// every subscript a compile-time integer — a non-literal subscript is itself the violation. An operand
    /// shape the walk cannot model (an unmodelled overlay, an exotic carrier) is ACCEPTED — the screen must
    /// never reject legal source it cannot prove misaligned.</summary>
    private void ScreenBitAlignment(Place p, string clause, string role)
    {
        long extra = 0;
        Place core = p;
        while (core is PlaceDecorator dec)
        {
            if (core is RefModPlace rm)
            {
                if (ConstIndex(rm.Start) is not { } s0)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallBitAlignment,
                        $"CALL {role} '{p.Item.CobolName}': a bit item's reference-modification leftmost position "
                        + $"shall consist of only fixed-point numeric literals (ISO {clause})");
                    return;
                }
                extra += s0 - 1;
            }
            core = dec.Inner;
        }
        AccessPath? path = core switch { MemberPlace mp => mp.Path, DynTablePlace dp => dp.Path, _ => null };
        if (path is null) return;
        var chain = new List<DataItem>();
        for (var d = core.Item; d is not null; d = d.Parent) chain.Insert(0, d);
        var subs = new Queue<string>();
        foreach (var seg in path.Segments)
        {
            if (seg is FixedTableSegment ft) subs.Enqueue(ft.OneBasedIndex);
            else if (seg is DynTableSegment dt) subs.Enqueue(dt.OneBasedIndex);
        }
        long bit = 0;
        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0)
            {
                int within = BitLayout.StartBitWithin(chain[i - 1], chain[i]);
                if (within < 0) return;
                bit += within;
            }
            bool tabled = chain[i].Occurs is not null || chain[i].IsDynamicTable || chain[i].OccursSpec is not null;
            if (tabled && subs.Count > 0)
            {
                if (ConstIndex(subs.Dequeue()) is not { } k)
                {
                    ctx.Edition.Error(DiagnosticCatalog.CallBitAlignment,
                        $"CALL {role} '{p.Item.CobolName}': a bit item's subscripts shall consist of only "
                        + "fixed-point numeric literals or all-literal arithmetic expressions without "
                        + $"exponentiation (ISO {clause})");
                    return;
                }
                bit += (k - 1) * (long)BitLayout.WidthBits(chain[i]);
            }
        }
        bit += extra;
        if (bit % BitLayout.BitsPerCharacter != 0)
            ctx.Edition.Error(DiagnosticCatalog.CallBitAlignment,
                $"CALL {role} '{p.Item.CobolName}' starts at bit {bit} of its record — a bit item passed by "
                + $"reference shall be aligned on a byte boundary (ISO {clause} / §8.5.1.6.3)");
    }

    /// <summary>Evaluate a rendered subscript/ref-mod index that SR6/SR8 permit — an integer literal or an
    /// all-literal + - * / ( ) expression (no exponentiation; identifiers make it non-constant → null).</summary>
    private static long? ConstIndex(string rendered)
    {
        string s = rendered.Trim();
        if (long.TryParse(s, out long direct)) return direct;
        foreach (char ch in s)
            if (!(char.IsDigit(ch) || ch is '+' or '-' or '*' or '/' or '(' or ')' or ' ')) return null;
        int i = 0;
        long? r = AddSub(s, ref i);
        return r is not null && SkipWs(s, ref i) == s.Length ? r : null;

        static int SkipWs(string t, ref int j) { while (j < t.Length && t[j] == ' ') j++; return j; }
        static long? AddSub(string t, ref int j)
        {
            long? v = MulDiv(t, ref j);
            while (v is not null && SkipWs(t, ref j) < t.Length && t[j] is '+' or '-')
            {
                char op = t[j++];
                long? w = MulDiv(t, ref j);
                v = w is null ? null : op == '+' ? v + w : v - w;
            }
            return v;
        }
        static long? MulDiv(string t, ref int j)
        {
            long? v = Primary(t, ref j);
            while (v is not null && SkipWs(t, ref j) < t.Length && t[j] is '*' or '/')
            {
                char op = t[j++];
                long? w = Primary(t, ref j);
                v = w is null or 0 && op == '/' ? null : op == '*' ? v * w : v / w;
            }
            return v;
        }
        static long? Primary(string t, ref int j)
        {
            if (SkipWs(t, ref j) >= t.Length) return null;
            if (t[j] == '(')
            {
                j++;
                long? v = AddSub(t, ref j);
                if (SkipWs(t, ref j) >= t.Length || t[j] != ')') return null;
                j++;
                return v;
            }
            if (t[j] is '+' or '-')
            {
                char sign = t[j++];
                long? v = Primary(t, ref j);
                return sign == '-' ? -v : v;
            }
            int start = j;
            while (j < t.Length && char.IsDigit(t[j])) j++;
            return j > start && long.TryParse(t[start..j], out long n) ? n : null;
        }
    }
}
