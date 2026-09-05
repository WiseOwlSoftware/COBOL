// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.CodeGen.Emit;

/// <summary>
/// ⛔ <b>ISO §14.6.13.2's EXEMPTION TABLE, AS A STRUCTURE.</b> The clause states five sibling conditions over one
/// subject — the content of a sending operand that is not valid — and each carries its OWN list of contexts in
/// which the reference is exempt from raising. This value names the context ONCE at the reference site; every
/// rule then reads its own list off it.
///
/// <para><b>Why this is not a boolean.</b> It was one (<c>floatCheck</c> / <c>floatSendingExempt</c>), and a
/// boolean can carry exactly ONE rule's list — so it carried rule 3's, and rule 2 could not be implemented at all
/// without either inheriting the wrong exemptions or opening a second parallel flag that would drift from the
/// first (kb/Work PB230). The lists genuinely differ:</para>
/// <list type="bullet">
/// <item><b>Rule 2</b> (a fixed-point numeric sending item that would evaluate false in a numeric class condition
/// → EC-DATA-INCOMPATIBLE) exempts <b>two</b> contexts: "a sending item is referenced in a class condition, or …
/// a sending item is processed in a VALIDATE statement".</item>
/// <item><b>Rule 3</b> (a standard-float sending operand that is ±Inf/NaN → EC-DATA-NOT-FINITE) exempts
/// <b>four</b>: those two, plus "a sending item is referenced in a sign condition" and "the sending and receiving
/// items in a MOVE statement are defined with the same standard floating-point usage specification".</item>
/// </list>
/// <para>So a SIGN condition and a same-usage MOVE are exempt for a float operand and <b>NOT</b> for a
/// fixed-point one — a distinction no single flag can express, and the exact distinction the standard draws
/// deliberately: §8.8.4.7.4 GR2 gives a float sign test a well-defined answer for NaN (it reads the IEEE sign
/// bit), and §14.9.25.4 GR6c makes a same-usage MOVE a verbatim transfer with no conversion. Fixed-point has
/// neither special rule, so neither exemption.</para>
/// <para>Rule 1 (a boolean sending item) shares rule 2's two-entry list; when it is wired it reads
/// <see cref="FixedPointChecked"/>'s sibling off this same value rather than growing a third flag.</para>
/// </summary>
internal enum SendingRef
{
    /// <summary>An ordinary sending reference — no exemption applies and every rule's raise is emitted.</summary>
    Normal = 0,

    /// <summary>The operand of a CLASS condition (§8.8.4.4). Exempt from rules 1, 2 AND 3 — the first dash of
    /// each list. The class test inspects the content precisely in order to CATEGORIZE it, so raising on the
    /// very content it was asked to report would make the test unable to answer.</summary>
    ClassCondition,

    /// <summary>The operand of a SIGN condition (§8.8.4.7). Exempt from rule 3 ONLY (its second dash).</summary>
    SignCondition,

    /// <summary>The source of a MOVE whose sending and receiving items share one standard floating-point usage
    /// specification, endianness aside (§14.9.25.4 GR6c — a verbatim transfer). Exempt from rule 3 ONLY (its
    /// third dash); it is unreachable for a fixed-point sender, which never takes this path.</summary>
    SameUsageMove,

    /// <summary>An operand processed by a VALIDATE statement (§14.9.47). Exempt from rules 1, 2 AND 3 — the last
    /// dash of each list; §14.6.13.2 rule 1 instead sets the condition "when invalid data is detected during item
    /// identification", which is VALIDATE's own stage discipline (§14.9.47.4 GR6).</summary>
    Validate,
}

/// <summary>The per-rule readings of <see cref="SendingRef"/> — each one IS its rule's exemption list, written
/// once, next to the other. A rule added here states its list beside these two rather than growing a flag.</summary>
internal static class SendingRefRules
{
    /// <summary>§14.6.13.2 <b>rule 2</b>: emit the EC-DATA-INCOMPATIBLE checked read of a FIXED-POINT numeric
    /// sending operand? Exempt in a class condition and in VALIDATE — and in nothing else.</summary>
    public static bool FixedPointChecked(this SendingRef r) =>
        r is not (SendingRef.ClassCondition or SendingRef.Validate);

    /// <summary>§14.6.13.2 <b>rule 3</b>: emit the EC-DATA-NOT-FINITE checked read of a STANDARD-FLOAT sending
    /// operand? Exempt in a class condition, a sign condition, a same-usage MOVE and VALIDATE — i.e. everywhere
    /// but an ordinary reference.</summary>
    public static bool FloatChecked(this SendingRef r) => r is SendingRef.Normal;
}
