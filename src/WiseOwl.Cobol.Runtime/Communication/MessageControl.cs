// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Communication;

/// <summary>The three formats of a communication description entry (ISO/IEC 1989:2002 A.1.2.1).</summary>
public enum CommunicationFormat
{
    /// <summary>Format 1, <c>CD cd-name-1 FOR [INITIAL] INPUT</c>: an area of 87 character positions.</summary>
    Input,

    /// <summary>Format 2, <c>CD cd-name-1 FOR OUTPUT</c>: an area of 10 + 13 × integer-1 character positions.</summary>
    Output,

    /// <summary>Format 3, <c>CD cd-name-1 FOR [INITIAL] I-O</c>: an area of 33 character positions.</summary>
    InputOutput,
}

/// <summary>The logical path an ENABLE or DISABLE statement names (ISO/IEC 1989:2002 A.2.2.1, A.2.3.1).</summary>
public enum CommunicationPath
{
    /// <summary><c>INPUT</c>: the queue structure the input CD designates.</summary>
    Input,

    /// <summary><c>INPUT TERMINAL</c>: the source the input CD's SYMBOLIC SOURCE names.</summary>
    InputTerminal,

    /// <summary><c>I-O TERMINAL</c>: the terminal the input-output CD's SYMBOLIC TERMINAL names.</summary>
    InputOutputTerminal,

    /// <summary><c>OUTPUT</c>: the destinations of the output CD's table.</summary>
    Output,
}

/// <summary>The indicator of a SEND statement (ISO/IEC 1989:2002 Table A.3), by the value of its identifier-2.</summary>
public enum SendIndicator
{
    /// <summary>No indicator: a portion of a message or of a segment.</summary>
    None = 0,

    /// <summary>ESI, the end of the current segment.</summary>
    EndOfSegment = 1,

    /// <summary>EMI, the end of the current message.</summary>
    EndOfMessage = 2,

    /// <summary>EGI, the end of the current group of messages.</summary>
    EndOfGroup = 3,
}

/// <summary>What one RECEIVE statement leaves behind: the CD's area, the receiving item's image, and whether control
/// passes as if data had been made available (ISO/IEC 1989:2002 A.2.5.3 GR4, GR5 c) or to the NO DATA phrase (GR5 a).</summary>
public readonly record struct CommunicationReceipt(string Area, string Into, bool DataAvailable);

/// <summary>
/// ⛔ THE STATEMENTS OF THE COMMUNICATION FACILITY AT RUN TIME (ISO/IEC 1989:2002 Annex A.2; COBOL-85's communication module;
/// docs/rearchitecture/DESIGN-communication-facility.md; kb/Work PB4677). The compiled program hands over the CD's area as its
/// image and stores back the image each call returns: "The message control system (MCS) always references the record according
/// to the data description defined in" A.1.2.3 GR2, GR16 and GR24, so the positions are <see cref="CommunicationArea"/>'s, and a
/// user's redefinitions of the area never reach this class. The run unit's side of the dialogue (the portion of a message it
/// has released or begun to receive) is <see cref="MessageControlSession"/>; everything the MCS holds is
/// <see cref="MessageStore"/>.
/// </summary>
public static class MessageControl
{
    private static MessageControlSession Session => RunUnit.Current.MessageControl;

    /// <summary>The ACCEPT MESSAGE COUNT statement (A.2.1).</summary>
    public static string AcceptMessageCount(string area) => Session.AcceptMessageCount(area);

    /// <summary>The ENABLE statement (A.2.3). <paramref name="key"/> is the COBOL-85 KEY phrase's value, or null.</summary>
    public static string Enable(string cd, CommunicationFormat format, CommunicationPath path, string area, string? key) =>
        Session.SetPath(cd, format, path, area, key, enable: true);

    /// <summary>The DISABLE statement (A.2.2). <paramref name="key"/> is the COBOL-85 KEY phrase's value, or null.</summary>
    public static string Disable(string cd, CommunicationFormat format, CommunicationPath path, string area, string? key) =>
        Session.SetPath(cd, format, path, area, key, enable: false);

    /// <summary>The PURGE statement (A.2.4).</summary>
    public static string Purge(string cd, CommunicationFormat format, string area) => Session.Purge(cd, format, area);

    /// <summary>The SEND statement (A.2.6). <paramref name="text"/> is the image of identifier-1, or null when FROM is not written.</summary>
    public static string Send(string cd, CommunicationFormat format, string area, string? text, SendIndicator indicator) =>
        Session.Send(cd, format, area, text, indicator);

    /// <summary>The RECEIVE statement (A.2.5). <paramref name="noDataPhrase"/> is whether the NO DATA phrase is written: without
    /// it, "execution of the run unit is suspended until data is made available" (GR5 b).</summary>
    public static CommunicationReceipt Receive(string cd, CommunicationFormat format, string area, bool segment, string into, bool noDataPhrase) =>
        Session.Receive(cd, format, area, segment, into, noDataPhrase);

    /// <summary>The area of a CD with the INITIAL clause before the first procedure division statement (A.1.2.3 GR6, GR28). Only the program that establishes the run unit
    /// can have been scheduled by the MCS; a CALLed program's INITIAL CD is initialized to spaces "in all other cases" (§1985:XIV.2.2.4 GR28; kb/Work PB5282).</summary>
    public static string InitialArea(string cd, CommunicationFormat format, string area) =>
        Session.InitialArea(cd, format, area, scheduledProgram: RunUnit.Current.Modules.RunningIsMain);

    /// <summary>Table A.3: "Any character other than 1, 2, or 3 will be interpreted as 0."</summary>
    public static SendIndicator IndicatorOf(string image) =>
        image.Length == 1 && image[0] is >= '1' and <= '3' ? (SendIndicator)(image[0] - '0') : SendIndicator.None;
}
