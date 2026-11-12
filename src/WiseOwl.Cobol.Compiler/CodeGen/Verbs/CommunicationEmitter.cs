// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE COMPILER HALF OF THE COMMUNICATION FACILITY (ISO/IEC 1989:2002 Annex A; docs/rearchitecture/DESIGN-communication-facility.md §5; kb/Work PB4677). Every statement renders one
/// call of <c>WiseOwl.Cobol.Runtime.Communication.MessageControl</c>: the CD's area goes over as its image, through <see cref="OperandText.RecordAreaImage"/>, the one record-area channel, and
/// the image the call returns comes back through <see cref="SequentialIoEmitter.EmitImageInto"/>, its receiving inverse, verbatim, because other descriptions of the area read the same storage.
/// </summary>
internal sealed class CommunicationEmitter(EmitContext ctx, NumericRenderer num, PlaceTable places, SequentialIoEmitter seqIo)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (RECEIVE's phrases nest statement lists).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The USE FOR DEBUGGING trigger emitter — property-wired by <see cref="UnitEmitters"/> (docs/CONTROL_FLOW_DESIGN.md D20).</summary>
    internal DebugTriggerEmitter Debug { get; set; } = null!;

    private static string FormatOf(CommunicationDescription cd) => RuntimeApi.CommunicationEnum(cd.Format);

    /// <summary>The members of a program with a CD of the INITIAL clause: its once-per-instance flag.</summary>
    public void EmitMembers(CodeWriter w)
    {
        if (ctx.Data.Communication.Declared.Any(cd => cd.IsInitial))
            w.Line("private bool __initialCdSet;   // the INITIAL CD's symbolic names are set before the first statement (ISO/IEC 1989:2002 A.1.2.3 GR6, GR28)");
    }

    /// <summary>A.1.2.3 GR6, GR28: "The symbolic names are inserted or the initialization to spaces is completed prior to the execution of the first procedure division statement". Once per
    /// instance, as the storage it writes is: a canceled or initial program's fresh instance is in its initial state again.</summary>
    public void EmitActivation(CodeWriter w)
    {
        foreach (var cd in ctx.Data.Communication.Declared)
        {
            if (!cd.IsInitial || places.Of(cd.Area) is not { } area) continue;
            using (w.Block("if (!__initialCdSet)"))
            {
                w.Line("__initialCdSet = true;");
                seqIo.EmitImageInto(area, RuntimeApi.McsInitialArea(CsLiteral(cd.Key), FormatOf(cd), OperandText.RecordAreaImage(area)), storesIntoOwnItem: false);
            }
        }
    }

    public void Emit(BoundCommunicationStatement n)
    {
        string image = OperandText.RecordAreaImage(n.Area);
        string cd = CsLiteral(n.Cd.Key);
        string format = FormatOf(n.Cd);
        switch (n.Verb)
        {
            case CommunicationVerb.AcceptMessageCount:
                StoreArea(n, RuntimeApi.McsAcceptMessageCount(image));
                break;
            case CommunicationVerb.Enable or CommunicationVerb.Disable:
                string key = n.Key is { } written ? OperandText.AsTransferString(written, num, "the KEY phrase of") : "null";
                string path = RuntimeApi.CommunicationEnum(n.Path);
                StoreArea(n, n.Verb is CommunicationVerb.Enable
                    ? RuntimeApi.McsEnable(cd, format, path, image, key)
                    : RuntimeApi.McsDisable(cd, format, path, image, key));
                break;
            case CommunicationVerb.Purge:
                StoreArea(n, RuntimeApi.McsPurge(cd, format, image));
                break;
            case CommunicationVerb.Send:
                string text = n.Text is { } from ? SendingText(from) : "null";
                string indicator = n.IndicatorItem is { } item
                    ? RuntimeApi.McsIndicatorOf(OperandText.AsTransferString(item, num, "SEND WITH"))
                    : RuntimeApi.CommunicationEnum(n.Indicator);
                StoreArea(n, RuntimeApi.McsSend(cd, format, image, text, indicator));
                break;
            case CommunicationVerb.Receive:
                EmitReceive(n, cd, format, image);
                break;
            default:
                throw new InvalidOperationException($"unknown communication statement {n.Verb}");
        }
    }

    /// <summary>⛔ THE TEXT OF SEND's identifier-1 (kb/Work PB5378). §1985:XIV.3.6.4 GR3 counts the text length against "the size of the data
    /// item referenced by identifier-1", and "the size of an elementary data item or a group item is the number of characters in
    /// standard data format of the item" (§1985:IV.4.3.4), so at --std 85 an elementary item and a group are measured, and
    /// transferred, in the same characters: an elementary binary or packed item as its digits (<see cref="OperandText.AsTransferString"/>),
    /// and a group with such an item as its storage image with each of them replaced by its digits
    /// (<c>CobolStandardData.Image</c>), the size <see cref="ItemLength.StandardDataFormatCharacters"/> counts. From 2002 the CD's records
    /// are measured in storage positions (<c>CommunicationSectionBinder.CheckRecords</c>) and a group is its storage image.</summary>
    private string SendingText(BoundOperand from)
    {
        string text = OperandText.AsTransferString(from, num, "SEND FROM");
        return ctx.Data.DialectLevel < 2002 && from is BoundFieldOperand { Place: { Item.IsGroup: true } group and not RefModPlace }
            && ItemLength.StandardLayout(group.Item) is { } layout
            ? RuntimeApi.StandardDataImage(text, layout)
            : text;
    }

    private void StoreArea(BoundCommunicationStatement n, string call) => seqIo.EmitImageInto(n.Area, call, storesIntoOwnItem: false);

    /// <summary>A.2.5.3 GR3-GR5: the receipt fills the area and the receiving item, and its answer picks the phrase.</summary>
    private void EmitReceive(BoundCommunicationStatement n, string cd, string format, string image)
    {
        var w = ctx.Writer;
        // §1985:IV.4.3.8: identifier-1's subscripts and reference modifiers are evaluated once, as the first operation of the statement —
        // before the MCS updates the CD (a subscript may read TEXT LENGTH), so the receipt lands in the place the statement identified (kb/Work PB5279).
        var into = PlaceIdentification.Freeze(n.Into!, PlaceIdentification.Hoister(ctx));
        string receipt = $"__receipt{ctx.Names.NextEc()}";
        w.Line($"var {receipt} = {RuntimeApi.McsReceive(cd, format, image, n.Segment, OperandText.RecordAreaImage(into, use: OperandText.AreaUse.Receiving), n.NoData is not null)};");
        seqIo.EmitImageInto(n.Area, $"{receipt}.Area", storesIntoOwnItem: false);
        // A reference-modified receiver is a window, whose write splices; the image store is for an item's whole storage.
        if (into is RefModPlace) w.Line(PlaceRenderer.Write(into, $"{receipt}.Into"));
        else seqIo.EmitImageInto(into, $"{receipt}.Into", storesIntoOwnItem: false);
        // §1985:XV.3.2.4 GR8 b: a cd-name's section runs after a RECEIVE "that does not result in the execution of the NO DATA
        // imperative-statement". The WITH DATA statements are inside the RECEIVE (§1985:XIV.3.5.4 GR4), so the section lands after them (kb/Work PB5295).
        if (n.WithData is null && n.NoData is null) { Debug.LandAfterAction(); return; }
        using (w.Block($"if ({receipt}.DataAvailable)"))
        {
            using (Debug.OweAfterAction())
                if (n.WithData is { } yes) Statements.EmitStatementList(yes);
            Debug.LandAfterAction();
        }
        using (w.Block("else"))
            if (n.NoData is { } no) Statements.EmitStatementList(no);
    }
}
