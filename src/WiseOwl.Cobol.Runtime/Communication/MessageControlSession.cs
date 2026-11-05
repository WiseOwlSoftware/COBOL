// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// One run unit's dialogue with the message control system (ISO/IEC 1989:2002 Annex A.2): the portion of each message it has
/// released and not ended, the remainder of each message it has begun to receive, and the terminal each input-output CD first
/// named. All of it lives in the run unit and none of it in <see cref="MessageStore"/>, and that is ISO/IEC 1989:2002 14.5.10
/// rule 2 by construction: "remaining portions of any partially-received messages are eliminated from the queue. Any portions of
/// messages transferred from the run unit by a SEND statement, and not terminated by an EMI or EGI, are purged from the system."
/// </summary>
internal sealed class MessageControlSession(MessageStore? store = null)
{
    /// <summary>The status key values of Table A.1 this implementation reports.</summary>
    private const string Ok = "00", Disabled = "10", AlreadySet = "15", Unknown = "20", UnknownSource = "21", BadCount = "30",
        BadKey = "40", TextTooLong = "50", NoText = "60", CapacityExceeded = "65", NoPortion = "70", Combination = "80";

    /// <summary>The polling interval of a RECEIVE that waits for data (A.2.5.3 GR5 b).</summary>
    private static readonly TimeSpan WaitInterval = TimeSpan.FromMilliseconds(100);

    private readonly Dictionary<string, PendingSend> _sends = new(StringComparer.Ordinal);
    private readonly List<PendingReceive> _receives = [];
    private readonly Dictionary<string, string> _terminals = new(StringComparer.Ordinal);

    private MessageStore Store => store ??= MessageStore.Open();

    internal string AcceptMessageCount(string image)
    {
        var area = new CommunicationArea(image);
        area.SetStatus(CommunicationFormat.Input, Store.Run(state =>
        {
            if (state.FindQueue(area.QueuePath()) is not { } node) return Unknown;
            area.SetMessageCount(node.CompleteMessages);
            return Ok;
        }));
        return area.Image;
    }

    /// <summary>A.2.2 and A.2.3. "No action is taken when [a statement] is executed that specifies a source or destination that is
    /// already [so], except that the value in the status key data item indicates this condition."</summary>
    internal string SetPath(string cd, CommunicationFormat format, CommunicationPath path, string image, string? key, bool enable)
    {
        var area = new CommunicationArea(image);
        area.SetStatus(format, Store.Run(state =>
        {
            switch (path)
            {
                case CommunicationPath.Input:
                    return state.FindQueue(area.QueuePath()) is { } queue
                        ? SetOne(queue, key, enable, state)
                        : Unknown;
                case CommunicationPath.InputTerminal:
                    return state.FindSource(area.SourceName(format)) is { } source ? SetOne(source, key, enable, state) : UnknownSource;
                case CommunicationPath.InputOutputTerminal:
                    if (LockedTerminalChanged(cd, area, format)) return UnknownSource;
                    return state.FindTerminal(area.SourceName(format)) is { } terminal ? SetOne(terminal, key, enable, state) : UnknownSource;
                default:
                    return SetOutput(area, key, enable, state);
            }
        }));
        return area.Image;
    }

    private static string SetOne(IMessageEndpoint endpoint, string? key, bool enable, MessageStoreState state)
    {
        if (!endpoint.Admits(key)) return BadKey;
        if (endpoint.Enabled == enable) return AlreadySet;
        endpoint.Enabled = enable;
        state.Touch();
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
                state.Touch();
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
    internal string Purge(string cd, CommunicationFormat format, string image)
    {
        var area = new CommunicationArea(image);
        area.SetStatus(format, Store.Run(state =>
        {
            if (format is CommunicationFormat.InputOutput)
            {
                if (LockedTerminalChanged(cd, area, format)) return Unknown;
                string name = area.SourceName(format);
                if (state.FindTerminal(name) is not { } terminal) return Unknown;
                if (!_sends.Remove(TerminalKey(name))) return NoPortion;
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
                else if (!_sends.Remove(DestinationKey(name)))
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
        }));
        return area.Image;
    }

    /// <summary>A.2.6.3. The order of the refusals is the order of the rules that take no action: the destination count (A.1.2.3
    /// GR17), the text length (GR3, status 50), and a portion with no text (Table A.1, status 60; GR9's last sentence).</summary>
    internal string Send(string cd, CommunicationFormat format, string image, string? text, SendIndicator indicator)
    {
        var area = new CommunicationArea(image);
        // The portions as they stand: a store that refuses the statement's write means it took no action (Table A.1's 9x), so the run unit's half goes back too.
        var before = _sends.ToDictionary(pair => pair.Key, pair => pair.Value.Copy(), StringComparer.Ordinal);
        string status = Store.Run(state =>
        {
            if (format is CommunicationFormat.Output && ValidCount(area) is null) return BadCount;
            int textLength = area.TextLength(format) ?? int.MaxValue;
            if (text is not null && textLength > text.Length) return TextTooLong;
            if (indicator is SendIndicator.None && (text is null || textLength == 0)) return NoText;
            string portion = text is null ? "" : text[..textLength];
            return format is CommunicationFormat.Output
                ? SendToDestinations(area, portion, indicator, state)
                : SendToTerminal(cd, area, format, portion, indicator, state);
        });
        if (status == MessageStore.StoreRefused)
        {
            _sends.Clear();
            foreach (var (key, pending) in before) _sends[key] = pending;
        }
        area.SetStatus(format, status);
        return area.Image;
    }

    private string SendToDestinations(CommunicationArea area, string portion, SendIndicator indicator, MessageStoreState state)
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
            else if (Release(DestinationKey(name), portion, indicator) is { } message && !destination.TryEnqueue(message, state))
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
    private string SendToTerminal(string cd, CommunicationArea area, CommunicationFormat format, string portion, SendIndicator indicator, MessageStoreState state)
    {
        if (LockedTerminalChanged(cd, area, format)) return Unknown;
        string name = area.SourceName(format);
        if (state.FindTerminal(name) is not { } terminal) return Unknown;
        if (!terminal.Enabled) return Disabled;
        if (Release(TerminalKey(name), portion, indicator) is { } message) terminal.Transmit(message, state);
        return Ok;
    }

    /// <summary>A.2.6.3 GR6, GR10, GR11: add one portion to the message this run unit is releasing to <paramref name="key"/>, closing
    /// the segment on any indicator. The message, once an EMI or EGI ends it, goes to the MCS; until then it "does not logically exist
    /// for the MCS" (GR7).</summary>
    private StoredMessage? Release(string key, string portion, SendIndicator indicator)
    {
        if (!_sends.TryGetValue(key, out var pending)) _sends[key] = pending = new PendingSend();
        pending.Segment.Append(portion);
        if (indicator is SendIndicator.None) return null;
        pending.Segments.Add(pending.Segment.ToString());
        pending.Segment.Clear();
        if (indicator is SendIndicator.EndOfSegment) return null;
        _sends.Remove(key);
        return StoredMessage.Complete(pending.Segments, indicator is SendIndicator.EndOfGroup, RunUnit.Current.Clock.Now());
    }

    private static string DestinationKey(string name) => "D:" + name;

    private static string TerminalKey(string name) => "T:" + name;

    /// <summary>A.2.5.3. A remainder this run unit holds answers first (GR10: "only subsequent execution of RECEIVE statements in that
    /// run unit shall cause the remaining portion of the message to be returned").</summary>
    internal CommunicationReceipt Receive(string cd, CommunicationFormat format, string image, bool segment, string into, bool noDataPhrase)
    {
        var area = new CommunicationArea(image);
        while (true)
        {
            var outcome = TryReceive(cd, format, area, segment, into);
            if (outcome.Status is not null)   // refused: GR5 c) passes control "as if data had been made available"
            {
                area.SetStatus(format, outcome.Status);
                return new CommunicationReceipt(area.Image, into, DataAvailable: true);
            }
            if (outcome.Into is { } filled)
            {
                area.SetStatus(format, Ok);
                return new CommunicationReceipt(area.Image, filled, DataAvailable: true);
            }
            if (noDataPhrase)   // GR5 a): "terminated with the indication that action is complete"
            {
                area.SetTextLength(format, 0);
                area.SetStatus(format, Ok);
                return new CommunicationReceipt(area.Image, into, DataAvailable: false);
            }
            Thread.Sleep(WaitInterval);   // GR5 b): another run unit, or a device, may yet make data available
        }
    }

    private (string? Status, string? Into) TryReceive(string cd, CommunicationFormat format, CommunicationArea area, bool segment, string into)
    {
        (string? Status, string? Into) answer = default;
        PendingReceive? taken = null;
        string status = Store.Run(state =>
        {
            string[] path;
            if (format is CommunicationFormat.InputOutput)
            {
                if (LockedTerminalChanged(cd, area, format)) { answer = (UnknownSource, null); return Ok; }
                string terminal = area.SourceName(format);
                if (state.FindTerminal(terminal) is null) { answer = (UnknownSource, null); return Ok; }
                path = [terminal];
            }
            else
            {
                path = area.QueuePath();
                if (state.FindQueue(path) is null) { answer = (Unknown, null); return Ok; }
            }
            var pending = _receives.Find(r => r.Format == format && r.Answers(path));
            if (pending is null)
            {
                var next = format is CommunicationFormat.Input
                    ? state.TakeFromQueue(path)
                    : state.TakeFromTerminal(path[0]);
                if (next is not { } found) return Ok;   // no data: the caller decides between GR5 a) and b)
                pending = taken = new PendingReceive(format, found.Path, found.Message);
                _receives.Add(pending);
            }
            answer = (null, Transfer(pending, area, format, segment, into));
            return Ok;
        });
        if (status == Ok) return answer;
        // A store that cannot answer reports the implementor's 9x status (Table A.1) and the statement takes no action: a message it could not remove stays the store's.
        if (taken is not null) _receives.Remove(taken);
        return (status, null);
    }

    /// <summary>A.2.5.3 GR3, GR8, GR9: the text fills the area left to right "without space fill", and A.1.2.3 GR12 and GR33 give the end key.</summary>
    private string Transfer(PendingReceive pending, CommunicationArea area, CommunicationFormat format, bool segment, string into)
    {
        var (text, endKey) = pending.Next(segment, into.Length);
        if (pending.Finished) _receives.Remove(pending);
        if (format is CommunicationFormat.Input) area.SetQueuePath(pending.Path);
        var message = pending.Message;
        area.SetReceived(format, message.Date, message.Time, message.Source, text.Length, endKey);
        return string.Concat(text, into.AsSpan(text.Length));
    }

    /// <summary>A.1.2.3 GR6 and GR28: the names of what scheduled the run unit (<see cref="MessageStore.Scheduled"/>), or spaces.</summary>
    internal string InitialArea(string cd, CommunicationFormat format, string image)
    {
        var area = new CommunicationArea(image);
        var scheduled = Store.Scheduled;
        if (format is CommunicationFormat.Input)
            area.SetQueuePath(scheduled);
        else
        {
            string terminal = scheduled.Length > 0 ? scheduled[0] : "";
            area.SetTerminal(terminal);
            // GR30: the name of the terminal that scheduled the run unit "shall not be changed by the program".
            if (terminal.Length > 0) _terminals[cd] = terminal;
        }
        return area.Image;
    }

    /// <summary>A.1.2.3 GR30 and GR31: after the first statement referencing an input-output CD, or from the start when the MCS scheduled
    /// the run unit, a changed SYMBOLIC TERMINAL makes "the execution of any statement referencing cd-name-1 ... unsuccessful".</summary>
    private bool LockedTerminalChanged(string cd, CommunicationArea area, CommunicationFormat format)
    {
        string name = area.SourceName(format);
        if (_terminals.TryGetValue(cd, out string? first)) return !string.Equals(first, name, StringComparison.Ordinal);
        _terminals[cd] = name;
        return false;
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
