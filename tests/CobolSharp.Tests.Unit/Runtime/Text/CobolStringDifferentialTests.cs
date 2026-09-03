// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using CobolSharp.Runtime;
using CobolSharp.Runtime.Text;
using Xunit;

namespace CobolSharp.Tests.Unit.Runtime.Text;

/// <summary>
/// The Stage-0 character substrate differential oracle (data-model migration, ADR §10/§13). Proves
/// <see cref="CobolString"/> — the typed-path alphanumeric store — bit-for-bit identical to the legacy byte path
/// (<see cref="StorageHelpers"/>) it will replace, BEFORE the Stage-3 character flip wires it in. This is the
/// character analogue of <c>CobolNumDifferentialTests</c>.
///
/// <para><b>MOVE</b> is proven byte-identical over arbitrary content (incl. binary / LOW-VALUE / HIGH-VALUE),
/// left- and right-justified, across a grid of source patterns × receiver widths. <b>Comparison</b> is proven
/// sign-identical to <see cref="StorageHelpers.CompareFieldToField"/> over a grid of base strings × trailing
/// spaces — the grid deliberately excludes non-space trailing bytes (NUL / control), where the legacy
/// <c>TrimEnd()</c> path diverges from COBOL's <c>0x20</c>-only space-extension (ISO §8.8.4.2.7 r2); a few
/// known-answer tests pin that COBOL-correct extension directly.</para>
/// </summary>
public sealed class CobolStringDifferentialTests
{
    private static readonly byte[][] MoveSources =
    [
        [],
        [0x41],                               // "A"
        [0x41, 0x42, 0x43],                   // "ABC"
        [0x20, 0x20],                         // two spaces
        [0x41, 0x20, 0x42],                   // "A B"
        [0x00, 0xFF, 0x7F, 0xC1],             // binary / LOW- & HIGH-VALUE
        [0x41, 0x42, 0x43, 0x44, 0x45, 0x46], // "ABCDEF"
        [0xFF, 0xFF],                         // high bytes
    ];

    private static readonly int[] ReceiverWidths = [1, 2, 3, 6, 8];

    [Fact]
    public void Move_LeftJustified_IsByteIdenticalToLegacy()
        => AssertMoveMatchesLegacy(justifiedRight: false);

    [Fact]
    public void Move_JustifiedRight_IsByteIdenticalToLegacy()
        => AssertMoveMatchesLegacy(justifiedRight: true);

    private static void AssertMoveMatchesLegacy(bool justifiedRight)
    {
        foreach (byte[] src in MoveSources)
        {
            string value = CobolString.FromWindow(src);   // Latin-1 decode of the source window
            foreach (int width in ReceiverWidths)
            {
                // legacy byte path
                var legacy = new byte[width];
                if (justifiedRight)
                    StorageHelpers.MoveStringToJustifiedField(legacy, 0, width, value);
                else
                    StorageHelpers.MoveStringToField(legacy, 0, width, value);

                // typed path: Store (justify/pad/truncate) → ToWindow (Latin-1 encode)
                var typed = new byte[width];
                CobolString.ToWindow(CobolString.Store(value, width, justifiedRight), typed);

                Assert.True(legacy.AsSpan().SequenceEqual(typed),
                    $"MOVE mismatch (justifiedRight={justifiedRight}, width={width}, src=[{Hex(src)}]): " +
                    $"legacy=[{Hex(legacy)}] typed=[{Hex(typed)}]");
            }
        }
    }

    [Fact]
    public void Window_RoundTrip_IsIdentity()
    {
        foreach (byte[] src in MoveSources)
        {
            string s = CobolString.FromWindow(src);
            Assert.Equal(src.Length, s.Length);
            var back = new byte[src.Length];
            CobolString.ToWindow(s, back);
            Assert.True(src.AsSpan().SequenceEqual(back), $"round-trip changed [{Hex(src)}] → [{Hex(back)}]");
        }
    }

    [Fact]
    public void Compare_IsSignIdenticalToLegacy_OnSpacePaddedData()
    {
        // bases hold no whitespace/NUL/control; the only trailing whitespace introduced is the COBOL space,
        // so the legacy TrimEnd() path and COBOL's 0x20 space-extension agree (see class remarks).
        string[] bases = ["", "A", "AB", "ABC", "AZ", "A0", "ÁB", "B"];
        var operands = new List<string>();
        foreach (string b in bases)
            for (int trail = 0; trail <= 3; trail++)
                operands.Add(b + new string(' ', trail));

        foreach (string left in operands)
            foreach (string right in operands)
            {
                var lb = ToLatin1(left);
                var rb = ToLatin1(right);
                int legacy = Math.Sign(StorageHelpers.CompareFieldToField(lb, 0, lb.Length, rb, 0, rb.Length));
                int typed = Math.Sign(CobolString.Compare(left, right));
                Assert.True(legacy == typed,
                    $"compare sign mismatch \"{left}\" vs \"{right}\": legacy={legacy} typed={typed}");
            }
    }

    [Theory]
    [InlineData("AB", "AB ", 0)]      // shorter operand space-extended → equal
    [InlineData("AB", "ABA", -1)]     // space (0x20) < 'A' at the extended position
    [InlineData("ABZ", "AB", 1)]      // 'Z' > space
    [InlineData("Á", "A", 1)]    // high byte 0xC1 (193) > 'A' (65), ordinal
    [InlineData("A", "A", 0)]
    public void Compare_ImplementsCobolSpaceExtension(string left, string right, int expectedSign)
        => Assert.Equal(expectedSign, Math.Sign(CobolString.Compare(left, right)));

    private static byte[] ToLatin1(string s)
    {
        var b = new byte[s.Length];
        for (int i = 0; i < s.Length; i++)
            b[i] = (byte)s[i];
        return b;
    }

    private static string Hex(byte[] b) => Convert.ToHexString(b);
}
