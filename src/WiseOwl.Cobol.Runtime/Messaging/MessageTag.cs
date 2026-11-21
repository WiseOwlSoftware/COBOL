// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>The party a <see cref="MessageTag"/> names (ISO §2023:3.120 message requestor and message server; §2023:8.8.4.2.14).</summary>
public enum MessageParty
{
    /// <summary>No party: the NULL content (ISO §2023:8.4.3.10.4 GR4 — "guaranteed not to represent a message server or message requestor").</summary>
    None = 0,
    /// <summary>A message server, named by its server name.</summary>
    Server,
    /// <summary>A message requestor, identified by its run unit.</summary>
    Requestor,
    /// <summary>A tag decoded from storage positions this run unit never rendered (<see cref="MessageTagImage.TagOf"/>; kb/Work PB4838): it
    /// names no party, is not NULL, keeps its positions as its name, and belongs to no message control system of this run unit.</summary>
    Unresolved,
}

/// <summary>
/// The message-tag carrier (ISO §2023:13.18.60.4 GR9 — "The class and category of a message-tag data item are message-tag";
/// §2023:8.5.2.9; kb/Work PB4740, <c>docs/rearchitecture/DESIGN-message-control.md</c> §4.2). The standard leaves the
/// alignment, representation and size of a message-tag data item to the implementor (Annex A.1 item 212, the
/// documented determination <c>DOC-A.1-212</c>): the item rides a managed slot and occupies no character positions of
/// its own, the slot rule of DOC-A.1-210 (<see cref="FunctionPointer"/>).
/// <para>The contents are the MCS's identity, the party's role, the server's name or the requestor run unit's
/// identity, and the message's serial number. <see cref="Null"/> (the default) is the value with no party
/// (§2023:8.4.3.10.3 SR1 b), §2023:8.4.3.10.4 GR4). Only the message control system makes a non-null tag; a program copies tags
/// (SET, a strongly-typed group MOVE, a parameter, a message) and compares them.</para>
/// <para>⛔ A <c>readonly struct</c>, NOT a record: C# has no <c>==</c> an emitter could mistake for §2023:8.8.4.2.14's
/// equality, which <see cref="SameParty"/> alone decides.</para>
/// </summary>
public readonly struct MessageTag
{
    /// <summary>The null content of class message-tag (ISO §2023:8.4.3.10.4 GR4; §2023:13.18.63.4 GR4's initial state).</summary>
    public static readonly MessageTag Null = default;

    internal MessageTag(string mcs, MessageParty party, string name, long message)
    {
        Mcs = mcs;
        Party = party;
        Name = name;
        Message = message;
    }

    /// <summary>The MCS identity this tag belongs to (null for <see cref="Null"/>).</summary>
    internal string? Mcs { get; }

    /// <summary>The party's role.</summary>
    internal MessageParty Party { get; }

    /// <summary>The server name (a server tag) or the requestor run unit's identity (a requestor tag).</summary>
    internal string? Name { get; }

    /// <summary>The message's serial number; it takes no part in <see cref="SameParty"/>.</summary>
    internal long Message { get; }

    /// <summary>True when this tag holds the null content.</summary>
    public bool IsNull => Party == MessageParty.None;

    /// <summary>ISO §2023:8.8.4.2.14: "The operands are equal if they reference the same message requestor or message server
    /// of the same name or one is NULL and the other is NULL". Two server tags of one MCS and one server name, two
    /// requestor tags of one MCS and one requestor run unit, or two NULLs; the message number takes no part.</summary>
    public static bool SameParty(MessageTag a, MessageTag b) =>
        a.Party == b.Party
        && (a.Party == MessageParty.None
            || (string.Equals(a.Mcs, b.Mcs, StringComparison.Ordinal)
                && string.Equals(a.Name, b.Name, StringComparison.Ordinal)));
}
