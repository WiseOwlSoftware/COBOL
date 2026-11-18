// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Listing;

/// <summary>
/// ⛔ THE ONE LISTING MODEL (docs/rearchitecture/DESIGN-listing.md section 4; kb/Work PB5923, PB5925): built once by the
/// compiler's <c>ListingBuilder</c> from the source and the compiler's own data, and read by every renderer. Every type is
/// an immutable record; lines are referred to by <see cref="ListingLine.Number"/> and files by <see cref="ListingFile.Id"/>.
/// <para>Step 1 of the design's section 11 (the 1985 standard minimum) builds the header, the files, the inclusions, the
/// source lines and the programs; later steps add their sections beside them.</para>
/// </summary>
/// <param name="Header">The compiler, the edition, the compilation timestamp, the main source and the outcome.</param>
/// <param name="Files">Every file read: the source and each library text.</param>
/// <param name="Inclusions">Every COPY statement processed, in text order.</param>
/// <param name="Lines">The source section, in listing order: every physical line of every file read, each inclusion's lines
/// after the last line of its COPY statement.</param>
/// <param name="Programs">Every source unit, in text order, with its nesting and listing line range.</param>
/// <param name="Sections">Each section of the listing: produced, or why not (DESIGN-listing.md section 4.3).</param>
public sealed record ListingDocument(
    ListingHeader Header,
    IReadOnlyList<ListingFile> Files,
    IReadOnlyList<ListingInclusion> Inclusions,
    IReadOnlyList<ListingLine> Lines,
    IReadOnlyList<ListingProgram> Programs,
    IReadOnlyList<ListingSectionStatus> Sections);

/// <summary>How far the compilation got (the compiler's outcome, in the listing's own terms).</summary>
public enum ListingOutcome { Success, FrontendError, BindError, BackendError, OutputNotWritable }

/// <summary>The listing's identification (DESIGN-listing.md section 6.1).</summary>
/// <param name="CompilerVersion">The compiler's informational version.</param>
/// <param name="Edition">The edition's year as <c>--std</c> spells it (85, 2002, 2014, 2023).</param>
/// <param name="EditionName">The edition's name.</param>
/// <param name="CompiledAt">The compilation's one instant (DESIGN-listing.md section 5): the same instant WHEN-COMPILED and
/// DATE-COMPILED's replacement denote.</param>
/// <param name="MainSource">The path of the source file compiled.</param>
/// <param name="Outcome">How far the compilation got.</param>
public sealed record ListingHeader(string CompilerVersion, int Edition, string EditionName, DateTimeOffset CompiledAt,
    string MainSource, ListingOutcome Outcome);

/// <summary>Whether a file is the compilation's source or a library text a COPY statement read.</summary>
public enum ListingFileRole { Source, Library }

/// <summary>One file the compilation read.</summary>
/// <param name="Id">1-based; the source is 1.</param>
/// <param name="Path">The path as the compilation read it.</param>
/// <param name="Role">Source or library text.</param>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 digest of the file's bytes as read.</param>
public sealed record ListingFile(int Id, string Path, ListingFileRole Role, string Sha256);

/// <summary>What became of a COPY statement.</summary>
public enum ListingInclusionOutcome { Found, NotFound, Circular }

/// <summary>One COPY statement and the library text it brought in.</summary>
/// <param name="Id">1-based, in text order.</param>
/// <param name="Parent">The inclusion whose library text holds the COPY statement, or null for the source.</param>
/// <param name="Depth">1 for a COPY statement of the source, one more per library text it is nested in.</param>
/// <param name="CopyLine">The listing line the COPY word stands on.</param>
/// <param name="CopyLastLine">The listing line the statement ends on; the inclusion's lines follow it.</param>
/// <param name="TextName">The text-name or literal the statement names.</param>
/// <param name="LibraryName">The library-name or literal of its OF / IN phrase, or null.</param>
/// <param name="File">The <see cref="ListingFile.Id"/> of the library text read, or null when none was.</param>
/// <param name="Outcome">Found, not found or circular.</param>
public sealed record ListingInclusion(int Id, int? Parent, int Depth, int CopyLine, int CopyLastLine, string TextName,
    string? LibraryName, int? File, ListingInclusionOutcome Outcome);

/// <summary>The reference format a physical line was read in.</summary>
public enum ListingLineForm { Fixed, Free }

/// <summary>What a physical line is: the front end's <c>PhysicalLineKind</c>, with a comment-entry candidate settled by the
/// parse (DESIGN-listing.md section 4.4.1). The compiler maps one to the other in ONE switch a drift test holds total.</summary>
public enum ListingLineKind
{
    Source, Comment, PageEjectComment, DebuggingLine, Continuation, Directive, CommentEntry, CcvsExcluded, NotAnIndicator, Blank,
}

/// <summary>Which replacement, if any, the compiler made of a line's text (<see cref="ListingLine.Replaced"/>).</summary>
public enum ListingReplacement
{
    /// <summary>The line was compiled as written.</summary>
    None,
    /// <summary>The first line of a DATE-COMPILED paragraph, whose paragraph "is replaced during compilation with a paragraph
    /// of the form" <c>DATE-COMPILED. current date.</c> (§1985:VI.3.4.4 GR1): the listing prints the replacement in its place.</summary>
    DateCompiled,
    /// <summary>A further line of the comment-entry of a replaced DATE-COMPILED paragraph: replaced with the paragraph, so
    /// the text listing does not print it (the model keeps the written text).</summary>
    DateCompiledEntry,
}

/// <summary>One physical line of the source section (DESIGN-listing.md section 4.2).</summary>
/// <param name="Number">The listing line number, 1-based, unique in the document.</param>
/// <param name="File">The <see cref="ListingFile.Id"/> the line is read from.</param>
/// <param name="FileLine">The 1-based physical line of that file.</param>
/// <param name="Inclusion">The <see cref="ListingInclusion.Id"/> the line belongs to; null for the source.</param>
/// <param name="Depth">The inclusion depth, 0 for the source.</param>
/// <param name="Form">Fixed or free form.</param>
/// <param name="Kind">What the line is.</param>
/// <param name="Text">The physical text as the file holds it, tabs expanded.</param>
/// <param name="Omitted">A false conditional-compilation branch omitted the line.</param>
/// <param name="Listed">The visibility pass's answer: the text renderer prints it.</param>
/// <param name="PageBreakBefore">The visibility pass's answer: a page starts before it.</param>
/// <param name="Program">The <see cref="ListingProgram.Id"/> of the innermost program containing the line.</param>
/// <param name="Replacement">Which replacement the compiler made of the line, if any.</param>
/// <param name="Replaced">What the compiler compiled in place of <paramref name="Text"/>, when it is printed in its place.</param>
public sealed record ListingLine(int Number, int File, int FileLine, int? Inclusion, int Depth, ListingLineForm Form,
    ListingLineKind Kind, string Text, bool Omitted, bool Listed, bool PageBreakBefore, int? Program,
    ListingReplacement Replacement, string? Replaced);

/// <summary>The kind of source unit a <see cref="ListingProgram"/> is.</summary>
public enum ListingProgramKind { Program, Function, Class, Interface, Factory, Object, Method }

/// <summary>One source unit of the compilation group (DESIGN-listing.md section 6.5).</summary>
/// <param name="Id">0-based, in text order.</param>
/// <param name="Name">The name its identification paragraph declares, as written.</param>
/// <param name="Kind">Program, function, class, ...</param>
/// <param name="Parent">The <see cref="Id"/> of the unit that directly contains it, or null.</param>
/// <param name="Depth">0 for an outermost unit.</param>
/// <param name="FirstLine">The first listing line of the unit.</param>
/// <param name="LastLine">The last listing line of the unit.</param>
public sealed record ListingProgram(int Id, string Name, ListingProgramKind Kind, int? Parent, int Depth, int FirstLine, int LastLine);

/// <summary>The sections of a listing.</summary>
public enum ListingSection { Source, Programs }

/// <summary>Whether a section was produced and, when not, why (DESIGN-listing.md section 4.3): a section is never left out
/// silently.</summary>
public sealed record ListingSectionStatus(ListingSection Section, bool Produced, string? Reason);
