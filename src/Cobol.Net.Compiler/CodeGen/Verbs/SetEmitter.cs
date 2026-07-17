// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The SET-family emitter (P7 Step 9i — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): SET … TO / UP-DOWN BY / pointer F4 senders / OCCURS-DYNAMIC capacity /
/// condition-names TO TRUE, plus the ONE SET-target store/augment pair PERFORM VARYING and SEARCH ride.</summary>
internal sealed class SetEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith, PtrEmitter ptr)
{
    /// <summary><c>SET … TO value</c> (ISO §14.9.39 Format 1): the sender is evaluated ONCE into an integer temp
    /// (GR2 — "the value of the sending operand is determined once"), then each receiver takes it by kind: an
    /// index-name or index data item receives it unchanged (GR2a/GR2b — in the §3.5 model an index IS its 1-based
    /// occurrence number, so cross-table conversion is the identity); a numeric data item receives the occurrence
    /// number through its own PICTURE store (GR2c). Range checking (EC-RANGE-INDEX) awaits the EC model — COBOL-85
    /// has no exception conditions, so the unchecked store IS the 85 semantics.</summary>
    public void EmitSetTo(BoundSetTo s)
    {
        string tmp = $"__set{ctx.Names.NextSet()}";
        ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(num.Render(s.Value, ReceiverContext.None), 0)});");
        foreach (var t in s.Targets) StoreSetTarget(t, new NumX(tmp, 0));
    }

    /// <summary><c>SET pointer… TO {NULL | pointer}</c> (ISO §14.9.39 Format 4; Phase-4b increment 1): copy
    /// the NULL singleton or the source pointer's carrier into each target in order (GR — a straight handle
    /// copy; a data pointer carries no PICTURE store).</summary>
    public void EmitSetPointer(BoundSetPointer s)
    {
        string src = s.ToNull ? "ManagedPointer.Null"
            : s.Address is { } a ? ptr.AddressOfText(a)   // ADDRESS OF sender (F7; Phase-4b inc 2)
            : PlaceRenderer.Read(s.Source!);
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET pointer (ISO §14.9.39 Format 4/7)");
    }

    /// <summary><c>SET program-pointer… TO {NULL | program-pointer}</c> (ISO §14.9.39 Format 9; P10 Step 7):
    /// a straight carrier copy — the Format-4 data-pointer twin over <c>ProgramPointer</c>.</summary>
    public void EmitSetProgramPointer(BoundSetProgramPointer s)
    {
        string src = s.ToNull ? "ProgramPointer.Null" : PlaceRenderer.Read(s.Source!);
        foreach (var t in s.Targets)
            ctx.Writer.Line(PlaceRenderer.Write(t, src) + "   // SET program-pointer (ISO §14.9.39 Format 9)");
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is evaluated ONCE
    /// (GR3), then each index is adjusted by it (GR4).</summary>
    public void EmitSetUpDown(BoundSetUpDown s)
    {
        string tmp = $"__set{ctx.Names.NextSet()}";
        ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(num.Render(s.Amount, ReceiverContext.None), 0)});");
        foreach (var t in s.Targets) AugmentSetTarget(t, s.Down, new NumX(tmp, 0));
    }

    /// <summary>SET Format 14 (ISO §14.9.39 GR29; OCCURS DYNAMIC, data-model D9): the amount is evaluated ONCE,
    /// then the owning table's current capacity is set / raised / lowered through the runtime — new occurrences
    /// seeded (§8.5.1.9.5), clamped to the minimum, and EC-FLOW-SEARCH raised if a SEARCH of the same table is
    /// active (GR31). The register carries no storage; the operation is on the <c>CobolDynTable&lt;T&gt;</c> itself.</summary>
    public void EmitSetCapacity(BoundSetCapacity s)
    {
        string tmp = $"__cap{ctx.Names.NextSet()}";
        ctx.Writer.Line($"long {tmp} = (long)({NumericRenderer.Align(num.Render(s.Amount, ReceiverContext.None), 0)});");
        string call = s.Kind switch
        {
            SetCapacityKind.To => "SetCapacity",
            SetCapacityKind.UpBy => "CapacityUpBy",
            _ => "CapacityDownBy",
        };
        ctx.Writer.Line($"{PlaceRenderer.RenderPath(s.Table, AccessDir.Sending)}.{call}({tmp});");
    }

    /// <summary>THE store into a SET-style target (shared by SET TO and PERFORM VARYING initialization): an
    /// index-name field or index data item takes the integer value UNCHANGED (§14.9.39 GR2a/2b — an index IS its
    /// occurrence number); a numeric data item takes it through its own PICTURE store (GR2c).</summary>
    public void StoreSetTarget(BoundSetTarget t, NumX value)
    {
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} = (long)({NumericRenderer.Align(value, 0)});");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                ctx.Writer.Line(PlaceRenderer.Write(p, $"(long)({NumericRenderer.Align(value, 0)})"));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, value, CobolRounding.Truncation);
                break;
        }
    }

    /// <summary>THE augment of a SET-style target by ±amount (shared by SET UP/DOWN BY and PERFORM VARYING):
    /// index-name / index data item → plain occurrence-number arithmetic; a numeric data item → an in-place add
    /// through its PICTURE store (legal as a VARYING induction variable, §14.9.28 GR13; a plain SET UP/DOWN on a
    /// numeric item is invalid COBOL — the edition validator will diagnose it, the behavior is the natural add).</summary>
    public void AugmentSetTarget(BoundSetTarget t, bool down, NumX amount)
    {
        string op = down ? "-" : "+";
        switch (t)
        {
            case SetIndexTarget ix:
                ctx.Writer.Line($"{ix.IndexField} {op}= (long)({NumericRenderer.Align(amount, 0)});");
                break;
            case SetPlaceTarget { Place: var p } when p.Item.Pic is { Usage: Usage.Index }:
                ctx.Writer.Line(PlaceRenderer.Write(p, $"(long)({PlaceRenderer.Read(p)} {op} {NumericRenderer.Align(amount, 0)})"));
                break;
            case SetPlaceTarget { Place: var p }:
                arith.StoreArith(p, num.Combine(num.FieldNum(p), op, amount, ReceiverContext.None), CobolRounding.Truncation);
                break;
        }
    }

    public void EmitSet(BoundSetConditions set)
    {
        foreach (var (parent, cond) in set.Sets)
        {
            var (low, _) = cond.Values[0];   // SET TO TRUE stores the first VALUE (ISO §14.9.39 Format 5)
            var pic = parent.Item.Pic;
            // A FIGURATIVE-word VALUE (SPACE/ZERO/QUOTE/HIGH-VALUE/LOW-VALUE, incl. ALL forms) fills the
            // conditional variable to its width (§8.3.3.6.4 GR2), not the WORD stored as characters — the
            // fill char is category-aware (national/boolean HIGH/LOW-VALUE = the D-N3 pin). '0' for boolean/
            // numeric ZERO. Only reaches the string categories here (numeric SET handles ZERO natively).
            string? figFill = pic is { Category: PicCategory.Alphanumeric or PicCategory.NumericEdited
                or PicCategory.National or PicCategory.Boolean } ? FigurativeWordFill(low, pic.Category) : null;
            string rhs = figFill is not null
                ? $"new string({figFill}, {pic!.Length})"
                : pic?.Category switch
            {
                // National joins the character store (its 88-VALUE is the prefix-stripped N"…" text);
                // a boolean parent stores its B"…" bits with the §14.6.8.6 zero pad.
                PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                    RuntimeApi.StrStore(CsLiteral(CobolLiteral.Decode(low)), $"{pic.Length}"),
                PicCategory.Boolean =>
                    RuntimeApi.StrStoreBoolean(CsLiteral(CobolLiteral.Decode(low)), $"{pic.Length}", false),
                PicCategory.Numeric =>
                    ArithmeticEmitter.Narrow(RuntimeApi.NumStore(UnscaledAtScale(low, pic.Scale), $"{pic.Scale}", parent.Item.ProfileName), parent.Item),
                _ => LoudValue("string", $"SET condition '{cond.Name}' over a group parent"),
            };
            ctx.Writer.Line(PlaceRenderer.Write(parent, rhs));
        }
    }

    /// <summary>The category-aware C# <c>char</c>-literal a level-88 figurative-word VALUE fills with (SET TO
    /// TRUE, ISO §14.9.39 Format 5 + §8.3.3.6.4 GR2), or null when the operand is not a bare figurative word
    /// (a quoted / N"…" / B"…" / numeric literal takes the store path). Tolerates the ALL-prefixed spelling.</summary>
    private string? FigurativeWordFill(string raw, PicCategory cat)
    {
        string w = raw.Trim();
        if (w.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && w.Length > 3
            && (char.IsWhiteSpace(w[3]) || char.IsLetter(w[3])))
            w = w[3..].TrimStart();
        return FigurativeConstants.KindOf(w, includeNull: true) is { } k
            ? FigurativeConstants.Fill(k, ctx.Data.Collating, cat, ctx.Data.NationalCollating) : null;   // the ONE service (P7 Step 4)
    }

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ─────────────────────────────────────────────────────────────

}
