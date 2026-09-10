// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// <b>ISO §13.16.3 SR8</b>, its last sentence: "For any other entry describing an elementary item, a PICTURE
/// clause shall be specified except as indicated in Syntax rule 9." — the CLOSING GUARD that guarantees no
/// elementary item ever reaches code generation without a <see cref="DataItem.Pic"/>.
///
/// <para><b>Why this exists (kb/Work PB487).</b> The rule was enforced only for the two usages that had a
/// dedicated <see cref="PicPending"/> mark, NATIONAL and BIT. Every other picture-less elementary entry escaped
/// with <c>Pic == null</c> and CRASHED the compiler — an unhandled <c>System.NullReferenceException</c> in
/// <c>MoveEmitter.ConvertSource</c>, or a raw Roslyn <c>CS0103</c> the user cannot act on. Measured on plain
/// COBOL with no exotic clause in it at all:
/// <code>
///   01 M.
///   01 N PIC X(4).
///   …  MOVE "ABCD" TO M
/// </code>
/// M is elementary (nothing is subordinate to it) and has no PICTURE, so it is SR8-nonconforming; the compiler
/// answered with a stack trace and no diagnostic. It is the same hole the PB487 catch-all reached through
/// <c>01 M MESSAGE-TAG.</c>, but it is reachable WITHOUT the catch-all, which is why closing the §13.16.2 clause
/// list does not close it.
///
/// <para><b>Where the picture-less usages are handled instead.</b> §13.16.3 SR8's own first sentence exempts
/// binary-char/short/long/double, float-short/long/extended, index, message-tag, object reference, pointer,
/// function-pointer and program-pointer — for those <c>BindEntry</c> SYNTHESIZES a <see cref="PicInfo"/>, and
/// SR9's VALUE-implied PICTURE would arrive with one too, if it were synthesized — it is not (kb/Work PB504,
/// inventory row SR-13.16.3-9). So the test here is simply "elementary and still no Pic after every synthesis and
/// inheritance pass has run", which is exactly the population SR8 governs and needs no second copy of the
/// exemption list to maintain — plus the one carve-out the diagnostic loop below states and argues: a VALUE that
/// is a FIGURATIVE constant, the case SR9's "length … as specified in 8.3.3" cannot measure and the case this
/// tree has already settled as accept-and-flag.</para>
///
/// <para><b>Two forests, deliberately.</b> The DIAGNOSTIC is reported over <see cref="ConformanceForest"/> so a
/// TYPEDEF template is named once rather than once per <c>TYPE</c> reference site — the entry the programmer
/// must change. The RECOVERY shape is then applied over <see cref="CompositionForest"/>, because
/// <c>ExpandTypes</c> has already cloned the broken template and a clone would otherwise carry the null into
/// codegen with its own diagnostic suppressed. Same rule, one message, no escape.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.16.3 SR8 — every elementary item has a PICTURE or a synthesized profile by now.
    /// <para>Runs immediately after <c>UsageInheritancePass</c>, which is the last pass that can legitimately
    /// FILL a null <c>Pic</c> (a group header shedding its usage to leaves, §13.18.60.4 GR1; a PICTURE-less
    /// INDEX or OBJECT REFERENCE leaf taking the inherited profile). Anything still null at this point is a
    /// declaration defect, not a pending adjudication.</para></summary>
    internal void CheckPictureRequired()
    {
        foreach (var item in ConformanceForest())
        {
            if (!IsPictureLessLeaf(item)) continue;
            // ⛔ THE ONE SR9 CARVE-OUT, AND IT IS THE FIGURATIVE ONE ONLY. SR8 defers to SR9 in its own words —
            // "except as indicated in Syntax rule 9" — and SR9 keys the implied PICTURE on a LITERAL WITH A
            // LENGTH: "The PICTURE clause may be omitted for an elementary item when an alphanumeric, boolean, or
            // national literal that is not a zero-length literal is specified in the data-item format of the VALUE
            // clause" (ISO §13.16.3 SR9), the implied width being "the length of the literal as specified in 8.3.3,
            // Literals". A FIGURATIVE constant has no such length — that is the whole premise of the >>FLAG-14
            // VALUE-FIG-CON-LENGTH option (§7.3.15.4 GR4 k: "A figurative constant specified in the VALUE clause of
            // a data item with no specified length shall be flagged"), whose existence presupposes the construct is
            // ACCEPTED. That reading is settled on this tree and pinned green by FlagDirectiveTests, and a closing
            // guard must not silently RE-ADJUDICATE it — so `01 A VALUE SPACE.` is exempt from the DIAGNOSTIC and
            // from the diagnostic only: the recovery shape below still gives it a Pic.
            // ⛔ The exemption is NOT "any VALUE clause", and the difference is a WRONG ANSWER. `01 B VALUE "AB".`
            // is SR9's own case, and this compiler synthesizes the implied PICTURE NOWHERE (kb/Work PB504, inventory
            // row SR-13.16.3-9 = NOT-IMPLEMENTED); exempting it would bind B as the one-character recovery item and
            // DISPLAY B would print "A" — measured. Loudly requiring the PICTURE that SR9 says may be omitted is
            // debt PB504 owns and states; silently storing a truncated value is not. A numeric literal VALUE
            // (`01 F VALUE 42.`) is outside SR9 altogether and SR8 simply requires the PICTURE.
            if (item.RawValue is { } rawValue && IsFigurativeValueText(rawValue)) continue;
            using var _ = Edition.At(item);
            Edition.Error(DiagnosticCatalog.UsageClauseCompatibility,
                $"data item '{item.CobolName ?? "FILLER"}': a PICTURE clause shall be specified for an elementary "
                + "item (ISO §13.16.3 SR8) — it may be omitted only for an item whose usage is "
                + "binary-char, binary-short, binary-long, binary-double, float-short, float-long, "
                + "float-extended, index, message-tag, object reference, pointer, function-pointer or "
                + "program-pointer, or when a VALUE clause supplies an alphanumeric, boolean or national "
                + "literal (SR9)");
        }

        // The recovery shape, over the COMPOSED forest so a TYPE/SAME AS clone of a defective template cannot
        // carry a null Pic into the emitter behind an already-reported diagnostic. Errors have been raised, so
        // the compile HALTS before codegen either way — this is the belt to that braces, and it is what makes
        // "no elementary item reaches the emitter without a Pic" an INVARIANT rather than a hope.
        foreach (var item in CompositionForest())
            if (IsPictureLessLeaf(item))
                item.Pic = PicInfo.Recovery();
    }

    /// <summary>An entry that is NEITHER a group NOR elementary: no PICTURE and no subordinate entries.
    /// <para>⛔ The test cannot be <see cref="DataItem.IsElementary"/>, which is DEFINED as
    /// <c>Pic is not null</c> — asking it about a picture-less item is vacuously false and the guard would never
    /// fire (measured: the first cut of this pass was a no-op for exactly that reason). “Elementary” in
    /// §13.16.3 SR8's sense is a property of the SOURCE — §8.5.1.3 makes an entry with nothing subordinate to it
    /// elementary whether or not it has a PICTURE — so the predicate is the CHILD COUNT.</para>
    /// <para>Levels 66 (RENAMES) and 88 (condition-name) are not data description entries in Format 1's sense
    /// (§13.16.2 Formats 2–4) and never carry a PICTURE, so they are excluded by level.</para>
    /// <para>An UNEXPANDED <c>TYPE</c> or <c>SAME AS</c> reference is excluded too, and by the rule's own words:
    /// SR8 governs "any other entry DESCRIBING an elementary item", and such an entry describes nothing —
    /// §13.18.57.4 GR1 and §13.18.49.4 GR1/GR2 give it the REFERENCED entry's description. <c>ExpandTypes</c>
    /// clears both fields at every reference SITE, but a nested reference inside a TYPEDEF TEMPLATE
    /// (<c>01 OUTER-T TYPEDEF STRONG. 05 SUB TYPE INNER-T.</c>) is expanded per clone and the template's own
    /// entry keeps its <c>TypeRefName</c> — measured: the first cut of this guard rejected exactly that legal
    /// program (tests/conformance/2002/typedef_nested_strong.cob).</para></summary>
    private static bool IsPictureLessLeaf(DataItem item) =>
        item.Pic is null && item.Children.Count == 0 && item.Level is not (66 or 88)
        && item.TypeRefName is null && item.SameAsName is null;

    /// <summary>Does this raw VALUE operand denote a FIGURATIVE CONSTANT (ISO §8.3.3.6) rather than a literal
    /// with a length? The word list is <see cref="CobolNet.CodeGen.FigurativeConstants.KindOf"/> — THE
    /// figurative-word table for the whole compiler, so this rule cannot drift from the emitter's — plus the
    /// <c>ALL literal-1</c> form, which §8.3.3.6 makes a figurative constant too and which likewise has no
    /// length of its own (it is repeated to the size of the item, so an item of unstated size gives it none).
    /// <para>NULL/NULLS is deliberately NOT admitted here (<c>includeNull</c> left false): it is the class-pointer
    /// figurative, and a pointer item is picture-less by USAGE with a synthesized profile — it never reaches this
    /// guard. Admitting it would only silence SR8 for `01 P VALUE NULL.`, which has no pointer usage at all.</para>
    /// <para>The ALL strip is the same one <c>ScreenValueLiteral</c> performs (a parse-tree GetText concatenates
    /// the tokens, so `ALL SPACES` arrives as "ALLSPACES"); a VALUE operand is a literal or a figurative constant
    /// and nothing else, so a word beginning "ALL" is always the ALL form.</para></summary>
    private static bool IsFigurativeValueText(string raw)
    {
        bool all = raw.Length > 3 && raw.StartsWith("ALL", StringComparison.OrdinalIgnoreCase);
        return all || CobolNet.CodeGen.FigurativeConstants.KindOf(raw) is not null;
    }
}
