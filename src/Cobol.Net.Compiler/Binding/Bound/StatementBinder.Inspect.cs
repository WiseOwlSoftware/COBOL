// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>An INSPECT TALLYING operand-kind (ISO §14.9.22.2 Format 1: ALL / LEADING / CHARACTERS). The ordinals
/// match the runtime <c>CobolInspect.Tally*</c> constants — the emitter passes them straight through.</summary>
public enum InspectTallyKind { All = 0, Leading = 1, Characters = 2 }

/// <summary>An INSPECT REPLACING operand-kind (ISO §14.9.22.2 Format 2: ALL / FIRST / LEADING / CHARACTERS).
/// Ordinals match the runtime <c>CobolInspect.Replace*</c> constants.</summary>
public enum InspectReplaceKind { All = 0, First = 1, Leading = 2, Characters = 3 }

/// <summary>One flattened TALLYING operand: its counter (identifier-2 — counts ADD into it, §14.9.22.4 GR11),
/// kind, pattern (null for CHARACTERS, whose implied 1-character operand always matches — GR8e), and per-operand
/// BEFORE/AFTER delimiters (GR9). The flattening order across ALL counters of the statement IS the GR8a shared
/// comparison-cycle order.</summary>
public sealed record BoundInspectTally(
    Place Counter, InspectTallyKind Kind, BoundOperand? Pattern, BoundOperand? Before, BoundOperand? After);

/// <summary>One flattened REPLACING operand: kind, pattern (null for CHARACTERS), equal-length replacement
/// (§14.9.22.4 GR14 — a figurative replacement was already expanded to the pattern size at bind time, SR6), and
/// per-operand BEFORE/AFTER delimiters (GR9). Source order = the GR8a shared-cycle order.</summary>
public sealed record BoundInspectReplace(
    InspectReplaceKind Kind, BoundOperand? Pattern, BoundOperand Replacement, BoundOperand? Before, BoundOperand? After);

/// <summary>The CONVERTING phrase (ISO §14.9.22.2 Format 4): the positional from→to character maps (GR20) and the
/// single BEFORE/AFTER region. A figurative <paramref name="To"/> was expanded to <paramref name="From"/>'s size
/// at bind time (SR9/GR22).</summary>
public sealed record BoundInspectConvert(BoundOperand From, BoundOperand To, BoundOperand? Before, BoundOperand? After);

/// <summary>INSPECT (ISO §14.9.22). Formats 1–3 carry the flattened tallying/replacing operand lists; format 4
/// carries <see cref="Converting"/>. A format 3 executes as two successive statements — tallying then replacing —
/// over the same identifier-1 (GR19). <see cref="Backward"/> reverses the scan direction (2023-only, gated at
/// bind time).</summary>
public sealed record BoundInspect(
    Place Target,
    IReadOnlyList<BoundInspectTally> Tallying,
    IReadOnlyList<BoundInspectReplace> Replacing,
    BoundInspectConvert? Converting,
    bool Backward) : BoundStatement;

public sealed partial class StatementBinder
{
    /// <summary>Bind INSPECT (ISO §14.9.22): resolve identifier-1 (SR1 — an alphanumeric group or an elementary
    /// usage-DISPLAY item), flatten the TALLYING/REPLACING operands across all counters in SOURCE order (the GR8a
    /// shared-cycle order), and bind CONVERTING to its from/to maps (GR20). BACKWARD is 2023-only
    /// (VERSION_CHANGE_REFERENCE row 77 / E.3.3 item 34); TRAILING and tallying FIRST are not in any ISO format —
    /// both fail loud rather than silently aliasing to ALL.</summary>
    private BoundStatement BindInspect(Core.InspectStatementContext ins)
    {
        if (refs.Resolve(ins.dataReference()) is not { } target)
            return new BoundUnsupported($"INSPECT of unresolvable item '{ins.dataReference().GetText()}'");
        // SR1: identifier-1 is an alphanumeric/national group or an elementary usage DISPLAY/NATIONAL item — a
        // binary/packed/float/index elementary item has no character image to inspect.
        if (target.Item.Pic is { } tp && tp.Usage is not Usage.Display)
            return new BoundUnsupported(
                $"INSPECT identifier-1 '{target.Item.CobolName}' of USAGE {tp.Usage} (ISO §14.9.22.3 SR1 — usage display or national only)");

        bool backward = ins.BACKWARD() is not null;
        if (backward && data.Edition.DialectLevel < 2023)
            data.Edition.Error("COBOLNET0845", "INSPECT BACKWARD was introduced by ISO/IEC 1989:2023 (§14.9.22.2; "
                + $"version-change reference row 77); it requires --std 2023 (targeting COBOL-{data.Edition.DialectLevel})");

        var tallying = new List<BoundInspectTally>();
        if (ins.inspectTallyingPhrase() is { } tallyPhrase)
            foreach (var item in tallyPhrase.inspectTallyingItem())
            {
                if (refs.Resolve(item.dataReference()) is not { } counter)
                    return new BoundUnsupported($"INSPECT TALLYING counter '{item.dataReference().GetText()}'");
                if (counter.Item.Pic is not { Category: PicCategory.Numeric })
                    data.Edition.Error("COBOLNET0847", $"INSPECT TALLYING counter '{counter.Item.CobolName}' shall "
                        + "be an elementary numeric data item (ISO §14.9.22.3 SR5)");
                foreach (var fc in item.inspectForClause())
                {
                    // GR10: ALL and LEADING are transitive across the bare operands that follow them until the
                    // next adjective. The format requires an adjective on the first operand, so the All seed is
                    // only a lenient default for that (ungrammatical) case.
                    InspectTallyKind last = InspectTallyKind.All;
                    foreach (var cp in fc.inspectCountPhrase())
                    {
                        var (before, after) = InspectDelimiters(cp.inspectDelimiters());
                        if (cp.CHARACTERS() is not null)
                        {
                            tallying.Add(new BoundInspectTally(counter, InspectTallyKind.Characters, null, before, after));
                            continue;
                        }
                        if (cp.FIRST() is not null || cp.TRAILING() is not null)
                            return new BoundUnsupported("INSPECT TALLYING FOR "
                                + (cp.FIRST() is not null ? "FIRST" : "TRAILING (non-ISO extension)")
                                + " — ISO §14.9.22.2 Format 1 admits ALL / LEADING / CHARACTERS only");
                        InspectTallyKind kind = cp.ALL() is not null ? InspectTallyKind.All
                            : cp.LEADING() is not null ? InspectTallyKind.Leading
                            : last;   // a bare operand inherits the governing adjective (GR10)
                        if (cp.ALL() is not null || cp.LEADING() is not null) last = kind;
                        tallying.Add(new BoundInspectTally(counter, kind, InspectCharOperand(cp.inspectChar()).Op, before, after));
                    }
                }
            }

        var replacing = new List<BoundInspectReplace>();
        if (ins.inspectReplacingPhrase() is { } replPhrase)
        {
            // GR16: ALL, FIRST, and LEADING are transitive across following bare operands until the next adjective.
            InspectReplaceKind? last = null;
            foreach (var item in replPhrase.inspectReplacingItem())
            {
                var (before, after) = InspectDelimiters(item.inspectDelimiters());
                if (item.CHARACTERS() is not null)
                {
                    var (rep, _) = InspectCharOperand(item.inspectChar(0));
                    // SR7: with CHARACTERS, literal-3 shall be ONE character (an identifier-5 of another size is
                    // the runtime GR15 case — the runtime uses its first character, deterministic).
                    if (rep is BoundStringLiteral { Value.Length: not 1 } bad)
                        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING CHARACTERS BY a {bad.Value.Length}-"
                            + "character literal — literal-3 shall be one character in length (ISO §14.9.22.3 SR7)");
                    replacing.Add(new BoundInspectReplace(InspectReplaceKind.Characters, null, rep, before, after));
                    continue;
                }
                if (item.TRAILING() is not null)
                    return new BoundUnsupported("INSPECT REPLACING TRAILING (non-ISO extension — not in ISO §14.9.22.2 Format 2)");
                InspectReplaceKind kind = item.ALL() is not null ? InspectReplaceKind.All
                    : item.FIRST() is not null ? InspectReplaceKind.First
                    : item.LEADING() is not null ? InspectReplaceKind.Leading
                    : last ?? InspectReplaceKind.All;   // bare operand pair — GR16 (All only when ungrammatically first)
                if (item.ALL() is not null || item.FIRST() is not null || item.LEADING() is not null) last = kind;

                var (pat, _) = InspectCharOperand(item.inspectChar(0));
                var (rep2, figurative) = InspectCharOperand(item.inspectChar(1));
                if (figurative && rep2 is BoundStringLiteral f && InspectStaticWidth(pat) is { } wp && wp != f.Value.Length)
                    // SR6 / GR14: a figurative literal-3 is expanded (or contracted) to the size of literal-1 /
                    // identifier-3 — e.g. ALL "AB" BY SPACES replaces with "  ". (The legacy skipped the operand.)
                    rep2 = new BoundStringLiteral(new string(f.Value[0], wp));
                else if (pat is BoundStringLiteral lp && rep2 is BoundStringLiteral lr && !figurative
                         && lp.Value.Length != lr.Value.Length)
                    // SR6: non-figurative literal-1 / literal-3 of unequal size is illegal — statically known, so
                    // diagnosed at compile time (the identifier-size mismatch is the runtime GR14 EC case).
                    data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING: literal '{lp.Value}' and replacement "
                        + $"'{lr.Value}' differ in size (ISO §14.9.22.3 SR6 — equal size unless the replacement is figurative)");
                replacing.Add(new BoundInspectReplace(kind, pat, rep2, before, after));
            }
        }

        BoundInspectConvert? converting = null;
        if (ins.inspectConvertingPhrase() is { } conv)
        {
            var (from, _) = InspectCharOperand(conv.inspectChar(0));
            var (to, figurative) = InspectCharOperand(conv.inspectChar(1));
            if (figurative && to is BoundStringLiteral f && InspectStaticWidth(from) is { } wf && wf != f.Value.Length)
                to = new BoundStringLiteral(new string(f.Value[0], wf));   // SR9/GR22 — figurative literal-5 takes literal-4's size
            else if (from is BoundStringLiteral lf && to is BoundStringLiteral lt && !figurative
                     && lf.Value.Length != lt.Value.Length)
                data.Edition.Error("COBOLNET0846", $"INSPECT CONVERTING: '{lf.Value}' and '{lt.Value}' differ in "
                    + "size (ISO §14.9.22.3 SR9 — equal size unless literal-5 is figurative)");
            BoundOperand? before = null, after = null;
            foreach (var ba in conv.inspectBeforeAfterPhrase())
            {
                var (op, _) = InspectCharOperand(ba.inspectChar());
                if (ba.BEFORE() is not null) before = op;
                else after = op;
            }
            converting = new BoundInspectConvert(from, to, before, after);
        }

        return new BoundInspect(target, tallying, replacing, converting, backward);
    }

    /// <summary>Bind a per-operand BEFORE/AFTER delimiter pair (ISO §14.9.22.2 after-before-phrase; both may
    /// appear on one operand, in either order — disambiguated by token index, since the grammar's two alternatives
    /// share the merged BEFORE/AFTER accessors). INITIAL is a noise word.</summary>
    private (BoundOperand? Before, BoundOperand? After) InspectDelimiters(Core.InspectDelimitersContext? ctx)
    {
        if (ctx is null) return (null, null);
        var chars = ctx.inspectChar();
        if (ctx.BEFORE() is { } b && ctx.AFTER() is { } a)
        {
            var first = InspectCharOperand(chars[0]).Op;
            var second = InspectCharOperand(chars[1]).Op;
            return b.Symbol.TokenIndex < a.Symbol.TokenIndex ? (first, second) : (second, first);
        }
        if (ctx.BEFORE() is not null) return (InspectCharOperand(chars[0]).Op, null);
        if (ctx.AFTER() is not null) return (null, InspectCharOperand(chars[0]).Op);
        return (null, null);
    }

    /// <summary>Bind an INSPECT operand (identifier-3..7 / literal-1..5 / figurative): a figurative constant is an
    /// implicit ONE-character item (ISO §14.9.22.3 SR3 — and a figurative beginning with ALL is forbidden there);
    /// an identifier reads its FULL raw image at run time (GR5/GR6 — no trimming; GR4d de-signs a signed numeric
    /// operand at the read). <c>Figurative</c> reports the figurative origin so SR6/SR9 can expand a replacement
    /// to the pattern size.</summary>
    private (BoundOperand Op, bool Figurative) InspectCharOperand(Core.InspectCharContext c)
    {
        var fig = c.figurativeConstant() ?? c.literal()?.nonNumericLiteral()?.figurativeConstant();
        if (fig is not null)
        {
            if (fig.STRINGLIT() is not null)
                return (new BoundOperandError(
                    "INSPECT operand ALL \"literal\" (ISO §14.9.22.3 SR3 — a figurative constant beginning with ALL is not permitted)"), false);
            return (new BoundStringLiteral(InspectFigurativeChar(fig).ToString()), true);
        }
        if (c.literal()?.nonNumericLiteral()?.STRINGLIT() is { } s)
            return (new BoundStringLiteral(DecodeCobolString(s.GetText())), false);
        if (c.dataReference() is { } dref)
        {
            if (refs.Resolve(dref) is not { } p)
                return (new BoundOperandError($"INSPECT operand '{dref.GetText()}'"), false);
            if (p.Item.IsGroup || p.Item.Pic is { Usage: not Usage.Display })
                data.Edition.Error("COBOLNET0847", $"INSPECT operand '{dref.GetText()}' shall be an elementary "
                    + "usage-display item (ISO §14.9.22.3 SR2)");
            return (new BoundFieldOperand(p), false);
        }
        // The grammar admits a numeric literal here; SR3 does not (alphanumeric/boolean/national literals only).
        return (new BoundOperandError(
            $"INSPECT operand '{c.GetText()}' (ISO §14.9.22.3 SR3 — a numeric literal is not a valid INSPECT literal)"), false);
    }

    /// <summary>The single character a figurative INSPECT operand denotes (ISO §14.9.22.3 SR3 — an implicit
    /// one-character item; HIGH/LOW-VALUE are U+00FF/U+0000 per COBOLNET_DESIGN §14.9, matching the emitter's
    /// figurative fills).</summary>
    private static char InspectFigurativeChar(Core.FigurativeConstantContext fig) =>
        fig.ZERO() is not null ? '0'
        : fig.SPACE() is not null ? ' '
        : fig.HIGH_VALUE() is not null ? '\u00ff'
        : fig.LOW_VALUE() is not null || fig.NULL_() is not null ? '\u0000'
        : fig.QUOTE_() is not null ? '"'
        : ' ';

    /// <summary>The compile-time-known character width of an INSPECT operand's run-time image, or null. A literal
    /// is its own length; an identifier's raw image width is static — alphanumeric/edited items their PIC length,
    /// a numeric item its digit count (the GR4d de-signed image excludes any separate sign position), a group its
    /// image width. Sizes both SR6/SR9 figurative expansion and the literal/literal equal-size checks rest on.</summary>
    private static int? InspectStaticWidth(BoundOperand op) => op switch
    {
        BoundStringLiteral s => s.Value.Length,
        BoundFieldOperand f when f.Place.Item.IsGroup => f.Place.Item.ImageWidth,
        BoundFieldOperand { Place.Item.Pic: { Category: PicCategory.Numeric } pic } => pic.Digits,
        BoundFieldOperand { Place.Item.Pic: { } pic } => pic.Length,
        _ => null,
    };
}
