// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.CodeGen;

using static WiseOwl.Cobol.CodeGen.Emit.EmitText;

/// <summary>
/// ⛔ THE COMPILER HALF OF THE ASYNCHRONOUS MESSAGING FACILITY (ISO §2023:14.9.38 SEND, §2023:14.9.31 RECEIVE; docs/rearchitecture/DESIGN-message-control.md §4.4, §4.5;
/// kb/Work PB4744). Each statement renders ONE call of <c>AsyncMessaging</c> and stores the outcome back through the ordinary channels:
/// <list type="bullet">
/// <item>The message is identifier-1 as a file record is sent: its image, <see cref="OperandText.RecordAreaImage"/>, and beside it its extent table,
/// <see cref="OperandText.RecordAreaExtents"/>, decided by the same predicate. A function or method value (SEND's identifier-1 may be one) has no record area and
/// sends its operand text.</item>
/// <item>RECEIVE stores the message by the receiving inverse a READ uses, arm for arm: an out-of-line identifier-1 (a variable-length group, a dynamic-length item)
/// through <see cref="SequentialIoEmitter.EmitOutOfLineInto"/>, any other through <see cref="SequentialIoEmitter.EmitImageInto"/>, left-justified and space-filled as a group
/// move is (§2023:14.9.25.4). data-name-2 takes the length through the ordinary numeric store (<see cref="ArithmeticEmitter.StoreArith"/>) and data-name-1 takes the tag.</item>
/// <item>⛔ THE EXCEPTION CONDITION IS THE RUNTIME'S, THE SELECTION THE SITE'S. <c>AsyncMessaging</c> sets the condition to exist when the statement's own
/// <see cref="IProfiledStatement.CheckingProfile"/> enables it; the site then takes the §2023:14.6.13.1.3 / §2023:14.6.13.1.4 path over the raised name
/// (<see cref="EcEmitter.EmitSelection"/>, the shape of the GOBACK … RAISING pickup, whose name is also chosen at run time) unless the statement's ON EXCEPTION
/// phrase takes it (§2023:14.6.13.1.3 1) and §2023:14.6.13.1.4 1)). Whether the statement is successful, and so which phrase runs, never depends on checking (D-MCS-CHECK, Q8).</item>
/// </list>
/// </summary>
internal sealed class MessagingEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec,
    SequentialIoEmitter seqIo, ArithmeticEmitter arith)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the exception phrases nest statement lists).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>SEND Format 1 (§2023:14.9.38.4 GR4 to GR6): the server tag lands in data-name-1 when a server took the message.</summary>
    public void Emit(BoundSendToServer n)
    {
        var w = ctx.Writer;
        string m = $"__msg{ctx.Names.NextEc()}";
        string server = n.ServerLiteral is { } literal ? OperandText.AsString(literal, num) : CsLiteral(n.ServerName ?? "");
        var (image, extents, slots) = SentMessage(n.From);
        w.Line($"var {m} = {RuntimeApi.AsyncSendToServer(server, image, extents, slots, Checking(n))};");
        using (w.Block($"if ({m}.Successful)"))
            w.Line(PlaceRenderer.Write(n.Returning, $"{m}.Tag") + "   // §2023:14.9.38.4 GR6 - the tag of the server run unit and the message sent");
        EmitOutcome(m, n.OnException, n.NotOnException);
    }

    /// <summary>SEND Format 2 (§2023:14.9.38.4 GR7, GR8): answers the requestor data-name-2 identifies.</summary>
    public void Emit(BoundSendResponse n)
    {
        string m = $"__msg{ctx.Names.NextEc()}";
        var (image, extents, slots) = SentMessage(n.From);
        string raising = n.Raising is { } ecName ? CsLiteral(ecName.Name) : "null";
        ctx.Writer.Line($"var {m} = {RuntimeApi.AsyncSendResponse(PlaceRenderer.Read(n.To), image, extents, slots, raising, n.RaisingLast ? "true" : "false", Checking(n))};");
        EmitOutcome(m, n.OnException, n.NotOnException);
    }

    /// <summary>RECEIVE (§2023:14.9.31.4): the message, its length and its sender land even when the statement is unsuccessful with a message in hand (a reply that
    /// raised, a message longer than identifier-1, §0 Q6, Q7).</summary>
    public void Emit(BoundReceiveMessage n)
    {
        var w = ctx.Writer;
        string m = $"__msg{ctx.Names.NextEc()}";
        string wait = n.Wait switch
        {
            ReceiveWait.Poll => RuntimeApi.MessageWaitOf(MessageWaitKind.MessageReceived),
            ReceiveWait.Seconds => RuntimeApi.MessageWaitOf(MessageWaitKind.Seconds, StatementEmitter.IntervalCall(num.Render(n.Seconds!, ReceiverContext.None))),
            _ => RuntimeApi.MessageWaitOf(MessageWaitKind.Forever),
        };
        // GR3: a message longer than identifier-1's capacity is EC-MCS-MESSAGE-LENGTH. An elementary message tag is its 8 positions (DOC-A.1-212, the token image of
        // kb/Work PB4838); one that holds an OCCURS DYNAMIC table has no allocated maximum (its capacity is the implementor's maximum, §2023:8.5.1.9.1, which
        // FileModel.MaxRecordSize leaves out as no record holds one), so no message is too long for it.
        bool unbounded = !SlotWindow.CarriedBySlot(n.Into.Item) && DataItem.SelfAndDescendantsOf(n.Into.Item).Any(i => i.IsDynamicTable);
        string capacity = SlotWindow.CarriedBySlot(n.Into.Item) ? n.Into.Item.ByteWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : unbounded ? $"{nameof(AsyncMessaging)}.{nameof(AsyncMessaging.UnboundedCapacity)}"
            : FileModel.MaxRecordSize(n.Into.Item).ToString(System.Globalization.CultureInfo.InvariantCulture);
        w.Line($"var {m} = {RuntimeApi.AsyncReceive(PlaceRenderer.Read(n.From), capacity, wait, Checking(n))};");
        using (w.Block($"if ({m}.Image is not null)"))
        {
            string image = $"{m}.Image";
            if (FileModel.IsOutOfLineRecord(n.Into.Item) && !SlotWindow.CarriedBySlot(n.Into.Item))
                seqIo.EmitOutOfLineInto(n.Into, image, $"{m}.Extents", fixedForm: false);
            else if (HoldsTags(n.Into))   // the tags are slots: the image goes in without their placeholder positions (FromMessageImage)
                w.Line(PlaceRenderer.WriteMessageGroupImage(n.Into, RuntimeApi.StrStore(image, $"{n.Into.Item.ByteWidth}"), "RECEIVE GIVING"));
            else if (!SlotWindow.CarriedBySlot(n.Into.Item))
                seqIo.EmitImageInto(n.Into, image, storesIntoOwnItem: false);   // the sender's storage bytes: verbatim, as other descriptions of the area read them
            else   // an elementary tag: its content is the message's one slot, never its token positions, which name a tag of the sending run unit
                w.Line(PlaceRenderer.Write(n.Into, RuntimeApi.AsyncTagOf($"{m}.Slots")));
            // "The slots follow" (design section 4.4): the group's leaf vector is rebuilt with the message's tags in storage order (AsyncMessaging.WithTags) and stored back.
            if (HoldsTags(n.Into))
                w.Line(PlaceRenderer.WriteGroupLeaves(n.Into, RuntimeApi.AsyncWithTags(PlaceRenderer.GroupLeaves(n.Into), $"{m}.Slots")));
            // §2023:14.6.11 7): the run unit's end answers an unanswered message with spaces of identifier-1's size, and an area with no allocated maximum has the
            // size the store just recreated it to (its dynamic tables at the message's occurrences, raised to each FROM minimum), which only its current image states.
            if (unbounded)
                w.Line(RuntimeApi.AsyncReceived($"{m}.Tag", $"{OperandText.RecordAreaImage(n.Into, transfer: true)}.Length") + ";");
        }
        using (w.Block($"if ({m}.Length is {{ }} {m}len)"))
            arith.StoreArith(n.Length, new NumX($"{m}len", 0), CobolRounding.Truncation);   // GR3 - "the length ... of the message received"
        using (w.Block($"if (!{m}.Tag.IsNull)"))
            w.Line(PlaceRenderer.Write(n.From, $"{m}.Tag") + "   // GR1 a) - the tag of the requestor whose message this is");
        EmitOutcome(m, n.OnException, n.NotOnException);
    }

    /// <summary>The image and the extent table of identifier-1 as the message (design section 4.4): a record area's, or a value's operand text.</summary>
    private (string Image, string Extents, string Slots) SentMessage(BoundOperand from)
    {
        if (from is BoundFieldOperand { Place: var place })
        {
            // A strongly-typed group holding a message tag has the one-way transfer image only (DataItem.TransferImageCapable): the sender's storage positions, a tag's
            // as its token (kb/Work PB4838), which the receiver's FromMessageImage skips - all the length checks and the receiver's positions need - and its tags travel beside the image as the message's slots (design section 4.4).
            string image = place.Item.IsGroup && !place.ImageCapable && place.TransferImageCapable
                ? OperandText.AsStorageImage(place, "SEND FROM", transfer: true)
                : OperandText.RecordAreaImage(place, transfer: true);   // a variable-length group holding a tag: its one-way current-extent image
            string slots = HoldsTags(place) ? RuntimeApi.AsyncTagsOf(PlaceRenderer.GroupLeaves(place))
                : SlotWindow.CarriedBySlot(place.Item) ? $"new object?[] {{ {PlaceRenderer.Read(place)} }}"   // an elementary tag: its 8 token positions, the tag beside them
                : "[]";
            return (image, OperandText.RecordAreaExtents(place, transfer: true) ?? "null", slots);
        }
        return (OperandText.AsTransferString(from, num, "SEND FROM"), "null", "[]");
    }

    /// <summary>Does the message area hold message tags - a group with a message-tag member (SEND SR3 and RECEIVE SR2 leave no other slot class in it)? An elementary tag is
    /// the other case, its own one slot (<see cref="SentMessage"/> and the RECEIVE's slot-carried store).</summary>
    private static bool HoldsTags(Place area) => area.Item.IsGroup && SlotWindow.MembersOf(area.Item).Any();

    private static string Checking(IProfiledStatement n) => CsLiteral(n.CheckingProfile.Encoded);

    /// <summary>The raise (when the runtime set a condition), then the phrase pair chosen by the outcome (§0 Q8).</summary>
    private void EmitOutcome(string m, IReadOnlyList<BoundStatement>? onException, IReadOnlyList<BoundStatement>? notOnException)
    {
        var w = ctx.Writer;
        // §2023:14.6.13.1.4 3): a nonfatal condition a USE declarative or WHEN handled on a statement that then completes does not run its NOT phrase - the statement's
        // selection result decides, as SequentialIoEmitter.EmitNotPhrase's gate does for the I-O statements.
        string handled = $"{m}handled";
        bool gated = ecState.Active && notOnException is not null;
        if (gated) w.Line($"bool {handled} = false;");
        if (ecState.Active)
        {
            // §2023:14.6.13.1.3 1) / §2023:14.6.13.1.4 1): a conditional phrase without the NOT phrase takes the condition; otherwise the selection runs over the
            // name the message control system chose, and Table 13's fatality - known only at run time - decides the default.
            using (w.Block($"if ({m}.Raised is {{ }} {m}name)"))
            {
                if (onException is null)
                {
                    string r = ec.EmitSelection($"{m}name",
                        EcEmitter.FatalTermination($"{m}name", $"{m}.Detail ?? ({m}name + \" was set to exist by a message control statement and not resumed (ISO 14.6.13.1.3 5)/7))\")"),
                        fatalWhen: $"{m}.RaisedFatal");
                    if (gated) w.Line($"{handled} = DispatchResult.RanAHandler({r});");
                }
                else w.Line("// the statement's ON EXCEPTION phrase takes the condition (§2023:14.6.13.1.3 1); §2023:14.6.13.1.4 1))");
            }
        }
        if (onException is not null)
        {
            using (w.Block($"if (!{m}.Successful)")) Statements.EmitStatementList(onException);
            if (notOnException is not null)
                using (w.Block("else")) Statements.EmitStatementList(notOnException);
        }
        else if (notOnException is not null)
            using (w.Block($"if ({m}.Successful{(gated ? $" && !{handled}" : "")})")) Statements.EmitStatementList(notOnException);
    }
}
