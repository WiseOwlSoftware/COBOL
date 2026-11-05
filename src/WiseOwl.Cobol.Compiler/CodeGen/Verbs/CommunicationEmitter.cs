// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;
using WiseOwl.Cobol.Runtime.Communication;

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
                string text = n.Text is { } from ? OperandText.AsTransferString(from, num, "SEND FROM") : "null";
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

    private void StoreArea(BoundCommunicationStatement n, string call) => seqIo.EmitImageInto(n.Area, call, storesIntoOwnItem: false);

    /// <summary>A.2.5.3 GR3-GR5: the receipt fills the area and the receiving item, and its answer picks the phrase.</summary>
    private void EmitReceive(BoundCommunicationStatement n, string cd, string format, string image)
    {
        var w = ctx.Writer;
        var into = n.Into!;
        string receipt = $"__receipt{ctx.Names.NextEc()}";
        w.Line($"var {receipt} = {RuntimeApi.McsReceive(cd, format, image, n.Segment, OperandText.RecordAreaImage(into), n.NoData is not null)};");
        seqIo.EmitImageInto(n.Area, $"{receipt}.Area", storesIntoOwnItem: false);
        seqIo.EmitImageInto(into, $"{receipt}.Into", storesIntoOwnItem: false);
        if (n.WithData is null && n.NoData is null) return;
        using (w.Block($"if ({receipt}.DataAvailable)"))
            if (n.WithData is { } yes) Statements.EmitStatementList(yes);
        using (w.Block("else"))
            if (n.NoData is { } no) Statements.EmitStatementList(no);
    }
}
