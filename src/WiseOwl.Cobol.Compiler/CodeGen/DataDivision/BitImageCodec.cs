// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.CodeGen.Emit;
using WiseOwl.Cobol.Compiler.CodeGen.Roslyn;

namespace WiseOwl.Cobol.Compiler.CodeGen.DataDivision;

/// <summary>THE BIT IMAGE CODEC (D19/PB43, D20/PB79; DESIGN-architecture-review §8.3, kb/Work PB2337): the run-time faces of
/// USAGE BIT storage in a record struct — a bit group's elementary <c>AsBits()</c>/<c>FromBits</c> face and the byte face
/// derived from it, and the PACKED image of a §2023:8.5.1.6.3 run inside any other group's <c>AsImage()</c>/<c>FromImage</c>
/// (<see cref="GroupImageCodec"/> asks it for a run's slice). Every member is placed by <see cref="BitLayout"/>'s one walk,
/// and the stored implicit filler between and after them (<see cref="BitLayout.FillersOf"/>, kb/Work PB3564) is read and
/// written here alone. <see cref="BitAreaOf"/> is also the placement the compile-time seed composes over
/// (<see cref="ImageInitializer"/>), so the seed and the face cannot place a member at two positions.</summary>
internal static class BitImageCodec
{
    /// <summary>A BIT GROUP's byte face IS its bit face packed (§2023:13.18.29.4 GR1b — "treated as though it were an elementary
    /// data item … PICTURE 1(m)", kb/Work PB3564): <c>AsImage()</c> is the m positions of <c>AsBits()</c> through
    /// CobolBits.Pack and <c>FromImage</c> unpacks m positions into <c>FromBits</c>, so ONE composition — the §2023:8.5.1.6.3
    /// area, with its stored implicit filler — stands behind both faces (<see cref="EmitBitMethods"/>). Emitted for an
    /// image-capable bit group in place of <see cref="GroupImageCodec"/>'s member-by-member run packing, which a bit group
    /// that begins mid-byte cannot use: it measures its runs from that position (kb/Work PB3158), and the run packing's
    /// byte-rounded slices cannot state it.</summary>
    internal static void EmitBitGroupFaces(DataItem group, CodeWriter w)
    {
        w.Line($"public readonly string AsImage() => {RuntimeApi.BitsPack("AsBits()", $"{BitLayout.ExtentBits(group)}")};");
        w.Line($"public void FromImage(string __s) => FromBits({RuntimeApi.BitsUnpack("__s", $"{BitLayout.ExtentBits(group)}")});");
        EmitBitMethods(group, w);
    }

    /// <summary>A BIT GROUP's elementary face (§2023:13.18.29.4 GR1b — "treated as though it were an elementary data item
    /// … PICTURE 1(m), where m is the bit length of the group"; D20/PB79): <c>AsBits()</c> is the group's m
    /// boolean positions and <c>FromBits</c> distributes m boolean positions back to the members. The packed byte
    /// form stays <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image); the two agree because
    /// both compose the same area.
    ///
    /// <para>⛔ BOTH DIRECTIONS PLACE THE MEMBERS BY <see cref="BitLayout.StartBitWithin"/>, and the width is
    /// <see cref="BitLayout.ExtentBits"/> — not a sum of the members' <see cref="BitLayout.RunBits"/>, which is
    /// what both halves used to do. A sum is only right while every member is "immediately following … an item of
    /// the SAME LEVEL" (§2023:8.5.1.6.3's one byte-sharing case); a member at a different level number starts at "the
    /// first bit position of the first available byte" and the skipped bits are implicit filler. MEASURED at
    /// b7004e6b on `01 BG GROUP-USAGE BIT. 05 B1 PIC 1(2) VALUE B"11". 03 B2 PIC 1(2) VALUE B"11".`:
    /// <c>DISPLAY BG</c> printed FOUR boolean positions while <c>FUNCTION LENGTH(BG)</c> — which reads the same
    /// walk through <c>AsIfPic</c> — answered TEN. The elementary face and the length disagreed about the same
    /// group.</para></summary>
    internal static void EmitBitMethods(DataItem group, CodeWriter w)
    {
        w.Line($"public readonly string AsBits() => {BitAreaOf(group, BitCarrierOf, storedFillers: true)};");
        using (w.Block("public void FromBits(string __b)"))
        {
            // pad with boolean zeros / truncate to m (§2023:14.6.8.6)
            w.Line($"__b = {RuntimeApi.StrStoreBoolean("__b", $"{BitLayout.ExtentBits(group)}", justifiedRight: false)};");
            foreach (var m in BitMembers(group))
            {
                int at = BitLayout.StartBitWithin(group, m);
                EmitRunMemberFromBits(m, "__b", at < 0 ? 0 : at, w);
            }
            // ⛔ THE IMPLICIT FILLER BETWEEN THE RUNS TAKES WHAT THE AREA HOLDS THERE (§2023:8.5.1.6.3 "implicitly described as a
            // filler elementary bit data item", kb/Work PB3564) — the positions are part of the m the group is described
            // with (§2023:13.18.29.4 GR1b), so a MOVE of m boolean positions to the group stores them all.
            foreach (var filler in BitLayout.FillersOf(group))
                if (filler.Kind is BitFillerKind.RunTail)
                    w.Line($"{filler.ClrName} = {RuntimeApi.BitsSlice("__b", $"{filler.StartBit}", $"{filler.Bits}")};");
        }
    }

    /// <summary>The members of <paramref name="group"/> that OCCUPY storage — a redefining child overlays its
    /// target and adds none (§2023:13.18.44), and a non-data child (a condition-name entry) is not a member at all.
    /// The one member list for every bit composition below, so they cannot walk different populations.</summary>
    private static IEnumerable<DataItem> BitMembers(DataItem group) =>
        group.Children.Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary));

    /// <summary>⛔ THE ONE §2023:8.5.1.6.3 AREA COMPOSITION for a bit group: each member's boolean carrier placed at
    /// the bit position the walk gives it, with the implicit-filler zeros between and after them, for the
    /// group's whole <see cref="BitLayout.ExtentBits"/> — §2023:13.18.29.4 GR1b's "PICTURE 1(m), where m is the bit
    /// length of the group". <paramref name="carrierOf"/> is what a member contributes: its FIELD for the
    /// emitted <c>AsBits()</c> face, its INITIAL value for the compile-time image seed, and the two therefore
    /// cannot place the same member at two different positions.
    ///
    /// <para>The filler positions are §2023:8.5.1.6.3's — "implicit filler bit positions are generated … as needed to
    /// advance alignment to a required natural boundary for the next item within that group" and, at the end,
    /// "as needed to increase the number of bits to fill an integral number of characters". On the emitted face
    /// (<paramref name="storedFillers"/>) they are the owner's STORED filler fields (<see cref="BitLayout.FillersOf"/>,
    /// kb/Work PB3564), because "the filler bit positions are implicitly described as a filler elementary bit data
    /// item" and so hold what the last move put there; on the seed's face they are ZEROS, the boolean initial state
    /// §2023:13.18.63 gives a position no VALUE reaches (the string backing the seed starts stores them itself).</para></summary>
    internal static string BitAreaOf(DataItem group, Func<DataItem, string> carrierOf, bool storedFillers = false)
    {
        var parts = new List<string>();
        int at = 0;
        // The gaps between the runs are the owner's STORED filler on the emitted face (kb/Work PB3564) and the boolean
        // initial state on the compile-time seed's, which composes a string backing that stores them itself.
        var stored = storedFillers
            ? BitLayout.FillersOf(group).Where(f => f.Kind is BitFillerKind.RunTail).ToDictionary(f => f.StartBit)
            : null;
        foreach (var m in BitMembers(group))
        {
            int start = BitLayout.StartBitWithin(group, m);
            if (start < 0) start = at;      // an unmodelled overlay chain — keep the composition total
            if (start > at)
                parts.Add(stored is not null && stored.TryGetValue(at, out var filler)
                    ? (filler.Bits == start - at ? filler.ClrName
                        : throw new InvalidOperationException($"the implicit filler after bit {at} of '{group.CobolName}' is {filler.Bits} bits, the walk's gap {start - at} (kb/Work PB3564)"))
                    : EmitText.CsLiteral(new string('0', start - at)));
            parts.Add(carrierOf(m));
            at = start + BitLayout.RunBits(m);
        }
        int total = BitLayout.ExtentBits(group);
        if (total > at) parts.Add(EmitText.CsLiteral(new string('0', total - at)));
        return parts.Count > 0 ? string.Join(" + ", parts) : "\"\"";
    }

    /// <summary>Distribute one run member's slice of an unpacked bit carrier — THE one distributor, ridden by
    /// both the bit-group <c>FromBits</c> face and the record <c>FromImage</c> run loop (kb/Work PB161: the
    /// FromImage copy lacked the OCCURS loop its sibling here had, so a <c>PIC 1(8) USAGE BIT OCCURS 3</c>
    /// member made the generated record fail backend compilation with CS0029 — the sixth instance of the
    /// two-arm-dispatch defect shape).</summary>
    private static void EmitRunMemberFromBits(DataItem m, string carrier, int at, CodeWriter w)
    {
        int per = m.IsGroup ? m.AsIfPic!.Length : m.Pic!.Length;
        // The receiving twin of BitCarrierOf's backing arm (kb/Work PB3166): the member's bits go into the first `per`
        // bit positions of the class backing, every other bit of it left as it was.
        if (BackingOf(m) is { } backing)
        {
            w.Line($"{backing} = {RuntimeApi.BitsWriteWindow(backing, "0", RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}"))};");
            return;
        }
        if (m.Occurs is { } o)
        {
            // The occurrences are one STRIDE apart (§2023:13.18.1.4 GR2 — an ALIGNED table's or member's, kb/Work PB3156,
            // PB3167), each `per` wide.
            int stride = BitLayout.StrideBits(m);
            using (w.Block($"for (int __i = 0; __i < {o}; __i++)"))
                w.Line(m.IsGroup
                    ? $"{m.ClrName}[__i].FromBits({RuntimeApi.BitsSlice(carrier, $"{at} + __i * {stride}", $"{per}")});"
                    : $"{m.ClrName}[__i] = {RuntimeApi.BitsSlice(carrier, $"{at} + __i * {stride}", $"{per}")};");
            // The stored filler BETWEEN the occurrences (§2023:8.5.1.6.3, kb/Work PB3564) takes what the carrier holds there.
            if (BitLayout.TablePadOf(m) is { } pad)
                w.Line($"{pad.ClrName} = {RuntimeApi.BitsStridePads(carrier, $"{at}", $"{per}", $"{stride}", $"{o}")};");
        }
        else
        {
            w.Line(m.IsGroup
                ? $"{m.ClrName}.FromBits({RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")});"
                : $"{m.ClrName} = {RuntimeApi.BitsSlice(carrier, $"{at}", $"{per}")};");
        }
    }

    /// <summary>A §2023:8.5.1.6.3 run member's BIT CARRIER expression — THE one carrier law (kb/Work PB161): a bit
    /// leaf's '0'/'1' string field; a bit GROUP's <c>AsBits()</c>; a fixed-OCCURS member the concatenation of
    /// every occurrence's carrier (the AsImage run packer read the raw <c>string[]</c> field before, CS1503).</summary>
    private static string BitCarrierOf(DataItem m)
    {
        // ⛔ A TIER-B CANONICAL HAS NO FIELD OF ITS OWN NAME, only its class's one string backing (§2023:13.18.44.4 GR1 — the
        // shared area is addressed through the backing; PhysicalModel.BuildPhysicals), so its carrier is its first
        // WidthBits bits unpacked from that backing (kb/Work PB3166). `m.ClrName` named a field that does not exist.
        if (BackingOf(m) is { } backing) return RuntimeApi.BitsReadWindow(backing, "0", $"{BitLayout.WidthBits(m)}");
        if (m.Occurs is null) return m.IsGroup ? $"{m.ClrName}.AsBits()" : m.ClrName;
        string occurrences = m.IsGroup ? $"global::System.Array.ConvertAll({m.ClrName}, __e => __e.AsBits())" : m.ClrName;
        // ⛔ THE OCCURRENCES SIT ONE STRIDE APART, NOT ONE WIDTH (§2023:13.18.1.4 GR2, kb/Work PB3156): an ALIGNED table puts
        // every occurrence at the first bit of a byte, so the carrier carries the filler between them — the extent
        // BitLayout.TableBits states, which BitAreaOf and the run packer both advance by. A table whose layout does not
        // depend on where in a byte it begins (kb/Work PB3167) has stride = width, and keeps the plain concatenation.
        // ⛔ THAT FILLER IS STORED (§2023:8.5.1.6.3 "implicitly described as a filler elementary bit data item", kb/Work PB3564): the
        // table's TablePad field holds the positions between the occurrences, so a group move that wrote them is read back.
        return !BitLayout.HasTablePad(m)
            ? $"string.Concat({occurrences})"
            : RuntimeApi.BitsStrided(occurrences, $"{BitLayout.StrideBits(m)}",
                (BitLayout.TablePadOf(m) ?? throw new InvalidOperationException(
                    $"the strided bit table '{m.CobolName}' has no stored filler field (kb/Work PB3564)")).ClrName);
    }

    /// <summary>The C# name of the string backing that STORES bit member <paramref name="m"/> when it is the canonical of a
    /// Tier-B REDEFINES class (<see cref="RedefinesTier.StringCanonical"/>), else null: such an item is no field of its
    /// own name, and every composer that reaches it as a §2023:8.5.1.6.3 run member or a bit-group member (the carrier
    /// face, the distributor, and <c>PhysicalModel.BuildPhysicals</c>' field) reads and writes the backing instead.
    /// A bit item is never an OCCURS data-name-2 (§2023:13.18.44.3 SR5), so the backing holds exactly one occurrence.</summary>
    private static string? BackingOf(DataItem m) =>
        m.IsCanonical && m.Class is { Tier: RedefinesTier.StringCanonical } cls && ReferenceEquals(cls.Canonical, m)
            ? cls.BackingClrName
            : null;

    /// <summary>The stored trailing filler of <paramref name="run"/> as the run's PACKED IMAGE carries it: the filler
    /// <see cref="BitLayout.RunTailOf"/> names when it fills exactly the characters the run occupies
    /// (<see cref="BitLayout.RunCharacters"/>), else null. The two are the same positions for every run of a group that
    /// begins on a byte of the record; a bit group that begins MID-BYTE measures the gap to its next run from that
    /// position (§2023:8.5.1.6.3, kb/Work PB3158), which no character image of it states — its elementary bit face
    /// (<c>AsBits</c>) is where those positions live, and its byte face is derived from that (<see cref="EmitBitGroupFaces"/>).</summary>
    private static BitFiller? ImageTailOf(IReadOnlyList<DataItem> run)
    {
        var tail = BitLayout.RunTailOf(run);
        return tail is not null && BitLayout.RunBitsOf(run) + tail.Bits == BitLayout.RunCharacters(run) * BitLayout.BitsPerCharacter
            ? tail : null;
    }

    /// <summary>A §2023:8.5.1.6.3 run's PACKED image: its members' carriers back to back, then its stored trailing filler
    /// (<see cref="ImageTailOf"/>), packed over every position — so the slice is the whole of the characters the
    /// run occupies (kb/Work PB3564).</summary>
    internal static string PackedRunOf(IReadOnlyList<DataItem> run)
    {
        var tail = ImageTailOf(run);
        var carriers = run.Select(BitCarrierOf).ToList();
        if (tail is not null) carriers.Add(tail.ClrName);
        return RuntimeApi.BitsPack(string.Join(" + ", carriers), $"{BitLayout.RunBitsOf(run) + (tail?.Bits ?? 0)}");
    }

    /// <summary>Distribute a §2023:8.5.1.6.3 run's slice of a group image — the <c>FromImage</c> half of
    /// <see cref="PackedRunOf"/> (D19/PB43): the run's LEADER field, <paramref name="width"/> characters at character
    /// <paramref name="off"/> of the image <c>__s</c>, unpacks the shared byte(s) ONCE and distributes the boolean positions
    /// back to each member in declaration order; the continuations have Width 0 and consume no slice.</summary>
    internal static void EmitRunFromImage(IReadOnlyList<DataItem> run, int off, int width, CodeWriter w)
    {
        int runBits = BitLayout.RunBitsOf(run);
        // The run's stored trailing filler (§2023:8.5.1.6.3, kb/Work PB3564) is unpacked with it and stored back below.
        var tail = ImageTailOf(run);
        // ⚠ The carrier local is named for the run's OFFSET, not a bare `__bits`: a group may hold SEVERAL
        // runs (two bit items separated by a character item are two runs, §2023:8.5.1.6.3 — the character item
        // breaks the same-level adjacency), and they all land in this one method scope. The first version
        // emitted `var __bits` per run and a two-run group failed to compile with CS0128.
        string carrier = $"__bits{off}";
        w.Line($"var {carrier} = {RuntimeApi.BitsUnpack($"__s.Substring({off}, {width})", $"{runBits + (tail?.Bits ?? 0)}")};");
        int at = 0;
        foreach (var m in run)
        {
            // THE one distributor (kb/Work PB161) — a bit GROUP member spreads to its subordinates
            // (FromBits, D20/PB79), a fixed-OCCURS member loops its occurrences.
            EmitRunMemberFromBits(m, carrier, at, w);
            at = RecordLayout.ItemSize(RecordLayout.Plus(at, BitLayout.RunBits(m)));
        }
        // ⛔ WHAT THE IMAGE HELD AFTER THE LAST MEMBER IS THE FILLER'S VALUE NOW — MOVE "AB" TO G stores 'A''s low
        // seven bits here, and the next DISPLAY of G reads them back (§2023:14.9.25.4 GR4: the group move is an
        // alphanumeric move and so round-trips the bytes).
        if (tail is not null) w.Line($"{tail.ClrName} = {RuntimeApi.BitsSlice(carrier, $"{at}", $"{tail.Bits}")};");
    }
}
