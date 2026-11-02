// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ <b>REFERENCE MODIFICATION</b> (ISO §8.4.3.3, DESIGN-architecture-review §8.3 (4), kb/Work PB2373): "Reference modification
/// defines a unique data item by specifying an identifier, a leftmost position, and a length" (§8.4.3.3.1). The rules a written
/// <c>(start:length)</c> is held to and the view it builds: the SR1 identifier exclusions (<see cref="RefModExclusion"/>), the SR3
/// count (<see cref="ScreenRefModCount"/>), the SR4 literal range screens (<see cref="LiteralRefModRangeViolation"/>,
/// <see cref="ScreenRefModLiterals"/>), reading the two positions (<see cref="ReadRefMod"/>, whose positions are bound by
/// <see cref="ReferenceResolver"/>'s subscript binder) and the GR5 view over the identifier's place (<see cref="RefModView"/>,
/// <see cref="RefModOf"/>, <see cref="ResolveItemRefMod"/>), plus the lexical read of what a data-division operand carries after
/// its base word (<see cref="ReadOperandSuffixes"/>).
/// </summary>
internal sealed class RefModBinder(ReferenceResolver refs, DataBinder data)
{
    /// <summary>⛔ §8.4.3.3.3 SR3 — "Identifier-1 shall not be a reference-modification format identifier." The
    /// grammar cannot express it, so the written reference is COUNTED (<see cref="WrittenReference.RefModCount"/>);
    /// ONE screen for every entry that reads a reference-modified identifier — <see cref="Resolve"/> and
    /// <see cref="ResolveForAddressOf"/> (kb/Work PB1407). False having reported.</summary>
    internal bool ScreenRefModCount(Core.DataReferenceContext dref, WrittenReference written, string name)
    {
        if (written.RefModCount <= 1) return true;
        if (!refs.Probing && refs.Diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
            data.Edition.Error(DiagnosticCatalog.RefModOfRefMod,
                $"'{name}' carries {written.RefModCount} reference modifications; a reference-modified item cannot itself "
                + "be reference-modified (ISO §8.4.3.3.3 SR3). Compose the positions into one modifier instead.");
        return false;
    }

    /// <summary>⛔ THE ONE ADMISSION OF A REFERENCE MODIFIER, from the written reference to the screened
    /// <see cref="RefModSpec"/> (kb/Work PB1407 extracted it from <see cref="Resolve"/> so ADDRESS OF reads the same
    /// rules): §8.4.3.3.3 SR1 — WHAT identifier-1 MAY BE (kb/Work PB70; a bind-time rejection, COBOL1647, never
    /// the run-time NotImplemented a sending ref-mod used to reach nor the silent drop a receiving one fell into),
    /// the spec read off whichever carrier the lexer chose, and the LITERAL out-of-range screen (kb/Work PB1707
    /// part 1, R60 — refused while checking is off, compiled with a warning while it is on, the run-time raise then
    /// being what the program asked for). <paramref name="described"/> is identifier-1's DESCRIPTION: the item its
    /// place stands for, not the name as written — for a no-THROUGH RENAMES alias that is data-name-2, whose
    /// attributes §13.18.45.4 GR1 makes data-name-1's own. Null having reported, with the answer in
    /// <paramref name="refusal"/>.</summary>
    internal RefModSpec? ReadScreenedRefMod(Core.DataReferenceContext dref, WrittenReference written, DataItem described,
        out RefResolution? refusal)
    {
        refusal = null;
        if (RefModExclusion(described) is { } why)
        {
            if (!refs.Probing && refs.Diagnosed.Add(dref))   // R30 purity: a probe never diagnoses (kb/Work PB157)
                data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                    $"'{DataBinder.WrittenText(dref)}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            refusal = RefResolution.Refused(DataBinder.WrittenText(dref));
            return null;
        }
        if (ReadRefMod(written.RefModPart!) is not { } spec)   // RefModCount > 0 ⇒ the first modifier is in hand
        {
            refusal = refs.SegmentFailure(DataBinder.WrittenText(dref));   // a bound the materializer refused or deferred (§8.4.3.3.3 SR4)
            // A REFUSED segment was reported by whoever refused it (the materializer, or ReadRefMod's omitted
            // leftmost-position), so the reference counts as diagnosed: a caller that asks (ADDRESS OF's binder) then
            // adds no second, misleading "mis-subscripted" error of its own.
            if (refusal.Outcome == RefOutcome.Reported && !refs.Probing) refs.Diagnosed.Add(dref);
            return null;
        }
        if (!ScreenRefModLiterals(dref, described, spec, dref.Start.Line))
        {
            refusal = RefResolution.Refused(DataBinder.WrittenText(dref));
            return null;
        }
        return spec;
    }

    /// <summary>Reference-modify an ALREADY-RESOLVED place — §8.4.3.1.4 GR1 g)'s tail ("a reference modifier
    /// applies to the identifier on the left") over an identifier that is not a data-NAME reference. Its one
    /// caller today is the §8.4.3.4 inline method invocation, whose §8.4.3.4.4 GR1 c) temporary has no
    /// <c>dataReference</c> context of its own (kb/Work PB428). It is a thin ENTRY, not a second reading: the
    /// §8.4.3.3.3 SR1 screen is <see cref="RefModExclusion"/> and the view is <see cref="RefModView"/> — the
    /// same two the syntactic path runs, in the same order, with the same COBOL1647.</summary>
    internal Place? RefModOf(Place inner, Core.RefModPartContext rmp, string what)
    {
        if (RefModExclusion(inner.Item) is { } why)
        {
            data.Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted,
                $"'{what}': reference modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
            return null;
        }
        return ReadRefMod(rmp) is { } spec ? RefModView(inner.Item, inner, spec) : null;
    }

    /// <summary>⛔ THE ONE REFERENCE-MODIFICATION VIEW BUILDER (kb/Work PB205): the SUBSTRATE wrap that turns an
    /// item's place into the §8.4.3.3.4 GR5 unique data item, written down once for BOTH the syntactic path
    /// (<see cref="Resolve"/>, where the positions come off the source) and the DATA-DIVISION path
    /// (<see cref="ResolveItemRefMod"/>, where a clause carries integer literals). Before this factoring the wrap
    /// lived INSIDE the parse-context-keyed path, so the CONTROL clause — which §13.18.16.3 SR4 expressly permits
    /// to be reference-modified — could not reach it and silently bound the WHOLE item instead.
    /// <para>Callers screen §8.4.3.3.3 SR1 (<see cref="RefModExclusion"/>) first; a null here is an item whose
    /// substrate this backend cannot slice (a numeric item that is not usage DISPLAY), never a rule violation.</para></summary>
    internal Place? RefModView(DataItem item, Place inner, RefModSpec spec)
    {
        // Reference modification is over a character string. A GROUP (SR1 — "an alphanumeric group item" / "a group
        // item that is neither a strongly-typed group nor a variable-length group") is viewed through its character
        // IMAGE — the unique data item is an elementary alphanumeric item over the group's positions (§8.4.3.3.4
        // GR6); a Tier-B view (RedefViewPlace) is already a character window. A NUMERIC USAGE-DISPLAY item is viewed
        // through its character image likewise (NC224A's TEST-1-DATA(3:) over PIC 9(6)); a numeric item of usage
        // NATIONAL (SR1 admits it) has no national image channel yet — the loud stage.
        if (item.IsGroup)
        {
            // ⛔ A BIT GROUP TAKES BIT POSITIONS, NOT CHARACTER POSITIONS (kb/Work PB173). §8.4.3.3.3 SR1's last
            // sentence admits a bit group as identifier-1 "treated as [an] elementary data item", §13.18.29.4
            // GR1b makes that item PICTURE 1(m) of usage bit, and §8.4.3.3.4 GR5a then says in so many words:
            // "If the usage of identifier-1 is bit, positions used in evaluation are bit positions". So it wraps
            // as its UNPACKED boolean string, never as AsImage()'s ceil(m/8) packed characters — the units the
            // boolean channel already counts (ConditionBinder's widths read OperandPic.Length, which is
            // ExtentBits) and the units RefModPlace.Category already reports.
            // ⛔ A NATIONAL GROUP TAKES ITS OWN NATIONAL POSITIONS, THE SAME WAY (kb/Work PB327). It used to
            // keep GroupImagePlace on the premise that "a national leaf contributes ImageWidth = Length
            // character positions, never byte-doubled, so its AsImage() IS its national-position string". That
            // premise died when AsImage() became the BYTE image so a national leaf could ride a file record:
            // §13.18.60.4 GR8 leaves the size of a national character to the implementor and D-N1 pins TWO
            // bytes, UTF-16BE. §8.4.3.3.4 GR5a's "otherwise, positions used in evaluation are character
            // positions" means the item's OWN characters, so a national group wraps as its AsNat() string.
            // ⛔ A TIER-B WINDOW NEEDS NO WRAP IN EITHER UNIT, and for the BIT one that is now a derived fact
            // rather than an untested omission (kb/Work PB203 closing PB173's open "RELATED" question): a
            // RedefViewPlace over a bit member reads its BOOLEAN CARRIER — CobolBits.ReadWindow over the class
            // backing — so the RefModPlace built below slices §8.4.3.3.4 GR5a's bit positions structurally,
            // exactly as BitImagePlace's AsBits() does for a struct-stored bit group. Measured: with
            // `01 A PIC X(2). 01 BV REDEFINES A GROUP-USAGE BIT. 05 BV1 PIC 1(8). 05 BV2 PIC 1(8).` holding
            // B"0100100001001001", `BV(1:3)` is B"010" and `MOVE ALL B"0" TO BV(2:3)` zeroes positions 2-4.
            if (inner is not RedefViewPlace)
                inner = !item.IsAsIfElementary ? new GroupImagePlace(inner)
                    : item.GroupUsage is GroupUsage.Bit ? new BitImagePlace(inner)
                    : new NatImagePlace(inner);
        }
        else if (item.Pic?.Category is PicCategory.Numeric)
        {
            // ⛔ BOTH USAGES §8.4.3.3.3 SR1 ADMITS (kb/Work PB646) — the SAME pair RefModExclusion above already
            // names, and this gate named only one of them: the exclusion test admitted `PIC 9(6) USAGE NATIONAL`
            // and then this arm returned null, so the operand fell through to the Tier-C runtime loud
            // ("a COBOL feature that is not yet implemented was reached at run time: reference 'RM(2:3)'"). The
            // wrap is the same for both, and §8.4.3.3.4 GR3 is why: a usage-national item "is operated upon for
            // purposes of reference modification as if it were redefined as a data item of class and category
            // national of the SAME SIZE", and its size in national character positions is exactly the digit run
            // NumericImagePlace exposes (GR5a counts character positions; D-N1 makes one national position one
            // UTF-16 char). GR6c's category answer is already written — RefModPlace.CategoryOf — and this is
            // the arm that makes it reachable.
            if (item.Pic is not { IsCharacterFormNumeric: true }) return null;   // THE ONE character-form predicate
            // P5.7: the bind-time wrap decision reads the COLLECTED early facts (same mid-bind timing the
            // deleted flag had — MarkRefModStoreImage records the SAME item during this statement's bind).
            // (A view over engine state has no cell to be image-backed: Place.OwnsStorageCell, kb/Work PB1943.)
            if ((!inner.OwnsStorageCell || !data.IsImageBackedEarly(item)) && inner is not RedefViewPlace)
                inner = new NumericImagePlace(inner);
        }
        // National/boolean items reference-modify in their OWN character positions (§8.4.3.3 GR1/GR5a — a
        // national position is one UTF-16 char, a bit position one '0'/'1' char, under D-N1/D-B1); alphanumeric,
        // alphabetic, alphanumeric-edited and numeric-edited items are character strings already.
        return new RefModPlace(inner, spec.Start, spec.Length) { AllowZeroLength = spec.AllowZeroLength };
    }

    /// <summary>A ref-mod view of an ALREADY-RESOLVED item whose leftmost-position and length are COMPILE-TIME
    /// INTEGERS — the DATA-DIVISION form. The three data-division clauses that admit a reference-modified operand
    /// all restrict it to integer literals for the same reason (ISO §13.18.16.3 SR4 "Data-name-1 may be
    /// reference-modified. If it is, leftmost-position and length shall be integer literals."; §13.18.54.3 SR8;
    /// §13.18.57.3 SR10), so there is no expression to evaluate and no statement context to evaluate it in — the
    /// view is the SAME <see cref="RefModView"/> the procedure division builds, reached without a parse context.
    /// <para><paramref name="length"/> null is §8.4.3.3.2's bracketed, omitted length — the slice runs to the
    /// rightmost position (§8.4.3.3.4 GR5c last sentence). Null return: the item has no place, or §8.4.3.3.3 SR1
    /// does not admit it (the CALLER rejects that at bind with COBOL1647 — this is the backstop).</para>
    /// <para>SR1 and the view read the PLACE's item, as <see cref="Resolve"/> does — a no-THROUGH level-66 alias is
    /// described by data-name-2 (§13.18.45.4 GR1), never by its own entry (kb/Work PB1380).</para></summary>
    public Place? ResolveItemRefMod(DataItem item, int start, int? length, bool allowZeroLength = false) =>
        refs.PlaceForItem(item, []) is { } inner && RefModExclusion(inner.Item) is null
            ? RefModView(inner.Item, inner, new RefModSpec(new PositionConstant(start), length is { } l ? new PositionConstant(l) : null, allowZeroLength))
            : null;

    /// <summary>ISO §8.4.3.3.3 SR1, read as an EXCLUSION test: the reason <paramref name="item"/> may NOT be
    /// identifier-1 of a reference modification, or null when it may. Admitted: a boolean, national, alphanumeric or
    /// alphabetic item (bullets 1–4 — this model's <see cref="PicCategory.Alphanumeric"/> covers alphabetic and
    /// alphanumeric-edited, <see cref="PicCategory.National"/> covers national-edited); a numeric-edited item and a
    /// numeric item of usage DISPLAY or NATIONAL, each "not subordinate to a strongly-typed group item" (bullets 5,
    /// 8); a group that is neither strongly typed nor variable-length (bullet 9; §8.5.1.12 — a variable-length group
    /// has a dynamic-length item or a dynamic-capacity table subordinate to it). Everything else — a numeric item of
    /// BINARY / PACKED / COMP-5 / float usage, an index item, a pointer, an object reference — is excluded. Bit and
    /// national GROUPS are "treated as elementary" by SR1's last sentence, and GROUP-USAGE IS modelled — kb/Work
    /// PB79 landed 2026-08-18 (DEVLOG 1317): <c>AsIfPic</c> / <c>OperandPic</c> / <c>IsAsIfElementary</c> carry the
    /// as-if description, so this predicate's group bullet admits them for the right reason. Their SUBSTRATE then
    /// differs by usage: a bit group wraps as a <c>BitImagePlace</c> over its unpacked boolean string, because
    /// §8.4.3.3.4 GR5a evaluates a bit item's positions as BIT positions (kb/Work PB173), and a national group
    /// wraps as a <c>NatImagePlace</c> over its <c>AsNat()</c> national-position string — §13.18.29.4 GR2b's as-if
    /// PICTURE N(m) counts national positions, which since kb/Work PB327 are no longer the same string as the
    /// group's byte image.</summary>
    internal static string? RefModExclusion(DataItem item)
    {
        if (item.IsGroup)
            return StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item"
                : HasVariableLengthSubordinate(item)
                    ? "a variable-length group item (ISO §8.5.1.12 — a dynamic-length item or a dynamic-capacity table is subordinate to it)"
                : null;
        // A leaf subordinate to a strongly-typed group (its own class/category still decides bullets 1–4).
        bool underStrong = StrongTypeModel.IsStronglyTyped(item);
        return item.Pic switch
        {
            null => "an item without character positions",
            { Category: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } => null,
            { Category: PicCategory.NumericEdited } =>
                underStrong ? "a numeric-edited item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric, Usage: Usage.Index } => "an index data item (class index, ISO §13.18.60)",
            { Category: PicCategory.Numeric, Usage: Usage.Display or Usage.National, IsFloat: false } =>
                underStrong ? "a numeric item subordinate to a strongly-typed group item" : null,
            { Category: PicCategory.Numeric } p =>
                $"a numeric item of USAGE {p.Usage} (SR1 admits usage DISPLAY or NATIONAL only)",
            { Category: PicCategory.ObjectReference } => "an object reference",
            { Category: PicCategory.Pointer or PicCategory.ProgramPointer or PicCategory.FunctionPointer } => "a pointer",
            { } p => $"an item of category {p.Category}",
        };
    }

    /// <summary>⛔ THE ONE COMPILE-TIME RANGE SCREEN of a reference modification (kb/Work PB1707 part 1; owner decision
    /// R60) — the bind-time half of <c>CobolString.RefModOutOfRange</c>, written for LITERAL positions. ISO §8.4.3.3.4
    /// 5) b): leftmost-position is a position of the item, 1 through its size; 5) c): a specified length is a positive
    /// nonzero integer (zero only under REF-MOD-ZERO-LENGTH, §7.3.23) and "The sum of leftmost-position and length
    /// minus the value one shall be less than or equal to the number of positions in the data item referenced by
    /// identifier-1". A violation is the fatal EC-BOUND-REF-MOD; §14.6.13.1.3 8)'s last paragraph licenses the
    /// compiler to produce no code for one it detects while checking is not enabled, and GnuCOBOL (cobc/typeck.c,
    /// "offset of '%s' out of bounds" / "length of '%s' out of bounds") refuses it, so the caller reports
    /// <see cref="DiagnosticCatalog.RefModLiteralOutOfRange"/> there and the warning twin where checking is on.
    /// <para>Only an item of FIXED size is screened — the size is then a compile-time fact: a DYNAMIC LENGTH item
    /// (§8.5.1.10), an ANY LENGTH item (§13.18.2) and an item with an OCCURS DEPENDING beneath it (or a dynamic
    /// table) have a run-time size, which is the run-time range test's. <paramref name="start"/> /
    /// <paramref name="length"/> are null for a position that is not an integer literal; a bound is judged only when
    /// it is known, so <c>X(1:N)</c> is never screened and <c>X(7:N)</c> is refused on its start alone. A length
    /// literal that exceeds the whole item is refused whatever the start, because no start in 1..size can make it fit.
    /// Returns the violated clause's words, or null.</para></summary>
    internal static string? LiteralRefModRangeViolation(DataItem item, Int128? start,
        Int128? length, bool omittedLength, bool allowZeroLength)
    {
        if (item.IsDynamicLength || item.IsAnyLength || HasOdoOnOrBeneathOrDynamicTable(item)) return null;
        int size = RefModPlace.PositionCount(item);
        if (start is { } s && (s < 1 || s > size))
            return $"leftmost-position {s} is not a position of the {size}-position item (ISO §8.4.3.3.4 5) b))";
        if (omittedLength || length is not { } len) return null;
        if (len == 0 && !allowZeroLength)
            return "a zero length is not a positive nonzero integer (ISO §8.4.3.3.4 5) c); REF-MOD-ZERO-LENGTH is not on)";
        // (written `len > size - s2 + 1`, never `s2 + len - 1 > size`: a literal near Int128's top would wrap the sum;
        // s2 is within 1..size here, so the rewritten form cannot overflow)
        if (len > 0 && (start is { } s2 ? len > size - s2 + 1 : len > size))
            return $"the reference modification ({(start is { } s3 ? s3.ToString() : "…")}:{len}) extends past the "
                + $"{size}-position item (ISO §8.4.3.3.4 5) c))";
        return null;
    }

    private static bool HasOdoOnOrBeneathOrDynamicTable(DataItem item) =>
        DataItem.HasOdoOnOrBeneath(item) || item.IsDynamicTable || HasVariableLengthSubordinate(item);

    /// <summary>The decimal-integer LITERAL a rendered position is, or null when it is anything else (a data-name
    /// read, an expression, a narrowing call) — the screen's "is this position known at compile time". A figurative
    /// ZERO is included: <see cref="BindPosition"/> reads it as <c>PositionConstant(0)</c> (kb/Work PB3960).</summary>
    private static Int128? LiteralPosition(Position? position) => position switch
    {
        PositionConstant { Value: >= 0 } c => c.Value,
        PositionWideConstant w when w.Value >= 0 => w.Value,   // past long: the screen names the literal itself (kb/Work PB2938)
        _ => null,
    };

    /// <summary>The hook that answers "is checking for EC-BOUND-REF-MOD enabled at this source line?" — installed by
    /// <c>StatementBinder</c> (which owns the statement-level TURN fold, overlays included) exactly as
    /// <see cref="MaterializeSegment"/> is, and null on the DATA-division resolution paths, where no statement
    /// exists for a checking directive to govern (so the screen reads null as "not enabled").</summary>
    internal Func<int, bool>? RefModCheckingAt { get; set; }

    /// <summary>Screen the literal positions of a reference modification of <paramref name="described"/> written at
    /// <paramref name="line"/> and report a violation once per written reference: the error
    /// (<see cref="DiagnosticCatalog.RefModLiteralOutOfRange"/>, the reference is refused — the caller returns
    /// false) where EC-BOUND-REF-MOD checking is not enabled, the warning twin where it is (the reference binds).</summary>
    private bool ScreenRefModLiterals(Core.DataReferenceContext dref, DataItem described, RefModSpec spec, int line)
    {
        if (refs.Probing) return true;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        if (LiteralRefModRangeViolation(described, LiteralPosition(spec.Start), LiteralPosition(spec.Length),
                spec.Length is null, spec.AllowZeroLength) is not { } why) return true;
        bool checking = RefModCheckingAt?.Invoke(line) ?? false;
        if (refs.Diagnosed.Add(dref))
        {
            string at = $"'{DataBinder.WrittenText(dref)}': {why}";
            if (checking) data.Edition.Warning(DiagnosticCatalog.RefModLiteralOutOfRangeChecked, at);
            else data.Edition.Error(DiagnosticCatalog.RefModLiteralOutOfRange, at);
        }
        return checking;
    }

    /// <summary>§8.5.1.12 — a variable-length group has a dynamic-length elementary item or a dynamic-capacity table
    /// as a subordinate (at any depth).</summary>
    internal static bool HasVariableLengthSubordinate(DataItem group)
    {
        foreach (var c in group.Children)
            if (c.IsDynamicLength || c.IsDynamicTable || (c.IsGroup && HasVariableLengthSubordinate(c))) return true;
        return false;
    }

    // ── The ONE reference-modification reader (ISO §8.4.3.3.2) ───────────────────────────────────────────────
    // A ref-mod reaches the binder as ONE parse node, `refModPart : (LPAREN | FNARG_LPAREN | REF_LPAREN) refModSpec
    // (RPAREN | FNARG_RPAREN | REF_RPAREN)` — three paren flavours because the lexer types the paren before it can see
    // the colon: REF_LPAREN straight after a data name (kb/Work PB2113 — this was a second carrier, a SUBSCRIPT-mode
    // token group with a depth-0 colon, until the mode was removed), FNARG_LPAREN straight after a ZERO-ARGUMENT
    // function name (§8.4.3.2.3 SR6's catalog precondition; PB48) and LPAREN after a ')'. Internal because the
    // intrinsic binder reads the SAME node for a ref-modified FUNCTION RESULT (§8.4.3.3.3 SR2, fix-queue PB8) — a
    // second copy of this reader there is exactly the one-rule-two-places defect PB4 was.

    /// <summary>Read a <c>refModPart</c>. Null when a start/length expression uses a form the segment renderer does not
    /// handle, so the caller fails loud rather than emitting a wrong slice — or on an OMITTED leftmost-position, which
    /// is reported HERE, in the one reader, so a sending operand, a receiving operand, an ADDRESS OF operand and a
    /// function result all name §8.4.3.3.2 (kb/Work PB1407, PB1458). The grammar admits the omission
    /// (<c>refModSpec : leftmost=functionArgument? COLON …</c>) precisely so that it reaches this report rather than
    /// a raw parse error.</summary>
    internal RefModSpec? ReadRefMod(Core.RefModPartContext rmp)
    {
        var spec = rmp.refModSpec();
        if (spec.leftmost is null)
        {
            // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written modifier (Diagnosed).
            if (!refs.Probing && refs.Diagnosed.Add(rmp))
                data.Edition.Error(DiagnosticCatalog.RefModLeftmostPositionOmitted,
                    $"'{DataBinder.WrittenText(rmp).Trim()}': a reference modifier is written ( leftmost-position : "
                    + "[ length ] ) and only the length may be omitted, so the colon cannot lead (ISO §8.4.3.3.2). Write "
                    + "the leftmost position explicitly, for example (1:2).");
            return null;
        }
        if (refs.Subscripts.BindPosition(new PositionSegment(SubscriptBinder.LeafTokens(spec.leftmost), spec.leftmost), SegmentPosition.RefMod)
            is not { } rmStart) return null;
        Position? rmLen = null;
        if (spec.length is { } lengthExpr)
        {
            if (refs.Subscripts.BindPosition(new PositionSegment(SubscriptBinder.LeafTokens(lengthExpr), lengthExpr), SegmentPosition.RefMod)
                is not { } l) return null;
            rmLen = l;
        }
        // §7.3.23 / §8.4.3.3.4 item 5c: the ref-mod allows a zero-length result iff REF-MOD-ZERO-LENGTH is ON at
        // this site's source line (the group's compile-time directive fold; OFF everywhere by default).
        return new RefModSpec(rmStart, rmLen, data.RefModZeroLength.IsOnAt(rmp.Start.Line));
    }

    /// <summary>The suffixes written on ONE data reference, read LEXICALLY — how many subscript lists and
    /// reference modifiers it carries, and, for a single ref-mod whose two positions are INTEGER LITERALS, their
    /// values.
    /// <para>This is the DATA-DIVISION read: the clauses that admit a reference-modified operand all restrict it
    /// to integer literals (ISO §13.18.16.3 SR4, §13.18.54.3 SR8, §13.18.57.3 SR10), so no expression renderer and
    /// no procedure-phase state are involved. A position that is anything else — a data-name, an arithmetic
    /// expression, a figurative constant — comes back <see cref="RefModSuffixes.NonLiteral"/>, for the CLAUSE's own
    /// syntax rule to reject with its own diagnostic.</para></summary>
    internal static RefModSuffixes ReadOperandSuffixes(Core.DataReferenceContext dref)
    {
        int refMods = 0, subscripts = 0;
        Core.RefModPartContext? first = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            // A qualification carries its OWN (subscriptPart | refModPart)* tail — `C IN G (1:3)` hangs the
            // ref-mod off the qualification, not off the base word.
            if (suffix.qualification() is { } q)
            {
                subscripts += q.subscriptPart().Length;
                foreach (var rp in q.refModPart()) { refMods++; first ??= rp; }
            }
            else if (suffix.refModPart() is { } rmp) { refMods++; first ??= rmp; }
            else if (suffix.subscriptPart() is not null) subscripts++;
        }
        if (refMods != 1) return new RefModSuffixes(refMods, subscripts, null, null, false);

        var (start, length, ok, beyond) = ReadLiteralPositions(first!);
        return ok && beyond
            ? new RefModSuffixes(refMods, subscripts, null, null, false, BeyondHostLimit: true)
            : new RefModSuffixes(refMods, subscripts, ok ? start : null, ok ? length : null, !ok);
    }

    /// <summary>A <c>refModPart</c> as integer literals: each position shall be ONE integer literal (§13.18.16.3 SR4
    /// and its twins), so its whole text is the value or the read fails; an omitted length — §8.4.3.3.2's bracketed
    /// form — is the only omission permitted.
    /// ⛔ Read through THE ONE integer-literal reader, <see cref="WiseOwl.Cobol.Validation.IntegerOperandRules.TryHostValue(string, out int, out bool)"/>
    /// (kb/Work PB1579): an int.TryParse sent an 11-digit literal to the "not an integer literal" branch, a false
    /// sentence; the reader answers "integer literal, beyond the host range" (<c>Beyond</c>) instead.</summary>
    private static (int Start, int? Length, bool Ok, bool Beyond) ReadLiteralPositions(Core.RefModPartContext rmp)
    {
        var spec = rmp.refModSpec();
        if (spec.leftmost is null || WritesFigurativeZero(spec.leftmost)
            || !WiseOwl.Cobol.Validation.IntegerOperandRules.TryHostValue(spec.leftmost.GetText(), out int start, out bool b0))
            return (0, null, false, false);
        if (spec.length is null) return (start, null, true, b0);
        return !WritesFigurativeZero(spec.length)
            && WiseOwl.Cobol.Validation.IntegerOperandRules.TryHostValue(spec.length.GetText(), out int len, out bool b1)
            ? (start, len, true, b0 || b1) : (0, null, false, false);
    }

    /// <summary>True when the position's source is the figurative ZERO (the <c>ZERO_ARITH</c> token the rewriter mints,
    /// whose text is "0"): §13.18.16.3 SR4 and its twins want an INTEGER LITERAL, and ZERO is a figurative constant
    /// (§8.8.1.1), so <see cref="ReadLiteralPositions"/> must not take its text for the literal <c>0</c> (kb/Work PB3960).</summary>
    private static bool WritesFigurativeZero(IParseTree position) =>
        SubscriptBinder.LeafTokens(position).Exists(t => t.Type == Core.ZERO_ARITH);
}

/// <summary>What ONE data reference carries after its base word, read lexically by
/// <see cref="RefModBinder.ReadOperandSuffixes"/> for the DATA-DIVISION clauses (kb/Work PB205).</summary>
/// <param name="RefMods">How many reference-modification carriers are written. More than one violates ISO
/// §8.4.3.3.3 SR3 ("Identifier-1 shall not be a reference-modification format identifier").</param>
/// <param name="Subscripts">How many subscript carriers are written.</param>
/// <param name="Start">The leftmost-position integer literal, when exactly one ref-mod is written and both its
/// segments are integer literals; null otherwise.</param>
/// <param name="Length">The length integer literal — null both for "no readable ref-mod" and for §8.4.3.3.2's
/// omitted, bracketed length, which <see cref="RefMods"/> and <see cref="Start"/> tell apart.</param>
/// <param name="NonLiteral">Exactly one ref-mod is written and a segment of it is NOT an integer literal — the
/// shape §13.18.16.3 SR4 / §13.18.54.3 SR8 / §13.18.57.3 SR10 each reject.</param>
/// <param name="BeyondHostLimit">Exactly one ref-mod is written, both segments ARE integer literals, and one of them
/// lies beyond the host range (kb/Work PB1579). <see cref="Start"/> and <see cref="Length"/> are then null: a
/// clause that lays the slice out (the CONTROL prior-value copy, §13.18.16.4 GR3) reports COBOL2427 through
/// <see cref="WiseOwl.Cobol.Validation.IntegerOperandRules.BeyondLimitMessage"/> rather than allocate a saturated
/// bound.</param>
internal readonly record struct RefModSuffixes(
    int RefMods, int Subscripts, int? Start, int? Length, bool NonLiteral, bool BeyondHostLimit = false);
