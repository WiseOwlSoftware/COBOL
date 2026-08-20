// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;
using CobolClass = CobolNet.Runtime.CobolClass;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The CODED CHARACTER SET an alphabet references (ISO §12.3.7.4 GR7 + Table 6; kb/Work PB110) and the runtime
/// membership test of the alphabet-name class condition (§8.8.4.4.4 GR3 a; kb/Work PB109): the ordinal↔character
/// correspondence per phrase (the CONFORMANCE.md determinations) and the three membership kinds.
/// </summary>
public sealed class CodedCharacterSetTests
{
    /// <summary>The identity sets: ordinal n is code unit / scalar n−1; STANDARD-1/2 stop at 128 (ISO/IEC 646 IRV);
    /// UCS-4/UTF-8 ordinals skip the surrogate block (not scalar values) and reach the supplementary planes as
    /// surrogate PAIRS (one character, two code units).</summary>
    [Fact]
    public void CharAt_FollowsEachPhrasesCorrespondence()
    {
        var std = new CodedCharacterSet("STANDARD-1", National: false, null, null);
        Assert.Equal(128, std.OrdinalCount);
        Assert.Equal("\0", std.CharAt(1));
        Assert.Equal("A", std.CharAt(66));
        Assert.Equal("", std.CharAt(128));
        Assert.Null(std.CharAt(129));
        Assert.Null(std.CharAt(0));

        var native = new CodedCharacterSet("NATIVE", National: false, null, null);
        Assert.Equal(65536, native.OrdinalCount);
        Assert.Equal("é", native.CharAt(0xEA));           // ordinal 234 → U+00E9
        Assert.Equal("￿", native.CharAt(65536));

        var ucs4 = new CodedCharacterSet("UCS-4", National: true, null, null);
        Assert.Equal("A", ucs4.CharAt(66));
        Assert.Equal("퟿", ucs4.CharAt(0xD800));       // the last BMP scalar before the surrogate block
        Assert.Equal("", ucs4.CharAt(0xD801));       // the block is SKIPPED — surrogates are not characters
        Assert.Equal("\U00010000", ucs4.CharAt(0xD801 + 0x2000));   // a supplementary character is ONE ordinal, a PAIR of code units
    }

    /// <summary>A literal-phrase alphabet's set: the ordinals ARE the collating positions + 1 (GR7 k4's determination) —
    /// the specified characters by RepByPos, the unspecified tail arithmetically (GR7 k3's placement).</summary>
    [Fact]
    public void CharAt_LiteralAlphabet_UsesTheCollatingPositions()
    {
        // ALPHABET "Z" THRU "A" (the pb110 corpus shape): positions 0..25 = Z..A; the unspecified 256-block chars
        // follow in native order (codes 0..64 at positions 26..90, 91..255 at 91..); code units ≥ 256 take the tail.
        var pos = new ushort[256];
        var rep = new ushort[256];
        ushort next = 0;
        for (char c = 'Z'; c >= 'A'; c--) { pos[c] = next; rep[next] = c; next++; }
        for (int c = 0; c < 256; c++)
            if (c < 'A' || c > 'Z') { pos[c] = next; rep[next] = (ushort)c; next++; }
        var table = new CollatingTable(pos, rep, 256, HighValue: (char)255, LowValue: 'Z');
        var set = new CodedCharacterSet("literal-phrase", National: false, table, null);
        Assert.Equal("Z", set.CharAt(1));                   // position 0
        Assert.Equal("A", set.CharAt(26));                  // position 25
        Assert.Equal("\0", set.CharAt(27));                 // the first unspecified char (code 0)
        Assert.Equal("Ā", set.CharAt(257));            // the ≥256 tail: position 256 → code unit 256
        Assert.Equal(65536, set.OrdinalCount);

        // The NATIONAL sparse table's inverse (the FOR NATIONAL literal-phrase alphabet): specified codes 65/90 at
        // positions 0/1; every unspecified code unit c takes position NextFree + (c - |specified < c|).
        var nat = new NationalCollatingTable(Codes: [65, 90], Positions: [1, 0], RepByPos: [90, 65], NextFree: 2,
            HighValue: (char)0xFFFF, LowValue: 'Z');
        var natSet = new CodedCharacterSet("literal-phrase", National: true, null, nat);
        Assert.Equal("Z", natSet.CharAt(1));                 // position 0
        Assert.Equal("A", natSet.CharAt(2));                 // position 1
        Assert.Equal("\0", natSet.CharAt(3));                // the first unspecified code unit (0)
        Assert.Equal("B", natSet.CharAt(3 + 66 - 1));        // code 66: 65 specified codes below it... position = 2 + (66 - 1)
    }

    /// <summary>The runtime membership kinds (§8.8.4.4.4 GR3 a — kb/Work PB109): Ascii = the 128 ISO 646 characters;
    /// ScalarValues = well-formed UTF-16 (an unpaired surrogate is not a character of UCS-4/UTF-8); AllNative is total;
    /// a zero-length operand is FALSE (GR1).</summary>
    [Fact]
    public void IsInCodedSet_ThreeKinds()
    {
        Assert.True(CobolClass.IsInCodedSet("Hi !~\t", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet("café", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet("", CobolClass.CodedSetKind.Ascii));
        Assert.False(CobolClass.IsInCodedSet(null, CobolClass.CodedSetKind.AllNative));
        Assert.True(CobolClass.IsInCodedSet("café￿", CobolClass.CodedSetKind.AllNative));
        Assert.True(CobolClass.IsInCodedSet("a\U00010000b", CobolClass.CodedSetKind.ScalarValues));
        Assert.False(CobolClass.IsInCodedSet("a\uD800b", CobolClass.CodedSetKind.ScalarValues));    // unpaired high
        Assert.False(CobolClass.IsInCodedSet("a\uDC00", CobolClass.CodedSetKind.ScalarValues));     // unpaired low
    }
}
