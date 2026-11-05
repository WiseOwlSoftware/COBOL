// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>
/// The MCS's view of a CD's record area: the character positions of ISO/IEC 1989:2002 A.1.2.3 GR2 (input, 87 positions), GR16
/// (output, 10 + 13 × integer-1) and GR24 (input-output, 33). A mutable copy of the image the program handed over; each statement
/// renders it back with <see cref="Image"/>. Positions below are 0-based, the rules' numbering minus one.
/// </summary>
internal sealed class CommunicationArea(string image)
{
    private readonly char[] _chars = image.ToCharArray();

    /// <summary>The characters of the symbolic names the MCS reads and writes.</summary>
    internal const int NameLength = 12;

    /// <summary>The width of one occurrence of the output table: the error key and the symbolic destination (GR16 d).</summary>
    private const int OutputEntryLength = 13;

    private const int OutputHeaderLength = 10;

    /// <summary>The image the program stores back into the CD's area.</summary>
    internal string Image => new(_chars);

    /// <summary>The occurrences of the output table the area holds, its integer-1 (GR16).</summary>
    internal int DestinationCapacity => Math.Max(0, (_chars.Length - OutputHeaderLength) / OutputEntryLength);

    /// <summary>GR2 a-d: data-name-1 through data-name-4, as written without their trailing spaces.</summary>
    internal string[] QueuePath() => [Name(0), Name(12), Name(24), Name(36)];

    /// <summary>GR5: "the contents of the data items referenced by data-name-1 through data-name-4 will contain the symbolic names
    /// of all the levels of the queue structure."</summary>
    internal void SetQueuePath(IReadOnlyList<string> path)
    {
        for (int level = 0; level < 4; level++)
            SetText(level * NameLength, NameLength, level < path.Count ? path[level] : "");
    }

    /// <summary>GR2 g, the symbolic source, or GR24 c, the symbolic terminal.</summary>
    internal string SourceName(CommunicationFormat format) => Name(format is CommunicationFormat.Input ? 62 : 14);

    internal void SetTerminal(string name) => SetText(14, NameLength, name);

    /// <summary>A received message's date and time ("YYMMDD", "HHMMSSTT"), source, text length and end key (GR8-GR12; GR26, GR27,
    /// GR32, GR33): the two formats lay them out at different positions and in a different order.</summary>
    internal void SetReceived(CommunicationFormat format, string date, string time, string source, int length, char endKey)
    {
        int at = format is CommunicationFormat.Input ? 48 : 0;
        SetText(at, 6, date);
        SetText(at + 6, 8, time);
        if (format is CommunicationFormat.Input) SetText(62, NameLength, source);
        SetTextLength(format, length);
        _chars[format is CommunicationFormat.Input ? 78 : 30] = endKey;
    }

    /// <summary>GR2 h, GR16 b, GR24 d: the text length, whose written value the SEND statement reads and whose filled count the
    /// RECEIVE statement writes. Null when it is no unsigned integer, which no size can admit.</summary>
    internal int? TextLength(CommunicationFormat format) => Integer(TextLengthAt(format), 4);

    internal void SetTextLength(CommunicationFormat format, int length) =>
        SetText(TextLengthAt(format), 4, length.ToString("D4", CultureInfo.InvariantCulture));

    private static int TextLengthAt(CommunicationFormat format) => format switch
    {
        CommunicationFormat.Input => 74,
        CommunicationFormat.Output => 4,
        _ => 26,
    };

    /// <summary>GR2 j, GR16 c, GR24 f: the status key (Table A.1).</summary>
    internal void SetStatus(CommunicationFormat format, string status) => SetText(format switch
    {
        CommunicationFormat.Input => 79,
        CommunicationFormat.Output => 8,
        _ => 31,
    }, 2, status);

    /// <summary>GR2 k: the message count an ACCEPT MESSAGE COUNT statement updates.</summary>
    internal void SetMessageCount(int count) =>
        SetText(81, 6, Math.Min(count, 999_999).ToString("D6", CultureInfo.InvariantCulture));

    /// <summary>GR16 a: the destination count, or null when it is no unsigned integer.</summary>
    internal int? DestinationCount => Integer(0, 4);

    /// <summary>GR16 d 2: the symbolic destination of a 1-based occurrence of the table.</summary>
    internal string Destination(int occurrence) => Name(OutputHeaderLength + (occurrence - 1) * OutputEntryLength + 1);

    /// <summary>GR16 d 1: the error key of a 1-based occurrence of the table (Table A.2).</summary>
    internal void SetErrorKey(int occurrence, char key) =>
        _chars[OutputHeaderLength + (occurrence - 1) * OutputEntryLength] = key;

    private string Name(int at)
    {
        int end = Math.Min(at + NameLength, _chars.Length);
        return at >= end ? "" : new string(_chars, at, end - at).TrimEnd(' ');
    }

    private int? Integer(int at, int length)
    {
        if (at + length > _chars.Length) return null;
        int value = 0;
        for (int i = at; i < at + length; i++)
        {
            if (_chars[i] is < '0' or > '9') return null;
            value = value * 10 + (_chars[i] - '0');
        }
        return value;
    }

    private void SetText(int at, int length, string text)
    {
        for (int i = 0; i < length && at + i < _chars.Length; i++)
            _chars[at + i] = i < text.Length ? text[i] : ' ';
    }
}
