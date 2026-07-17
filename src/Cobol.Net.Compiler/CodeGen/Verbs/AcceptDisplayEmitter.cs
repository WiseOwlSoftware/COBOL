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
    /// <summary>DISPLAY (ISO §14.9.8): shows the sign-aware image (deSign defaults false — the operational sign is
    /// part of the displayed zoned representation, unlike a move to an alphanumeric receiver).</summary>
    public void EmitDisplay(BoundDisplay d)
    {
        var parts = d.Operands.Select(o => OperandText.AsString(o, num)).ToList();
        string image = parts.Count == 0 ? "\"\"" : string.Join(" + ", parts);
        ctx.Writer.Line(d.NoAdvancing ? $"System.Console.Write({image});" : $"System.Console.WriteLine({image});");
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
        // into the slice (§8.4.2.4), which coincides with the GR4 left-aligned store.
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
            if (!item.IsCharacterImage)
            {
                w.Line(LoudStmt(TierCIsland.Reason(item, "ACCEPT into group", "COMP/binary")));
                return;
            }
            string img = $"AcceptSource.Device({item.ImageWidth})";
            // A Tier-B view group's image IS its character window; a record-struct group distributes via FromImage.
            w.Line(target is RedefViewPlace ? PlaceRenderer.Write(target, img) : $"{PlaceRenderer.Read(target)}.FromImage({img});");
            return;
        }

        var pic = item.Pic!;
        switch (pic)
        {
            case { Category: PicCategory.Numeric, IsFloat: false }:
                // ImageWidth = digit count + a SIGN SEPARATE character position (ISO §13.18.52 GR6a).
                string image = $"AcceptSource.Device({item.ImageWidth})";
                w.Line(item.StoreAsImage || target is RedefViewPlace
                    ? PlaceRenderer.Write(target, image)   // image-stored zoned field / Tier-B window: the characters ARE the storage
                    : PlaceRenderer.Write(target, ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(image, item.ProfileName), item)));
                return;
            case { Category: PicCategory.Numeric }:   // COMP-1/COMP-2
                w.Line(LoudStmt($"ACCEPT into floating-point receiver '{item.CobolName}' (COMP-1/COMP-2 device conversion, deferred)"));
                return;
            default:
                // Alphanumeric / alphabetic / edited: the characters store as-is (GR3/GR4 — no editing, no
                // conversion; an edited PICTURE's Length counts every mask position).
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
        // the slice — §14.9.25.4 alphanumeric move into the §8.4.2.4 slice).
        if (target is RefModPlace)
        {
            w.Line(PlaceRenderer.Write(target, sendImage));
            return;
        }

        if (item.IsGroup)
        {
            if (!item.IsCharacterImage)
            {
                w.Line(LoudStmt(TierCIsland.Reason(item, "ACCEPT temporal into group", "COMP/binary")));
                return;
            }
            // A group receiver is an alphanumeric-category move (§14.9.25.4 GR4 — filled without conversion).
            string img = RuntimeApi.StrStore(sendImage, $"{item.ImageWidth}");
            w.Line(target is RedefViewPlace ? PlaceRenderer.Write(target, img) : $"{PlaceRenderer.Read(target)}.FromImage({img});");
            return;
        }

        switch (item.Pic!)
        {
            case { Category: PicCategory.Numeric, IsFloat: false }:
                // Numeric MOVE: decimal-point alignment with high-order truncation / zero fill (§14.9.25.4 GR6 —
                // the integer sender is at scale 0; the receiver keeps its LOW-order digits when smaller).
                string stored = ArithmeticEmitter.Narrow(RuntimeApi.NumStore(call, "0", item.ProfileName), item);
                w.Line(PlaceRenderer.Write(target, item.StoreAsImage
                    ? RuntimeApi.NumFormatDisplay(stored, item.ProfileName)
                    : stored));
                return;
            case { Category: PicCategory.Numeric }:   // COMP-1/COMP-2
                w.Line(LoudStmt($"ACCEPT temporal into floating-point receiver '{item.CobolName}' (COMP-1/COMP-2, deferred)"));
                return;
            case { Category: PicCategory.NumericEdited, EditMask: { } mask }:
                // A numeric sender into a numeric-edited receiver is EDITED into the mask (§14.9.25.4 GR5).
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormat(call, "0", CsLiteral(mask), ctx.EditCfgArgs)));
                return;
            case { Category: PicCategory.Alphanumeric, EditMask: { } amask }:
                // Alphanumeric-edited: the sending characters place into the mask positions (§13.18.40 insertion).
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatAlphanumeric(sendImage, CsLiteral(amask))));
                return;
            case { Category: PicCategory.Alphanumeric } anPic:
                // Alphanumeric MOVE: left-justified, right space-fill / right truncation (§14.9.25.4 GR6).
                w.Line(PlaceRenderer.Write(target, RuntimeApi.StrStore(sendImage, $"{anPic.Length}")));
                return;
            default:
                w.Line(LoudStmt($"ACCEPT temporal into receiver '{item.CobolName}' of unsupported category"));
                return;
        }
    }
}
