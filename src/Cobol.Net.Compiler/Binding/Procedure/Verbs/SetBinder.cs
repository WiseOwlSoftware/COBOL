// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Common;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET verb binder (P7 Step 10m — a real collaborator over <see cref="BinderContext"/>): the
/// 13-format dispatch with the CONTRACT-ORDER semantic re-routes preserved verbatim — the F10 pointer peek
/// FIRST (<c>host.Ptr.TryBindSetUpDown</c>), the F14 CAPACITY-register peek upstream of ResolveReceiving,
/// switches via the .AlterSwitches host edge (10n), objects via the OO host edge (10s).
/// <see cref="SetTargetOf"/> lives HERE (the host keeps a forwarder for ControlFlowBinder until 10t).</summary>
internal sealed class SetBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind a SET statement, dispatching by format (ISO §14.9.39; COBOLNET_DESIGN §12.3). The COBOL-85
    /// surface — Format 1 index/value assignment, Format 2 UP/DOWN BY, Format 4 condition-name TO TRUE — binds here;
    /// the later-edition formats (switches need SPECIAL-NAMES, pointers/objects their 2002 subsystems, TO FALSE the
    /// 2002 FALSE phrase) fail loud by NAME until their subsystem lands.</summary>
    public BoundStatement BindSet(Core.SetStatementContext set)
    {
        // F11 set-locale / F12 save-locale (§14.9.39; Annex A.4.9 item 9) — IMPLEMENTED since kb/Work PB64 T1 (it
        // used to be refused by name with COBOLNET1518 — kb/Work PB92 before that: "'LOCALE' is not defined").
        if (set.setLocaleStatement() is { } sl)
        {
            bool save = sl.cobolWord(0).Start.TokenIndex > sl.dataReference().Start.TokenIndex;   // F12: the words follow the identifier
            return save ? BindSaveLocale(sl) : BindSetLocale(sl);
        }
        // F6 attribute (§14.9.39; Annex A.4.2 item 24) — DECLINED: the screen module. It had NO grammar
        // alternative at all before kb/Work PB260, so `SET SG ATTRIBUTE HIGHLIGHT ON` was a generic COBOL0001
        // parse error pointing at ATTRIBUTE rather than a named refusal naming the facility.
        if (set.setScreenAttributeStatement() is { } sat)
        {
            ScreenFacility.ReportSetAttribute(ctx.Edition, sat.dataReference().GetText());
            return new BoundNop();
        }
        if (set.setLastExceptionStatement() is not null) return host.Ec.BindSetLastException();   // F13 (ISO §14.9.39; 2002+)
        if (set.setContentStatement() is { } sc) return BindSetContent(sc);   // F15 numeric-content (2014; kb/Work PB452)
        if (set.setEntryStatement() is { } se) return BindSetEntry(se);   // F9 + §8.4.3.13 ENTRY sender (P10 Step 7)
        if (set.setSizeStatement() is { } ss)
            return BindSetSize(ss.dataReference(), ss.arithmeticExpression());   // F16 explicit SIZE OF (2023)
        if (set.setToValueStatement() is { } tv) return BindSetTo(tv);
        if (set.setIndexStatement() is { } ud) return BindSetUpDown(ud);
        if (set.setBooleanStatement() is { } b) return BindSetCondition(b);
        if (set.setSwitchStatement() is { } sw) return host.Alter.SwitchBindSet(sw);   // Format 3 — external switches (ISO §14.9.39)
        if (set.setAddressStatement() is { } sa)
            return host.Ptr.BindSetAddress(sa);   // F7 both directions + ADDRESS OF senders (Phase-4b inc 2)
        if (set.setObjectReferenceStatement() is { } sor)
        {
            // A POINTER target (§14.9.39 Format 4 — SET pointer TO NULL/pointer) is bound BEFORE the
            // object-reference Format 5: both share the `SET dataRef+ TO objectReference` shape. A
            // PROGRAM-POINTER target selects Format 9 the same way (SR21; P10 Step 7).
            var sorCat = sor.dataReference().Length > 0
                ? ctx.Refs.Probe(sor.dataReference(0))?.Item.Pic?.Category : null;   // Probe — a format sniff (R30)
            if (sorCat is PicCategory.Pointer)
                return BindSetPointer(sor.dataReference(),
                    sor.objectReference().dataReference(), sor.objectReference().NULL_() is not null,
                    sor.objectReference().SELF() is not null || sor.objectReference().SUPER() is not null);
            if (sorCat is PicCategory.ProgramPointer)
                return BindSetProgramPointer(sor.dataReference(),
                    sor.objectReference().dataReference(), sor.objectReference().NULL_() is not null,
                    sor.objectReference().SELF() is not null || sor.objectReference().SUPER() is not null);
            return host.Oo.OoBindSetObjectRef(sor.dataReference(),
                senderRef: sor.objectReference().dataReference(),
                senderNull: sor.objectReference().NULL_() is not null,
                senderSelf: sor.objectReference().SELF() is not null,
                senderSuper: sor.objectReference().SUPER() is not null);
        }
        return new BoundUnsupported($"SET form '{set.GetText()}'");
    }

    /// <summary><c>SET CONTENT OF { identifier-14 } … TO { FARTHEST-FROM-ZERO [IN-ARITHMETIC-RANGE] |
    /// FLOAT-INFINITY | FLOAT-NOT-A-NUMBER | FLOAT-NOT-A-NUMBER-SIGNALING | NEAREST-TO-ZERO
    /// [IN-ARITHMETIC-RANGE] } [SIGN {NEGATIVE|POSITIVE}]</c> — ISO §14.9.39.2 Format 15 (numeric-content),
    /// COBOL-2014; kb/Work PB452.
    /// <para>⛔ MANDATORY BASE LANGUAGE, not an optional module: Annex A.3's processor-dependent list does not
    /// contain it and Annex A.4's ENTIRE SET inventory is three items — format 6 (A.4.2 #24), format 14
    /// (A.4.4 #3) and formats 11/12 (A.4.9 #9) — so §4.2.7 cannot decline it either.</para>
    /// <para>The value is a compile-time property of EACH receiver's own data description (and, under
    /// IN-ARITHMETIC-RANGE, of the arithmetic mode), so the statement resolves here to one store per receiver —
    /// which is also why several receivers of different descriptions each get a DIFFERENT value. The extremes
    /// come from <see cref="AlgebraicRanges"/>, the same evaluator the §15.43/§15.58/§15.83 intrinsics read:
    /// Annex D.32 states the equivalence outright ("<c>SET CONTENT OF numeric-item TO FARTHEST-FROM-ZERO</c> …
    /// the same as … <c>MOVE HIGHEST-ALGEBRAIC (numeric-item) TO numeric-item</c>"), so a second computation
    /// here would be one rule written down twice.</para>
    /// <para>SCREENS: SR31 — FARTHEST-FROM-ZERO / NEAREST-TO-ZERO take a NUMERIC data item (§8.5.2.12), which a
    /// numeric-EDITED item is not (§8.5.2.13), so this is NARROWER than the intrinsics' §15.x.3 r1 →
    /// COBOLNET1938; SR31 a) — the SIGN phrase is required when the receiver's two farthest-from-zero magnitudes
    /// differ → COBOLNET1939; SR32 — the three float words take a STANDARD floating-point usage (§3.166/§3.167,
    /// NOT COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) → COBOLNET1940.</para></summary>
    private BoundStatement BindSetContent(Core.SetContentStatementContext sc)
    {
        var v = sc.setContentValue();
        bool farthest = v.FARTHEST_FROM_ZERO() is not null;
        bool nearest = v.NEAREST_TO_ZERO() is not null;
        bool inArithmeticRange = v.IN_ARITHMETIC_RANGE() is not null;
        // GR32 c / GR36 c / GR33-GR35 all end the same way: "If the SIGN phrase is specified, the sign … is set
        // according to the SIGN specification; otherwise … positive." One reading, applied to every leg.
        bool negative = v.setContentSign()?.NEGATIVE() is not null;
        bool signWritten = v.setContentSign() is not null;

        var stores = new List<SetContentStore>();
        foreach (var dref in sc.dataReference())
        {
            if (host.Expr.ResolveReceiving(dref) is not { } place)
                return new BoundUnsupported($"SET CONTENT OF '{dref.GetText()}'");
            string name = place.Item.CobolName ?? dref.GetText();
            var pic = place.Item.Pic;

            if (farthest || nearest)
            {
                // SR31: "identifier-14 shall reference a numeric data item" — §8.5.2.12's category, so an
                // EDITED item, a group, an index item and a reference-modified reference are all refused.
                if (place is RefModPlace || place.Item.IsGroup
                    || pic is not { Category: PicCategory.Numeric } || pic.Usage is Usage.Index
                    || AlgebraicRanges.Of(pic, ctx.Data.DecimalPointIsComma) is not { } range)
                {
                    ctx.Edition.Error(DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO {(farthest ? "FARTHEST-FROM-ZERO" : "NEAREST-TO-ZERO")}: "
                        + "identifier-14 shall reference a numeric data item (ISO §14.9.39.3 syntax rule 31)");
                    return new BoundNop();
                }
                // SR31 a): the receiver's positive and negative extremes differ in magnitude — the two's-
                // complement containers of §13.18.60.4 GR12 — so "the value farthest away from zero permitted
                // by the specifications of identifier-14" (GR32 a) does not name one value, and the SIGN phrase
                // resolves it. Measured on the range itself, never on a usage list, so a future capacity
                // discipline with the same asymmetry inherits the rule.
                // ⚠ SR31 b) (the NEAREST-TO-ZERO twin) has NO violating input in this implementation and is
                // deliberately not written as a second test: the nonzero value nearest to zero is 10^(−scale)
                // for every fixed-point description and the carrier's subnormal minimum for every float one,
                // both of them MAGNITUDES that are identical in the two directions. It would become writable
                // only if AlgebraicRange gained a directional Nearest, and inventing one now would be a lookup
                // nothing reads (feedback_a_dead_lookup_is_also_unverified).
                if (farthest && !signWritten && range.FarthestNegative is { } fneg
                    && AlgebraicRanges.CompareMagnitude(range.Farthest, fneg) != 0)
                {
                    ctx.Edition.Error(DiagnosticCatalog.SetContentSignRequired,
                        $"SET CONTENT OF '{name}' TO FARTHEST-FROM-ZERO: this item's positive and negative "
                        + $"values farthest from zero are {range.Farthest} and {fneg}, whose absolute values "
                        + "differ, so the SIGN phrase shall be specified (ISO §14.9.39.3 syntax rule 31 a)");
                    return new BoundNop();
                }
                // GR32 a / GR36 a — the extreme the receiver's own description permits, in the direction the
                // SIGN phrase selects. An UNSIGNED receiver has no negative extreme: GR32 a still yields its
                // farthest-from-zero value and GR32 c then asks for a negative sign the item cannot hold, so
                // the literal is negated and the ORDINARY store rule drops the sign — no special case here.
                string? magnitude = farthest
                    ? (negative ? range.FarthestNegative ?? "-" + range.Farthest : range.Farthest)
                    : (negative ? (range.Nearest is { } n ? "-" + n : null) : range.Nearest);
                // Null only for a FLOATING-POINT numeric-edited description, whose nearest-to-zero value is
                // deliberately unmodelled — and SR31 has already refused every edited receiver above (category
                // numeric-edited is not category numeric). CHECKED rather than asserted, so this arm can never
                // become a silent null dereference if that screen ever moves.
                if (magnitude is null)
                {
                    ctx.Edition.Error(DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO NEAREST-TO-ZERO: identifier-14 shall reference a numeric "
                        + "data item (ISO §14.9.39.3 syntax rule 31)");
                    return new BoundNop();
                }
                string value = inArithmeticRange ? ClampToArithmeticRange(magnitude, farthest) : magnitude;
                // The store is BOUND here, not synthesized at emission: BoundMove classifies its per-target
                // dispatch at construction and the binder's storage-fact passes only see nodes that exist by
                // the end of binding (kb/Work PB348 — an emitter-built move escaped both and aborted a run).
                stores.Add(new SetContentStore(place,
                    new BoundMove(new BoundComputedOperand(new BoundNumLiteral(value)), [place]), Ieee: null));
                continue;
            }

            // SR32: FLOAT-INFINITY / FLOAT-NOT-A-NUMBER / FLOAT-NOT-A-NUMBER-SIGNALING take "a data item
            // described with a STANDARD floating-point usage" — §3.166's float-binary-32/-64/-128 and §3.167's
            // float-decimal-16/-34 and nothing else. The two families come from the ONE place they are written
            // down (UsageFamilies), so a future member is admitted here automatically (kb/Work PB174).
            if (place is RefModPlace || pic is not { } fpic
                || !(UsageFamilies.IsStandardBinaryFloat(fpic.Usage) || UsageFamilies.IsStandardDecimalFloat(fpic.Usage)))
            {
                ctx.Edition.Error(DiagnosticCatalog.SetContentNotStandardFloat,
                    $"SET CONTENT OF '{name}' TO {FloatWordOf(v)}: identifier-14 shall reference a data item "
                    + "described with a standard floating-point usage — FLOAT-BINARY-32/-64/-128 (ISO §3.166) or "
                    + "FLOAT-DECIMAL-16/-34 (§3.167). FLOAT-SHORT, FLOAT-LONG, FLOAT-EXTENDED, COMP-1 and COMP-2 "
                    + "are floating-point but not STANDARD floating-point, and carry no ISO/IEC 60559:2020 basic "
                    + "interchange format for the canonical representation to be taken from "
                    + "(ISO §14.9.39.3 syntax rule 32)");
                return new BoundNop();
            }
            stores.Add(new SetContentStore(place, Store: null,
                Ieee: IeeeSpecialOf(v), NegativeSign: negative));
        }
        return new BoundSetContent(stores);
    }

    /// <summary>§14.9.39.4 GR32 b) / GR36 b) — the IN-ARITHMETIC-RANGE phrase: the content is set either to the
    /// receiver's own extreme or to "the value … permitted by the specifications appropriate to the mode of
    /// arithmetic", whichever is CLOSER to zero for FARTHEST-FROM-ZERO and FARTHER from zero for NEAREST-TO-ZERO.
    /// The mode's extremes are <see cref="ArithmeticModes.IntermediateExtremes"/>; the comparison is exact
    /// scaled-BigInteger, because these magnitudes reach 10^±6176 and no CLR numeric type spans that.
    /// <para>⚠ LATENT BY MEASUREMENT, NOT BY ASSUMPTION: for every data description COBOL.NET can declare today
    /// the receiver's bound wins or ties (the widest carrier is binary64, whose extremes ARE the native
    /// intermediate's, and the standard modes' SDIDI is wider still), so this method currently always returns
    /// its argument. It is written as the real comparison anyway — the clamp starts biting the moment a wider
    /// carrier lands (a true IEEE binary128 FLOAT-BINARY-128 reaches 1.19E+4932, past the native intermediate),
    /// and then it bites without anyone having to remember it.</para></summary>
    private string ClampToArithmeticRange(string value, bool farthest)
    {
        var (modeFarthest, modeNearest) = ArithmeticModes.IntermediateExtremes(ctx.Data.Options.Arithmetic);
        string bound = farthest ? modeFarthest : modeNearest;
        int cmp = AlgebraicRanges.CompareMagnitude(value, bound);
        // FARTHEST: take the smaller magnitude. NEAREST: take the larger.
        bool takeBound = farthest ? cmp > 0 : cmp < 0;
        if (!takeBound) return value;
        return value.StartsWith('-') ? "-" + bound : bound;
    }

    /// <summary>Which of the three float value words §14.9.39.2 Format 15 wrote — for the SR32 diagnostic.</summary>
    private static string FloatWordOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? "FLOAT-INFINITY"
        : v.FLOAT_NOT_A_NUMBER() is not null ? "FLOAT-NOT-A-NUMBER"
        : "FLOAT-NOT-A-NUMBER-SIGNALING";

    /// <summary>§14.9.39.4 GR33/GR34/GR35 — WHICH canonical value Format 15 wrote. The parse tree is read
    /// exactly here and nowhere further in; the ENCODINGS and the Annex A.1 item 176 payload determination they
    /// stand for belong to <see cref="IeeeSpecials"/> (docs/CONFORMANCE.md §7, DOC-A.1-176), and the spelling
    /// belongs to the backend.</summary>
    private static IeeeSpecial IeeeSpecialOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? IeeeSpecial.Infinity
        : v.FLOAT_NOT_A_NUMBER() is not null ? IeeeSpecial.QuietNaN
        : IeeeSpecial.SignalingNaN;

    /// <summary><c>SET LOCALE {category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT}</c>
    /// (ISO §14.9.39 Format 11; DESIGN-locale-facility §4.3; kb/Work PB64 T1). The first operand is USER-DEFAULT (GR22 —
    /// the user default is set) or a SET of categories (the printed brace carries choice indicators — §5.2.6.4: one or
    /// more, each at most once, any order; a duplicate is COBOLNET1666). The TO operand is one dataReference split here:
    /// USER-DEFAULT / SYSTEM-DEFAULT (GR23b/c; SR25 — not with a USER-DEFAULT first operand → COBOLNET1667), a
    /// locale-name of the SPECIAL-NAMES LOCALE clause (SR26 — undeclared → COBOLNET1664), else identifier-10, which
    /// shall be an elementary data item of category data-pointer (SR27 → COBOLNET1668).</summary>
    private BoundStatement BindSetLocale(Core.SetLocaleStatementContext sl)
    {
        var words = sl.cobolWord();                 // [0] = LOCALE, [1..] = categories | USER-DEFAULT
        bool setsUserDefault = false;
        var categories = LocaleCategorySet.None;
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): USER-DEFAULT and the LC_ categories are
        // §8.9/§8.10 words the lexer does not tokenize, so the map reaches them only here.
        if (ctx.CobolWords.Is(words[1].GetText(), "USER-DEFAULT"))
        {
            setsUserDefault = true;
            if (words.Length > 2)
            {
                ctx.Edition.Error("COBOLNET1666", $"SET LOCALE USER-DEFAULT {words[2].GetText()}: USER-DEFAULT as the first operand stands alone — "
                    + "the general format's outer brace is a plain alternation of the category list OR USER-DEFAULT (ISO §14.9.39.2 format 11)");
                return new BoundNop();
            }
        }
        else
        {
            for (int i = 1; i < words.Length; i++)
            {
                string written = words[i].GetText().ToUpperInvariant();
                // The CANONICAL word decides the category (GR2/GR4); a de-reserved word resolves to null and
                // is not a category at all (GR3) - the diagnostic below then names it, as it should.
                string w = ctx.CobolWords.Resolve(written) ?? "";
                LocaleCategorySet cat = w switch
                {
                    "LC_ALL" => LocaleCategorySet.All,
                    "LC_COLLATE" => LocaleCategorySet.Collate,
                    "LC_CTYPE" => LocaleCategorySet.Ctype,
                    "LC_MESSAGES" => LocaleCategorySet.Messages,
                    "LC_MONETARY" => LocaleCategorySet.Monetary,
                    "LC_NUMERIC" => LocaleCategorySet.Numeric,
                    "LC_TIME" => LocaleCategorySet.Time,
                    _ => LocaleCategorySet.None,
                };
                if (cat == LocaleCategorySet.None)
                {
                    ctx.Edition.Error("COBOLNET1666", $"SET LOCALE {words[i].GetText()}: '{words[i].GetText()}' is not a locale category — "
                        + "the first operand is one or more of LC_ALL, LC_COLLATE, LC_CTYPE, LC_MESSAGES, LC_MONETARY, LC_NUMERIC, LC_TIME, "
                        + "or USER-DEFAULT (ISO §14.9.39.2 format 11)");
                    return new BoundNop();
                }
                // §5.2.6.4 — "any single alternative shall be specified only once": the SAME word twice is the violation
                // (LC_ALL beside LC_TIME is two different alternatives — redundant, legal).
                bool duplicate = false;
                for (int j = 1; j < i; j++) if (ctx.CobolWords.Is(words[j].GetText(), w)) duplicate = true;
                if (duplicate)
                {
                    ctx.Edition.Error("COBOLNET1666", $"SET LOCALE … {w}: the category {w} is specified more than once — each alternative "
                        + "of the category brace shall be specified at most once (ISO §14.9.39.2 format 11 / §5.2.6.4)");
                    return new BoundNop();
                }
                categories |= cat;
            }
        }
        // The TO operand: USER-DEFAULT / SYSTEM-DEFAULT / locale-name-1 / identifier-10.
        var to = sl.dataReference();
        string toText = to.GetText();
        string first = setsUserDefault ? "SET LOCALE USER-DEFAULT" : $"SET LOCALE {string.Join(' ', words.Skip(1).Select(x => x.GetText()))}";
        if (ctx.CobolWords.Is(toText, "USER-DEFAULT") || ctx.CobolWords.Is(toText, "SYSTEM-DEFAULT"))
        {
            bool user = ctx.CobolWords.Is(toText, "USER-DEFAULT");
            if (setsUserDefault)
            {
                ctx.Edition.Error("COBOLNET1667", $"{first} TO {toText.ToUpperInvariant()}: if USER-DEFAULT is specified as the first operand, "
                    + "identifier-10 or locale-name-1 shall be specified in the TO phrase (ISO §14.9.39.3 SR25)");
                return new BoundNop();
            }
            return new BoundSetLocale(categories, false, user ? LocaleSetSource.UserDefault : LocaleSetSource.SystemDefault, null, null);
        }
        // A bare word that is a declared locale-name (SR26) — before any data item of the same spelling: the
        // locale-name is the format's FIRST listed meaning for a user word here, and §8.3.2.2 keeps the two name
        // types apart by context.
        if (to.ChildCount == 1 && ctx.Data.Locales.TryGetValue(toText, out var symbol))
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.LocaleName, symbol, null);
        // identifier-10: an elementary data item of category data-pointer (SR27).
        if (ctx.Refs.Probe(to) is { } probe)
        {
            if (probe.Item.Pic?.Category is not PicCategory.Pointer)
            {
                ctx.Edition.Error("COBOLNET1668", $"{first} TO {toText}: identifier-10 shall reference an elementary data item of category "
                    + $"data-pointer (ISO §14.9.39.3 SR27) — '{toText}' is {probe.Item.Pic?.Category.ToString() ?? "a group"}");
                return new BoundNop();
            }
            var place = ctx.Refs.Resolve(to);
            if (place is null) return new BoundNop();
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.SavedPointer, null, place);
        }
        // Neither: the ONE undeclared-locale-name diagnostic, with this site named (SR26).
        ctx.Data.ResolveLocaleName(toText, $"{first} TO {toText}",
            "ISO §14.9.39.3 SR26 — locale-name-1 shall be specified in the LOCALE clause of the SPECIAL-NAMES paragraph; "
            + "or SR27 — identifier-10 shall reference an elementary data item of category data-pointer, and no such item is declared");
        return new BoundNop();
    }

    /// <summary><c>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §14.9.39 Format 12; kb/Work PB64 T1):
    /// identifier-11 shall be an elementary data item of category data-pointer (SR28 → COBOLNET1668); the words after TO
    /// are LOCALE and LC_ALL (GR26) or USER-DEFAULT (GR27) — the predicate admitted exactly those.</summary>
    private BoundStatement BindSaveLocale(Core.SetLocaleStatementContext sl)
    {
        bool userDefault = ctx.CobolWords.Is(sl.cobolWord(1).GetText(), "USER-DEFAULT");
        var target = sl.dataReference();
        if (ctx.Refs.Resolve(target) is not { } place || place.Item.Pic?.Category is not PicCategory.Pointer)
        {
            ctx.Edition.Error("COBOLNET1668", $"SET {target.GetText()} TO LOCALE {(userDefault ? "USER-DEFAULT" : "LC_ALL")}: identifier-11 shall "
                + "reference an elementary data item of category data-pointer (ISO §14.9.39.3 SR28)");
            return new BoundNop();
        }
        return new BoundSaveLocale(place, userDefault);
    }

    /// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1). Receivers may mix index-names and data
    /// items; the sender is any integer-valued operand (an index-name sender reads its occurrence number, §3.5).</summary>
    /// <summary>SET data-pointer assignment (§14.9.39 Format 4; Phase-4b increment 1): every target shall
    /// be USAGE POINTER (COBOLNET0869 otherwise); the sender is the NULL figurative or another data pointer
    /// (SELF/SUPER are object-only — 0869). ADDRESS OF senders/receivers are increment 2 (staged loud).</summary>
    private BoundStatement BindSetPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, Core.DataReferenceContext? senderRef,
        bool toNull, bool senderIsSelfSuper)
    {
        if (senderIsSelfSuper)
        {
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                "SET … TO SELF/SUPER: SELF and SUPER are object references, not data pointers "
                + "(ISO §14.9.39 Format 4/5 — the sender of a pointer SET is NULL or another pointer)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (ctx.Refs.Resolve(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET '{t.GetText()}': the receiving operand of a data-pointer SET shall be USAGE POINTER "
                    + "(ISO §14.9.39 Format 4)");
                return new BoundNop();
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET pointer — sender shape");
            if (ctx.Refs.Resolve(senderRef) is not { } sp || sp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET … TO '{senderRef?.GetText()}': a data-pointer sender shall be NULL or another "
                    + "USAGE POINTER item (ISO §14.9.39 Format 4; ADDRESS OF senders are a later increment)");
                return new BoundNop();
            }
            source = sp;
        }
        return new BoundSetPointer(targets, source, toNull);
    }

    /// <summary>SET program-pointer assignment (ISO §14.9.39 Format 9; SR21 — every target AND the sender
    /// shall be category program-pointer; P10 Step 7): the data-pointer Format-4 twin over the ProgramPointer
    /// carrier. The sender is NULL or another program-pointer; SELF/SUPER are object references (0869).</summary>
    private BoundStatement BindSetProgramPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, Core.DataReferenceContext? senderRef,
        bool toNull, bool senderIsSelfSuper)
    {
        if (senderIsSelfSuper)
        {
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                "SET … TO SELF/SUPER: SELF and SUPER are object references, not program pointers "
                + "(ISO §14.9.39 Format 5/9 — the sender of a program-pointer SET is NULL, another "
                + "program-pointer, or an ENTRY program-address-identifier)");
            return new BoundNop();
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (ctx.Refs.Resolve(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET '{t.GetText()}': the receiving operand of a program-pointer SET shall be USAGE "
                    + "PROGRAM-POINTER (ISO §14.9.39 Format 9 SR21)");
                return new BoundNop();
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (senderRef is null) return new BoundUnsupported("SET program-pointer — sender shape");
            if (ctx.Refs.Resolve(senderRef) is not { } sp
                || sp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET … TO '{senderRef?.GetText()}': a program-pointer sender shall be NULL, another "
                    + "USAGE PROGRAM-POINTER item, or an ENTRY program-address-identifier "
                    + "(ISO §14.9.39 Format 9 SR21 / §8.4.3.13)");
                return new BoundNop();
            }
            source = sp;
        }
        return new BoundSetProgramPointer(targets, source, toNull);
    }

    /// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> (ISO §14.9.39 Format 9 with the
    /// §8.4.3.13 program-address-identifier sender; P10 Step 7): every target shall be category
    /// program-pointer (SR21); the ENTRY operand names the program (§8.4.3.13 GR1 — a literal, or an
    /// identifier whose VALUE names it per §8.3.2.2).</summary>
    private BoundStatement BindSetEntry(Core.SetEntryStatementContext se)
    {
        var targets = new List<Place>(se.dataReference().Length);
        // The LAST dataReference is the ENTRY identifier operand when no literal is present — the grammar
        // shape is `SET dataReference+ TO ENTRY (nonNumericLiteral | dataReference)`.
        var drefs = se.dataReference();
        bool identForm = se.nonNumericLiteral() is null;
        int targetCount = identForm ? drefs.Length - 1 : drefs.Length;
        if (targetCount < 1) return new BoundUnsupported("SET … TO ENTRY — no receiving operand");
        for (int i = 0; i < targetCount; i++)
        {
            if (ctx.Refs.Resolve(drefs[i]) is not { } tp || tp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET '{drefs[i].GetText()}': the receiving operand of SET … TO ENTRY shall be USAGE "
                    + "PROGRAM-POINTER (ISO §14.9.39 Format 9 SR21 / §8.4.3.13)");
                return new BoundNop();
            }
            targets.Add(tp);
        }
        if (!identForm)
        {
            var nn = se.nonNumericLiteral();
            if (nn?.STRINGLIT() is not { } lit)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET … TO ENTRY {nn?.GetText()}: the ENTRY literal shall be an alphanumeric literal "
                    + "naming a program (ISO §8.4.3.13 / §8.3.2.2)");
                return new BoundNop();
            }
            return new BoundSetEntry(targets, CobolLiteral.Decode(lit.GetText()), null);
        }
        if (ctx.Refs.Resolve(drefs[^1]) is not { } namePlace)
        {
            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                $"SET … TO ENTRY '{drefs[^1].GetText()}': the ENTRY identifier is unresolvable (ISO §8.4.3.13 GR1a)");
            return new BoundNop();
        }
        return new BoundSetEntry(targets, null, namePlace);
    }

    public BoundStatement BindSetTo(Core.SetToValueStatementContext tv)
    {
        // SET Format 14 (ISO §14.9.39; the OCCURS DYNAMIC feature, data-model D9): a CAPACITY-register target
        // reroutes to a capacity change. It runs BEFORE the F4/F5 pointer/object reroutes — a register is numeric,
        // so it would otherwise fall through to the Format-1 store and throw at CapacityRegisterPlace.Write.
        if (DynTryBindSetCapacity(tv.dataReference(), tv.arithmeticExpression(), SetCapacityKind.To) is { } dcap)
            return dcap;
        // SET Format 16 SIZE-OF-absent bare form (ISO §14.9.39; SIZE OF is optional): `SET dyn TO n` on a
        // dynamic-length elementary item reroutes to the length-set. A dynamic-length item is alphanumeric/national,
        // so the Format-1 value path cannot carry it — the peek disambiguates on the resolved item type.
        if (DynTrySetSize(tv.dataReference(), tv.arithmeticExpression()) is { } dsz) return dsz;
        // The Format-5 SEMANTIC re-route (D-U7): `SET U TO A` parses HERE (alternative order — a
        // dataReference sender is an arithmeticExpression prefix), but an object-reference TARGET selects
        // §14.9.39 Format 5. Detect on the FIRST target; mixed target categories then fail SR8 inside.
        if (tv.dataReference() is { Length: > 0 } tds
            && OoBinder.OoExtractBareReference(tv.arithmeticExpression()) is { } senderDref)
        {
            var t0 = ctx.Refs.Probe(tds[0])?.Item.Pic?.Category;        // Probe — format sniffs; the selected
            var s0 = ctx.Refs.Probe(senderDref)?.Item.Pic?.Category;    // format's own bind demands (R30)
            // A POINTER on either side selects Format 4 (SET pointer TO pointer) — the Format-1 numeric
            // path cannot carry a ManagedPointer.
            if (t0 is PicCategory.Pointer || s0 is PicCategory.Pointer)
                return BindSetPointer(tds, senderDref, toNull: false, senderIsSelfSuper: false);
            // A PROGRAM-POINTER on either side selects Format 9 (SET pp TO pp — SR21; P10 Step 7).
            if (t0 is PicCategory.ProgramPointer || s0 is PicCategory.ProgramPointer)
                return BindSetProgramPointer(tds, senderDref, toNull: false, senderIsSelfSuper: false);
            // Either side being an object reference selects Format 5 (§14.9.39 F5; D-U7).
            if (t0 is PicCategory.ObjectReference || s0 is PicCategory.ObjectReference)
                return host.Oo.OoBindSetObjectRef(tds, senderDref, senderNull: false, senderSelf: false, senderSuper: false);
        }
        var targets = new List<BoundSetTarget>();
        foreach (var dref in tv.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetTo(targets, host.Expr.BindIndexWindowExpr(tv.arithmeticExpression()));   // SET is an r7 window (kb/Work R29)
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2) — with the Format-10
    /// data-pointer re-route on the FIRST target's category (the D-U7 semantic-re-route pattern; the two
    /// formats share one grammar shape).</summary>
    public BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        if (host.Ptr.TryBindSetUpDown(ud) is { } ptr) return ptr;   // F10 — pointer arithmetic (Phase-4b inc 2)
        if (DynTryBindSetCapacity(ud.dataReference(), ud.arithmeticExpression(),
                ud.DOWN() is not null ? SetCapacityKind.DownBy : SetCapacityKind.UpBy) is { } dcap)
            return dcap;   // F14 — dynamic-capacity change (OCCURS DYNAMIC, D9)
        var targets = new List<BoundSetTarget>();
        foreach (var dref in ud.dataReference())
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{dref.GetText()}'");
            targets.Add(t);
        }
        return new BoundSetUpDown(targets, host.Expr.BindIndexWindowExpr(ud.arithmeticExpression()), ud.DOWN() is not null);
    }

    /// <summary>SET Format 14 (ISO §14.9.39; OCCURS DYNAMIC, data-model D9): reroute when the FIRST target resolves
    /// to a dynamic-table CAPACITY register — <c>SET reg {TO | UP BY | DOWN BY} n</c> changes the table's current
    /// capacity. A non-register first target returns <see langword="null"/> so the normal Format-1/2 path continues
    /// (the non-consuming peek idiom, mirroring <c>PtrTryBindSetUpDown</c>). The register is the SOLE receiver of a
    /// capacity SET (one capacity per statement); a second/mixed target is COBOLNET1524.</summary>
    private BoundStatement? DynTryBindSetCapacity(
        IReadOnlyList<Core.DataReferenceContext> targets, Core.ArithmeticExpressionContext amount, SetCapacityKind kind)
    {
        // A PURE capacity-register peek (NOT refs.Resolve, which would route an OO `prop OF obj` first target through
        // the property hook and enqueue a spurious pending op — OCCURS DYNAMIC review #7).
        if (targets.Count == 0 || ctx.Refs.CapacityRegisterFor(targets[0]) is not { } cap) return null;
        if (targets.Count > 1)
        {
            ctx.Edition.Error("COBOLNET1524",
                $"SET '{cap.RegisterItem.CobolName}' {SetCapacityKindText(kind)}: a dynamic-table CAPACITY register "
                + "is the sole receiver of a SET Format 14 statement (ISO §14.9.39; §13.18.38 Format 4)");
            return new BoundNop();
        }
        return new BoundSetCapacity(cap.Table, host.Expr.BindIndexWindowExpr(amount), kind);
    }

    private static string SetCapacityKindText(SetCapacityKind kind) =>
        kind switch { SetCapacityKind.To => "TO", SetCapacityKind.UpBy => "UP BY", _ => "DOWN BY" };

    /// <summary>SET [SIZE OF] data-name-3 TO n (ISO §14.9.39 Format 16, COBOL-2023): set the current length of a
    /// dynamic-length elementary item. data-name-3 shall itself be dynamic-length (SR33 → COBOLNET1568). The 2023
    /// introduction gate is on the <see cref="BoundSetSize"/> node (VersionConformancePass semantic arm), covering
    /// both the explicit SIZE OF form and the bare re-routed form. Whether EC-STORAGE-NOT-AVAIL checking is enabled
    /// at this statement is captured from the TurnState NOW (§14.9.39.4 GR37/GR38 — the nonfatal condition the
    /// negative/clamp legs set), mirroring the CONTINUE AFTER EC-CONTINUE-LESS-THAN-ZERO capture.</summary>
    private BoundStatement BindSetSize(Core.DataReferenceContext dref, Core.ArithmeticExpressionContext amount)
    {
        if (host.Expr.ResolveReceiving(dref) is not { } p)
            return new BoundUnsupported($"SET SIZE OF '{dref.GetText()}'");
        if (!p.Item.IsDynamicLength)
        {
            ctx.Edition.Error("COBOLNET1568",
                $"SET SIZE OF '{p.Item.CobolName}': data-name-3 shall be a dynamic-length elementary item "
                + "(ISO §14.9.39 Format 16 SR33)");
            return new BoundNop();
        }
        bool checkStorage = ctx.EcState.Turn.Enabled("EC-STORAGE-NOT-AVAIL", null, dref.Start.Line);
        return new BoundSetSize(p, host.Expr.BindIndexWindowExpr(amount), p.Item.DynLengthLimit, checkStorage);
    }

    /// <summary>The SIZE-OF-absent bare-form peek (ISO §14.9.39 Format 16): reroute `SET dyn TO n` when the sole,
    /// bare (unqualified/unsubscripted) target resolves to a dynamic-length elementary item; null otherwise so the
    /// normal Format-1/5 path continues. Guarding on a bare name BEFORE resolving keeps a speculative resolve off
    /// the OO property hook (the DynTryBindSetCapacity discipline — a dynamic-length item is never a property).</summary>
    private BoundStatement? DynTrySetSize(
        IReadOnlyList<Core.DataReferenceContext> targets, Core.ArithmeticExpressionContext amount)
    {
        if (targets.Count != 1 || targets[0].dataReferenceSuffix().Length != 0) return null;
        // An index-name target belongs to Format 1 — peek it away BEFORE ResolveReceiving, whose demanding
        // Resolve would report COBOLNET1639 on a name that is legally not a data item (R30).
        if (host.Expr.IndexFieldOf(targets[0]) is not null) return null;
        if (host.Expr.ResolveReceiving(targets[0]) is not { Item.IsDynamicLength: true } p) return null;
        bool checkStorage = ctx.EcState.Turn.Enabled("EC-STORAGE-NOT-AVAIL", null, targets[0].Start.Line);
        return new BoundSetSize(p, host.Expr.BindIndexWindowExpr(amount), p.Item.DynLengthLimit, checkStorage);
    }

    /// <summary>A SET receiving operand: an INDEXED BY index-name (its <c>long</c> field) or a resolvable data item
    /// (an index data item or an integer item — the emitter dispatches on its usage).</summary>
    public BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) =>
        host.Expr.IndexFieldOf(dref) is { } ix ? new SetIndexTarget(ix)
        : host.Expr.ResolveReceiving(dref) is { } p ? new SetPlaceTarget(p)   // a SET receiver IS a receiving operand
        : null;

    /// <summary><c>SET condition-name+ TO TRUE</c> (ISO §14.9.39 Format 4). TO FALSE needs the 2002 <c>WHEN SET TO
    /// FALSE</c> VALUE phrase (SR7) — loud until the 88 model captures it.</summary>
    public BoundStatement BindSetCondition(Core.SetBooleanStatementContext b)
    {
        if (b.TRUE_() is null)
            return new BoundUnsupported("SET condition-name TO FALSE (the VALUE … WHEN SET TO FALSE phrase, COBOL-2002+, ISO §14.9.39 SR7)");
        var sets = new List<(Place, Condition88)>();
        foreach (var dref in b.dataReference())
        {
            if (host.Cond.ConditionOf(dref) is not { } cond) return new BoundUnsupported($"SET '{dref.GetText()}' TO TRUE (not a condition-name)");
            // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
            if (ctx.Refs.ResolveForItem(dref, cond.Parent) is not { } parent)
                return new BoundUnsupported($"SET condition '{cond.Name}' (unresolvable conditional variable)");
            sets.Add((parent, cond));
        }
        return new BoundSetConditions(sets);
    }
}
