// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// What the §2023:6.5 logical conversion decided of each physical line of one text, recorded where it decides it
/// (DESIGN-listing.md sections 4.4.1 and 7.2): the KIND of every line, for every compilation (one byte per line), and, only when
/// a source listing is produced, where on a line the conversion DISCARDED program text as an inline comment (§2023:6.5 GR3) or as
/// a comment-entry (kb/Work PB5949). Discarded text never reaches the lexer, so the listing would otherwise have no reading of it
/// but a pattern of its own.
/// </summary>
internal sealed class PhysicalLineRecord(int lines, bool recordsDiscarded)
{
    /// <summary>What each physical line is, 0-based.</summary>
    public PhysicalLineKind[] Kinds { get; } = new PhysicalLineKind[lines];

    /// <summary>Per 1-based physical line, the 0-based physical column from which the conversion discarded the rest of the line's
    /// program text, and what that text is; null when no listing is produced.</summary>
    public Dictionary<int, (int Column, SourceTextClass Class)>? Discarded { get; } = recordsDiscarded ? [] : null;
}
