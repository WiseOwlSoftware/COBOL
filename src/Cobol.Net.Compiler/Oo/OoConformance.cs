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
                        if (ObjectRefAssignmentMismatch(table, rp, brp, activeClassSenderAdmitted: false) is { } werr)
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
                    var impl = factory ? cls.FindFactoryMethod(proto.ExternalizedName) : cls.FindMethod(proto.ExternalizedName);   // the roster key (PB303)
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
                            if (ObjectRefAssignmentMismatch(table, rp, prp, activeClassSenderAdmitted: false) is { } werr)
                                edition.Error("COBOLNET0841",
                                    $"class '{cls.Name}', method '{impl.Name}': RETURNING: {werr} "
                                    + "(ISO §9.3.8.2.3 rules 5a/5c2)");
                            else if (!(rp.ObjectRef ?? ObjectRefDescriptor.Universal)
                                         .SameDescriptionAs(prp.ObjectRef ?? ObjectRefDescriptor.Universal))
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
            // The WHOLE §13.18.60.2 description, not just the name (kb/Work PB389): two items described
            // `OBJECT REFERENCE C` and `OBJECT REFERENCE FACTORY OF C ONLY` are different descriptions and
            // §9.3.8.2.3 rule 2 c) makes them non-conformant, so their keys must differ.
            PicCategory.ObjectReference => "O:" + (p.ObjectRef ?? ObjectRefDescriptor.Universal).SignatureKey,
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
                // §9.3.8.2.3 rule 2 — the INVARIANT direction, and it names all four axes: a) universal ⇔
                // universal, b) the SAME interface-name, c) the same object-class-name "and the presence or
                // absence of the FACTORY and ONLY phrases is the same in both interfaces", d) ACTIVE-CLASS with
                // the same FACTORY presence. Before kb/Work PB389 this compared the class NAME alone, so a
                // FACTORY OF or ONLY difference passed as identical.
                var fd = f.ObjectRef ?? ObjectRefDescriptor.Universal;
                var ad = a.ObjectRef ?? ObjectRefDescriptor.Universal;
                return fd.SameDescriptionAs(ad) ? null
                    : $"object-reference description mismatch (formal {fd.Spelled}, argument {ad.Spelled} — "
                      + "§9.3.8.2.3 rule 2 requires the same kind, name, FACTORY presence and ONLY presence)";
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
            // ── Class pointer (ISO §14.8.2.3.2, the class-pointer paragraph): "If either the argument or the
            // formal parameter is of class pointer, the corresponding formal parameter or argument shall be of
            // class pointer and the corresponding items shall be of the same category" — which the
            // f.Category != a.Category compare above has already proven. A PICTURE-less pointer has no length,
            // USAGE variant or JUSTIFIED clause left to differ in. The SECOND sentence — "If either is a
            // restricted pointer, both shall be restricted and of the same type" — is enforced HERE, over BOTH
            // restriction models, because both are now declarable: `POINTER TO type-name-1` (§13.18.60.4 GR23,
            // kb/Work PB153) carries RestrictedTypeName, and `PROGRAM-POINTER TO program-prototype-name-1` /
            // `FUNCTION-POINTER TO function-prototype-name-1` (GR25/GR26, kb/Work PB452 + PB817) carry
            // RestrictedPrototypeName. Until they were declarable this was dead text under a staged-loud
            // declaration, and the comment that stood here said so; a rule whose subject becomes declarable and
            // whose screen does not follow is a silent under-rejection (feedback_scan_all_similar).
            case PicCategory.Pointer:
            case PicCategory.ProgramPointer:
            case PicCategory.FunctionPointer:
                if (!string.Equals(f.RestrictedTypeName, a.RestrictedTypeName, StringComparison.OrdinalIgnoreCase))
                    return $"restricted data-pointer mismatch (formal {PointerRestrictionText(f.RestrictedTypeName, "type")}, "
                        + $"argument {PointerRestrictionText(a.RestrictedTypeName, "type")} — §14.8.2.3.2: if either is a "
                        + "restricted pointer, both shall be restricted and of the same type)";
                if (!string.Equals(f.RestrictedPrototypeName, a.RestrictedPrototypeName, StringComparison.OrdinalIgnoreCase))
                    return $"restricted pointer mismatch (formal {PointerRestrictionText(f.RestrictedPrototypeName, "prototype")}, "
                        + $"argument {PointerRestrictionText(a.RestrictedPrototypeName, "prototype")} — §14.8.2.3.2: if either "
                        + "is a restricted pointer, both shall be restricted and of the same type)";
                return null;
            default:
                // Unreachable by construction: PicCategory.Group never reaches here (formal.IsGroup returned
                // above, and a group item has no PicInfo), and every other member has an arm. Pinned by
                // OoConformanceCategoryDriftTests — a NEW category must gain an arm, not fall in here, because
                // this arm REJECTS LEGAL SOURCE for whatever lands in it.
                return $"formal category {f.Category} has no §14.8.2.3.2 conformance rule";
        }
    }

    /// <summary>How one side of the §14.8.2.3.2 restricted-pointer compare reads in a diagnostic: the
    /// restriction operand, or "unrestricted" when the side carries none. The rule's failure mode is
    /// restricted-vs-unrestricted as often as it is two different names, so the message has to be able to say
    /// both.</summary>
    private static string PointerRestrictionText(string? restriction, string kind) =>
        restriction is null ? "unrestricted" : $"restricted to {kind} '{restriction}'";

    // ══ ISO §14.8.2.3.3 — ELEMENTARY ITEMS PASSED BY CONTENT OR BY VALUE ═════════════════════════════════
    // ⛔ THE ONE HOME FOR THE RULE, for EVERY activation form that imports §14.8.2 (kb/Work PB165). It used
    // to live as `OoBinder.OoContentMismatch`, private to INVOKE — so the Format-2 CALL lane, which
    // §14.9.4.3 SR25 puts under the very same clause, had NO by-content screen at all and
    // `CobolArgAdapt`'s converting views silently adapted a non-conforming pair instead (measured on the
    // pre-fix tree: `CALL "S" AS NESTED USING BY CONTENT A` with `A PIC X(4)` and a `PIC 9(4)` formal
    // printed `LA=0000`). One rule written in one place is what keeps INVOKE and CALL from drifting, and
    // it is the same discipline `DescriptionMismatch` above already carries for §14.8.2.3.2.
    //
    // The clause selects the rule by the FORMAL's shape (rule 2 — the regime for a NESTED call, a program
    // prototype, a method or a function):
    //   a) numeric formal        → "the same as for a COMPUTE statement"
    //   b) index-data-item formal→ "the same as for a SET statement"
    //   c) ANY LENGTH formal     → "its length is considered to match the length of the corresponding argument"
    //   d) otherwise             → "the same as for a MOVE statement"  (⇒ §14.9.25.3 Table 16)
    // and, ahead of those, the class-pointer / object-reference paragraph: "the conformance rules shall be
    // the same as if a SET statement were performed in the activating runtime element with the argument as
    // the sending operand and the corresponding formal parameter as the receiving operand."
    // §14.8.2.2 rule 2 states the GROUP twin in the same words ("the same as for a MOVE statement"), which
    // is why one entry point answers for a group formal too.

    /// <summary>ISO §14.8.2.3.3 — the BY CONTENT / BY VALUE conformance rules for an IDENTIFIER argument,
    /// per formal category: COMPUTE for numeric (any fixed-point numeric argument; float formals require the
    /// identical float usage — the cross-float CONTENT conversion is a documented later refinement), SET for
    /// object references (widening — the argument's class shall be the receiver's class or a subclass), MOVE
    /// otherwise (§14.9.25.3 Table 16). Null when conformant.</summary>
    public static string? ContentMismatch(OoClassTable? classes, DataItem formal, Place argPlace)
    {
        DataItem arg = argPlace.Item;
        // §8.4.3.3.4 GR2/GR6 (fix-queue PB72): a REF-MOD argument crosses as the ELEMENTARY plain-alphanumeric
        // (or GR6b/c national) view it creates, never as the inner item — whose numeric category would satisfy
        // the COMPUTE arm below for a slice that is class alphanumeric, and whose finer alphabetic/edited flags
        // refused legal Table-16 crossings. The view's category comes from the ONE GR6 reader
        // (RefModPlace.CategoryOf); a view is elementary by definition, never a group.
        PicCategory? argCat = argPlace is RefModPlace rmp ? rmp.Category : arg.Pic?.Category;
        bool argIsGroup = argPlace is not RefModPlace && arg.IsGroup;

        if (formal.IsGroup || formal.Pic?.Category is PicCategory.Alphanumeric)
        {
            if (argIsGroup)
                // ⛔ §14.8.2.2's VARIABLE-LENGTH SENTENCE FIRST, exactly as the BY REFERENCE sibling
                // (DescriptionMismatch above) applies it — "If either the formal parameter or the argument is a
                // variable length group, the formal parameter and the argument shall be compatible, as
                // described in 8.5.1.12" — an ADMISSION subject to a relation, not a prohibition, and
                // §14.8.2.3.3 rule 2d routes a BY CONTENT group crossing through the same MOVE rules.
                // ⚠ This line used to ask `IsImageCapable`, which is false for a variable-length group, so it
                // refused legal source — the residue kb/Work PB818 filed against PB204's rim. PB165 had to
                // repair it here rather than leave it: the extraction made this the rule's ONE home and put the
                // Format-2 CALL lane behind it, which turned PB818's latent INVOKE-only defect into a hard
                // failure of the landed golden conformance:2023/pb204_vlg_boundary. The predicate is PB204's
                // own, unchanged.
                return VariableLengthCompatibility.Mismatch(formal, arg)
                    ?? (arg.BoundaryImageCapable ? null : TierCIsland.Reason(arg, "argument group"));
            return argCat switch
            {
                PicCategory.Alphanumeric or PicCategory.NumericEdited => null,
                // Table 16: boolean→alphanumeric is a conforming MOVE; national→alphanumeric is NOT
                // (§14.9.25.3 — DISPLAY-OF is the sanctioned narrowing), so National keeps the mismatch arm.
                PicCategory.Boolean => null,
                PicCategory.Numeric when arg.Pic is { IsFloat: false, Scale: 0 } => null,   // MOVE integer→alnum
                _ => "no conforming MOVE rule applies (ISO §14.8.2.2 rule 2 / §14.9.25)",
            };
        }
        var f = formal.Pic!;
        return f.Category switch
        {
            // The numeric/float/object arms key on the VIEW category — a ref-mod view is never numeric or an
            // object reference (GR6c), so their arg.Pic detail reads are only reachable for a whole-item arg.
            // ⛔ RULE 2a IS "the same as for a COMPUTE statement", AND A COMPUTE TAKES ANY NUMERIC SENDER —
            // fixed-point or floating-point, either direction. The float restrictions that used to sit here
            // ("a float formal takes the identical float usage BY CONTENT") are an INVOKE CARRIER limitation,
            // not a conformance rule, and moving them into the shared rule REJECTED LEGAL SOURCE the moment
            // the CALL lane started asking: kb/Work PB238 landed the float crossing for a Format-2 CALL
            // deliberately (§14.2.3 GR10's "COMPUTE statement without the ROUNDED phrase" makes
            // `01 F FLOAT-LONG VALUE 1.5` reach a `PIC S9(3)V99` BY VALUE formal as 001.50), and the landed
            // golden conformance:2023/pb238_call_format2_operands proves it. The carrier residue stays where
            // the carrier is — OoBinder screens it for INVOKE alone, next to its other marshalling limits.
            PicCategory.Numeric =>
                argIsGroup ? "a group argument does not conform to a numeric formal (§14.8.2.3.3)"
                : argCat is PicCategory.Numeric ? null
                : "COMPUTE-rule conformance needs a numeric argument (ISO §14.8.2.3.3 rule 2a)",
            PicCategory.ObjectReference =>
                argCat is PicCategory.ObjectReference && arg.Pic is { } ap
                    ? ObjectRefAssignmentMismatch(classes, ap, f)
                    : "an object-reference formal takes an object-reference argument (SET rules, §14.8.2.3.3)",
            // ⭐ BOOLEAN / NATIONAL / NUMERIC-EDITED FORMALS ASK TABLE 16, NOT STRICT IDENTITY (fix-queue PB53).
            // This arm used to call DescriptionMismatch — which is §14.8.2.3.2, the BY **REFERENCE** rule —
            // described in its own comment as a "conservative strict gate". It was not conservative, it was the
            // WRONG CLAUSE: §14.8.2.3.3 rule 2d says a BY CONTENT crossing whose formal is not numeric, not an
            // index item and not ANY LENGTH conforms "as for a MOVE statement", i.e. by §14.9.25.3 Table 16.
            // Identity is far narrower, so three pairings the standard admits were refused with a "category
            // mismatch" naming a rule that does not govern the crossing:
            //     boolean → national · alphanumeric → boolean · national → boolean
            // ⚠ ANY LENGTH keeps its own answer FIRST: §14.8.2.3.3 rule 2c makes such a formal's length
            // "considered to match", which is a statement about LENGTH and leaves the category pair to 2d.
            _ => formal.IsAnyLength && !arg.IsAnyLength ? null
                : MoveTable16.Refusal(Table16Operand.Of(argPlace), Table16Operand.Of(formal)),
        };
    }

    /// <summary>ISO §14.8.2.3.3 rule 2a for an ARITHMETIC-EXPRESSION argument: "the conformance rules are the
    /// same as for a COMPUTE statement", whose receiving operand is category numeric — so a non-numeric formal
    /// has no conforming rule. The float sub-arm is the same documented refinement
    /// <see cref="ContentMismatch"/>'s identifier lane defers (the fixed-point→float CONTENT conversion).
    /// Null when conformant.</summary>
    public static string? ContentArithmeticMismatch(DataItem formal) =>
        formal.IsGroup || formal.Pic is not { Category: PicCategory.Numeric }
            ? "§14.8.2.3.3 rule 2a transfers an expression by the COMPUTE rules, which requires a "
              + "category-numeric formal parameter"
            : formal.Pic is { IsFloat: true }
            ? "the fixed-point→float CONTENT conversion is the same documented refinement the identifier arm "
              + "defers (ISO §14.8.2.3.3)"
            : null;

    /// <summary>ISO §14.8.2.3.3 rule 2d for a BOOLEAN-EXPRESSION or boolean-literal argument: the MOVE rules,
    /// i.e. §14.9.25.3 Table 16's BOOLEAN row, which admits a boolean or alphanumeric receiver and refuses
    /// alphabetic, numeric and numeric-edited ones. Null when conformant.
    /// <para>⚠ TABLE 16 ALSO ADMITS A NATIONAL RECEIVER, AND THIS RULE REFUSES IT ON PURPOSE — the IDENTIFIER
    /// arm refuses the same pairing through <see cref="ContentMismatch"/>'s conservative strict gate, and two
    /// arms of one rule disagreeing is worse than one named residue. Both are recorded together.</para></summary>
    public static string? ContentBooleanMismatch(DataItem formal) =>
        formal.IsGroup || formal.Pic is not { Category: PicCategory.Alphanumeric or PicCategory.Boolean }
            || formal.Pic is { Category: PicCategory.Alphanumeric, IsAlphabetic: true }
            ? "§14.8.2.3.3 rule 2d transfers it by the MOVE rules, and §14.9.25.3 Table 16 admits a boolean "
              + "sending operand only to a boolean or alphanumeric receiver"
            : null;

    /// <summary>The three sender categories a bound NONNUMERIC literal can actually be — §8.3.3.2 alphanumeric
    /// (including the hexadecimal format), §8.3.3.5 national and §8.3.3.4 boolean. ⛔ The bound tree renders all
    /// three as <c>BoundStringLiteral</c>, which carries the VALUE and not the CATEGORY, so a consumer holding
    /// only the bound node cannot tell them apart (kb/Work PB165 — measured: screening every such literal as
    /// alphanumeric refused `CALL … USING BY CONTENT N"AB" BY CONTENT B"01"` against `PIC N(2)` / `PIC 1(2)`
    /// formals, which are conforming Table-16 crossings). Until the literal's category rides the bound node,
    /// the conformance rule below refuses only what NO reading of rule 2d can admit.</summary>
    private static readonly Table16Operand[] NonNumericLiteralSenders =
    [
        new(PicCategory.Alphanumeric), new(PicCategory.National), new(PicCategory.Boolean),
    ];

    /// <summary>ISO §14.8.2.3.3 rule 2d for a NONNUMERIC literal argument — "the conformance rules are the same
    /// as for a MOVE statement", i.e. §14.9.25.3 Table 16 with the literal as the sending operand. Null when
    /// conformant. A group formal is §14.8.2.2 rule 2's MOVE, admitted by the GR4 conversion-free copy.
    /// <para>The verdict is Table 16's, asked once per sender category the literal could be
    /// (<see cref="NonNumericLiteralSenders"/>) — conformant when ANY of them is admitted. That is deliberately
    /// weaker than the rule the standard states for a KNOWN category, and it is the honest strength for a bound
    /// node that has lost the category: it still refuses the shape this screen exists for (a nonnumeric literal
    /// at a numeric or numeric-edited formal, which no sender category rescues) and it never rejects legal
    /// source. Narrowing it to the exact category is what carrying the category on the literal node buys.</para>
    /// </summary>
    public static string? ContentAlphanumericLiteralMismatch(DataItem formal)
    {
        if (formal.IsGroup) return null;
        var receiver = Table16Operand.Of(formal);
        return NonNumericLiteralSenders.Any(s => MoveTable16.Refusal(s, receiver) is null)
            ? null
            : "a nonnumeric literal argument has no conforming MOVE into this formal parameter under any "
              + "literal category (ISO §14.8.2.3.3 rule 2d / §14.9.25.3 Table 16)";
    }

    /// <summary>ISO §14.8.2.3.3 for a NUMERIC literal argument: rule 2a (COMPUTE) into a fixed-point numeric
    /// formal, and rule 2d's MOVE rules put an UNSIGNED INTEGER literal into an alphanumeric receiver as its
    /// digit characters (§14.9.25). Null when conformant.</summary>
    public static string? ContentNumericLiteralMismatch(DataItem formal, string raw) =>
        formal.Pic is { Category: PicCategory.Numeric, IsFloat: false }
        || (!formal.IsGroup && formal.Pic?.Category is PicCategory.Alphanumeric
            && !raw.Contains('.') && !raw.StartsWith('-') && !raw.StartsWith('+'))
            ? null
            : $"numeric literal argument {raw} does not conform to the formal parameter "
              + "(ISO §14.8.2.3.3 — no conforming COMPUTE/MOVE rule applies)";

    /// <summary>
    /// ⛔ THE ONE SENDER-INTO-RECEIVER TABLE FOR OBJECT REFERENCES — ISO §14.9.39.3 SR10 / SR12 / SR14 (SET
    /// format 5), reached also by §14.8.3.3 rule 1 (RETURNING delivery follows the SET rules), §14.8.2.3.3 (BY
    /// CONTENT object-reference arguments) and §9.3.8.2.3 rule 5 (covariant override/implements RETURNING).
    /// The receiver's DESCRIPTION selects the rule; the sender's description answers it.
    /// <list type="bullet">
    ///   <item><b>universal receiver</b> — SR8: "identifier-3 shall be any item of class object that is
    ///     permitted as a receiving item"; GR22 b) makes its content "a reference to any object". Nothing to
    ///     check.</item>
    ///   <item><b>interface-name receiver</b> — SR10: a) an interface identifying int-1 or inheriting from it;
    ///     b) an object-class-name whose b)1. FACTORY object (FACTORY written) or b)2. instance objects
    ///     implement int-1; c) an ACTIVE-CLASS reference, same two legs over the CONTAINING class.</item>
    ///   <item><b>object-class-name receiver</b> — SR12: a) an object-class-name sender, a)1. ONLY ⇒ the sender
    ///     is ONLY and names the SAME class, a)2. no ONLY ⇒ the same class or a subclass, a)3. FACTORY presence
    ///     equal; b) an ACTIVE-CLASS sender, b)1. the receiver is not ONLY, b)2. the sender's containing class
    ///     is the receiver's class or a subclass, b)3. FACTORY presence equal.</item>
    ///   <item><b>ACTIVE-CLASS receiver</b> — SR14: a) an ACTIVE-CLASS sender "where the presence or absence of
    ///     the FACTORY phrase is the same as in the data item referenced by identifier-3". (The rule as printed
    ///     constrains only the FACTORY axis — the containing class is necessarily the same one, since both
    ///     operands are written inside the same class definition, §13.18.60.3 SR16.)</item>
    /// </list>
    /// <para>The SELF and NULL senders of SR10 d)/e), SR12 c)/d) and SR14 b)/c) are not DESCRIPTIONS and are
    /// adjudicated at the SET site (<c>OoBinder.OoBindSetObjectRef</c>), which is also where SR11/SR13's
    /// class-NAME sender lives.</para>
    /// <para><paramref name="activeClassSenderAdmitted"/> is the ONE place the two rule sets differ.
    /// §14.9.39.3 SR10 c) and SR12 b) admit an ACTIVE-CLASS sender into an interface-typed or class-typed
    /// receiver; §9.3.8.2.3 rule 5 b)/c), the interface-conformance twin, enumerates no ACTIVE-CLASS leg — its
    /// 5 d) pairs ACTIVE-CLASS only with ACTIVE-CLASS. The interface-conformance callers therefore pass false
    /// and get the printed rule 5, rather than a shared function quietly widening one of the two.</para>
    /// <para>Null-tolerant on <paramref name="table"/> (no class table in the group ⇒ no OO checking —
    /// preserving the former <c>OoClasses?.</c> call shape). Returns null when the pair conforms, else the
    /// clause it violated.</para>
    /// <para>kb/Work PB389 renamed this from <c>ObjectRefWideningMismatch</c>: "widening" described only
    /// SR12 a)2., which was the single rule the scalar descriptor could express.</para>
    /// </summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable? table, PicInfo sender, PicInfo receiver,
        bool activeClassSenderAdmitted = true)
        => table is null ? null
            : ObjectRefAssignmentMismatch(table, sender.ObjectRef ?? ObjectRefDescriptor.Universal,
                receiver.ObjectRef ?? ObjectRefDescriptor.Universal, activeClassSenderAdmitted);

    /// <summary>The descriptor-level form of <see cref="ObjectRefAssignmentMismatch(OoClassTable?, PicInfo,
    /// PicInfo, bool)"/> — the SET format-5 table itself.</summary>
    public static string? ObjectRefAssignmentMismatch(OoClassTable table, ObjectRefDescriptor send,
        ObjectRefDescriptor recv, bool activeClassSenderAdmitted = true)
    {
        switch (recv.Kind)
        {
            // ── SR8 / GR22 b): a universal receiver constrains nothing. ──────────────────────────────────
            case ObjectRefKind.Universal:
                return null;

            // ── SR10: the receiver is described with an interface-name that identifies int-1. ────────────
            case ObjectRefKind.Interface:
            {
                if (table.FindInterface(recv.Name!) is not { } int1)
                    return $"unresolvable interface '{recv.Name}' in the receiver's description";
                switch (send.Kind)
                {
                    // a) an object reference described with an interface-name identifying int-1 or an
                    //    interface inheriting from int-1.
                    case ObjectRefKind.Interface:
                        if (table.FindInterface(send.Name!) is not { } si)
                            return $"unresolvable interface '{send.Name}' in the sending description";
                        return si == int1 || InheritsClosure(si).Contains(int1) ? null
                            : $"interface {send.Name} neither identifies nor inherits from interface "
                              + $"{recv.Name} (ISO §14.9.39.3 SR10 a))";
                    // b) an object reference described with an object-class-name: b)1. FACTORY written ⇒ the
                    //    FACTORY object of that class implements int-1; b)2. otherwise ⇒ its instance objects do.
                    case ObjectRefKind.ObjectClass:
                    {
                        if (table.Find(send.Name!) is not { } sc)
                            return $"unresolvable class '{send.Name}' in the sending description";
                        return table.ImplementsClosure(sc, send.Factory).Contains(int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of class {send.Name} "
                              + $"do{(send.Factory ? "es" : "")} not implement interface {recv.Name} "
                              + $"(ISO §14.9.39.3 SR10 b){(send.Factory ? "1" : "2")}.)";
                    }
                    // c) an object reference described with an ACTIVE-CLASS phrase — the same two legs, asked
                    //    of the class CONTAINING the sending data item.
                    case ObjectRefKind.ActiveClass:
                    {
                        if (!activeClassSenderAdmitted)
                            return $"an ACTIVE-CLASS object reference does not conform to a receiver described "
                                   + $"with interface-name '{recv.Name}' (ISO §9.3.8.2.3 rule 5 b) — its "
                                   + "alternatives are an interface-name and an object-class-name)";
                        if (table.Find(send.Name!) is not { } ac)
                            return $"unresolvable containing class '{send.Name}' of the ACTIVE-CLASS sender";
                        return table.ImplementsClosure(ac, send.Factory).Contains(int1) ? null
                            : $"the {(send.Factory ? "factory object" : "objects")} of the class containing the "
                              + $"ACTIVE-CLASS sender ({send.Name}) do{(send.Factory ? "es" : "")} not implement "
                              + $"interface {recv.Name} (ISO §14.9.39.3 SR10 c){(send.Factory ? "1" : "2")}.)";
                    }
                    default:
                        return "a UNIVERSAL object reference does not conform to a receiver described with "
                               + $"interface-name '{recv.Name}' (ISO §14.9.39.3 SR10 — its closed list of "
                               + "senders does not include a universal reference)";
                }
            }

            // ── SR12: the receiver is described with an object-class-name. ───────────────────────────────
            case ObjectRefKind.ObjectClass:
                switch (send.Kind)
                {
                    // a) an object-class-name sender.
                    case ObjectRefKind.ObjectClass:
                    {
                        // a)3. — the FACTORY axis is INVARIANT, checked first because it is independent of the
                        // class relation and its violation is the one the name comparison would hide.
                        if (send.Factory != recv.Factory)
                            return $"the FACTORY phrase is {(recv.Factory ? "" : "not ")}specified in the "
                                   + $"receiver's description and {(send.Factory ? "" : "not ")}in the sender's "
                                   + "— its presence or absence shall be the same (ISO §14.9.39.3 SR12 a)3.)";
                        // a)1. — an ONLY receiver takes an ONLY sender naming the SAME class, exactly.
                        if (recv.Only)
                            return send.Only && string.Equals(send.Name, recv.Name, StringComparison.OrdinalIgnoreCase)
                                ? null
                                : $"the receiver is described with the ONLY phrase, so the sender shall also be "
                                  + $"described ONLY and with the same object-class-name '{recv.Name}' (the "
                                  + $"sender is {send.Spelled}) (ISO §14.9.39.3 SR12 a)1.)";
                        // a)2. — otherwise the same class or a subclass.
                        var sc2 = table.Find(send.Name!);
                        var rc2 = table.Find(recv.Name!);
                        if (sc2 is null || rc2 is null)
                            return $"unresolvable class in the pair (sender {send.Name} to receiver {recv.Name})";
                        return sc2.ConformsTo(rc2) ? null
                            : $"class {send.Name} is not {recv.Name} or one of its subclasses "
                              + "(ISO §14.9.39.3 SR12 a)2.)";
                    }
                    // b) an ACTIVE-CLASS sender.
                    case ObjectRefKind.ActiveClass:
                    {
                        if (!activeClassSenderAdmitted)
                            return $"an ACTIVE-CLASS object reference does not conform to a receiver described "
                                   + $"with object-class-name '{recv.Name}' (ISO §9.3.8.2.3 rule 5 c) — its "
                                   + "subject is an object-class-name description)";
                        if (send.Factory != recv.Factory)
                            return $"the FACTORY phrase is {(recv.Factory ? "" : "not ")}specified in the "
                                   + $"receiver's description and {(send.Factory ? "" : "not ")}in the "
                                   + "ACTIVE-CLASS sender's — its presence or absence shall be the same "
                                   + "(ISO §14.9.39.3 SR12 b)3.)";
                        if (recv.Only)
                            return "the receiver is described with the ONLY phrase, so an ACTIVE-CLASS sender is "
                                   + "not permitted (ISO §14.9.39.3 SR12 b)1.)";
                        var ac2 = table.Find(send.Name!);
                        var rc3 = table.Find(recv.Name!);
                        if (ac2 is null || rc3 is null)
                            return $"unresolvable class in the pair (ACTIVE-CLASS sender in {send.Name} to "
                                   + $"receiver {recv.Name})";
                        return ac2.ConformsTo(rc3) ? null
                            : $"the class containing the ACTIVE-CLASS sender ({send.Name}) is not {recv.Name} or "
                              + "one of its subclasses (ISO §14.9.39.3 SR12 b)2.)";
                    }
                    default:
                        return $"{send.Spelled} does not conform to a receiver described with object-class-name "
                               + $"'{recv.Name}' (ISO §14.9.39.3 SR12 — its closed list of senders is an "
                               + "object-class-name reference, an ACTIVE-CLASS reference, SELF and NULL)";
                }

            // ── SR14: the receiver is described with an ACTIVE-CLASS phrase. ─────────────────────────────
            default:
                if (send.Kind is not ObjectRefKind.ActiveClass)
                    return $"{send.Spelled} does not conform to a receiver described with the ACTIVE-CLASS "
                           + "phrase (ISO §14.9.39.3 SR14 — its closed list of senders is an ACTIVE-CLASS "
                           + "reference, SELF and NULL)";
                return send.Factory == recv.Factory ? null
                    : $"the FACTORY phrase is {(recv.Factory ? "" : "not ")}specified in the receiver's "
                      + $"ACTIVE-CLASS description and {(send.Factory ? "" : "not ")}in the sender's — its "
                      + "presence or absence shall be the same (ISO §14.9.39.3 SR14 a))";
        }
    }

    /// <summary>
    /// Which §14.9.39.3 syntax rule governs a SET format-5 statement whose sender is <b>object-class-name-1</b>
    /// (a class NAME, not identifier-4), given the RECEIVER's §13.18.60.2 kind. Every one of those rules names
    /// the receiver's description in its own precondition, so the answer is read off the receiver and never
    /// hard-coded:
    /// <list type="bullet">
    ///   <item><b>interface-name receiver</b> — SR11: "If object-class-name-1 is specified and the data item
    ///     referenced by identifier-3 is described with an interface-name that identifies the interface int-1,
    ///     the factory object of object-class-name-1 shall be described with an IMPLEMENTS clause that
    ///     references int-1."</item>
    ///   <item><b>object-class-name receiver</b> — SR13: "If object-class-name-1 is specified and the data item
    ///     referenced by identifier-3 is described with an object-class-name, the data item shall be described
    ///     with the FACTORY phrase…".</item>
    ///   <item><b>ACTIVE-CLASS receiver</b> — SR14, whose closed list of senders ("the data item referenced by
    ///     identifier-4 shall be one of the following") is an ACTIVE-CLASS reference, SELF and NULL: a class
    ///     name is in none of them.</item>
    ///   <item><b>universal receiver</b> — SR8 only, which constrains nothing; the table returns null and this
    ///     label is never rendered.</item>
    /// </list>
    /// <para>⚠ kb/Work PB451: the SET site used to print "(ISO §14.9.39.3 SR13)" for ALL FOUR, so a program
    /// refused under SR11 or SR14 was told it had broken a rule whose own precondition was false for it — and
    /// the sender's identity, which is §14.9.39.4 GR10, was attributed to SR13 as well.</para>
    /// </summary>
    public static string ClassNameSenderRule(ObjectRefKind receiverKind) => receiverKind switch
    {
        ObjectRefKind.Interface => "ISO §14.9.39.3 SR11",
        ObjectRefKind.ObjectClass => "ISO §14.9.39.3 SR13",
        ObjectRefKind.ActiveClass => "ISO §14.9.39.3 SR14",
        _ => "ISO §14.9.39.3 SR8",
    };

    /// <summary>The transitive INHERITS closure of one interface (§11.8.4 GR2's interface half), the sender
    /// side of SR10 a) — "an interface-name that identifies int-1 or an interface inheriting from int-1".</summary>
    private static HashSet<OoInterfaceSymbol> InheritsClosure(OoInterfaceSymbol from)
    {
        var seen = new HashSet<OoInterfaceSymbol>();
        var stack = new Stack<OoInterfaceSymbol>([from]);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (!seen.Add(cur)) continue;
            foreach (var b in cur.Inherits) stack.Push(b);
        }
        return seen;
    }
}
