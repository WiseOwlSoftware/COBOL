// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime.Messaging;

namespace WiseOwl.Cobol.Runtime;

/// <summary>How a RECEIVE waits (ISO §2023:14.9.31.4 GR2; docs/rearchitecture/DESIGN-message-control.md §0 Q5).</summary>
public enum MessageWaitKind
{
    /// <summary>No CONTINUE phrase: until a message or an exception arrives.</summary>
    Forever,

    /// <summary>CONTINUE AFTER MESSAGE RECEIVED: ask once.</summary>
    MessageReceived,

    /// <summary>CONTINUE AFTER n SECONDS, n already evaluated by <see cref="CobolTiming.Interval(double)"/>'s rule (whole seconds, at most <see cref="CobolTiming.MaxSeconds"/>, -1 for a value below zero); zero and a negative value ask once.</summary>
    Seconds,
}

/// <summary>How long a RECEIVE waits for a message (§2023:14.9.31.4 GR2).</summary>
public readonly record struct MessageWait(MessageWaitKind Kind, long Seconds)
{
    internal static readonly MessageWait Forever = new(MessageWaitKind.Forever, 0);
    internal static readonly MessageWait Poll = new(MessageWaitKind.MessageReceived, 0);

    /// <summary>True when the RECEIVE asks once and never waits.</summary>
    internal bool AsksOnce => Kind == MessageWaitKind.MessageReceived || (Kind == MessageWaitKind.Seconds && Seconds <= 0);
}

/// <summary>
/// How one SEND or RECEIVE ended, as the generated statement needs it (docs/rearchitecture/DESIGN-message-control.md §5.7).
/// <para><see cref="Successful"/> chooses the ON EXCEPTION or NOT ON EXCEPTION phrase whatever the checking (§0 Q8). <see cref="Raised"/> is the level-3
/// exception-name the statement set to exist, non-null only when checking for it is enabled at the statement (§2023:14.6.13.1.1) and the unsuccessful
/// execution is a condition at all; the generated code then runs the §2023:14.6.13.1.3 / §2023:14.6.13.1.4 selection over it, and <see cref="RaisedFatal"/> is
/// Table 13's fatality of that name. <see cref="Image"/>, <see cref="Extents"/> and <see cref="Slots"/> are the message a RECEIVE took (an unsuccessful RECEIVE
/// of a reply that raised, or of a message longer than identifier-1, still carries its message, §0 Q6, Q7); <see cref="Length"/> is what data-name-2 receives, or
/// null when it stays unchanged; <see cref="Tag"/> is the server tag a SEND Format 1 returns or the requestor tag a RECEIVE reports.</para>
/// </summary>
public readonly record struct MessageOutcome(
    bool Successful, MessageTag Tag, string? Image, RecordExtents? Extents, MessageSlot[]? Slots, long? Length,
    string? Raised, bool RaisedFatal, string? Detail);

/// <summary>
/// ⛔ THE ASYNCHRONOUS MESSAGING FACILITY'S STATEMENTS, AS GENERATED CODE CALLS THEM (ISO §2023:14.9.38 SEND, §2023:14.9.31 RECEIVE;
/// docs/rearchitecture/DESIGN-message-control.md §5.7, §4.5; kb/Work PB4744). Each entry runs the statement on the current run unit's
/// <see cref="MessagingPeer"/> (which blocks until the protocol completes it) and then does the one thing the peer cannot: the statement's EXCEPTION CONDITION.
/// The peer names an unsuccessful execution's condition; whether it is RAISED is the statement's own checking (§2023:14.6.13.1.1 — "it is raised only if checking
/// for that exception condition is enabled"), so the entry asks the statement's <see cref="EcCheckingProfile"/>, and only then sets the last exception status.
/// <para>⛔ ONE MECHANISM FOR EVERY NAME. The message control system chooses the name (EC-MCS-NO-SERVER, EC-MCS-INVALID-TAG, the termination replies) and a server
/// chooses the name its requestor learns (§2023:14.9.38.4 GR8, §0 Q6), so no compile-time flag could name it: the statement carries its profile into the run
/// (<c>IProfiledStatement</c>), as an activating statement does for a name <c>GOBACK … RAISING LAST</c> stages. A level-1 or level-2 name a server forwards
/// sets nothing (Q6: "Only the lowest level exception-names are associated with exception status indicators", §2023:14.6.13.1.1).</para>
/// </summary>
public static class AsyncMessaging
{
    /// <summary>SEND Format 1 (§2023:14.9.38.4 GR4 to GR6): offers the message image to the run unit that serves <paramref name="server"/> and waits until one takes it.</summary>
    public static MessageOutcome SendToServer(string server, string image, RecordExtents? extents, MessageSlot[] slots, string checking) =>
        Outcome(RunUnit.Current.Messaging.Send(server, MessageBody.Carrying(image, extents, slots)), checking);

    /// <summary>SEND Format 2 (§2023:14.9.38.4 GR7, GR8): answers the requestor <paramref name="to"/> names. <paramref name="raising"/> is the written
    /// exception-name, <paramref name="raisingLast"/> the LAST phrase, which sends "the name of the last exception raised in the current run unit" or none.</summary>
    public static MessageOutcome SendResponse(MessageTag to, string image, RecordExtents? extents, MessageSlot[] slots, string? raising, bool raisingLast, string checking) =>
        Outcome(RunUnit.Current.Messaging.SendResponse(to, MessageBody.Carrying(image, extents, slots), raisingLast ? LastExceptionName() : raising), checking);

    /// <summary>The capacity of a message area that holds an OCCURS DYNAMIC table: it has no allocated maximum (§2023:8.5.1.9.1 - the number of occurrences is the implementor's
    /// maximum), and a RECEIVE recreates the table to the message, so no message is too long for it (kb/Work PB4744). The size the area then took, which the run unit's end
    /// answers an unanswered message with (§2023:14.6.11 7)), is stated by the program after its store (<see cref="Received"/>).</summary>
    public const int UnboundedCapacity = int.MaxValue;

    /// <summary>After a successful RECEIVE into an area of <see cref="UnboundedCapacity"/> has stored the message: the area is <paramref name="size"/> character positions, the
    /// temporary's size an implicit SEND at the run unit's end uses for the message <paramref name="tag"/> names (ISO §2023:14.6.11 7), §2023:14.6.12).</summary>
    public static void Received(MessageTag tag, int size) => RunUnit.Current.Messaging.Received(tag, size);

    /// <summary>RECEIVE (§2023:14.9.31.4): <paramref name="capacity"/> is identifier-1's maximum length in character positions, or <see cref="UnboundedCapacity"/>.</summary>
    public static MessageOutcome Receive(MessageTag from, int capacity, MessageWait wait, string checking) =>
        Outcome(RunUnit.Current.Messaging.Receive(from, capacity, wait), checking, lessThanZero: wait.Kind == MessageWaitKind.Seconds && wait.Seconds < 0);

    /// <summary>The slots a message carries beside its image (§2023:14.9.38.4 GR4 - "The message contains the content of" identifier-1; DOC-A.1-212; docs/rearchitecture/
    /// DESIGN-message-control.md section 4.4; kb/Work PB4957): one for every message tag that is not NULL in <paramref name="area"/> - an elementary message tag, or a
    /// group's leaf vector (<c>AsLeaves</c>: a nested group is a nested vector, a table an array of its occurrences) - each beside the token its positions hold in the
    /// image, the one <see cref="MessageTagImage.Of"/> renders. A NULL tag's positions are eight X"00", which decode to NULL with no slot. Every other leaf is image.</summary>
    public static MessageSlot[] SlotsOf(object? area)
    {
        var slots = new List<MessageSlot>();
        Collect(area);
        return [.. slots];

        void Collect(object? value)
        {
            if (value is MessageTag tag) { if (!tag.IsNull) slots.Add(new MessageSlot(MessageTagImage.Of(tag), tag)); }
            else if (value is Array elements and not byte[] and not char[])
                foreach (object? element in elements) Collect(element);
        }
    }

    /// <summary>⛔ A RECEIVED MESSAGE TAG IS WHAT THE MESSAGE HOLDS AT ITS POSITIONS (§2023:14.9.31.4 GR1b - "The content of identifier-1 is the content of the message
    /// sent by that SEND statement"; kb/Work PB4957). <paramref name="decoded"/> is the receiving tag as its positions decode in this run unit (<see cref="MessageTagImage.TagOf"/>,
    /// the one decode a file record's tag takes): eight X"00" are NULL, and a token this run unit did not assign is an unresolved tag that keeps its positions. Such a
    /// token that is one the message carries a slot for is that slot's tag - the sending run unit's identifier, which this one learns from the slot alone.
    /// Every other content stays what it decoded to, as it would from a file (DOC-A.1-212): a position past the end of a shorter message is a space.</summary>
    internal static MessageTag Resolve(MessageTag decoded, MessageSlot[]? slots)
    {
        if (decoded.Party == MessageParty.Unresolved && slots is not null)
            foreach (var slot in slots)
                if (string.Equals(slot.Token, decoded.Name, StringComparison.Ordinal)) return slot.Tag;
        return decoded;
    }

    /// <summary>A received message area's leaf vector with every message tag <see cref="Resolve"/>d against the message's <paramref name="slots"/>: the vector of the
    /// area just stored from the image, whose tags hold what their positions decode to (a record struct's <c>FromImage</c>; a storage cell's positions,
    /// <c>PlaceRenderer.ImagedGroupLeaves</c>).</summary>
    public static object?[] Resolved(object?[] leaves, MessageSlot[]? slots)
    {
        return (object?[])Replace(leaves)!;

        object? Replace(object? value)
        {
            if (value is MessageTag tag) return Resolve(tag, slots);
            if (value is not Array elements || value is byte[] or char[] || !MayHoldTags(elements.GetType().GetElementType()!)) return value;
            var copy = (Array)elements.Clone();
            for (int i = 0; i < copy.Length; i++) copy.SetValue(Replace(elements.GetValue(i)), i);
            return copy;
        }

        static bool MayHoldTags(Type element) => element == typeof(MessageTag) || element == typeof(object) || element.IsArray;
    }

    /// <summary>The content of an ELEMENTARY message tag a message was received into: its 8 positions of the message <paramref name="image"/> (a shorter message
    /// leaves the rest spaces, §2023:14.9.31.4 GR1b, as a group move does), decoded and <see cref="Resolve"/>d as a group's tag is.</summary>
    public static MessageTag TagOf(string image, MessageSlot[]? slots) =>
        Resolve(MessageTagImage.TagOf(image.Length >= PointerImage.Width ? image[..PointerImage.Width] : image.PadRight(PointerImage.Width)), slots);

    private static string? LastExceptionName() =>
        ExceptionState.LastName is { } name && name != ExceptionState.ObjectSentinel ? name : null;   // an exception OBJECT has no name to send (GR8: none raised sends none)

    private const string ContinueLessThanZero = "EC-CONTINUE-LESS-THAN-ZERO";

    /// <param name="lessThanZero">A RECEIVE whose CONTINUE AFTER value is negative (§0 Q5): the value is zero, and EC-CONTINUE-LESS-THAN-ZERO is set to exist as
    /// §2023:14.9.9.4 GR1 b) does for the CONTINUE statement; nonfatal, and a RECEIVE that also failed reports its own condition instead.</param>
    private static MessageOutcome Outcome(StatementResult result, string checking, bool lessThanZero = false)
    {
        string? raised = null;
        bool fatal = false;
        if ((result.ExceptionName ?? (lessThanZero ? ContinueLessThanZero : null)) is { } name
            && ExceptionCatalog.TryGet(name, out var info) && info.Level == 3      // a level-1 or level-2 name sets no condition (Q6)
            && EcCheckingProfile.Of(checking).Enabled(info.Name))
        {
            fatal = info.IsFatal;
            ExceptionState.Set(info.Name, fatal);
            raised = info.Name;
        }
        return new MessageOutcome(result.Successful, result.Tag, result.Image, result.Extents, result.Slots, result.Length, raised, fatal, result.Detail);
    }
}
