// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Validation;

/// <summary>
/// The edition-INVARIANT syntax-rule check catalog lifted out of the verb binders (P7 Step 10; the phase
/// doc's §Step 10). The contract (fixed at 10c, the AS-BUILT PLAN's convention): every <c>Check*</c> is a
/// PURE check — it reports to the ONE sink (<c>data.Edition</c>) with byte-identical message text and
/// returns the verdict (<c>true</c> = conformant); the CALLING verb binder owns all control flow (error
/// placeholders, statement aborts, operand rewrites). Edition GATING never lives here — the
/// <c>VersionConformancePass</c> is the sole edition funnel (DESIGN-version-conformance-pipeline); the
/// residual inline gates move VERBATIM with their verb binders until Exec Step E folds them.
/// </summary>
internal sealed class StatementValidation(DataBinder data)
{
    // ── INSPECT (ISO §14.9.22.3) — lifted at 10c ─────────────────────────────────────────────────────────────

    /// <summary>SR5 — a TALLYING counter shall be an elementary numeric data item.</summary>
    public bool CheckInspectTallyCounter(Place counter)
    {
        if (counter.Item.Pic is { Category: PicCategory.Numeric }) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT TALLYING counter '{counter.Item.CobolName}' shall "
            + "be an elementary numeric data item (ISO §14.9.22.3 SR5)");
        return false;
    }

    /// <summary>SR7 — with REPLACING CHARACTERS, literal-3 shall be ONE character (an identifier-5 of another
    /// size is the runtime GR15 case — the runtime uses its first character, deterministic).</summary>
    public bool CheckInspectCharactersReplacement(BoundOperand rep)
    {
        if (rep is not BoundStringLiteral { Value.Length: not 1 } bad) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING CHARACTERS BY a {bad.Value.Length}-"
            + "character literal — literal-3 shall be one character in length (ISO §14.9.22.3 SR7)");
        return false;
    }

    /// <summary>SR6 — non-figurative literal-1 / literal-3 of unequal size is illegal (statically known, so
    /// diagnosed at compile time; the identifier-size mismatch is the runtime GR14 EC case). Called on the
    /// no-figurative-expansion path — the figurative rewrite (bind logic) stays in the binder.</summary>
    public bool CheckInspectReplacingSize(BoundOperand pat, BoundOperand rep, bool figurative)
    {
        if (pat is not BoundStringLiteral lp || rep is not BoundStringLiteral lr || figurative
            || lp.Value.Length == lr.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING: literal '{lp.Value}' and replacement "
            + $"'{lr.Value}' differ in size (ISO §14.9.22.3 SR6 — equal size unless the replacement is figurative)");
        return false;
    }

    /// <summary>SR9 — CONVERTING literal-4 / literal-5 of unequal size (equal size unless literal-5 is
    /// figurative; the figurative expansion stays in the binder).</summary>
    public bool CheckInspectConvertingSize(BoundOperand from, BoundOperand to, bool figurative)
    {
        if (from is not BoundStringLiteral lf || to is not BoundStringLiteral lt || figurative
            || lf.Value.Length == lt.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT CONVERTING: '{lf.Value}' and '{lt.Value}' differ in "
            + "size (ISO §14.9.22.3 SR9 — equal size unless literal-5 is figurative)");
        return false;
    }

    /// <summary>SR2 — an INSPECT identifier operand shall be an elementary usage-display item.</summary>
    public bool CheckInspectOperandUsage(Place p, string refText)
    {
        if (!(p.Item.IsGroup || p.Item.Pic is { Usage: not Usage.Display })) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT operand '{refText}' shall be an elementary "
            + "usage-display item (ISO §14.9.22.3 SR2)");
        return false;
    }
}
