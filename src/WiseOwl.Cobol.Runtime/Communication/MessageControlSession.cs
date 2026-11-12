// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// One run unit's dialogue with the message control system (ISO/IEC 1989:2002 Annex A.2). What the run unit holds, its <see cref="Dialogue"/>,
/// lives here and none of it in <see cref="MessageStore"/>, and that is ISO/IEC 1989:2002 14.5.10 rule 2 by construction: "remaining portions of
/// any partially-received messages are eliminated from the queue. Any portions of messages transferred from the run unit by a SEND statement, and
/// not terminated by an EMI or EGI, are purged from the system."
/// <para>⛔ EVERY STATEMENT GOES THROUGH <see cref="Statement"/>, and that is what makes a 9x status mean no action (Table A.1, docs/CONFORMANCE.md):
/// the statement acts on a copy of the CD's area and a copy of the dialogue, and only a store that committed publishes them.</para>
/// </summary>
/// <param name="store">The store; null opens the one the run unit's environment names, at the first statement.</param>
/// <param name="awaitData">What a RECEIVE that found no data does before it asks again (A.2.5.3 GR5 b); null is the polling interval.</param>
internal sealed class MessageControlSession(MessageStore? store = null, Action? awaitData = null) : IDisposable
{
    /// <summary>The status key values of Table A.1 this implementation reports.</summary>
    private const string Ok = "00", Disabled = "10", AlreadySet = "15", Unknown = "20", UnknownSource = "21", BadCount = "30",
        BadKey = "40", TextTooLong = "50", NoText = "60", CapacityExceeded = "65", NoPortion = "70", Combination = "80";

    /// <summary>The error key values of Table 2 (§1985:XIV.2.2.6) this implementation reports.</summary>
    private const char NoErrorKey = '0', UnknownKey = '1', DisabledKey = '2', NoPartialKey = '4', AlreadyKey = '5', FullKey = '6';

    /// <summary>The polling interval of a RECEIVE that waits for data (A.2.5.3 GR5 b).</summary>
    private static readonly TimeSpan WaitInterval = TimeSpan.FromMilliseconds(100);

    private Dialogue _dialogue = new();

    private MessageStore Store => store ??= MessageStore.Open();

    /// <summary>The end of the run unit (<c>RunUnit.Terminate</c>): the store's handle, endpoint and presence byte given back. Idempotent; a store never opened is nothing to close.</summary>
    public void Dispose() => store?.Dispose();

    /// <summary>One statement over one area: the action's area and dialogue become the run unit's only when the store committed. A refused statement
    /// answers the area it was handed with only the status key changed.</summary>
    private (string Area, string Status) Statement(CommunicationFormat format, string image, Func<CommunicationArea, Dialogue, MessageStoreState, string> action)
    {
        var area = new CommunicationArea(image);
        var dialogue = _dialogue.Copy();
        string status = Store.Run(state => action(area, dialogue, state));
        if (MessageStore.IsRefusal(status))
        {
            area = new CommunicationArea(image);
            dialogue.UndoLocks();
        }
        else
        {
            _dialogue = dialogue;
        }
        area.SetStatus(format, status);
        return (area.Image, status);
    }

    /// <summary>§1985:XIV.3.1.4 GR1 counts "the number of complete messages that exist in the queue structure", and a message a run unit has begun to
    /// receive still exists in it until its last portion is returned (§1985:VI.6.24.4 GR3: STOP RUN removes "any message partially received by that run
    /// unit" from the queue): the store's <c>PartialReceives</c> rows add those of every live run unit (<see cref="MessageStoreState.PartlyReceivedFrom"/>, docs/CONFORMANCE.md §3.3).</summary>
    internal string AcceptMessageCount(string image) =>
        Statement(CommunicationFormat.Input, image, (area, dialogue, state) =>
        {
            string[] path = area.QueuePath();
            if (state.FindQueue(path) is not { } node) return Unknown;
            area.SetMessageCount((long)node.CompleteMessages + state.PartlyReceivedFrom(path));
            return Ok;
        }).Area;

    /// <summary>A.2.2 and A.2.3. "No action is taken when [a statement] is executed that specifies a source or destination that is
    /// already [so], except that the value in the status key data item indicates this condition."</summary>
    internal string SetPath(CdActivation cd, CommunicationFormat format, CommunicationPath path, string image, string? key, bool enable) =>
        Statement(format, image, (area, dialogue, state) =>
        {
            switch (path)
            {
                case CommunicationPath.Input:
                    string[] queuePath = area.QueuePath();
                    if (state.FindQueue(queuePath) is { } queue) return SetPaths(queue, key, enable, () => state.SetQueuePaths(queue, enable));
                    // The levels the MCS knows are judged for condition 15 beside the unknown ones below them: "new communication status key condition 15 and
                    // communication status key condition 20 both apply" is 80 (§1985:B.2.2). Neither answer takes any action.
                    return state.FindQueuePrefix(queuePath) is { } known && state.QueuePathsAre(known, enable) ? Combination : Unknown;
                case CommunicationPath.InputTerminal:
                    return state.FindSource(area.SourceName(format)) is { } source
                        ? SetPaths(source, key, enable, () => state.SetSourcePaths(source, enable))
                        : UnknownSource;
                case CommunicationPath.InputOutputTerminal:
                    if (dialogue.LockedTerminalChanged(cd, area, format)) return UnknownSource;
                    return state.FindTerminal(area.SourceName(format)) is { } terminal ? SetOne(terminal, key, enable) : UnknownSource;
                default:
                    return SetOutput(area, key, enable, state);
            }
        }).Area;

    /// <summary>The statements that act on logical paths (§1985:XIV.3.2.4 GR3, GR4; §1985:XIV.3.3.4 GR2, GR3): a statement that finds every path it names already so changes nothing.</summary>
    private static string SetPaths(IMessageEndpoint endpoint, string? key, bool enable, Func<bool> set) =>
        !endpoint.Admits(key) ? BadKey : set() ? Ok : AlreadySet;

    private static string SetOne(IMessageEndpoint endpoint, string? key, bool enable)
    {
        if (!endpoint.Admits(key)) return BadKey;
        if (endpoint.Enabled == enable) return AlreadySet;
        endpoint.Enabled = enable;
        return Ok;
    }

    /// <summary>A.2.2.3 GR6, A.2.3.3 GR5, Table A.2. A wrong key names the whole statement: the COBOL-85 status 40 takes no action.</summary>
    private static string SetOutput(CommunicationArea area, string? key, bool enable, MessageStoreState state)
    {
        if (ValidCount(area) is not { } count) return BadCount;
        for (int i = 1; i <= count; i++)
            if (state.FindDestination(area.Destination(i)) is { } checkedKey && !checkedKey.Admits(key))
            {
                SetErrorKeys(area, count, state, destination => destination.Enabled == enable ? AlreadyKey : NoErrorKey);
                return BadKey;
            }
        // §1985:XIV.3.2.4 GR1 and §1985:XIV.3.3.4 GR1 judge each occurrence by what the destination was when the statement began: every occurrence is judged
        // first and the destinations are changed after, so a destination named twice that was connected is not "already disconnected" by its second occurrence (kb/Work PB5376).
        bool unknown = false, already = false;
        var toChange = new List<DestinationEndpoint>();
        for (int i = 1; i <= count; i++)
        {
            char errorKey = NoErrorKey;
            if (state.FindDestination(area.Destination(i)) is not { } destination)
            {
                errorKey = UnknownKey;
                unknown = true;
            }
            else if (destination.Enabled == enable)
            {
                errorKey = AlreadyKey;
                already = true;
            }
            else
            {
                toChange.Add(destination);
            }
            area.SetErrorKey(i, errorKey);
        }
        foreach (var destination in toChange) destination.Enabled = enable;
        return unknown && already ? Combination : unknown ? Unknown : already ? AlreadySet : Ok;
    }

    /// <summary>§1985:XIV.2.2.4 GR22: when the MCS finds an error in DISABLE OUTPUT, ENABLE OUTPUT, PURGE or SEND, "each occurrence of data-name-4, up to
    /// and including the occurrence specified by the content of data-name-1, [is] updated". A statement refused as a whole before any destination is acted
    /// on (a wrong key, text too long, no text) still writes each occurrence, with what Table 2 says of that destination: unknown, or the
    /// <paramref name="known"/> value of a destination the MCS holds. The statement's own condition is the status key's (docs/CONFORMANCE.md §3.3).</summary>
    private static void SetErrorKeys(CommunicationArea area, int count, MessageStoreState state, Func<DestinationEndpoint, char> known)
    {
        for (int i = 1; i <= count; i++)
            area.SetErrorKey(i, state.FindDestination(area.Destination(i)) is { } destination ? known(destination) : UnknownKey);
    }

    /// <summary>A.1.2.3 GR17: "If ... the value of the data item referenced by data-name-1 is outside the range of 1 through integer-1,
    /// an error condition is indicated, no action is taken for any destination".</summary>
    private static int? ValidCount(CommunicationArea area) =>
        area.DestinationCount is { } count && count >= 1 && count <= area.DestinationCapacity ? count : null;

    /// <summary>A.2.4.3: the partial message goes; "Any message that has associated with it an EMI or EGI is not affected".</summary>
    internal string Purge(CdActivation cd, CommunicationFormat format, string image) =>
        Statement(format, image, (area, dialogue, state) =>
        {
            if (format is CommunicationFormat.InputOutput)
            {
                if (dialogue.LockedTerminalChanged(cd, area, format)) return Unknown;
                string name = area.SourceName(format);
                if (state.FindTerminal(name) is not { } terminal) return Unknown;
                if (!dialogue.Sends.Remove(TerminalKey(name))) return NoPortion;
                return terminal.Enabled ? Ok : Disabled;
            }
            if (ValidCount(area) is not { } count) return BadCount;
            bool unknown = false, disabled = false, noPortion = false;
            for (int i = 1; i <= count; i++)
            {
                char errorKey = NoErrorKey;
                if (state.FindDestination(area.Destination(i)) is not { } destination)
                {
                    errorKey = UnknownKey;
                    unknown = true;
                }
                else if (!dialogue.Sends.Remove(DestinationKey(area, i)))
                {
                    errorKey = NoPartialKey;
                    noPortion = true;
                }
                else if (!destination.Enabled)
                {
                    errorKey = DisabledKey;
                    disabled = true;
                }
                area.SetErrorKey(i, errorKey);
            }
            return (unknown, disabled) switch
            {
                (true, true) => Combination,
                (true, false) => Unknown,
                (false, true) => Disabled,
                _ => noPortion ? NoPortion : Ok,
            };
        }).Area;

    /// <summary>A.2.6.3. The order of the refusals is the order of the rules that take no action: the destination count (A.1.2.3
    /// GR17), the text length (GR3, status 50), and a portion with no text (Table A.1, status 60; GR9's last sentence).</summary>
    internal string Send(CdActivation cd, CommunicationFormat format, string image, string? text, SendIndicator indicator) =>
        Statement(format, image, (area, dialogue, state) =>
        {
            // GR31 binds every statement that references the CD, so the terminal is fixed before any status is decided (kb/Work PB5283).
            if (format is CommunicationFormat.InputOutput && dialogue.LockedTerminalChanged(cd, area, format)) return Unknown;
            if (format is CommunicationFormat.Output && ValidCount(area) is null) return BadCount;
            int textLength = area.TextLength(format) ?? int.MaxValue;
            string? refusal = text is not null && textLength > text.Length ? TextTooLong
                : indicator is SendIndicator.None && (text is null || textLength == 0) ? NoText
                : null;
            if (refusal is not null)
            {
                // GR22: the statement is refused before any destination, and each occurrence still says what Table 2 says of its destination.
                if (format is CommunicationFormat.Output)
                    SetErrorKeys(area, area.DestinationCount!.Value, state, destination => destination.Enabled ? NoErrorKey : DisabledKey);
                return refusal;
            }
            string portion = text is null ? "" : text[..textLength];
            return format is CommunicationFormat.Output
                ? SendToDestinations(area, dialogue, portion, indicator, state)
                : SendToTerminal(area, dialogue, format, portion, indicator, state);
        }).Area;

    private static string SendToDestinations(CommunicationArea area, Dialogue dialogue, string portion, SendIndicator indicator, MessageStoreState state)
    {
        int count = area.DestinationCount!.Value;
        bool unknown = false, disabled = false, full = false;
        for (int i = 1; i <= count; i++)
        {
            char errorKey = NoErrorKey;
            if (state.FindDestination(area.Destination(i)) is not { } destination)
            {
                errorKey = UnknownKey;
                unknown = true;
            }
            else if (dialogue.Release(DestinationKey(area, i), portion, indicator) is { } message && !destination.TryEnqueue(message))
            {
                errorKey = FullKey;
                full = true;
            }
            else if (!destination.Enabled)
            {
                errorKey = DisabledKey;
                disabled = true;
            }
            area.SetErrorKey(i, errorKey);
        }
        return full ? CapacityExceeded : unknown && disabled ? Combination : unknown ? Unknown : disabled ? Disabled : Ok;
    }

    /// <summary>Table A.1's "SEND input-output-cd": "Destination disabled. No action taken", where an output CD's destination holds
    /// the message until it is enabled.</summary>
    private static string SendToTerminal(CommunicationArea area, Dialogue dialogue, CommunicationFormat format, string portion, SendIndicator indicator, MessageStoreState state)
    {
        string name = area.SourceName(format);
        if (state.FindTerminal(name) is not { } terminal) return Unknown;
        if (!terminal.Enabled) return Disabled;
        if (dialogue.Release(TerminalKey(name), portion, indicator) is { } message) terminal.Transmit(message, state);
        return Ok;
    }

    /// <summary>The message this run unit is releasing to one occurrence of the DESTINATION TABLE. A destination named in two occurrences is a destination
    /// of the statement in each (§1985:XIV.3.6.4 GR6, docs/CONFORMANCE.md §3.3), so the key is the name and which occurrence of that name this is: two
    /// occurrences hold two messages, each with a segment and message control of its own (kb/Work PB5288). The separator is a control character, which no
    /// symbolic name holds (<see cref="MessageDefinitions"/>).</summary>
    private static string DestinationKey(CommunicationArea area, int occurrence)
    {
        string name = area.Destination(occurrence);
        int earlier = 0;
        for (int i = 1; i < occurrence; i++)
            if (McsNames.Comparer.Equals(area.Destination(i), name)) earlier++;
        return $"D:{McsNames.Key(name)}\u0001{earlier}";
    }

    private static string TerminalKey(string name) => "T:" + McsNames.Key(name);

    /// <summary>A.2.5.3. A remainder this run unit holds answers first (GR10: "only subsequent execution of RECEIVE statements in that
    /// run unit shall cause the remaining portion of the message to be returned").</summary>
    internal CommunicationReceipt Receive(CdActivation cd, CommunicationFormat format, string image, bool segment, string into, bool noDataPhrase)
    {
        while (true)
        {
            string? filled = null;
            var (answered, status) = Statement(format, image, (area, dialogue, state) =>
            {
                string[] path;
                if (format is CommunicationFormat.InputOutput)
                {
                    if (dialogue.LockedTerminalChanged(cd, area, format)) return UnknownSource;
                    string terminal = area.SourceName(format);
                    if (state.FindTerminal(terminal) is null) return UnknownSource;
                    path = [terminal];
                }
                else
                {
                    path = area.QueuePath();
                    if (state.FindQueue(path) is null) return Unknown;
                }
                var pending = dialogue.Receives.Find(r => r.Format == format && r.Answers(path));
                if (pending is null)
                {
                    var next = format is CommunicationFormat.Input
                        ? state.TakeFromQueue(path)
                        : state.TakeFromTerminal(path[0]);
                    if (next is not { } found)   // no data: the caller decides between GR5 a) and b)
                    {
                        area.SetNothingReceived(format);
                        return Ok;
                    }
                    pending = new PendingReceive(format, found.Path, found.Message, dialogue.NextReceive++);
                    dialogue.Receives.Add(pending);
                }
                filled = Transfer(dialogue, pending, area, format, segment, into, state);
                return Ok;
            });
            if (status != Ok) return new CommunicationReceipt(answered, into, DataAvailable: true);   // GR5 c): refused, control passes "as if data had been made available"
            if (filled is not null) return new CommunicationReceipt(answered, filled, DataAvailable: true);
            if (noDataPhrase) return new CommunicationReceipt(answered, into, DataAvailable: false);   // GR5 a): "terminated with the indication that action is complete"
            if (awaitData is not null) awaitData();   // GR5 b): another run unit, or a device, may yet make data available
            else Thread.Sleep(WaitInterval);
        }
    }

    /// <summary>§1985:XIV.3.1.4 GR1: a message of an input CD stays counted in its queue structure, to every run unit, from its first portion until its last, so the store's
    /// <c>PartialReceives</c> row is written when a portion leaves the message unfinished and removed when the last portion is returned.</summary>
    private static void RecordProgress(Dialogue dialogue, PendingReceive pending, CommunicationFormat format, MessageStoreState state)
    {
        if (pending.Finished) dialogue.Receives.Remove(pending);
        if (format is not CommunicationFormat.Input) return;
        if (pending.Finished && pending.Recorded)
            state.EndPartialReceive(pending.Serial);
        else if (!pending.Finished && !pending.Recorded)
            state.BeginPartialReceive(pending.Serial, pending.Path);
        pending.Recorded = !pending.Finished;
    }

    /// <summary>A.2.5.3 GR3, GR8, GR9: the text fills the area left to right "without space fill", and A.1.2.3 GR12 and GR33 give the end key.</summary>
    private static string Transfer(Dialogue dialogue, PendingReceive pending, CommunicationArea area, CommunicationFormat format, bool segment, string into, MessageStoreState state)
    {
        var (text, endKey) = pending.Next(segment, into.Length);
        RecordProgress(dialogue, pending, format, state);
        if (format is CommunicationFormat.Input) area.SetQueuePath(pending.Path);
        var message = pending.Message;
        // GR10: the symbolic name of a source "not known to the MCS" is reported as spaces (kb/Work PB5280).
        area.SetReceived(format, message.Date, message.Time, state.KnowsCommunicationTerminal(message.Source) ? message.Source : "", text.Length, endKey);
        return string.Concat(text, into.AsSpan(text.Length));
    }

    /// <summary>A.1.2.3 GR6 and GR28: the names of what scheduled the run unit (<see cref="MessageStore.Scheduled"/>), or spaces. <paramref name="scheduledProgram"/>: the
    /// program being activated is the one that established the run unit; any other program's INITIAL CD is initialized to spaces (§1985:XIV.2.2.4 GR28, GR6).</summary>
    internal string InitialArea(CdActivation cd, CommunicationFormat format, string image, bool scheduledProgram)
    {
        var area = new CommunicationArea(image);
        string[] scheduled = scheduledProgram ? Store.Scheduled : [];
        if (format is CommunicationFormat.Input)
            area.SetQueuePath(scheduled);
        else
        {
            string terminal = scheduled.Length > 0 ? scheduled[0] : "";
            area.SetTerminal(terminal);
            // GR30: the name of the terminal that scheduled the run unit "shall not be changed by the program".
            if (terminal.Length > 0) cd.Terminal = terminal;
        }
        return area.Image;
    }

    /// <summary>What the run unit holds between statements: the portion of each message it has released and not ended, the remainder of each message it
    /// has begun to receive. (The terminal each input-output CD first named is the CD's own, <see cref="CdActivation"/>.)</summary>
    private sealed class Dialogue
    {
        internal readonly Dictionary<string, PendingSend> Sends = new(StringComparer.Ordinal);
        internal readonly List<PendingReceive> Receives = [];
        private readonly List<CdActivation> _lockedHere = [];

        internal Dialogue Copy()
        {
            var copy = new Dialogue();
            foreach (var (key, pending) in Sends) copy.Sends[key] = pending.Copy();
            copy.NextReceive = NextReceive;
            copy.Receives.AddRange(Receives.Select(pending => pending.Copy()));
            return copy;
        }

        /// <summary>The serial the next message this run unit begins to receive takes: the key of its <c>PartialReceives</c> row.</summary>
        internal ulong NextReceive;

        /// <summary>A.2.6.3 GR6, GR10, GR11: add one portion to the message this run unit is releasing to <paramref name="key"/>, closing
        /// the segment on any indicator. The message, once an EMI or EGI ends it, goes to the MCS; until then it "does not logically exist
        /// for the MCS" (GR7).</summary>
        internal StoredMessage? Release(string key, string portion, SendIndicator indicator)
        {
            if (!Sends.TryGetValue(key, out var pending)) Sends[key] = pending = new PendingSend();
            pending.Segment.Append(portion);
            if (indicator is SendIndicator.None) return null;
            pending.Segments.Add(pending.Segment.ToString());
            pending.Segment.Clear();
            if (indicator is SendIndicator.EndOfSegment) return null;
            Sends.Remove(key);
            return StoredMessage.Complete(pending.Segments, indicator is SendIndicator.EndOfGroup, RunUnit.Current.Clock.Now());
        }

        /// <summary>A.1.2.3 GR30 and GR31: after the first statement referencing an input-output CD, or from the start when the MCS scheduled
        /// the run unit, a changed SYMBOLIC TERMINAL makes "the execution of any statement referencing cd-name-1 ... unsuccessful". "Changed" is of the CONTENT of the
        /// data item, so the comparison is exact: lowercase and uppercase letters are equivalent in the words of the source text, not in an item's content
        /// (kb/Work PB5381). The lock is the activation's (<paramref name="cd"/>), fixed by the first statement that references the CD, whatever its status.</summary>
        internal bool LockedTerminalChanged(CdActivation cd, CommunicationArea area, CommunicationFormat format)
        {
            string name = area.SourceName(format);
            if (cd.Terminal is { } first) return !string.Equals(first, name, StringComparison.Ordinal);
            cd.Terminal = name;
            _lockedHere.Add(cd);
            return false;
        }

        /// <summary>A refused statement (a 9x status, no action) fixed no terminal: the locks it set are lifted.</summary>
        internal void UndoLocks()
        {
            foreach (var cd in _lockedHere) cd.Terminal = null;
            _lockedHere.Clear();
        }
    }

    private sealed class PendingSend
    {
        internal readonly List<string> Segments = [];
        internal readonly StringBuilder Segment = new();

        internal PendingSend Copy()
        {
            var copy = new PendingSend();
            copy.Segments.AddRange(Segments);
            copy.Segment.Append(Segment);
            return copy;
        }
    }

    /// <summary>The message this run unit has begun to receive from <see cref="Path"/>; the remainder is "treated as a new message" or
    /// "a new segment" by the next RECEIVE (GR8 c, GR9 c).</summary>
    private sealed class PendingReceive(CommunicationFormat format, string[] path, StoredMessage message, ulong serial)
    {
        private int _segment;
        private int _offset;

        internal PendingReceive Copy() => new(format, path, message, serial) { _segment = _segment, _offset = _offset, Recorded = Recorded };

        /// <summary>The key of this message's <c>PartialReceives</c> row among the run unit's (an input CD's only).</summary>
        internal ulong Serial => serial;

        /// <summary>Whether the store holds the message's <c>PartialReceives</c> row.</summary>
        internal bool Recorded { get; set; }

        internal CommunicationFormat Format => format;
        internal string[] Path => path;
        internal StoredMessage Message => message;
        internal bool Finished => _segment >= message.Segments.Count;

        /// <summary>Whether this remainder lies in the queue structure the area names: "If less than all the levels of the queue hierarchy
        /// are specified, the MCS determines the next message" (A.1.2.3 GR5), so the specified levels are a prefix.</summary>
        internal bool Answers(string[] specified) => MessageStoreState.LevelIsUnder(path, specified);

        /// <summary>The next text of at most <paramref name="room"/> characters, and the end key A.1.2.3 GR12 and GR33 give it: 3 at the end of a
        /// group, 2 of a message, 1 of a segment (the SEGMENT phrase alone), 0 for less than that. "When more than one of the above conditions is
        /// satisfied simultaneously, the rule first satisfied in the order listed determines the content".</summary>
        internal (string Text, char EndKey) Next(bool segmentPhrase, int room)
        {
            var text = new StringBuilder();
            while (_segment < message.Segments.Count)
            {
                string current = message.Segments[_segment];
                int take = Math.Min(current.Length - _offset, room - text.Length);
                text.Append(current, _offset, take);
                _offset += take;
                if (_offset < current.Length) return (text.ToString(), '0');   // less than a message or segment
                _segment++;
                _offset = 0;
                if (_segment == message.Segments.Count) return (text.ToString(), message.EndOfGroup ? '3' : '2');
                if (segmentPhrase) return (text.ToString(), '1');
            }
            return (text.ToString(), message.EndOfGroup ? '3' : '2');
        }
    }
}
