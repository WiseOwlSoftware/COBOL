// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE READER of a described data item's ISO/IEC 1989:2023 §8.5.2 CLASS and CATEGORY for a syntax rule
/// that is WORDED in one of them, and the ONE English face for it.
/// <para>WHY IT EXISTS. Three rules of the file control entry are worded the same way and were each about to grow
/// their own copy of "is this item category alphanumeric?": §12.4.5.2 SR7 (<i>"Data-name-1 shall reference an
/// alphanumeric data item"</i>, ASSIGN … USING), §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 (<i>"category alphanumeric
/// or category national"</i>, the record keys). The first was written inline in <c>DataBinder</c>; the second pair
/// had no site at all (kb/Work PB743). One rule written down in more than one place is this repository's most
/// reproducible defect shape, so the predicate lives here and the rules cite it.</para>
/// <para>⛔ IT HAPPENED ANYWAY, AND HERE (kb/Work PB337). The GROUP arm's own definition — §13.18.29.4 GR3, what
/// an <i>alphanumeric group item</i> IS — was written down TWICE: as <c>item.IsGroup</c> here (the structural
/// conjunct alone) and completely, but privately, in <c>DataBinder.GroupValue</c>. The incomplete copy is the
/// one every rule above reads, so each of them admitted a strongly-typed group and a variable-length group,
/// which GR3 excludes by name. <see cref="IsAlphanumericGroup"/> is now the ONE spelling and
/// <c>DataBinder.GroupValue</c> calls it. The INTO-receiver rules of §14.9.30.3 SR1 b) and §14.9.34.3 SR2 b),
/// which name the same operand set, were given their first implementation through it rather than through a
/// fourth copy.</para>
/// <para>THE GROUP ARM IS §13.18.29.4 GR3 — <i>"If a GROUP-USAGE clause is not specified or implied for a group
/// item that is not strongly typed and is not a variable-length group, that group item is an alphanumeric group
/// item"</i> — so a plain group key is category alphanumeric and a <c>GROUP-USAGE NATIONAL</c> group is category
/// national. Both are read through <see cref="Table16Operand.Of(DataItem)"/>, the compiler's ONE classifier of an
/// item's category, which already resolves a bit / national group's as-if PICTURE (§13.18.29.4 GR1b/GR2b) and
/// answers <see cref="PicCategory.Group"/> for the plain group. Never a second walk of <c>Pic</c> /
/// <c>AsIfPic</c> / <c>GroupUsage</c>.</para>
/// <para>⚠ WHAT THIS SCREEN DELIBERATELY DOES NOT SPLIT, said out loud: <see cref="PicCategory"/> has no
/// <c>AlphanumericEdited</c> or <c>NationalEdited</c> member — an alphanumeric-edited item carries category
/// <see cref="PicCategory.Alphanumeric"/> and a national-edited one category <see cref="PicCategory.National"/>,
/// each distinguished only by <see cref="PicInfo.EditMask"/> (data-model design D-N6; kb/Work PB492) — so a
/// screen written here admits BOTH edited categories where the standard's category list names only the plain
/// ones. Since PB492 that is a CHOICE rather than a blindness (<c>PicInfo.IsCharacterEdited</c> would decide it),
/// and the choice is to keep erring toward accepting legal source. That is the established posture for a
/// category screen in this compiler
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
    /// name for a record key, and the elementary half of §14.9.30.3 SR1 b) / §14.9.34.3 SR2 b)'s INTO-receiver
    /// set. A <c>GROUP-USAGE NATIONAL</c> group qualifies through its as-if <c>PIC N(m)</c> — §13.18.29.4 GR2 b)
    /// makes a national group "treated as though it were an elementary data item of usage national … described
    /// with PICTURE N(m)", which is exactly the shape those rules name; a <c>GROUP-USAGE BIT</c> group does not
    /// (GR1 makes it category boolean).</summary>
    public static bool IsAlphanumericOrNational(DataItem item) => Admits(item, national: true);

    /// <summary>
    /// ⛔ ISO §13.18.29.4 GR3 — <i>"If a GROUP-USAGE clause is not specified or implied for a group item that is
    /// not strongly typed and is not a variable-length group, that group item is an alphanumeric group item."</i>
    /// THE ONE spelling of that definition; the rules worded <i>"an alphanumeric group item"</i> ask it here.
    /// <para>⛔ ALL THREE conjuncts, and the third and fourth are why this is not inlined anywhere. The copy
    /// inside <see cref="Admits"/> carried only the structural half (<c>item.IsGroup</c>) and therefore answered
    /// TRUE for a strongly-typed group and for a variable-length group — two shapes GR3 excludes by name — so
    /// every rule reading this predicate under-rejected them (kb/Work PB337: the DYNAMIC LENGTH <c>LIMIT</c>
    /// operand of §12.4.5.2 SR7 and the record keys of §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 each admitted a strong
    /// group). A second, COMPLETE copy lived privately in <c>DataBinder.GroupValue</c> for §13.18.63.3 SR14;
    /// ONE RULE WRITTEN DOWN IN TWO PLACES had already drifted, which is the whole reason it now lives here.</para>
    /// <para><c>IsGroup</c> (no PICTURE **and** it has subordinates) is the structural half no classifier
    /// carries: a PICTURE-less entry with no subordinates is not a group item and has no category at all, and
    /// admitting it would let a rule about data items answer about something that is not one.</para>
    /// </summary>
    public static bool IsAlphanumericGroup(DataItem item) =>
        item.IsGroup
        && item.GroupUsage is GroupUsage.None
        && !StrongTypeModel.IsStronglyTyped(item)
        && !VariableLengthCompatibility.IsVariableLength(item);

    /// <summary>
    /// ⛔ THE ONE READER of the USAGE a data item OPERATES with — the axis every rule worded <i>"a data item
    /// with usage display or usage national"</i> asks about (ISO §14.9.18.3 SR6 / §14.9.42.3 SR2, the
    /// termination-status operand; §8.4.3.3.4 GR6's unique data item; §14.9.41.3 SR6 b) 2.'s key comparison).
    /// <para>⛔ THE GROUP ANSWER IS §8.5.2.1's OWN SENTENCE — <i>"An alphanumeric group item is treated as
    /// though it had a usage of display."</i> Every site that instead asked <see cref="DataItem.OperandPic"/>
    /// alone read a NULL for such a group and concluded "no usage", which is not what the standard says: the
    /// group HAS a usage, and it is display. That conclusion was a measured rejects-legal-source defect at the
    /// status operand of both STOP RUN and GOBACK (kb/Work PB411), and a latent one at
    /// <c>SendingValueTemp.UsageOf</c>, where a reference-modified national or bit group's §14.9.25.4 GR1
    /// intermediate was built with category NATIONAL / BOOLEAN beside usage DISPLAY — one item, two usages.
    /// (Probed: the intermediate's VALUE is unchanged by the correction, so that half is an inconsistency
    /// removed rather than a wrong answer fixed — said plainly rather than claimed as a second repro.)</para>
    /// <para>⚠ §8.5.2.1's sentence is about an ALPHANUMERIC GROUP ITEM, which §3.11 defines by exclusion —
    /// <i>"group item except for a bit group item, a national group item, a strongly-typed group item, or a
    /// variable-length group item"</i> — so it is asked through <see cref="IsAlphanumericGroup"/>, the ONE
    /// spelling of that definition, never through <c>IsGroup</c>. A bit / national group answers from its
    /// §13.18.29.4 GR1b/GR2b as-if PICTURE (usage bit / usage national) like any elementary item. A STRONGLY-TYPED
    /// group and a VARIABLE-LENGTH group answer <see langword="null"/>: the standard states a usage for neither
    /// (§8.5.2.1 gives the first its type-name as class and category and no usage at all, and §8.5.1.12.1 says
    /// the second "is not equivalent to an alphanumeric data item"), and a caller that must have an answer says
    /// so at its own site rather than having one invented here.</para>
    /// </summary>
    public static Usage? UsageOf(DataItem item) =>
        item.OperandPic?.Usage ?? (IsAlphanumericGroup(item) ? Usage.Display : null);

    /// <summary>ONE walk for both predicates — the two rules differ only in whether category national is in the
    /// admitted set, and writing the walk twice is how the alphabetic and group arms would come to disagree.</summary>
    private static bool Admits(DataItem item, bool national) => Table16Operand.Of(item) switch
    {
        // §13.18.29.4 GR3's group arm, in its ONE spelling — never `item.IsGroup` alone (see IsAlphanumericGroup).
        { Category: PicCategory.Group } => IsAlphanumericGroup(item),
        { Category: PicCategory.Alphanumeric, IsAlphabetic: false } => true,
        { Category: PicCategory.National } => national,
        _ => false,
    };

    /// <summary>
    /// ⛔ CLASS index, message-tag, object or pointer (ISO §8.5.2) — the four-class set that THREE rules name
    /// with word-for-word identical text, and which therefore gets ONE predicate:
    /// <list type="bullet">
    /// <item>§13.16.3 SR24 e) — <i>"A data item of the class index, message-tag, object, or pointer"</i> may not
    /// be a conditional variable;</item>
    /// <item>§13.18.60.3 SR11 — <i>"An elementary data item of class index, message-tag, object, or pointer shall
    /// not be a conditional variable"</i>, the same prohibition stated the other way round;</item>
    /// <item>§14.7.6 rule 4 — <i>"Neither data item contains an OCCURS, REDEFINES, or RENAMES clause or is of
    /// class index, message-tag, object, or pointer"</i>, the CORRESPONDING-phrase exclusion (kb/Work PB391,
    /// which found the third asker holding a private one-usage copy: <c>Pic?.Usage is not Usage.Index</c>, so a
    /// POINTER namesake pair was excluded only by the accident of a private Table-16 copy's
    /// <c>_ =&gt; false</c> default, and deleting that copy would have turned the accident into a silent
    /// pointer copy).</item>
    /// </list>
    /// <para>⛔ IT IS §13.18.60.3 SR4'S POPULATION, resolved through SR4's own phrase reader rather than a
    /// hand-written class list. SR4 names "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER,
    /// and PROGRAM-POINTER phrases" — exactly the six USAGE phrases that produce exactly these four classes
    /// (§8.5.2) — and <see cref="Sr4PhraseOf"/> states that list as <see cref="Sr14PhraseOf"/>'s five plus
    /// INDEX. A second copy would drift the moment MESSAGE-TAG or FUNCTION-POINTER gains a bound model;
    /// <c>ConditionNameAssociationDriftTests</c> asserts the two populations are the same set.</para>
    /// <para>The WRITTEN clause is read as well as the resolved one, and both arms are load-bearing: a
    /// MESSAGE-TAG entry is refused non-support by <c>PictureAnalyzer.ParseUsage</c> (COBOLNET1943) and a
    /// FUNCTION-POINTER entry is staged there, so neither gains a <see cref="PicInfo"/> at all and only
    /// <see cref="DataItem.OwnUsage"/> sees them — while a usage acquired by §13.18.60.4 GR1 inheritance, a TYPE
    /// clone or a SAME AS copy writes no clause of its own and is visible only in the resolved
    /// <see cref="DataItem.Pic"/>.</para></summary>
    public static bool IsIndexMessageTagObjectOrPointer(DataItem item) =>
        Sr4PhraseOf(item.OwnUsage) is not null || Sr4PhraseOf(item.Pic?.Usage) is not null;

    /// <summary>Which of ISO §13.18.60.3 SR14's phrases a <see cref="DataItem.OwnUsage"/> names, or null.
    /// FUNCTION-POINTER is included: <c>PictureAnalyzer.ParseUsage</c> stages it loud (the P13 prototype band)
    /// so its <c>Pic</c> stays null and a resolved-usage arm never sees it, but the written clause is still
    /// visible here and the rule governs it. MESSAGE-TAG (kb/Work PB487) is the SECOND member in that position,
    /// for the same reason by a different route — the usage is declined non-support and refused by name.
    /// <para>SR14: "A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, or
    /// PROGRAM-POINTER phrase may be specified only for an elementary data item at level 1 or an elementary data
    /// item subordinate to a type declaration that includes the STRONG phrase."</para></summary>
    public static string? Sr14PhraseOf(Usage? u) => u switch
    {
        // MESSAGE-TAG is the FIRST phrase SR14 names. Its arm lands here rather than staying a forward
        // obligation because kb/Work PB487 gave the model a Usage member for it — the usage is DECLINED
        // non-support and refused by name (COBOLNET1943), so like FUNCTION-POINTER it is the WRITTEN
        // clause, never a bound class, that this screen sees.
        Usage.MessageTag => "MESSAGE-TAG",
        Usage.Pointer => "POINTER",
        Usage.ProgramPointer => "PROGRAM-POINTER",
        Usage.FunctionPointer => "FUNCTION-POINTER",
        Usage.ObjectReference => "OBJECT REFERENCE",
        _ => null,
    };

    /// <summary>ISO §13.18.60.3 SR4's list stated as what it IS — <see cref="Sr14PhraseOf"/>'s five phrases PLUS
    /// INDEX. Written as a union rather than a second hand-copied list so the one-phrase difference between the
    /// two rules is the code's own structure and the drift test can assert it directly
    /// (<c>Sr4 \ Sr14 == {INDEX}</c>). An index data item's <see cref="PicInfo"/> is
    /// <c>(Numeric, Usage.Index)</c> — the class is numeric, so the usage, not the category, identifies it.
    /// <para>SR4: "The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, and PROGRAM-POINTER
    /// phrases shall not be specified in a data item described with the CONSTANT RECORD clause, or in any item
    /// subordinate to a data item described with the CONSTANT RECORD clause."</para></summary>
    public static string? Sr4PhraseOf(Usage? u) => u is Usage.Index ? "INDEX" : Sr14PhraseOf(u);

    /// <summary>A short English face for a diagnostic — WHAT THE OPERAND IS, so a message names the reason
    /// instead of restating the rule it broke.
    /// <para>⛔ The GROUP arms are ordered the way <see cref="IsAlphanumericGroup"/> refuses, and the two
    /// §13.18.29.4 GR3 exclusions are named OUT LOUD: before kb/Work PB337 a PICTURE-less item with
    /// <c>GROUP-USAGE NONE</c> fell straight to <i>"not an elementary or group data item"</i>, which is false of
    /// every plain group — harmless only while no group could fail the predicate, and wrong the moment the
    /// strongly-typed and variable-length exclusions began to bite.</para></summary>
    public static string Face(DataItem item) =>
        item.Pic is { } pic
            ? pic.IsAlphabetic ? "a category-alphabetic item" : $"a category-{pic.Category.ToString().ToLowerInvariant()} item"
        : item.GroupUsage is not GroupUsage.None ? $"a {item.GroupUsage.ToString().ToLowerInvariant()} group item"
        : !item.IsGroup ? "not an elementary or group data item"
        : StrongTypeModel.IsStronglyTyped(item)
            ? "a strongly-typed group item (ISO §8.5.3.3), which §13.18.29.4 GR3 excludes from the alphanumeric "
              + "group items"
        : VariableLengthCompatibility.IsVariableLength(item)
            ? "a variable-length group (ISO §8.5.1.12.1), which §13.18.29.4 GR3 excludes from the alphanumeric "
              + "group items"
        : "an alphanumeric group item";
}
