// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Serialization;

namespace WiseOwl.Cobol.Listing;

/// <summary>
/// The source listing of one compilation: the source as read, with its COPY inclusions and programs. Lines are referred to by
/// their listing line number and files by their id. Every format (text, JSON) renders this one document.
/// </summary>
/// <remarks>⛔ THE ONE LISTING MODEL (docs/rearchitecture/DESIGN-listing.md section 4; kb/Work PB5923, PB5925): built once by
/// the compiler's <c>ListingBuilder</c> from the source and the compiler's own data, and read by every renderer. Every type is
/// an immutable record. The summaries are the JSON schema's descriptions (<see cref="Json.ListingJsonSchema"/>), so they state
/// what a reader of the listing needs; step notes go in remarks. Step 1 of the design's section 11 built the header, files,
/// inclusions, lines and programs; step 2 the JSON form; later steps add their sections beside them.</remarks>
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
    IReadOnlyList<ListingSectionStatus> Sections)
{
    /// <summary>The identifier of the JSON schema's major version 1 (DESIGN-listing.md section 7.3): every JSON listing of this
    /// major carries it as <c>$schema</c>.</summary>
    public const string SchemaUrn = "urn:wiseowl:cobol:listing:v1";

    /// <summary>The schema version this compiler writes, <c>1.&lt;minor&gt;</c>: the minor grows when a property or an enum
    /// value is added, the major only when one is removed, renamed or changes meaning (DESIGN-listing.md section 7.3).</summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>The identifier of the schema the document follows: <c>urn:wiseowl:cobol:listing:v1</c> for every version of
    /// major 1.</summary>
    [JsonPropertyName("$schema"), JsonPropertyOrder(-2), JsonRequired]
    public string Schema { get; init; } = SchemaUrn;

    /// <summary>The schema version the document was written at, <c>&lt;major&gt;.&lt;minor&gt;</c>. A reader of major 1
    /// reads every 1.x document and ignores the properties it does not know.</summary>
    [JsonPropertyOrder(-1), JsonRequired]
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
}

/// <summary>How far the compilation got (the compiler's outcome, in the listing's own terms).</summary>
public enum ListingOutcome
{
    /// <summary>The assembly was written.</summary>
    Success,
    /// <summary>The text stages or the parser refused the source.</summary>
    FrontendError,
    /// <summary>The binder refused the program.</summary>
    BindError,
    /// <summary>The backend could not produce the assembly.</summary>
    BackendError,
    /// <summary>The source compiled, but the host refused to write the assembly.</summary>
    OutputNotWritable,
}

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
public enum ListingFileRole
{
    /// <summary>The source file the compilation was asked to compile.</summary>
    Source,
    /// <summary>A library text a COPY statement read.</summary>
    Library,
}

/// <summary>One file the compilation read.</summary>
/// <param name="Id">1-based; the source is 1.</param>
/// <param name="Path">The path as the compilation read it.</param>
/// <param name="Role">Source or library text.</param>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 digest of the file's bytes as read.</param>
public sealed record ListingFile(int Id, string Path, ListingFileRole Role, string Sha256);

/// <summary>What became of a COPY statement.</summary>
public enum ListingInclusionOutcome
{
    /// <summary>The library text was found and copied.</summary>
    Found,
    /// <summary>No library text of that name was found; nothing was copied.</summary>
    NotFound,
    /// <summary>The library text copies itself, directly or through others; it was not copied again.</summary>
    Circular,
}

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
public enum ListingLineForm
{
    /// <summary>Fixed-form reference format: sequence area, indicator area, areas A and B, identification area.</summary>
    Fixed,
    /// <summary>Free-form reference format.</summary>
    Free,
}

/// <summary>What a physical line is: the front end's <c>PhysicalLineKind</c>, with a comment-entry candidate settled by the
/// parse (DESIGN-listing.md section 4.4.1). The compiler maps one to the other in ONE switch a drift test holds total.</summary>
public enum ListingLineKind
{
    /// <summary>A line of program text.</summary>
    Source,
    /// <summary>A comment line: the fixed-form <c>*</c> indicator, or the floating comment indicator.</summary>
    Comment,
    /// <summary>A comment line with the fixed-form slant (<c>/</c>) indicator, which ejects a page before it is listed.</summary>
    PageEjectComment,
    /// <summary>A debugging line: the fixed-form <c>D</c> indicator, or the floating <c>&gt;&gt;D</c> indicator.</summary>
    DebuggingLine,
    /// <summary>A fixed-form continuation line (the hyphen indicator).</summary>
    Continuation,
    /// <summary>A compiler directive line, where the edition has compiler directives.</summary>
    Directive,
    /// <summary>A line of the comment-entry of an IDENTIFICATION DIVISION paragraph such as AUTHOR or DATE-COMPILED.</summary>
    CommentEntry,
    /// <summary>A line the NIST CCVS column-7 conventions exclude (the <c>--nist</c> dialect only).</summary>
    CcvsExcluded,
    /// <summary>A line whose indicator area holds a character that is no indicator, read as program text.</summary>
    NotAnIndicator,
    /// <summary>A blank line: nothing but spaces in its program text.</summary>
    Blank,
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
public enum ListingProgramKind
{
    /// <summary>A program.</summary>
    Program,
    /// <summary>A user-defined function.</summary>
    Function,
    /// <summary>A class definition.</summary>
    Class,
    /// <summary>An interface definition.</summary>
    Interface,
    /// <summary>The factory definition of a class.</summary>
    Factory,
    /// <summary>The object (instance) definition of a class.</summary>
    Object,
    /// <summary>A method definition.</summary>
    Method,
}

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
public enum ListingSection
{
    /// <summary>The source section: every physical line of every file read.</summary>
    Source,
    /// <summary>The program map: every source unit, its nesting and its listing lines.</summary>
    Programs,
}

/// <summary>Whether a section was produced and, when not, why (DESIGN-listing.md section 4.3): a section is never left out
/// silently.</summary>
/// <param name="Section">The section.</param>
/// <param name="Produced">Whether the listing holds the section.</param>
/// <param name="Reason">Why the section was not produced (the phase that stopped it), or null when it was.</param>
public sealed record ListingSectionStatus(ListingSection Section, bool Produced, string? Reason);
