// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>A PROCEDURE DIVISION section (ISO §14.4.3): its contiguous paragraph pc range — paragraphs flatten
/// into the one pc sequence in source order, so a section IS the <see cref="PcRange"/> [StartPc, EndPc] — and its
/// own paragraph map for qualified procedure-name resolution (ISO §8.4.2.2: <c>para OF section</c>, and the
/// same-section implicit resolution of duplicated paragraph names).
/// <para>A section with ZERO paragraphs is legal (ISO §14.4.2 — "a section header followed by zero, one, or more
/// successive paragraphs"), and its range is <see cref="PcRange.IsEmpty"/>: there is no first statement to
/// transfer to. That is a CARRIED bit, never <c>EndPc &lt; StartPc</c> arithmetic — a legal inverted THRU range
/// produces the same numbers (kb/Work PB440; see <see cref="PcRange"/>).</para></summary>
internal sealed class SectionInfo(string name, int startPc)
{
    public string Name { get; } = name;

    /// <summary>The section's pc range — EMPTY until <see cref="CloseAt"/> records a collected paragraph, and
    /// PERMANENTLY empty for a zero-paragraph section.</summary>
    public PcRange Range { get; private set; } = PcRange.EmptyAt(startPc);

    public int StartPc => Range.Start;
    public int EndPc => Range.End;
    public Dictionary<string, int> Paras { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Close the section at the LAST pc its paragraph collection reached. A section that collected
    /// nothing passes <c>StartPc − 1</c> and stays EMPTY (§14.4.2) — the one place the zero-paragraph case is
    /// decided, so no consumer has to recognize it from the numbers.</summary>
    public void CloseAt(int lastPc)
    {
        if (lastPc >= Range.Start) Range = PcRange.Of(Range.Start, lastPc);
    }
}
