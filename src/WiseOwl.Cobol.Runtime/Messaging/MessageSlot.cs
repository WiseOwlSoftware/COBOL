// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// One message tag a message carries beside its image (docs/rearchitecture/DESIGN-message-control.md section 4.4; kb/Work PB4957): the tag, and the
/// <see cref="Token"/> that stands for it in the image - the 8 positions the SENDING run unit's <see cref="MessageTagImage.Of"/> rendered it as (DOC-A.1-212).
/// <para>⛔ A SLOT STANDS FOR POSITIONS, NOT FOR A PLACE IN A LIST. "The content of identifier-1 is the content of the message" (§2023:14.9.31.4 GR1b), so a
/// receiving message tag is what the message holds at ITS positions, decoded as a file record's tag is (<see cref="MessageTagImage.TagOf"/>), with the message's
/// slots standing in for the sending run unit's identifiers (<see cref="AsyncMessaging.Resolve"/>). A token names a tag of the run unit that wrote it, which the
/// receiving run unit cannot decode alone; the slot is how it learns that tag.</para>
/// <para>A <c>readonly struct</c>, not a record, for the reason <see cref="MessageTag"/> is one: no <c>==</c> over a tag an emitter could mistake for
/// §2023:8.8.4.2.14's equality.</para>
/// </summary>
public readonly struct MessageSlot
{
    /// <summary>A slot: <paramref name="tag"/>, which the image holds as <paramref name="token"/>.</summary>
    public MessageSlot(string token, MessageTag tag)
    {
        Token = token;
        Tag = tag;
    }

    /// <summary>The positions that stand for <see cref="Tag"/> in the message's image, as the sending run unit rendered it.</summary>
    public string Token { get; }

    /// <summary>The message tag those positions name.</summary>
    public MessageTag Tag { get; }
}
