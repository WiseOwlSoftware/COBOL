// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Procedure;

/// <summary>A PROCEDURE DIVISION section (ISO §14.4.3): its contiguous paragraph pc range — paragraphs flatten
/// into the one pc sequence in source order, so a section IS the inclusive range [StartPc, EndPc] (empty section
/// ⇒ StartPc &gt; EndPc) — and its own paragraph map for qualified procedure-name resolution (ISO §8.4.2.2:
/// <c>para OF section</c>, and the same-section implicit resolution of duplicated paragraph names).</summary>
internal sealed class SectionInfo(string name, int startPc)
{
    public string Name { get; } = name;
    public int StartPc { get; } = startPc;
    public int EndPc { get; set; } = startPc - 1;
    public Dictionary<string, int> Paras { get; } = new(StringComparer.OrdinalIgnoreCase);
}
