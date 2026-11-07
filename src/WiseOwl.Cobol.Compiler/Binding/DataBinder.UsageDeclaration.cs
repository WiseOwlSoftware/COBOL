// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// THE §2023:13.18.60.3 USAGE DECLARATION-PLACEMENT screen (kb/Work PB183) — the syntax rules of the USAGE clause
/// that restrict WHERE a usage phrase may be written, as opposed to what a PICTURE may accompany it (SR3 / SR5 /
/// SR12 / SR13 / SR20, enforced per entry in <c>PictureAnalyzer</c>) or what may REFERENCE the resulting item
/// (SR10, enforced in <c>ExpressionBinder</c>). Partial-class extension over <c>DataBinder</c>, run as the
/// declared <c>CheckUsageDeclarations</c> pass (see <c>BindPipeline</c>).
///
/// <para><b>Why this exists as its own pass.</b> Three rules of one clause, all with the same subject — a
/// WRITTEN data description entry — and not one of them was written down anywhere in the compiler. Measured on
/// 41bf3492, every one of these compiled and ran:</para>
/// <list type="bullet">
///   <item><c>01 G. 05 P USAGE POINTER.</c> — a pointer member of an ORDINARY group, SR14's headline case.</item>
///   <item><c>01 G USAGE POINTER. 05 A PIC X. 05 B PIC X.</c> — the same violation acquired through
///     §2023:13.18.60.4 GR1, which applies a group's usage "only to each elementary item in the group".</item>
///   <item><c>01 W IS TYPEDEF. 05 WP USAGE POINTER.</c> — a WEAK typedef template; SR14 permits the subordinate
///     form only under a type declaration that includes the STRONG phrase.</item>
///   <item><c>01 P USAGE POINTER. 01 G. 05 Q SAME AS P.</c> — the same level-05 pointer manufactured by a
///     fourth acquisition route.</item>
///   <item><c>01 PT IS TYPEDEF USAGE POINTER. 01 G. 05 A TYPE PT.</c> — the fifth, the TYPE twin of SAME AS
///     (§2023:13.18.57.4 GR1 and §2023:13.18.49.4 GR1 state the same "as though … coded in place" effect). The template is
///     a legal level-1 entry, so only the SITE breaks the rule; it compiled clean until kb/Work PB3129, and its
///     group then failed at a CALL or a prototype pair with a carrier diagnostic (COBOL1688 / COBOL1513).</item>
/// </list>
///
/// <para><b>⛔ The rule was verified against the PRINTED page before the screen was written.</b> SR14's reading
/// is restrictive enough to be exactly the shape the falsely-restrictive-OCR hazard produces, so PDF page 535
/// (printed folio 505) was rendered at 300dpi and read as an image: the printed SR14 is character-for-character
/// the transcription at <c>specs/ISO_COBOL_2023.md</c>. There is no OCR loss here and the rule really is this
/// restrictive.</para>
///
/// <para><b>⛔ SR14 OMITS INDEX; the neighbouring SR4 INCLUDES it.</b> Six usages there, five here — deliberate
/// drafting, not an oversight. <c>05 IX USAGE INDEX.</c> inside an ordinary group is LEGAL COBOL and stays legal;
/// <see cref="Sr14PlacementClass"/> and <see cref="CheckUsageDeclarationsPass.Sr4ConstantRecordUsage"/> are therefore two predicates and
/// must never be "unified". A positive golden (<c>pb183_usage_index_in_group_ok</c>) and a unit drift test are
/// what keep that true, because over-rejection — not under-rejection — is this screen's real hazard.</para>
///
/// <para><b>Level 77 satisfies SR14's first arm — a determination, derived, not assumed.</b> The rule says "at
/// level 1" and the standard elsewhere writes "level 1 or level 77" (§2023:8.5.1.6.3 alignment) and "a level 01 entry
/// or a level 77 entry" (§2023:14.9.4.3 SR1) when it means both, which is a real argument the other way. It loses to
/// two texts. §2023:8.5.1.3.2: "Three types of entries exist for which there is no true concept of level", the second
/// being "entries that specify noncontiguous working-storage, local storage, and linkage data items" — so a 77
/// entry is not AT some level greater than 1; it is outside the level system, and SR14's arm cannot exclude it
/// by level arithmetic. And §2023:13.11.1 declares the two spellings ALTERNATIVES for one thing: "Data elements that
/// bear no hierarchical relationship to any other data item may be described as records that are single
/// elementary items. Alternatively, such data elements ... may be described as separate data description entries
/// having level-number 77." SR14's evident subject is SUBORDINATION to a non-strong group; a 77 entry is defined
/// by having no such relationship. The surveyed implementations agree — GnuCOBOL's testsuite uses
/// <c>77 ptr USAGE POINTER.</c> throughout. The test is <see cref="CheckUsageDeclarationsPass.Sr14PermittedLevel"/>, ONE named predicate,
/// so the determination is a one-line change if the owner reads the letter the other way.</para>
///
/// <para><b>A syntax rule is a "shall", so the conforming response is a compile-time diagnostic</b> — the same
/// posture as SR14's drafting twin §2023:13.18.57.3 SR6 (COBOL1532, a plain <c>Edition.Error</c> with no
/// permissive escape). It is NOT dialect-gated: <c>--permissive</c> is the migration mode for constructs an
/// edition REMOVED, which this is not. GnuCOBOL accepts the shape, but the follow-GnuCOBOL rule covers
/// implementor-defined LATITUDE, and a syntax rule is not latitude.</para>
///
/// <para><b>Edition axis.</b> The MESSAGE-TAG phrase is a 2023 addition (VCR row 32) with a <c>Usage</c> member
/// (<c>Usage.MessageTag</c>, kb/Work PB487), so SR14's message-tag arm and SR21 (MESSAGE-TAG exclusivity) are
/// live; below 2023 the entry draws the COBOL0900 introduction gate (usage-message-tag-2023, kb/Work PB916) and
/// these rules are reported beside it, as COBOL1958 is beside FUNCTION-POINTER's. For POINTER / OBJECT REFERENCE / PROGRAM-POINTER / FUNCTION-POINTER
/// Annex E carries no note on §2023:13.18.60.3 — but Annex E's scope is 2014→2023 only, so its silence is a MISSING
/// observation, not evidence of no change, and these arms ship UNGATED (the un-narrowed reading) pending the ISO
/// 2002/2014 texts. Ungated is the safe direction here: it enforces the rule the compiler can actually cite.</para>
/// </summary>
public sealed partial class DataBinder
{

    /// <summary>"Subordinate to a TYPE DECLARATION that includes the STRONG phrase" — SR14's second arm, and
    /// §2023:13.16.3 SR24 g)'s second arm, which are the same words and so are ONE predicate (kb/Work PB488 made it
    /// the second caller and dropped the <c>Sr14</c> prefix; <c>DataBinder.ConditionName.cs</c>).
    /// ⛔ The test is the DECLARATION-side <see cref="DataItem.TypedefStrong"/> on an enclosing TYPEDEF
    /// template root, NOT the post-expansion <c>StrongTypeModel.StrongRoot</c> / <c>DataItem.StrongType</c>:
    /// both rules say "subordinate to a type declaration that includes the STRONG phrase", and screening the
    /// TEMPLATE — which <see cref="ConformanceForest"/> visits once and whose clones it prunes — is what makes
    /// this a once-per-source verdict anchored at the entry the programmer wrote. Reading the post-expansion
    /// flag instead would let a WEAK typedef's pointer member escape at the template and then fire once per
    /// TYPE reference site: wrong site, wrong count, and a diagnostic naming a line that is not the defect.
    /// </summary>
    internal static bool UnderStrongTypeDeclaration(DataItem item)
    {
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.DeclaresStrongType) return true;
        return false;
    }
}
