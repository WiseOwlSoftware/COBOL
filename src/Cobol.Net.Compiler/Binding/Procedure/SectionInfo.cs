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
/// <param name="isDeclarative">True for a section of the DECLARATIVES portion (ISO §14.3). It is a property of
/// the section, not of its pc numbers: the declarative sections share the ONE pc space with the ordinary
/// procedure division (they are entered by the USE dispatch or an explicit PERFORM — ISO §14.9.49.3 SR4), so "is this pc
/// below <c>EntryPc</c>" is an arithmetic re-derivation of a fact the collection already knows. The rules that
/// ask — §14.9.28.3 SR11's PERFORM range, and §14.9.49.3 SR3/SR4's declaratives boundary on every procedure-name
/// reference (<c>StatementValidation.CheckDeclarativesBoundary</c>) — ask about the SECTION.</param>
internal sealed class SectionInfo(string name, int startPc, bool isDeclarative = false)
{
    public string Name { get; } = name;

    /// <summary>Is this a section of the declaratives portion of the procedure division (ISO §14.3)?</summary>
    public bool IsDeclarative { get; } = isDeclarative;

    /// <summary>The section's pc range — EMPTY until <see cref="CloseAt"/> records a collected paragraph, and
    /// PERMANENTLY empty for a zero-paragraph section.</summary>
    public PcRange Range { get; private set; } = PcRange.EmptyAt(startPc);

    public int StartPc => Range.Start;
    public int EndPc => Range.End;
    /// <summary>The section's own paragraph declarations. A <see cref="ProcedureNameMap{T}"/>, not a bare
    /// dictionary, because ISO §8.4.2.2.3 SR7 — "If explicitly referenced, a paragraph-name shall not be
    /// duplicated within a section" — is a question about MULTIPLICITY, and a <c>TryAdd</c> map has already
    /// thrown the answer away by the time a reference asks (kb/Work PB466).</summary>
    public ProcedureNameMap<int> Paras { get; } = new();

    /// <summary>Close the section at the LAST pc its paragraph collection reached. A section that collected
    /// nothing passes <c>StartPc − 1</c> and stays EMPTY (§14.4.2) — the one place the zero-paragraph case is
    /// decided, so no consumer has to recognize it from the numbers.</summary>
    public void CloseAt(int lastPc)
    {
        if (lastPc >= Range.Start) Range = PcRange.Of(Range.Start, lastPc);
    }
}
