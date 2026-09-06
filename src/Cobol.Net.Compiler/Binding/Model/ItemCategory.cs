// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE READER of a described data item's ISO/IEC 1989:2023 §8.5.2 CATEGORY for a syntax rule that is WORDED
/// in categories, and the ONE English face for it.
/// <para>WHY IT EXISTS. Three rules of the file control entry are worded the same way and were each about to grow
/// their own copy of "is this item category alphanumeric?": §12.4.5.2 SR7 (<i>"Data-name-1 shall reference an
/// alphanumeric data item"</i>, ASSIGN … USING), §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 (<i>"category alphanumeric
/// or category national"</i>, the record keys). The first was written inline in <c>DataBinder</c>; the second pair
/// had no site at all (kb/Work PB743). One rule written down in more than one place is this repository's most
/// reproducible defect shape, so the predicate lives here and the rules cite it.</para>
/// <para>THE GROUP ARM IS §13.18.29.4 GR3 — <i>"If a GROUP-USAGE clause is not specified or implied for a group
/// item that is not strongly typed and is not a variable-length group, that group item is an alphanumeric group
/// item"</i> — so a plain group key is category alphanumeric and a <c>GROUP-USAGE NATIONAL</c> group is category
/// national. Both are read through <see cref="Table16Operand.Of(DataItem)"/>, the compiler's ONE classifier of an
/// item's category, which already resolves a bit / national group's as-if PICTURE (§13.18.29.4 GR1b/GR2b) and
/// answers <see cref="PicCategory.Group"/> for the plain group. Never a second walk of <c>Pic</c> /
/// <c>AsIfPic</c> / <c>GroupUsage</c>.</para>
/// <para>⚠ WHAT THE MODEL CANNOT SEE, said out loud: <see cref="PicCategory"/> has no
/// <c>AlphanumericEdited</c> member — an alphanumeric-edited item carries category
/// <see cref="PicCategory.Alphanumeric"/> — so a screen written here admits one where the standard's category
/// list does not. That is the established posture for a category screen in this compiler
/// (<c>RecordLayout.CategoryOfItem</c>, §14.9.41.3 SR6 b) 2.: <i>"where the model cannot tell two categories
/// apart the test passes — this screen exists to reject what the rule NAMES, never what this compiler cannot
/// classify"</i>), and it errs toward accepting legal source rather than rejecting it. Category ALPHABETIC is
/// visible (<c>PicInfo.IsAlphabetic</c>) and is excluded: §8.8.4.2.4's <i>"A class alphabetic operand shall be
/// treated as though it were an operand of class alphanumeric"</i> is a COMPARISON rule, not a category
/// identity.</para>
/// </summary>
public static class ItemCategory
{
    /// <summary>Category ALPHANUMERIC (ISO §8.5.2.4): an elementary <c>PIC X</c> item, or an alphanumeric group
    /// item (§13.18.29.4 GR3). Alphabetic (<c>PIC A</c>) is its own category and is excluded, as are numeric,
    /// numeric-edited, national, boolean and the PICTURE-less pointer / object-reference usages.</summary>
    public static bool IsAlphanumeric(DataItem item) => Admits(item, national: false);

    /// <summary>Category ALPHANUMERIC or category NATIONAL — the operand set §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2
    /// name for a record key. A <c>GROUP-USAGE NATIONAL</c> group qualifies through its as-if <c>PIC N(m)</c>
    /// (§13.18.29.4 GR2); a <c>GROUP-USAGE BIT</c> group does not (GR1 makes it category boolean).</summary>
    public static bool IsAlphanumericOrNational(DataItem item) => Admits(item, national: true);

    /// <summary>ONE walk for both predicates — the two rules differ only in whether category national is in the
    /// admitted set, and writing the walk twice is how the alphabetic and group arms would come to disagree.</summary>
    private static bool Admits(DataItem item, bool national) => Table16Operand.Of(item) switch
    {
        // §13.18.29.4 GR3's group arm. `IsGroup` (Pic is null AND it has children) is the STRUCTURAL half the
        // classifier does not carry: a PICTURE-less entry with no subordinates is not a group item and has no
        // category at all, and admitting it would let a rule about data items answer about something that is not
        // one.
        { Category: PicCategory.Group } => item.IsGroup,
        { Category: PicCategory.Alphanumeric, IsAlphabetic: false } => true,
        { Category: PicCategory.National } => national,
        _ => false,
    };

    /// <summary>A short English face for a diagnostic — WHAT THE OPERAND IS, so a message names the reason
    /// instead of restating the rule it broke.</summary>
    public static string Face(DataItem item) =>
        item.Pic is { } pic
            ? pic.IsAlphabetic ? "a category-alphabetic item" : $"a category-{pic.Category.ToString().ToLowerInvariant()} item"
        : item.GroupUsage is GroupUsage.None ? "not an elementary or group data item"
        : $"a {item.GroupUsage.ToString().ToLowerInvariant()} group item";
}
