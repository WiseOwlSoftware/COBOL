// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The ACCEPT / DISPLAY verb emitter (P7 Step 9c — a real collaborator over the per-unit
/// <see cref="EmitContext"/>; the DISPLAY half moved in from the orchestrator partial, making the Step-5
/// filename honest). Every runtime-member fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class AcceptDisplayEmitter(EmitContext ctx, NumericRenderer num)
{
    /// <summary>DISPLAY (ISO §14.9.11): shows the sign-aware image (deSign defaults false — the operational sign is
    /// part of the displayed zoned representation, unlike a move to an alphanumeric receiver). The UPON device routing
    /// (§14.9.11.3 SR2 / §14.9.11.4 GR8): a mnemonic bound to SYSERR writes the standard-error stream; CONSOLE / SYSOUT
    /// and the no-UPON default use the standard display device (standard output) — the latter path is byte-identical.</summary>
    public void EmitDisplay(BoundDisplay d)
    {
        // A VARIABLE-LENGTH group operand displays in the implementor-defined format §14.9.11.4 GR7 leaves to
        // us, documented as CONFORMANCE.md A.1 item 57 (kb/Work PB164): the generated CurrentImage() — fixed
        // members by the record-image member law, dynamic members at their CURRENT extent, following the
        // §15.50.4 r7 LENGTH-sum geometry in character positions (a NATIONAL member displays one character
        // per position where LENGTH counts two bytes — the sanctioned D-N1/D-N3 divergence; row 57 names the
        // shapes that stay loud instead: ODO members, in-element runtime lengths, INDEX leaves).
        // DISPLAY-ONLY by design: GR7 is a DISPLAY-statement determination, so the shared group-sender arm
        // (WRITE/RELEASE/compare) keeps its loud posture.
        // ⛔ `vp is not RedefViewPlace`: a Tier-B class-tier VIEW's Read() is its string WINDOW — spelling
        // .CurrentImage() on it is CS1061 on `string` (the PB176 skeptic round; whether a dynamic-length
        // member under REDEFINES is even legal is kb/Work PB177's screen question — the emitter defends
        // regardless, per the GroupImage doc's own law about window shapes).
        var parts = d.Operands.Select(o =>
            o is BoundFieldOperand { Place: { Item: { IsGroup: true, IsImageCapable: false } } vp }
                && vp is not RedefViewPlace
                && GroupImageCodec.CurrentExtentImageCapable(vp.Item)
            ? $"{PlaceRenderer.Read(vp)}.CurrentImage()"
            : OperandText.AsString(o, num)).ToList();
        string image = parts.Count == 0 ? "\"\"" : string.Join(" + ", parts);
        string sink = d.ToStdErr ? "System.Console.Error" : "System.Console";
        ctx.Writer.Line(d.NoAdvancing ? $"{sink}.Write({image});" : $"{sink}.WriteLine({image});");
    }

    /// <summary>ACCEPT (ISO §14.9.1). Format 1 (device) is the GR1–GR4 transfer: 80-character card-image records
    /// from standard input (<c>AcceptSource.Device</c>), stored ALIGNED LEFT by SIZE — explicitly NOT the MOVE
    /// rules (JUSTIFIED is irrelevant; GR4a left-aligned fill / GR4b leftmost truncation). Format 2 (temporal) is
    /// the GR6 transfer: the clock value as a conceptual UNSIGNED INTEGER USAGE DISPLAY item of fixed width
    /// (GR7–GR12) stored BY THE MOVE RULES — a numeric receiver decimal-aligns (keeps LOW-order digits on
    /// truncation), an edited receiver edits, an alphanumeric/group receiver takes the digit-string image. (The
    /// legacy stored temporal text left-justified-raw for every receiver — a §14.9.1.4 GR6 deviation its
    /// exact-width NIST receivers never exposed; pinned to the spec here, all editions.)</summary>
    public void EmitAccept(BoundAccept a)
    {
        if (a.Kind == AcceptKind.Device) EmitAcceptDevice(a.Target);
        else EmitAcceptTemporal(a.Target, a.Kind);
    }

    /// <summary>Format 1 — the device transfer (ISO §14.9.1.4 GR1–GR5). <c>AcceptSource.Device(n)</c> returns
    /// EXACTLY n characters (left-aligned, space-padded, excess ignored), so each receiver shape stores it whole:
    /// a character group distributes via <c>FromImage</c>; an elementary string field takes it directly; a native
    /// numeric receiver converts via <c>ParseDisplay</c> (GR1 — conversion is implementor-defined; ours
    /// decodes the zoned/separate-sign image per the receiver's profile, non-digits contributing no digit).</summary>
    private void EmitAcceptDevice(Place target)
    {
        var w = ctx.Writer;
        var item = target.Item;

        // A ref-modified receiver: the slice length IS the transfer size; the splice left-justifies/space-fills
        // into the slice (§8.4.3.3.4 GR6), which coincides with the GR4 left-aligned store.
        if (target is RefModPlace rm)
        {
            string len = rm.Length is { } l
                ? $"(int)({l})"
                : $"{rm.Inner.Item.Pic?.Length ?? rm.Inner.Item.ImageWidth} - (int)({rm.Start}) + 1";
            w.Line(PlaceRenderer.Write(target, $"AcceptSource.Device({len})"));
            return;
        }

        if (item.IsGroup)
        {
            // A group receiver takes the device characters positionally — the ONE group-image store (§14.9.25.4 GR4).
            w.Line(PlaceRenderer.WriteGroupImage(target, $"AcceptSource.Device({item.DisplayTextWidth})", "ACCEPT into group"));
            return;
        }

        var pic = item.Pic!;
        switch (pic)
        {
            case { Category: PicCategory.Numeric, IsFloat: false }:
                // The DEVICE window is CHARACTERS — digit count + a SIGN SEPARATE position (ISO §13.18.52
                // GR6a) — never the item's byte width: a PIC 9(4) COMP receiver reads FOUR typed digits and
                // stores TWO bytes (V59).
                string image = $"AcceptSource.Device({item.DisplayTextWidth})";
                w.Line(item.StoreAsImage || target is RedefViewPlace
                    ? PlaceRenderer.Write(target, image)   // image-stored zoned field / Tier-B window: the characters ARE the storage
                    : PlaceRenderer.Write(target, ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(image, item.ProfileName), item)));
                return;
            case { Category: PicCategory.Numeric }:   // COMP-1/COMP-2
                w.Line(LoudStmt($"ACCEPT into floating-point receiver '{item.CobolName}' (COMP-1/COMP-2 device conversion, deferred)"));
                return;
            case { Category: PicCategory.Boolean } bpic:
                // A BOOLEAN device receiver (SR1 does not exclude it): GR1's implementor-defined conversion is
                // AcceptSource.DeviceBoolean — each transferred '1' converts to boolean one, EVERY other
                // character (a '0', a pad space, any device character) to boolean zero, so the §13.18.40.4
                // GR14 '0'/'1' representation invariant holds for any input (the old default-arm raw store put
                // pad SPACES into boolean storage — kb/Work PB139).
                w.Line(PlaceRenderer.Write(target, $"AcceptSource.DeviceBoolean({bpic.Length})"));
                return;
            default:
                // Alphanumeric / alphabetic / national / edited: the characters store as-is (GR3/GR4 — no
                // editing; an edited PICTURE's Length counts every mask position). For a NATIONAL receiver the
                // GR1 conversion is DEFINED as the identity on the UTF-16 character substrate — one device
                // character per national position, so Device(pic.Length) is exact (kb/Work PB139).
                w.Line(PlaceRenderer.Write(target, $"AcceptSource.Device({pic.Length})"));
                return;
        }
    }

    /// <summary>Format 2 — the temporal transfer (ISO §14.9.1.4 GR6–GR12): the runtime returns the clock reading
    /// as the conceptual item's unsigned integer VALUE; the store follows the MOVE rules (§14.9.25.4) per the
    /// receiver's category, with the conceptual item's width fixed by GR7–GR12.</summary>
    private void EmitAcceptTemporal(Place target, AcceptKind kind)
    {
        var w = ctx.Writer;
        var item = target.Item;
        (string call, int digits) = kind switch
        {
            AcceptKind.Date => ("AcceptSource.Date()", 6),                  // GR7 — YYMMDD
            AcceptKind.DateYYYYMMDD => ("AcceptSource.DateYYYYMMDD()", 8),  // GR8 — YYYYMMDD (2002+)
            AcceptKind.Day => ("AcceptSource.Day()", 5),                    // GR9 — YYDDD
            AcceptKind.DayYYYYDDD => ("AcceptSource.DayYYYYDDD()", 7),      // GR10 — YYYYDDD (2002+)
            AcceptKind.Time => ("AcceptSource.Time()", 8),                  // GR11 — HHMMSScc
            _ => ("AcceptSource.DayOfWeek()", 1),                           // GR12 — 1=Monday … 7=Sunday
        };
        // The conceptual sending item's DISPLAY image: its digits zero-padded to the GR-defined width.
        string sendImage = RuntimeApi.NumFormatUnsignedDisplay(call, digits);

        // A ref-modified receiver slice takes the sending characters (left-justified, space-filled, truncated to
        // the slice — §14.9.25.4 alphanumeric move into the §8.4.3.3.4 GR6 slice).
        if (target is RefModPlace)
        {
            w.Line(PlaceRenderer.Write(target, sendImage));
            return;
        }

        if (item.IsGroup)
        {
            // A group receiver is an alphanumeric-category move (§14.9.25.4 GR4 — filled without conversion): the
            // ONE group-image store.
            w.Line(PlaceRenderer.WriteGroupImage(target, RuntimeApi.StrStore(sendImage, $"{item.DisplayTextWidth}"), "ACCEPT temporal into group"));
            return;
        }

        switch (item.Pic!)
        {
            case { Category: PicCategory.Numeric, IsFloat: false }:
                // Numeric MOVE: decimal-point alignment with high-order truncation / zero fill (§14.9.25.4 GR6 —
                // the integer sender is at scale 0; the receiver keeps its LOW-order digits when smaller).
                string stored = ArithmeticEmitter.Narrow(RuntimeApi.NumStore(call, "0", item.ProfileName), item);
                w.Line(PlaceRenderer.Write(target, item.StoreAsImage
                    ? RuntimeApi.NumFormatImage(stored, item.ProfileName)
                    : stored));
                return;
            case { Category: PicCategory.Numeric }:   // COMP-1/COMP-2
                w.Line(LoudStmt($"ACCEPT temporal into floating-point receiver '{item.CobolName}' (COMP-1/COMP-2, deferred)"));
                return;
            case { Category: PicCategory.NumericEdited } npic:
                // A numeric sender into a numeric-edited receiver is EDITED into the mask (§14.9.25.4 GR5) — the form
                // dispatch (fixed / floating-point) is RuntimeApi.EditFormatFor's (D21/PB66).
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFor(npic, new NumX(call, 0), call, "0", ctx.EditCfg(target.Item.Pic) + RuntimeApi.EditsArg(target.Item.Pic!.EditingRules))));
                return;
            case { Category: PicCategory.Alphanumeric, EditMask: { } amask }:
                // Alphanumeric-edited: the sending characters place into the mask positions (§13.18.40 insertion).
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatAlphanumeric(sendImage, CsLiteral(amask))));
                return;
            case { Category: PicCategory.Alphanumeric or PicCategory.National } snPic:
                // Alphanumeric MOVE: left-justified, right space-fill / right truncation; a JUSTIFIED receiver
                // right-justifies (left space-fill / left truncation, §14.9.25.4 GR6c). A NATIONAL receiver
                // stores exactly like alphanumeric on the character substrate (§14.6.8.5; the digit image rides
                // the D-N repertoire identity) — the same two stores MoveEmitter's national arm uses.
                w.Line(PlaceRenderer.Write(target, RuntimeApi.StrStoreAligned(sendImage, $"{snPic.Length}", item.Justified)));
                return;
            default:
                // §14.9.1.3 SR3 bind-rejects class alphabetic / boolean / index / object / pointer receivers,
                // so this arm is the safety net for a category the screen does not yet know.
                w.Line(LoudStmt($"ACCEPT temporal into receiver '{item.CobolName}' of unsupported category"));
                return;
        }
    }
}
