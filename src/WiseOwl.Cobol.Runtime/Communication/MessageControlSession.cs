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
            area = new CommunicationArea(image);
        else
            _dialogue = dialogue;
        area.SetStatus(format, status);
        return (area.Image, status);
    }

    internal string AcceptMessageCount(string image) =>
        Statement(CommunicationFormat.Input, image, (area, _, state) =>
        {
            if (state.FindQueue(area.QueuePath()) is not { } node) return Unknown;
            area.SetMessageCount(node.CompleteMessages);
            return Ok;
        }).Area;

    /// <summary>A.2.2 and A.2.3. "No action is taken when [a statement] is executed that specifies a source or destination that is
    /// already [so], except that the value in the status key data item indicates this condition."</summary>
    internal string SetPath(string cd, CommunicationFormat format, CommunicationPath path, string image, string? key, bool enable) =>
        Statement(format, image, (area, dialogue, state) =>
        {
            switch (path)
            {
                case CommunicationPath.Input:
                    return state.FindQueue(area.QueuePath()) is { } queue
                        ? SetOne(queue, key, enable)
                        : Unknown;
                case CommunicationPath.InputTerminal:
                    return state.FindSource(area.SourceName(format)) is { } source ? SetOne(source, key, enable) : UnknownSource;
                case CommunicationPath.InputOutputTerminal:
                    if (dialogue.LockedTerminalChanged(cd, area, format)) return UnknownSource;
                    return state.FindTerminal(area.SourceName(format)) is { } terminal ? SetOne(terminal, key, enable) : UnknownSource;
                default:
                    return SetOutput(area, key, enable, state);
            }
        }).Area;

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
                return BadKey;
        bool unknown = false, already = false;
        for (int i = 1; i <= count; i++)
        {
            char errorKey = '0';
            if (state.FindDestination(area.Destination(i)) is not { } destination)
            {
                errorKey = '1';
                unknown = true;
            }
            else if (destination.Enabled == enable)
            {
                errorKey = '5';
                already = true;
            }
            else
            {
                destination.Enabled = enable;
            }
            area.SetErrorKey(i, errorKey);
        }
        return unknown && already ? Combination : unknown ? Unknown : already ? AlreadySet : Ok;
    }

    /// <summary>A.1.2.3 GR17: "If ... the value of the data item referenced by data-name-1 is outside the range of 1 through integer-1,
    /// an error condition is indicated, no action is taken for any destination".</summary>
    private static int? ValidCount(CommunicationArea area) =>
        area.DestinationCount is { } count && count >= 1 && count <= area.DestinationCapacity ? count : null;

    /// <summary>A.2.4.3: the partial message goes; "Any message that has associated with it an EMI or EGI is not affected".</summary>
    internal string Purge(string cd, CommunicationFormat format, string image) =>
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
                string name = area.Destination(i);
                char errorKey = '0';
                if (state.FindDestination(name) is not { } destination)
                {
                    errorKey = '1';
                    unknown = true;
                }
                else if (!dialogue.Sends.Remove(DestinationKey(name)))
                {
                    errorKey = '4';
                    noPortion = true;
                }
                else if (!destination.Enabled)
                {
                    errorKey = '2';
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
    internal string Send(string cd, CommunicationFormat format, string image, string? text, SendIndicator indicator) =>
        Statement(format, image, (area, dialogue, state) =>
        {
            if (format is CommunicationFormat.Output && ValidCount(area) is null) return BadCount;
            int textLength = area.TextLength(format) ?? int.MaxValue;
            if (text is not null && textLength > text.Length) return TextTooLong;
            if (indicator is SendIndicator.None && (text is null || textLength == 0)) return NoText;
            string portion = text is null ? "" : text[..textLength];
            return format is CommunicationFormat.Output
                ? SendToDestinations(area, dialogue, portion, indicator, state)
                : SendToTerminal(cd, area, dialogue, format, portion, indicator, state);
        }).Area;

    private static string SendToDestinations(CommunicationArea area, Dialogue dialogue, string portion, SendIndicator indicator, MessageStoreState state)
    {
        int count = area.DestinationCount!.Value;
        bool unknown = false, disabled = false, full = false;
        for (int i = 1; i <= count; i++)
        {
            string name = area.Destination(i);
            char errorKey = '0';
            if (state.FindDestination(name) is not { } destination)
            {
                errorKey = '1';
                unknown = true;
            }
            else if (dialogue.Release(DestinationKey(name), portion, indicator) is { } message && !destination.TryEnqueue(message))
            {
                errorKey = '6';
                full = true;
            }
            else if (!destination.Enabled)
            {
                errorKey = '2';
                disabled = true;
            }
            area.SetErrorKey(i, errorKey);
        }
        return full ? CapacityExceeded : unknown && disabled ? Combination : unknown ? Unknown : disabled ? Disabled : Ok;
    }

    /// <summary>Table A.1's "SEND input-output-cd": "Destination disabled. No action taken", where an output CD's destination holds
    /// the message until it is enabled.</summary>
    private static string SendToTerminal(string cd, CommunicationArea area, Dialogue dialogue, CommunicationFormat format, string portion, SendIndicator indicator, MessageStoreState state)
    {
        if (dialogue.LockedTerminalChanged(cd, area, format)) return Unknown;
        string name = area.SourceName(format);
        if (state.FindTerminal(name) is not { } terminal) return Unknown;
        if (!terminal.Enabled) return Disabled;
        if (dialogue.Release(TerminalKey(name), portion, indicator) is { } message) terminal.Transmit(message, state);
        return Ok;
    }

    private static string DestinationKey(string name) => "D:" + name;

    private static string TerminalKey(string name) => "T:" + name;

    /// <summary>A.2.5.3. A remainder this run unit holds answers first (GR10: "only subsequent execution of RECEIVE statements in that
    /// run unit shall cause the remaining portion of the message to be returned").</summary>
    internal CommunicationReceipt Receive(string cd, CommunicationFormat format, string image, bool segment, string into, bool noDataPhrase)
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
                        area.SetTextLength(format, 0);
                        return Ok;
                    }
                    pending = new PendingReceive(format, found.Path, found.Message);
                    dialogue.Receives.Add(pending);
                }
                filled = Transfer(dialogue, pending, area, format, segment, into);
                return Ok;
            });
            if (status != Ok) return new CommunicationReceipt(answered, into, DataAvailable: true);   // GR5 c): refused, control passes "as if data had been made available"
            if (filled is not null) return new CommunicationReceipt(answered, filled, DataAvailable: true);
            if (noDataPhrase) return new CommunicationReceipt(answered, into, DataAvailable: false);   // GR5 a): "terminated with the indication that action is complete"
            if (awaitData is not null) awaitData();   // GR5 b): another run unit, or a device, may yet make data available
            else Thread.Sleep(WaitInterval);
        }
    }

    /// <summary>A.2.5.3 GR3, GR8, GR9: the text fills the area left to right "without space fill", and A.1.2.3 GR12 and GR33 give the end key.</summary>
    private static string Transfer(Dialogue dialogue, PendingReceive pending, CommunicationArea area, CommunicationFormat format, bool segment, string into)
    {
        var (text, endKey) = pending.Next(segment, into.Length);
        if (pending.Finished) dialogue.Receives.Remove(pending);
        if (format is CommunicationFormat.Input) area.SetQueuePath(pending.Path);
        var message = pending.Message;
        area.SetReceived(format, message.Date, message.Time, message.Source, text.Length, endKey);
        return string.Concat(text, into.AsSpan(text.Length));
    }

    /// <summary>A.1.2.3 GR6 and GR28: the names of what scheduled the run unit (<see cref="MessageStore.Scheduled"/>), or spaces. <paramref name="scheduledProgram"/>: the
    /// program being activated is the one that established the run unit; any other program's INITIAL CD is initialized to spaces (§1985:XIV.2.2.4 GR28, GR6).</summary>
    internal string InitialArea(string cd, CommunicationFormat format, string image, bool scheduledProgram)
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
            if (terminal.Length > 0) _dialogue.Terminals[cd] = terminal;
        }
        return area.Image;
    }

    /// <summary>What the run unit holds between statements: the portion of each message it has released and not ended, the remainder of each message it
    /// has begun to receive, and the terminal each input-output CD first named.</summary>
    private sealed class Dialogue
    {
        internal readonly Dictionary<string, PendingSend> Sends = new(StringComparer.Ordinal);
        internal readonly List<PendingReceive> Receives = [];
        internal readonly Dictionary<string, string> Terminals = new(StringComparer.Ordinal);

        internal Dialogue Copy()
        {
            var copy = new Dialogue();
            foreach (var (key, pending) in Sends) copy.Sends[key] = pending.Copy();
            copy.Receives.AddRange(Receives.Select(pending => pending.Copy()));
            foreach (var (cd, terminal) in Terminals) copy.Terminals[cd] = terminal;
            return copy;
        }

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
        /// the run unit, a changed SYMBOLIC TERMINAL makes "the execution of any statement referencing cd-name-1 ... unsuccessful".</summary>
        internal bool LockedTerminalChanged(string cd, CommunicationArea area, CommunicationFormat format)
        {
            string name = area.SourceName(format);
            if (Terminals.TryGetValue(cd, out string? first)) return !string.Equals(first, name, StringComparison.Ordinal);
            Terminals[cd] = name;
            return false;
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
    private sealed class PendingReceive(CommunicationFormat format, string[] path, StoredMessage message)
    {
        private int _segment;
        private int _offset;

        internal PendingReceive Copy() => new(format, path, message) { _segment = _segment, _offset = _offset };

        internal CommunicationFormat Format => format;
        internal string[] Path => path;
        internal StoredMessage Message => message;
        internal bool Finished => _segment >= message.Segments.Count;

        /// <summary>Whether this remainder lies in the queue structure the area names: "If less than all the levels of the queue hierarchy
        /// are specified, the MCS determines the next message" (A.1.2.3 GR5), so the specified levels are a prefix.</summary>
        internal bool Answers(string[] specified)
        {
            for (int level = 0; level < specified.Length && specified[level].Length > 0; level++)
                if (level >= path.Length || !string.Equals(specified[level], path[level], StringComparison.Ordinal))
                    return false;
            return true;
        }

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
