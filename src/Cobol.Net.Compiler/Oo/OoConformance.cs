// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>One covariant-return adapter pair (§9.3.8.2.3 rules 5a/5c2 — conforming COBOL that C# interface
/// implementation cannot express directly, because interface implementations demand the EXACT return type):
/// the emitter renders an EXPLICIT interface implementation <c>PROTO_RET I_CS.M(…) =&gt; this.M(…);</c> per
/// pair; <paramref name="Factory"/> marks the FACTORY side.</summary>
public readonly record struct AdapterPair(
    OoInterfaceSymbol Iface, OoMethodSymbol Proto, OoMethodSymbol Impl, bool Factory);

/// <summary>
/// The OO conformance SERVICE (P9 — R3: validation moved OFF the pass-1 symbol table, which stays a pure
/// lookup structure): the §9.3.8.2 override-signature check, the §9.3.11 IMPLEMENTS pass (returning the
/// covariant <see cref="AdapterPair"/>s instead of mutating table state), the ONE strict identical-description
/// check (§14.8.2.3.2), its runtime descriptor projection (D-U3), and the §14.8.3.3-rule-1 object-reference
/// widening direction. Stateless — every entry takes the <see cref="OoClassTable"/> it validates over.
/// </summary>
public static class OoConformance
{
    /// <summary>Validate every override's SIGNATURE against the overridden method (§9.3.8.2 method-signature
    /// conformance: the same formal count with identical descriptions, and identical RETURNING items — via
    /// <see cref="DescriptionMismatch"/>, the ONE description-equality check shared with INVOKE argument
    /// conformance so the two rules can never drift apart). Runs AFTER every class's data has bound (formals
    /// resolve at data-bind time, not pass-1). A violation is COBOLNET0829 — a COBOL-worded bind diagnostic,
    /// never a Roslyn CS0508/CS0115 on user source (the G4 rule).</summary>
    public static void ValidateOverrideSignatures(OoClassTable table, EditionContext edition)
    {
        foreach (var cls in table.Classes)
            foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
            {
                if (m.OverrideOf is not { } baseM) continue;
                string where = $"class '{cls.Name}', method '{m.Name}' overriding '{baseM.Name}'";
                if (m.Binding!.Formals.Count != baseM.Binding!.Formals.Count)
                {
                    edition.Error("COBOLNET0829", $"{where}: {m.Binding!.Formals.Count} formal parameter(s) vs the "
                        + $"overridden method's {baseM.Binding!.Formals.Count} (ISO §9.3.8.2 — an override's signature "
                        + "shall conform)");
                    continue;
                }
                for (int i = 0; i < m.Binding!.Formals.Count; i++)
                    if (DescriptionMismatch(baseM.Binding!.Formals[i].Item, m.Binding!.Formals[i].Item) is { } err)
                        edition.Error("COBOLNET0829", $"{where}: formal parameter #{i + 1} "
                            + $"('{m.Binding!.Formals[i].Item.CobolName}'): {err} (ISO §9.3.8.2)");
                if ((m.Binding!.Returning is null) != (baseM.Binding!.Returning is null))
                    edition.Error("COBOLNET0829", $"{where}: RETURNING presence differs from the overridden "
                        + "method (ISO §9.3.8.2)");
                else if (m.Binding!.Returning is { } r && baseM.Binding!.Returning is { } br)
                {
                    // §9.3.8.2.3 rules 5a/5c2 — a COVARIANT object-reference RETURNING is legal: a universal
                    // base accepts any object-reference override; a typed base accepts the SAME class or a
                    // SUBCLASS (C# 9+ covariant returns render it directly). Everything else stays the strict
                    // rule-6 identical-description check.
                    if (r.Pic is { Category: PicCategory.ObjectReference } rp
                        && br.Pic is { Category: PicCategory.ObjectReference } brp)
                    {
                        if (ObjectRefWideningMismatch(table, rp, brp) is { } werr)
                            edition.Error("COBOLNET0829", $"{where}: RETURNING item: {werr} "
                                + "(ISO §9.3.8.2.3 rules 5a/5c2 — the override's class shall be the same "
                                + "class or a subclass of the overridden method's)");
                    }
                    else if (DescriptionMismatch(br, r) is { } rerr)
                        edition.Error("COBOLNET0829", $"{where}: RETURNING item: {rerr} (ISO §9.3.8.2)");
                }
            }
    }

    /// <summary>The §9.3.11 IMPLEMENTS conformance pass (via §9.3.8.2.3 — D-I1: the BINDER is the authority;
    /// Roslyn satisfaction is provably insufficient BOTH directions: the C# projection is lossy for
    /// non-object descriptions [PIC 9(4) and 9(8) both emit `ref long` — Roslyn under-rejects], and C#
    /// interface implementations forbid covariant returns that rules 5a/5c2 PERMIT [Roslyn over-rejects
    /// conforming COBOL — cured by the explicit-implementation adapters]). Runs AFTER all class AND
    /// interface formals resolve. The check runs over the §11.8.4 GR2 transitive CLOSURE (direct IMPLEMENTS
    /// + interface-INHERITed + class-INHERITed) per class; each violation is COBOLNET0841 citing the
    /// numbered rule. RETURNS the covariant-return <see cref="AdapterPair"/>s (P9 R3 — a validation pass
    /// reports its findings; it does not mutate the symbol table).</summary>
    public static IReadOnlyList<AdapterPair> ValidateImplements(OoClassTable table, EditionContext edition)
    {
        var adapters = new List<AdapterPair>();
        foreach (var cls in table.Classes)
        {
            Check(cls, table.ImplementsClosure(cls, factory: false), factory: false);
            Check(cls, table.ImplementsClosure(cls, factory: true), factory: true);
        }
        return adapters;

        void Check(OoClassSymbol cls, IReadOnlyList<OoInterfaceSymbol> closure, bool factory)
        {
            string side = factory ? "factory " : "";
            foreach (var iface in closure)
                foreach (var proto in iface.AllPrototypes())
                {
                    var impl = factory ? cls.FindFactoryMethod(proto.Name) : cls.FindMethod(proto.Name);
                    if (impl is null)
                    {
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}': the {side}interface '{iface.Name}' requires a method "
                            + $"'{proto.Name}' and none is defined or inherited (ISO §9.3.11 — a class "
                            + "shall implement ALL the method prototypes of its interfaces, including "
                            + "inherited ones)");
                        continue;
                    }
                    if (impl.Binding!.Formals.Count != proto.Binding!.Formals.Count)
                    {
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}', method '{impl.Name}': {impl.Binding!.Formals.Count} formal(s) vs "
                            + $"the '{iface.Name}' prototype's {proto.Binding!.Formals.Count} (ISO §9.3.8.2.3 rule 1)");
                        continue;
                    }
                    for (int i = 0; i < impl.Binding!.Formals.Count; i++)
                        if (DescriptionMismatch(proto.Binding!.Formals[i].Item, impl.Binding!.Formals[i].Item) is { } err)
                            edition.Error("COBOLNET0841",
                                $"class '{cls.Name}', method '{impl.Name}', formal #{i + 1}: {err} "
                                + $"(ISO §9.3.8.2.3 rules 2/3 vs interface '{iface.Name}' — identical "
                                + "descriptions; the C# projection cannot check this)");
                    if ((impl.Binding!.Returning is null) != (proto.Binding!.Returning is null))
                        edition.Error("COBOLNET0841",
                            $"class '{cls.Name}', method '{impl.Name}': RETURNING presence differs from the "
                            + $"'{iface.Name}' prototype (ISO §9.3.8.2.3 rule 4)");
                    else if (impl.Binding!.Returning is { } r && proto.Binding!.Returning is { } pr)
                    {
                        if (r.Pic is { Category: PicCategory.ObjectReference } rp
                            && pr.Pic is { Category: PicCategory.ObjectReference } prp)
                        {
                            if (ObjectRefWideningMismatch(table, rp, prp) is { } werr)
                                edition.Error("COBOLNET0841",
                                    $"class '{cls.Name}', method '{impl.Name}': RETURNING: {werr} "
                                    + "(ISO §9.3.8.2.3 rules 5a/5c2)");
                            else if (!string.Equals(rp.ObjectClassName, prp.ObjectClassName,
                                         StringComparison.OrdinalIgnoreCase))
                                // Conformant-but-covariant: C# needs the explicit-implementation adapter.
                                adapters.Add(new AdapterPair(iface, proto, impl, factory));
                        }
                        else if (DescriptionMismatch(pr, r) is { } rerr)
                            edition.Error("COBOLNET0841",
                                $"class '{cls.Name}', method '{impl.Name}': RETURNING: {rerr} "
                                + "(ISO §9.3.8.2.3 rule 6)");
                    }
                }
        }
    }

    /// <summary>The RUNTIME projection of the strict-conformance rule (D-U3 — the universal-dispatch
    /// wave): ONE descriptor string per description, computed at BIND time on both sides of a universal
    /// crossing; the generated <c>__CobolInvoke</c> switch compares for STRING EQUALITY and raises
    /// EC-OO-UNIVERSAL on mismatch (ISO §14.9.23.4 GR7c — §14.8.2/§14.8.3 conformance through a universal
    /// receiver is checked at runtime, §9.3.8.2.1 NOTE). Locked invariant (unit-tested): descriptor
    /// equality ⇔ <see cref="DescriptionMismatch"/> == null over every carried category — derived NEXT TO
    /// the one mismatch function so the two projections cannot drift (feedback_one_mechanism_per_job).
    /// Deliberate strictness deltas, both LOUD-fail directions (AS-BUILT notes): equality cannot express
    /// §14.8.2.2 rule 1's by-ref group-PREFIX leniency (a smaller formal group raises EC-OO-UNIVERSAL
    /// through universal where the TYPED path accepts a prefix), and JUSTIFIED is encoded on alphanumeric
    /// items while the group⇄alphanumeric image pairing carries none. Not-carried categories return the
    /// <c>T:!</c> sentinel — bind rejects them from universal crossings (0866) before any box exists, and
    /// the sentinel deliberately matches nothing the callee ever emits as a checked formal.</summary>
    public static string ConformanceDescriptor(DataItem item)
    {
        if (item.IsGroup)
            // A VARIABLE-LENGTH group crosses as its §8.5.1.12 component carrier, so its descriptor is that
            // layout's canonical signature (kb/Work PB204) — without it the item fell to the T:! sentinel and
            // bind refused every universal crossing of legal source (COBOLNET0866).
            return item.CurrentExtentImageCapable ? $"V:{VariableLengthCompatibility.Signature(item)}"
                : item.IsImageCapable ? $"S:{item.ImageWidth}:N" : "T:!";
        if (item.Pic is not { } p) return "T:!";
        return p.Category switch
        {
            PicCategory.ObjectReference =>
                p.ObjectClassName is { } cls ? "O:" + cls.ToUpperInvariant() : "O:*",
            PicCategory.Numeric =>
                $"N:{p.Usage}:{p.Digits}:{p.Scale}:{(p.Signed ? "S" : "U")}:{p.SignKind}:"
                + (item.BlankWhenZero ? "B" : "-"),
            PicCategory.Alphanumeric =>
                // An ANY LENGTH item's length is runtime-varying (ISO §13.18.2 GR1) — encoded '*' so the pair
                // semantics track DescriptionMismatch (ANY LENGTH must MATCH between the sides; when both carry
                // it the length compare is void). Through UNIVERSAL dispatch §14.9.23.3 SR7c (:28530) bans an
                // ANY LENGTH formal outright: a concrete argument descriptor never equals 'S:*', so the crossing
                // raises EC-OO-UNIVERSAL (loud) — the one permissive corner (an ANY LENGTH argument meeting an
                // ANY LENGTH formal matches instead of raising) is a documented strictness delta, same family
                // as the by-ref group-prefix delta above.
                $"S:{(item.IsAnyLength ? "*" : p.Length.ToString())}:{(item.Justified ? "J" : "N")}",
            _ => "T:!",
        };
    }

    /// <summary>The ONE strict IDENTICAL-DESCRIPTION check — §14.8.2.3.2 (BY REFERENCE parameters ONLY; BY
    /// CONTENT follows §14.8.2.3.3 COMPUTE/MOVE/SET rules in the binder mode dispatch) and §9.3.8.2
    /// override-signature validation. Identical = same category; numeric: same USAGE + SIGN representation +
    /// BLANK WHEN ZERO + digits + scale + sign; alphanumeric: same length + JUSTIFIED; object reference: same
    /// declared class; group: image crossing with equal character length (except the §14.8.2.2 rule-1 BY
    /// REFERENCE prefix case — <paramref name="byRefGroupPrefix"/> allows a SMALLER formal). Null when
    /// conformant. This strictness keeps BY REFERENCE marshaling TYPE-PRESERVING (the slice-2 design fact);
    /// CONTENT conversions qualify the owner class internal profiles instead.</summary>
    public static string? DescriptionMismatch(DataItem formal, DataItem arg, bool byRefGroupPrefix = false,
        bool anyLengthActivationRelax = false)
    {
        // ANY LENGTH (ISO §13.18.2). PAIR mode (the default — override/implements signatures and the universal
        // descriptor; the §9.3.8.2/§14.8.2 conformance tables :12177/:12247/:12335/:12383 list ANY LENGTH among
        // the clauses that shall be THE SAME between corresponding items): the clause must match between the
        // sides, and when both carry it the length compare is void — both lengths track the same runtime
        // argument (GR1). ACTIVATION mode (<paramref name="anyLengthActivationRelax"/> — INVOKE arguments
        // §14.8.2.3.2 rules d/e (:25375-25377), BY CONTENT (:25414 rule c), and RETURNING delivery §14.8.3.3
        // rules 4/5 (:25503-25505)): parameter 1 (the formal / the sending returning item) being ANY LENGTH
        // makes its length "considered to match" the other side's; the OTHER side being ANY LENGTH alone stays
        // a mismatch (rule e / rule 4 — the pairing must be declared on the formal/receiver too).
        if (!anyLengthActivationRelax && formal.IsAnyLength != arg.IsAnyLength)
            return "ANY LENGTH mismatch (the corresponding items shall have the same ANY LENGTH clause — "
                + "ISO §14.8.2/§9.3.8.2 conformance tables)";
        if (anyLengthActivationRelax && arg.IsAnyLength && !formal.IsAnyLength)
            return "the argument/receiver is described with ANY LENGTH — the corresponding formal/sender shall "
                + "be described with ANY LENGTH too (ISO §14.8.2.3.2 rule e / §14.8.3.3 rule 4)";
        // Relaxes ONLY the length compares below; category and JUSTIFIED checks stay (the §14.8.2 table row).
        bool anyLengthFormal = formal.IsAnyLength;

        if (formal.IsGroup)
        {
            if (!(arg.IsGroup || arg.Pic?.Category is PicCategory.Alphanumeric))
                return "a group formal requires a group or alphanumeric argument";
            // ⛔ §14.8.2.2 / §14.8.3.2, THE VARIABLE-LENGTH SENTENCE, BEFORE the capability screens below
            // (kb/Work PB204). "If either the formal parameter or the argument is a variable length group, the
            // formal parameter and the argument shall be compatible, as described in 8.5.1.12" — an ADMISSION
            // subject to a relation, not a prohibition, and §14.9.4.3 SR25 imports it into a Format-2 CALL. The
            // Tier-C arms below used to answer this case, so every such crossing was refused at compile time
            // (COBOLNET1688) however conforming it was; SR12 — "Identifier-2 shall not reference a
            // variable-length group" — is FORMAT 1's rule and reaches neither AS NESTED nor INVOKE.
            if (VariableLengthCompatibility.Mismatch(formal, arg) is { } vlWhy)
                return vlWhy;
            // The ONE Tier-C reason source (kb/Work PB164 — a hand-rolled string here went stale twice as the
            // island shrank; TierCIsland.Reason names the leaf kind the predicate actually tests). The predicate
            // is BoundaryImageCapable, not IsImageCapable: a COMPATIBLE variable-length group crosses through
            // its current-extent codec (kb/Work PB204), so only a group with no boundary image at all — a
            // pointer/object-class leaf, or a variable-length shape outside the current-extent gate — is loud.
            if (arg.IsGroup && !arg.BoundaryImageCapable)
                return TierCIsland.Reason(arg, "argument group");
            if (!formal.BoundaryImageCapable)
                return TierCIsland.Reason(formal, "formal group");
            // §14.8.2.2 rule 1 (BY REFERENCE): the formal may be SMALLER than (a prefix of) the argument —
            // the callee sees the leading formal-width character positions; the tail survives write-back.
            // Override signatures and RETURNING pairs keep strict equality.
            return byRefGroupPrefix
                ? (formal.ImageWidth > arg.ImageWidth
                    ? $"the formal ({formal.ImageWidth} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1 — the formal shall not be larger)"
                    : null)
                : arg.ImageWidth != formal.ImageWidth
                    ? $"character length mismatch (formal {formal.ImageWidth}, argument {arg.ImageWidth})"
                    : null;
        }
        if (formal.Pic is not { } f)
            return "the formal parameter has no resolvable description (PICTURE-less item — a later slice)";
        if (arg.IsGroup)
        {
            if (f.Category is not PicCategory.Alphanumeric)
                return "a group argument requires a group or alphanumeric formal";
            // §8.5.1.12.1: a variable-length group is compatible only with a compatible GROUP ("not equivalent
            // to an alphanumeric data item"), so an ELEMENTARY formal — which §14.8.2.2 rule 1 admits for a
            // fixed-length group — is a mismatch, and it is that sentence that says so, not the Tier-C island.
            if (VariableLengthCompatibility.Mismatch(formal, arg) is { } vlWhy) return vlWhy;
            if (!arg.BoundaryImageCapable) return "the argument group has no character image (Tier-C)";
            if (anyLengthFormal) return null;   // §14.8.2.3.2 rule d — the formal's length matches the argument's
            return byRefGroupPrefix
                ? (f.Length > arg.ImageWidth
                    ? $"the formal ({f.Length} character positions) exceeds the argument "
                      + $"({arg.ImageWidth}) (ISO §14.8.2.2 rule 1)"
                    : null)
                : arg.ImageWidth != f.Length
                    ? $"character length mismatch (formal {f.Length}, argument {arg.ImageWidth})"
                    : null;
        }
        if (arg.Pic is not { } a)
            return "the argument has no resolvable description (PICTURE-less item — a later slice)";
        if (f.Category != a.Category)
            return $"category mismatch (formal {f.Category}, argument {a.Category})";
        switch (f.Category)
        {
            case PicCategory.ObjectReference:
                return string.Equals(f.ObjectClassName, a.ObjectClassName, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : $"declared class mismatch (formal '{f.ObjectClassName ?? "universal"}', argument "
                      + $"'{a.ObjectClassName ?? "universal"}')";
            case PicCategory.Numeric:
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2.3.2 rule 2 "
                        + "requires the same USAGE clause BY REFERENCE)";
                if (f.SignKind != a.SignKind)
                    return $"SIGN clause mismatch (formal {f.SignKind}, argument {a.SignKind} — "
                        + "§14.8.2.3.2 rule 2: the SIGN clauses shall be the same)";
                if (formal.BlankWhenZero != arg.BlankWhenZero)
                    return "BLANK WHEN ZERO mismatch (§14.8.2.3.2 rule 2)";
                return f.Digits != a.Digits || f.Scale != a.Scale || f.Signed != a.Signed
                    ? $"numeric description mismatch (formal {(f.Signed ? "S" : "")}9({f.Digits}) scale "
                      + $"{f.Scale}, argument {(a.Signed ? "S" : "")}9({a.Digits}) scale {a.Scale})"
                    : null;
            case PicCategory.Alphanumeric:
                if (formal.Justified != arg.Justified)
                    return "JUSTIFIED mismatch (§14.8.2.3.2 rule 2)";
                // "The same PICTURE clause" is not implied by equal LENGTH: an alphanumeric-EDITED picture
                // (§13.18.40 simple insertion — the B / 0 / / positions) and a plain X picture of the same
                // character count are different PICTURE clauses, and rule 2 names the clause, not the size.
                if (!string.Equals(f.EditMask, a.EditMask, StringComparison.Ordinal))
                    return $"PICTURE mismatch (formal '{f.EditMask ?? "X(" + f.Length + ")"}', argument "
                        + $"'{a.EditMask ?? "X(" + a.Length + ")"}' — §14.8.2.3.2 rule 2 requires the same "
                        + "PICTURE clause)";
                // ANY LENGTH: the length is considered to match (§14.8.2.3.2 rule d / §14.8.3.3 rule 5 in
                // activation mode; both-sides-varying in pair mode — the top-of-function match rule).
                return !anyLengthFormal && f.Length != a.Length
                    ? $"length mismatch (formal X({f.Length}), argument X({a.Length}))"
                    : null;
            // ── The remaining PICTURE categories (ISO §14.8.2.3.2 rule 2, the SAME clause list the arms above
            // spell out per category). ⛔ THESE THREE USED TO FALL INTO A `default:` THAT ANSWERED "formal
            // category {c} is not yet carried across INVOKE", which made a category-boolean, category-national
            // or numeric-edited FORMAL PARAMETER impossible in every passing mode — BY REFERENCE, BY CONTENT
            // and bare alike (fix-queue PB46). Nothing was missing: all three are string-carried
            // (OoClassTable.StringCarried), so the marshaling arms already carried them; only this screen said
            // no. The standard contemplates them explicitly — §14.8.2.3.2's own lettered exceptions b and c
            // pair a BIT GROUP with an elementary bit item and a NATIONAL GROUP with an elementary national
            // item of the same position count.
            case PicCategory.NumericEdited:
            case PicCategory.National:
            case PicCategory.Boolean:
                if (formal.Justified != arg.Justified)
                    return "JUSTIFIED mismatch (§14.8.2.3.2 rule 2)";
                if (formal.BlankWhenZero != arg.BlankWhenZero)
                    return "BLANK WHEN ZERO mismatch (§14.8.2.3.2 rule 2)";
                // USAGE is a rule-2 clause in its own right, and for these categories it is NOT implied by the
                // category: a boolean item is USAGE DISPLAY or USAGE BIT (§13.18.60.3 SR5) and both map to the
                // same D-B1 character storage, so only this compare keeps the declarations identical.
                if (f.Usage != a.Usage)
                    return $"USAGE mismatch (formal {f.Usage}, argument {a.Usage} — §14.8.2.3.2 rule 2 "
                        + "requires the same USAGE clause BY REFERENCE)";
                if (f.SignKind != a.SignKind)
                    return $"SIGN clause mismatch (formal {f.SignKind}, argument {a.SignKind} — "
                        + "§14.8.2.3.2 rule 2: the SIGN clauses shall be the same)";
                // An EDITED picture's identity is its editing character-string, which equal length does not imply.
                if (!string.Equals(f.EditMask, a.EditMask, StringComparison.Ordinal))
                    return $"PICTURE mismatch (formal '{f.EditMask}', argument '{a.EditMask}' — "
                        + "§14.8.2.3.2 rule 2 requires the same PICTURE clause)";
                // A FORMAT-2 (LOCALE) picture's identity (§14.8.2.3.2 rule 2 / §8.5.3.1 rule 2 — PB64 T6): both
                // masks are null, so the compare above passes vacuously; the rule requires the same SIZE phrase
                // (the Length compare below carries it — Length = integer-1), the same character-string, and the
                // same locale — "both specify the LOCALE phrase without a locale-name or both … with the same
                // external identification" (the ONE identity, LocaleSymbol.SameLocaleAs over L1 normalization).
                if ((f.LocaleEdit is not null) != (a.LocaleEdit is not null))
                    return "PICTURE mismatch (only one of the pair is a format 2 LOCALE picture — "
                        + "§14.8.2.3.2 rule 2 requires the same PICTURE clause)";
                if (f.LocaleEdit is { } fle && a.LocaleEdit is { } ale)
                {
                    if (!string.Equals(fle.Picture, ale.Picture, StringComparison.Ordinal))
                        return $"PICTURE mismatch (formal '{fle.Picture}', argument '{ale.Picture}' — "
                            + "§14.8.2.3.2 rule 2 requires the same PICTURE clause)";
                    bool same = fle.Locale.IsCurrent == ale.Locale.IsCurrent
                        && (fle.Locale.IsCurrent || fle.Locale.Named!.SameLocaleAs(ale.Locale.Named!));
                    if (!same)
                        return $"LOCALE mismatch (formal {fle.Locale}, argument {ale.Locale} — §14.8.2.3.2 rule 2: "
                            + "both shall specify the LOCALE phrase without a locale-name or with the same "
                            + "external identification)";
                }
                return !anyLengthFormal && f.Length != a.Length
                    ? $"length mismatch (formal {f.Category} ({f.Length}), argument {a.Category} ({a.Length}))"
                    : null;
            // ── Class pointer (ISO §14.8.2.3.2, the class-pointer paragraph): "the corresponding formal
            // parameter or argument shall be of class pointer and the corresponding items shall be of the same
            // category" — which the f.Category != a.Category compare above has already proven. A PICTURE-less
            // pointer has no length, USAGE variant or JUSTIFIED clause left to differ in. The RESTRICTED forms
            // ("if either is a restricted pointer, both shall be restricted and of the same type") are not
            // modeled at all — PictureAnalyzer stages the TO-prototype/TO-type-name declarations loud
            // (COBOLNET0899), so an unrestricted pair is the only shape that reaches here.
            case PicCategory.Pointer:
            case PicCategory.ProgramPointer:
                return null;
            default:
                // Unreachable by construction: PicCategory.Group never reaches here (formal.IsGroup returned
                // above, and a group item has no PicInfo), and every other member has an arm. Pinned by
                // OoConformanceCategoryDriftTests — a NEW category must gain an arm, not fall in here, because
                // this arm REJECTS LEGAL SOURCE for whatever lands in it.
                return $"formal category {f.Category} has no §14.8.2.3.2 conformance rule";
        }
    }

    /// <summary>The §14.8.3.3-rule-1 / SET-SR12a2 WIDENING direction for object-reference assignment pairs
    /// (INVOKE RETURNING delivery, BY CONTENT object-reference arguments, covariant override RETURNING —
    /// specs/ISO_COBOL.md:25456-25458, :31340): a UNIVERSAL receiver accepts any object reference; a typed
    /// receiver accepts the SAME class or a SUBCLASS. Distinct from <see cref="DescriptionMismatch"/> strict
    /// identity, which stays correct for BY REFERENCE (§14.8.2.3.2) and invariant override formals.
    /// Null-tolerant on <paramref name="table"/> (no class table in the group ⇒ no OO checking — preserving
    /// the former <c>OoClasses?.</c> call shape).</summary>
    public static string? ObjectRefWideningMismatch(OoClassTable? table, PicInfo sender, PicInfo receiver)
    {
        if (table is null) return null;
        if (receiver.ObjectClassName is not { } recvName) return null;   // universal receiver (SET SR8)
        if (sender.ObjectClassName is not { } sendName)
            return "a UNIVERSAL object reference does not conform to a typed receiver "
                + "(ISO SET SR12 — the sender shall be of the receiver class or a subclass)";
        // An INTERFACE-typed receiver accepts a class that IMPLEMENTS it — through the §11.8.4 GR2 closure
        // (SET SR10/§9.3.8.2 interface conformance) — or an interface that INHERITS it.
        if (table.FindInterface(recvName) is { } recvIface)
        {
            if (table.Find(sendName) is { } sc)
                return table.ImplementsClosure(sc, factory: false).Contains(recvIface)
                    ? null
                    : $"class {sendName} does not implement interface {recvName} (ISO §9.3.8.2/SET SR10)";
            if (table.FindInterface(sendName) is { } si)
                return si == recvIface || si.Inherits.Contains(recvIface)
                       || ImpliedBy(si, recvIface)
                    ? null
                    : $"interface {sendName} does not inherit interface {recvName}";
            return $"unresolvable sender '{sendName}'";

            static bool ImpliedBy(OoInterfaceSymbol from, OoInterfaceSymbol target)
            {
                var seen = new HashSet<OoInterfaceSymbol>();
                var stack = new Stack<OoInterfaceSymbol>([from]);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    if (!seen.Add(cur)) continue;
                    if (cur == target) return true;
                    foreach (var b in cur.Inherits) stack.Push(b);
                }
                return false;
            }
        }
        var sendCls = table.Find(sendName);
        var recvCls = table.Find(recvName);
        if (sendCls is null || recvCls is null)
            return $"unresolvable class in the pair (sender {sendName} to receiver {recvName})";
        return sendCls.ConformsTo(recvCls)
            ? null
            : $"class {sendName} is not {recvName} or one of its subclasses (ISO SET SR12a2 via §14.8.3.3 rule 1)";
    }
}
