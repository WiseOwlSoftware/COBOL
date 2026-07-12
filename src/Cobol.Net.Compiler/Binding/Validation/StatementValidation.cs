// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

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

    // ── MOVE (ISO §14.9.25.3) — lifted at 10e ────────────────────────────────────────────────────────────────

    /// <summary>ISO §14.9.25.3 SR2 (data-model D17): if a receiving operand is a strongly-typed group, the sending
    /// operand shall be a group item of the SAME type (§8.5.3.3 — a strong record accepts only a same-type whole-record
    /// source; its individual fields are still set by ordinary field MOVEs, and a strong-type SENDER to a non-strong
    /// receiver is permitted per Table 16). A mismatch → COBOLNET1533.</summary>
    public bool CheckStrongMove(BoundOperand source, IReadOnlyList<Place> receivers)
    {
        bool ok = true;
        DataItem? sender = source is BoundFieldOperand sf ? sf.Place.Item : null;
        foreach (var r in receivers)
        {
            if (!StrongTypeModel.IsStrongGroup(r.Item)) continue;
            if (sender is null || !StrongTypeModel.SameStrongType(sender, r.Item))
            {
                ok = false;
                data.Edition.Error(DiagnosticCatalog.StrongMoveMismatch, "MOVE to strongly-typed group "
                    + $"'{r.Item.CobolName ?? r.Item.CsName}': the sending operand shall be a group item of the same "
                    + "type (ISO §14.9.25.3 SR2 / §8.5.3.3)");
            }
        }
        return ok;
    }


    // ── Arithmetic composite of operands (ISO §14.7 rule 2) — lifted at 10e ──────────────────────────────────

    /// <summary>The per-edition COMPOSITE-OF-OPERANDS check (ISO §14.7 rule 2, NATIVE arithmetic, the four
    /// arithmetic statements ONLY — COMPUTE expressions are explicitly exempt, §8.8.1.2 r7): the hypothetical item
    /// superimposing the statement's fixed-point operands aligned on their decimal points shall not exceed the
    /// edition's digit cap (18 at COBOL-85; the 2023 text says 31). Float/binary-native operands are excluded
    /// (rule 2b — the composite is then over the remaining operands).</summary>
    public bool CheckComposite(string verb, IEnumerable<BoundExpr> operands, IEnumerable<Receiver> receivers)
    {
        if (data.Options.Arithmetic != ArithmeticMode.Native) return true;   // §14.7 r2 applies to native only
        int maxInt = 0, maxFrac = 0;
        void Shape(int digits, int scale)
        {
            maxInt = Math.Max(maxInt, digits - scale);   // a negative (P-scaled) scale ADDS integer positions
            maxFrac = Math.Max(maxFrac, Math.Max(0, scale));
        }
        void OfExpr(BoundExpr e)
        {
            switch (e)
            {
                case BoundNumRef { Place.Item.Pic: { Category: PicCategory.Numeric, IsFloat: false } p }:
                    Shape(p.Digits, p.Scale);
                    break;
                case BoundNumLiteral lit:
                    string t = lit.Text.TrimStart('+', '-');
                    int dot = t.IndexOf('.');
                    Shape(t.Count(char.IsAsciiDigit), dot < 0 ? 0 : t.Length - dot - 1);
                    break;
            }
        }
        foreach (var e in operands) OfExpr(e);
        foreach (var r in receivers)
            if (r.Place.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } rp)
                Shape(rp.Digits, rp.Scale);

        // The cap is 31 at EVERY edition (ISO §14.7 rule 2a — the 2023 text). A COBOL-85-specific tightening to
        // 18 was considered and REFUTED by the conformance corpus itself: CCVS-85 NC101A multiplies 9(3)V9(3) by
        // 9(18) (composite 21) as a deliberate SIZE ERROR test, and every conforming '85 implementation accepts
        // it — so the 18-digit figure does not govern the composite (it caps '85 PICTURE/literal capacity only).
        int composite = maxInt + maxFrac;
        if (composite <= 31) return true;
        data.Edition.Error("COBOLNET0805",
            $"{verb}: the composite of operands spans {composite} digits ({maxInt} integer + {maxFrac} fraction); "
            + "ISO/IEC 1989 caps the composite of operands at 31 digits (§14.7 rule 2)");
        return false;
    }

    // ── INITIALIZE (ISO §14.9.20.3) — lifted at 10e ──────────────────────────────────────────────────────────

    /// <summary>SR6 — the same category shall not be repeated in a REPLACING phrase.</summary>
    public bool CheckInitializeReplacingUnique(
        IReadOnlyList<(InitializeCategory Cat, BoundOperand Value)> existing, InitializeCategory cat)
    {
        if (!existing.Any(r => r.Cat == cat)) return true;
        data.Edition.Error("COBOLNET0834",
            $"INITIALIZE REPLACING repeats category {cat} (ISO §14.9.20.3 SR6 — each category at most once)");
        return false;
    }

    /// <summary>SR5 — identifier-1 shall not have a RENAMES clause (a level-66 entry).</summary>
    public bool CheckInitializeTargetRenames(string name, IReadOnlyList<DataItem> named)
    {
        if (!named.Any(i => i.Renames is not null)) return true;
        data.Edition.Error("COBOLNET0835",
            $"INITIALIZE '{name}' — identifier-1 shall not have a RENAMES clause (ISO §14.9.20.3 SR5)");
        return false;
    }

    // ── Sequential file I/O (ISO §14.9.27 / §14.9.51) — lifted at 10h ───────────────────────────────────────

    /// <summary>§14.9.27 SR8 — OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause.</summary>
    public bool CheckOpenSharingAllOther(FileModel file, SharingMode? effectiveSharing)
    {
        if (!(effectiveSharing is SharingMode.AllOther && file.LockMode is null)) return true;
        data.Edition.Error("COBOLNET1512", $"OPEN of file '{file.CobolName}' with SHARING WITH ALL OTHER "
            + "requires the file to have a LOCK MODE clause (ISO §14.9.27 SR8)");
        return false;
    }

    /// <summary>§14.9.51 SR19 (the silent-drop bug class) — the END-OF-PAGE / NOT END-OF-PAGE phrase requires
    /// a LINAGE clause in the file's file description entry.</summary>
    public bool CheckWriteEopLinage(FileModel file)
    {
        if (file.Linage is not null) return true;
        data.Edition.Error("COBOLNET0860", $"WRITE … END-OF-PAGE on file '{file.CobolName}', whose file "
            + "description entry has no LINAGE clause (ISO §14.9.51 SR19)");
        return false;
    }

    /// <summary>§14.9.51 SR18 — ADVANCING PAGE and END-OF-PAGE shall not both be specified in one WRITE.</summary>
    public bool CheckWriteEopAdvancingPage(bool advancingPage)
    {
        if (!advancingPage) return true;
        data.Edition.Error("COBOLNET0861", "WRITE … ADVANCING PAGE with an END-OF-PAGE phrase: the two "
            + "shall not both be specified in a single WRITE statement (ISO §14.9.51 SR18)");
        return false;
    }

    /// <summary>§14.9.51 SR13 — with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES
    /// mnemonic (the caller resolves the mnemonic test through the per-unit registry).</summary>
    public bool CheckWriteAdvancingMnemonic(FileModel file, bool advancingNamesMnemonic)
    {
        if (!(file.Linage is not null && advancingNamesMnemonic)) return true;
        data.Edition.Error("COBOLNET0862", $"WRITE … ADVANCING mnemonic-name on file '{file.CobolName}', whose "
            + "file description entry contains a LINAGE clause (ISO §14.9.51 SR13)");
        return false;
    }

    // ── The relational-operand SR checkpoint (ISO §8.8.4.2.2 / §8.8.4.2.3; lifted from ConditionBinder's
    //    CheckedRelational at P7 Step 10t/3 — the 10o deviation-(b) pure-lift discharged). ────────────────────

    /// <summary>The edition-invariant SR checks that ride the ONE <c>BoundRelational</c> checkpoint — reached
    /// by every relation (IF, EVALUATE pairings/ranges, PERFORM UNTIL, SEARCH WHEN, sole-operand conditions).
    /// A PURE emission check (no verdict — the caller always builds the node; the checks are side-effect
    /// diagnostics): class-boolean comparability (§8.8.4.2.2 Format 2 — boolean operands compare only with a
    /// boolean or the figurative ZERO, equality only — COBOLNET0844), and the strongly-typed-group rule
    /// (§8.8.4.2.3 SR1: same type both sides; SR4: a strong group with a boolean/object/pointer leaf is
    /// equality-only — COBOLNET1535, data-model D17 residue).</summary>
    public void CheckRelationalOperands(BoundOperand left, string op, BoundOperand right)
    {
        static bool IsBoolOperand(BoundOperand o) => o switch
        {
            BoundBoolOperand => true,   // a boolean EXPRESSION (B-op tier, increment 2)
            BoundStringLiteral { Category: PicCategory.Boolean } => true,
            BoundAllLiteral { Category: PicCategory.Boolean } => true,
            BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category is PicCategory.Boolean,
            BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Boolean,
            _ => false,
        };
        bool lb = IsBoolOperand(left), rb = IsBoolOperand(right);
        if (lb || rb)
        {
            static bool BoolCompatible(BoundOperand o) =>
                o is BoundFigurative { Kind: 'Z' } || o switch
                {
                    BoundBoolOperand => true,
                    BoundStringLiteral { Category: PicCategory.Boolean } => true,
                    BoundAllLiteral { Category: PicCategory.Boolean } => true,
                    BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category is PicCategory.Boolean,
                    BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Boolean,
                    _ => false,
                };
            if (!(BoolCompatible(left) && BoolCompatible(right)))
                data.Edition.Error("COBOLNET0844", "a boolean operand may be compared only with another "
                    + "boolean operand or the figurative constant ZERO (ISO §8.8.4.2.2; §8.8.4.2.1 F1 "
                    + "SR2/SR3 exclude class boolean from the general relation)");
            else if (op is not ("==" or "!="))
                data.Edition.Error("COBOLNET0844", "boolean operands compare for equality only — an ordering "
                    + "relation is not defined for class boolean (ISO §8.8.4.2.2 Format 2)");
        }
        // §8.8.4.2.3 SR1 (data-model D17): if either operand is a strongly-typed group, both shall be of the same
        // type (§8.5.3.3). This is the ONE relation checkpoint, so it also covers EVALUATE pairings/ranges,
        // PERFORM UNTIL, and SEARCH WHEN. (SR4 — a strong group with boolean/object/pointer elements admits only
        // equality — is staged residue, inc 4.)
        DataItem? sl = left is BoundFieldOperand fl ? fl.Place.Item : null;
        DataItem? sr = right is BoundFieldOperand fr ? fr.Place.Item : null;
        if ((sl is { } && StrongTypeModel.IsStrongGroup(sl)) || (sr is { } && StrongTypeModel.IsStrongGroup(sr)))
        {
            if (sl is null || sr is null || !StrongTypeModel.SameStrongType(sl, sr))
                data.Edition.Error(DiagnosticCatalog.StrongCompareMismatch, "a strongly-typed group may be compared only with a group of the "
                    + "same type (ISO §8.8.4.2.3 SR1 / §8.5.3.3)");
            // §8.8.4.2.3 SR4 (D17 inc 4, staged loud): a strong group whose elements include class boolean,
            // object-reference, or pointer may be compared only for equality — an ordering relation on such a group
            // is not defined/implemented.
            else if (op is not ("==" or "!=") && (ContainsNonOrderableLeaf(sl) || ContainsNonOrderableLeaf(sr)))
                data.Edition.Error("COBOLNET1535", "a strongly-typed group containing a boolean, object-reference, "
                    + "or pointer element may be compared only for equality (ISO §8.8.4.2.3 SR4) — an ordering "
                    + "relation is not implemented (data-model D17 residue)");
        }
    }

    /// <summary>True when a group (or elementary) item has any leaf of class boolean / object-reference / pointer —
    /// the categories that make a strongly-typed group comparable only for equality (ISO §8.8.4.2.3 SR4).</summary>
    private static bool ContainsNonOrderableLeaf(DataItem item)
    {
        if (item.IsElementary)
            return item.Pic?.Category is PicCategory.Boolean or PicCategory.ObjectReference or PicCategory.Pointer;
        foreach (var c in item.Children)
            if (ContainsNonOrderableLeaf(c)) return true;
        return false;
    }
}
