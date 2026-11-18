// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// What ONE physical line of a source or library text is, as the §2023:6.5 logical conversion decided while it read the
/// line (docs/rearchitecture/DESIGN-listing.md section 4.4.1; kb/Work PB5923). The conversion is the one place that reads
/// the indicator area (<see cref="FixedFormLine"/>, kb/Work PB1966) and the program text of a line, so it records its
/// answer here and every later reader (the source listing) reads the RESULT, never the columns a second time
/// (<see cref="ReferenceFormatMap.KindAt"/>).
/// <para>One byte per physical line, written for every compilation (the conversion decides it anyway).</para>
/// </summary>
public enum PhysicalLineKind : byte
{
    /// <summary>A line of program text (a fixed-form space indicator, or a free-form line holding text).</summary>
    Source,

    /// <summary>A comment line: the fixed-form <c>*</c> indicator (§1985:IV.7.2.4), or a line whose first character-string is
    /// the floating comment indicator where the edition reads one (§2023:6.2.3.1).</summary>
    Comment,

    /// <summary>A comment line with the fixed-form slant (<c>/</c>) indicator, which ejects a page before it is listed
    /// (§1985:IV.7.2.4, §2023:6.3.7.2).</summary>
    PageEjectComment,

    /// <summary>A debugging line: the fixed-form <c>D</c> indicator, or the floating <c>&gt;&gt;D</c> indicator (2002
    /// clause 6.1.2).</summary>
    DebuggingLine,

    /// <summary>A fixed-form continuation line (the hyphen indicator).</summary>
    Continuation,

    /// <summary>A compiler directive line (§2023:7.3.3), where the edition has compiler directives.</summary>
    Directive,

    /// <summary>A line of the body of an obsolete IDENTIFICATION DIVISION comment-entry paragraph (AUTHOR, DATE-COMPILED, ...),
    /// which the conversion discards as commentary.</summary>
    CommentEntry,

    /// <summary>A line of a comment-entry paragraph the conversion could not place (the division was not known), carried for
    /// <c>CommentEntryResolver</c> to decide after COPY (docs/CONFORMANCE.md D-CE2): a reader settles it from the parse.</summary>
    CommentEntryCandidate,

    /// <summary>A line the NIST CCVS column-7 conventions exclude (the <c>--nist</c> dialect only, kb/Work PB1494).</summary>
    CcvsExcluded,

    /// <summary>A line whose indicator area holds a character that is no indicator (COBOL2616), read as program text.</summary>
    NotAnIndicator,

    /// <summary>A blank line: nothing but spaces in its program text (§2023:6.3.6).</summary>
    Blank,
}
