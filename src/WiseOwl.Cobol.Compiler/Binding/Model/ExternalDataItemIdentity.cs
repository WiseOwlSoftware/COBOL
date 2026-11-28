// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>⛔ THE IDENTITY OF ONE EXTERNAL DATA ITEM AS A VALUE WHOSE PARTS STAY APART (kb/Work PB5888 review R1-1): the
/// external record it lies in (<see cref="Record"/>, the record's externalized name, which an <c>AS literal-1</c> makes ANY
/// character-string, periods included), the item's own name (<see cref="Item"/>, null when the item IS the record), the
/// occurrence a COBOL-2002 integer-literal subscript selects (<see cref="Occurrence"/>), and the storage it occupies in the record
/// (<see cref="Offset"/>, <see cref="Width"/>). Two items are the same corresponding external data item iff these values are equal,
/// part by part (record equality). While the identity was one dotted string, the record <c>AS "A.B.C"</c> and item C of the record
/// <c>AS "A.B"</c> both spelled <c>A.B.C@0:4</c>, and two RELATIVE KEY items in two records were taken for one.
/// <para><see cref="Key"/> is the value's encoding for a carrier that holds a string (the run-time descriptor <c>OoEmitter</c>
/// emits, the PADDING segment of <c>FileEntryIdentity</c>): each name is written with its length before it, so the encoding is
/// injective and string equality of two keys is equality of the values; no separator inside a name can move a boundary.</para></summary>
public readonly record struct ExternalDataItemIdentity(string Record, string? Item, string Occurrence, int? Offset, int Width)
{
    /// <summary>The injective encoding: <c>&lt;length&gt;:&lt;record&gt;|&lt;length or -1&gt;:&lt;item&gt;|&lt;occurrence&gt;|&lt;offset or ?&gt;|&lt;width&gt;</c>.
    /// The occurrence is digits and commas, the offset and width digits, so only the length-prefixed names hold free text.</summary>
    public string Key =>
        string.Create(CultureInfo.InvariantCulture,
            $"{Record.Length}:{Record}|{Item?.Length ?? -1}:{Item}|{Occurrence}|{(Offset is { } o ? o.ToString(CultureInfo.InvariantCulture) : "?")}|{Width}");
}
