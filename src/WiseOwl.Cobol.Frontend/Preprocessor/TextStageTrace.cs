// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>What became of one COPY statement the text stages processed (<see cref="TextInclusion.Outcome"/>).</summary>
public enum TextInclusionOutcome
{
    /// <summary>The library text was found and incorporated.</summary>
    Found,
    /// <summary>No library text was incorporated: not found, unreadable, or the statement could not be parsed.</summary>
    NotFound,
    /// <summary>The library text would copy itself (§2023:7.2.3.4 GR12): skipped.</summary>
    Circular,
}

/// <summary>
/// One COPY statement the text stages processed, and where its library text landed (DESIGN-listing.md section 4.4).
/// </summary>
/// <param name="Id">1-based, in the order the statements were met, which is text order.</param>
/// <param name="Parent">The inclusion whose library text holds the COPY statement, or null for the source text.</param>
/// <param name="Depth">1 for a COPY statement of the source text, one more per library text it is nested in.</param>
/// <param name="CopyFirst">The physical line the COPY word stands on.</param>
/// <param name="CopyLast">The physical line the statement's separator period stands on: the listing places the library
/// text after it.</param>
/// <param name="TextName">The text-name or literal the statement names.</param>
/// <param name="LibraryName">The library-name or literal of its OF / IN phrase, or null when it has none.</param>
/// <param name="File">The library text read (the key of its <see cref="ReferenceFormatMap"/>), or null when none was.</param>
/// <param name="Outcome">Found, not found or circular.</param>
/// <param name="ResultantFirst">The first 1-based line of the RESULTANT text (the parser's token lines) the library text
/// occupies, or 0 when it occupies none.</param>
/// <param name="ResultantLast">The last such line (inclusive), or 0.</param>
/// <param name="Suppress">The statement writes the SUPPRESS phrase: its library text "is not listed" (§2023:7.2.3.4 GR4).</param>
public sealed record TextInclusion(int Id, int? Parent, int Depth, SourceOrigin CopyFirst, SourceOrigin CopyLast,
    string TextName, string? LibraryName, string? File, TextInclusionOutcome Outcome, int ResultantFirst, int ResultantLast,
    bool Suppress)
{
    /// <summary>Whether 1-based resultant <paramref name="line"/> lies in this inclusion's library text.</summary>
    public bool Holds(int line) => ResultantFirst > 0 && line >= ResultantFirst && line <= ResultantLast;

    /// <summary>The copy member this statement names, as the cross-reference lists it: the text-name, then <c>OF</c> and the library-name
    /// when it writes one.</summary>
    public string MemberName => LibraryName is { } library ? TextName + " OF " + library : TextName;

    /// <summary>The Annex C fold of <see cref="MemberName"/> (<c>CobolNames.UpperFold</c>): the cross-reference's sort key, which every other
    /// symbol's name has too (DESIGN-listing.md section 6.7), so <c>payrec</c> and <c>PAYREC</c> name one member.</summary>
    public string MemberFold => WiseOwl.Cobol.Runtime.CobolNames.UpperFold(MemberName);
}

/// <summary>What changed the LISTING directive's state (<see cref="TextListingSwitch.Cause"/>).</summary>
public enum TextListingSwitchCause
{
    /// <summary>A LISTING directive (§2023:7.3.18.3 GR4, GR5): the line itself is always listed (GR3), and the state it sets
    /// governs the lines after it.</summary>
    Directive,
    /// <summary>A written POP directive restored a saved state (§2023:7.3.20.4 GR1, GR3): the state governs the lines after it.</summary>
    Pop,
    /// <summary>The implicit POP ALL "immediately preceding the END PERFORM phrase" of an exception-checking PERFORM
    /// (§2023:14.9.28.4 GR14) restored a saved state: the state governs its RESULTANT line on.</summary>
    ImplicitPop,
    /// <summary>A library text ended: "the LISTING directive that is in effect for the COPY statement itself is considered to
    /// be in effect, regardless of any LISTING directives in the library text" (§2023:7.2.3.4 GR5); the state governs the
    /// lines after the inclusion's.</summary>
    InclusionEnd,
}

/// <summary>One change of the LISTING directive's state, decided by the conditional-compilation driver (the one holder of
/// the state, which a PUSH saves and a POP restores) and recorded where it acts (DESIGN-listing.md section 4.4).</summary>
/// <param name="Cause">What changed it.</param>
/// <param name="On">The state from then on: true for ON (§2023:7.3.18.3 GR5), false for OFF (GR4).</param>
/// <param name="Inclusion">The inclusion whose library text holds the directive (null: the source text); for
/// <see cref="TextListingSwitchCause.InclusionEnd"/>, the inclusion that ended; null for an implicit POP.</param>
/// <param name="Line">The 1-based physical line of the directive in that text; for an implicit POP, the 1-based RESULTANT line
/// of the END PERFORM phrase; 0 for an inclusion's end.</param>
public sealed record TextListingSwitch(TextListingSwitchCause Cause, bool On, int? Inclusion, int Line);

/// <summary>A directive line the listing acts on at its place: a PAGE directive processed (§2023:7.3.19.4 GR2).</summary>
/// <param name="Inclusion">The inclusion whose library text holds the directive, or null for the source text.</param>
/// <param name="Line">The 1-based physical line of the directive in that text.</param>
public sealed record TextDirectiveSite(int? Inclusion, int Line);

/// <summary>One transfer of a DISPLAY directive to the source listing (§2023:7.3.12.4 GR1, GR5 a), GR6).</summary>
/// <param name="Inclusion">The inclusion whose library text holds the directive, or null for the source text.</param>
/// <param name="Line">The 1-based physical line of the directive in that text.</param>
/// <param name="Text">The data transferred: the operands' images, in the order written (DOC-A.1-53).</param>
public sealed record TextDisplayTransfer(int? Inclusion, int Line, string Text);

/// <summary>A physical line where a replacing action matched (§2023:7.2.3.4 GR8; §2023:7.2.4.4 GR8), so the text compiled for
/// it is not the text written.</summary>
/// <param name="Inclusion">For COPY REPLACING, the inclusion whose library text was replaced; null for a REPLACE statement,
/// whose text is placed by <paramref name="ResultantLine"/>.</param>
/// <param name="Origin">The physical line the match begins on.</param>
/// <param name="ResultantLine">For a REPLACE statement, the 1-based RESULTANT line the replacement begins on; 0 for COPY
/// REPLACING.</param>
public sealed record TextReplacedLine(int? Inclusion, SourceOrigin Origin, int ResultantLine);

/// <summary>
/// ⛔ THE TEXT STAGES' TRACE for the source listing (DESIGN-listing.md section 4.4; kb/Work PB5923, PB5925): what the text
/// stages did, recorded at the one place each acts, so the listing never re-derives a fact from text. One trace per run of
/// the text stages, created beside that run's <c>DiagnosticBag</c>. It records the inclusions (step 1) and, where the stages
/// act on them (step 4a), the LISTING state's changes, the PAGE directives, the DISPLAY transfers to the listing and the
/// lines a replacing action changed. The run whose diagnostics are the compilation's is the run the listing reads
/// (<c>Frontend.ListingTrace</c>).
/// <para>A compilation that asks for no listing passes <see cref="None"/> (DESIGN-listing.md section 9: no listing, no listing
/// work). ⛔ EVERY RECORDING CALL IS ASKED ONLY WHEN <see cref="Records"/> IS TRUE, so a call's arguments (an origin the
/// caller computes, a switch) are never evaluated for a compilation without a listing; <see cref="None"/>'s recording methods
/// THROW, so a call site that forgets the question fails every compilation that reaches it without a listing, the
/// no-work drift test (<c>ListingOffDoesNoWorkDriftTests</c>) first (kb/Work PB5923 review R1-3).</para>
/// <para>The inclusions are recorded in the merged driver's OUTPUT frame (<c>CopyProcessor.ExpandCopiesOneLevel</c> begins
/// one, <c>ConditionalCompilationProcessor.Run.RenderCopybook</c> places its library text) and carried through REPLACE to
/// the resultant frame by the one line map REPLACE answers (<c>CopyProcessor.ReplaceLineMap</c>), so a copybook copied
/// twice is two inclusions with two resultant ranges. A directive, a DISPLAY transfer and a COPY REPLACING match are recorded
/// with the inclusion OPEN when the stage meets them, so the listing places them in the right copy of a copybook copied twice;
/// a REPLACE match, made after every inclusion has closed, is placed by its resultant line.</para>
/// </summary>
public class TextStageTrace
{
    /// <summary>The trace that records nothing: a compilation without a listing.</summary>
    public static readonly TextStageTrace None = new NoTrace();

    private sealed class Open(int id, int? parent, int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
        string? libraryName, string? file, TextInclusionOutcome outcome, bool suppress)
    {
        public readonly int Id = id;
        public readonly int? Parent = parent;
        public readonly int Depth = depth;
        public readonly SourceOrigin CopyFirst = copyFirst, CopyLast = copyLast;
        public readonly string TextName = textName;
        public readonly string? LibraryName = libraryName;
        public readonly string? File = file;
        public readonly TextInclusionOutcome Outcome = outcome;
        public readonly bool Suppress = suppress;
        public int OutputFirst = -1;   // 0-based line of the merged driver's output frame, -1 when not placed
        public int OutputCount;
        public int ResultantFirst, ResultantLast;
    }

    private readonly List<Open> _inclusions = [];
    private readonly Stack<Open> _open = new();
    private readonly List<TextListingSwitch> _listingSwitches = [];
    private readonly List<TextDirectiveSite> _pages = [];
    private readonly List<TextDisplayTransfer> _displays = [];
    private readonly List<TextReplacedLine> _replaced = [];
    private readonly List<(int OutputLine, SourceOrigin Origin)> _replacedInOutput = [];

    /// <summary>Whether this trace records (false only for <see cref="None"/>).</summary>
    public virtual bool Records => true;

    /// <summary>Every COPY statement processed, in the order met.</summary>
    public IReadOnlyList<TextInclusion> Inclusions => [.. _inclusions.Select(o => new TextInclusion(o.Id, o.Parent, o.Depth,
        o.CopyFirst, o.CopyLast, o.TextName, o.LibraryName, o.File, o.Outcome, o.ResultantFirst, o.ResultantLast, o.Suppress))];

    /// <summary>Every change of the LISTING directive's state, in the order the driver made them.</summary>
    public IReadOnlyList<TextListingSwitch> ListingSwitches => _listingSwitches;

    /// <summary>Every PAGE directive processed, in the order met.</summary>
    public IReadOnlyList<TextDirectiveSite> PageDirectives => _pages;

    /// <summary>Every DISPLAY directive transfer to the listing, in the order processed (§2023:7.3.12.4 GR1).</summary>
    public IReadOnlyList<TextDisplayTransfer> Displays => _displays;

    /// <summary>Every physical line a replacing action matched on, in the order met.</summary>
    public IReadOnlyList<TextReplacedLine> ReplacedLines => _replaced;

    /// <summary>The inclusion whose library text the stages are now processing, or null for the source text.</summary>
    private int? Current => _open.TryPeek(out var open) ? open.Id : null;

    /// <summary>A COPY statement begins its inclusion (<c>CopyProcessor.ExpandCopiesOneLevel</c>, once
    /// <c>ResolveOneCopy</c> has decided it); the inclusion now open is the parent of any COPY met in its library text until
    /// <see cref="EndInclusion"/>. <paramref name="suppress"/>: the statement writes SUPPRESS; <paramref name="replacedLines"/>:
    /// the physical lines of the library text where its REPLACING phrase matched (the replacing action ran before the
    /// inclusion began, in <c>CopyProcessor.ResolveOneCopy</c>).</summary>
    internal virtual void BeginInclusion(int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
        string? libraryName, string? file, TextInclusionOutcome outcome, bool suppress, IReadOnlyList<SourceOrigin> replacedLines)
    {
        var open = new Open(_inclusions.Count + 1, Current, depth, copyFirst, copyLast,
            textName, libraryName, file, outcome, suppress);
        _inclusions.Add(open);
        _open.Push(open);
        foreach (var origin in replacedLines) _replaced.Add(new TextReplacedLine(open.Id, origin, 0));
    }

    /// <summary>The library text of the inclusion now open was rendered at 0-based line <paramref name="outputFirst"/> of the
    /// merged driver's output frame, <paramref name="lineCount"/> lines (<c>ConditionalCompilationProcessor.Run.RenderCopybook</c>).</summary>
    internal virtual void PlaceInclusion(int outputFirst, int lineCount)
    {
        var open = _open.Peek();
        open.OutputFirst = outputFirst;
        open.OutputCount = lineCount;
    }

    /// <summary>The inclusion now open is finished.</summary>
    internal virtual void EndInclusion() => _open.Pop();

    /// <summary>The LISTING directive at physical <paramref name="at"/> of the text now processed set the state to
    /// <paramref name="on"/> (<c>ConditionalCompilationProcessor.Run.Render</c>, an emitting branch only).</summary>
    internal virtual void ListingDirective(SourceOrigin at, bool on) =>
        _listingSwitches.Add(new TextListingSwitch(TextListingSwitchCause.Directive, on, Current, at.Line));

    /// <summary>The POP directive at physical <paramref name="at"/> of the text now processed restored the state
    /// <paramref name="on"/>.</summary>
    internal virtual void ListingPopped(SourceOrigin at, bool on) =>
        _listingSwitches.Add(new TextListingSwitch(TextListingSwitchCause.Pop, on, Current, at.Line));

    /// <summary>An implicit POP ALL at 1-based RESULTANT <paramref name="line"/> restored the state <paramref name="on"/>.</summary>
    internal virtual void ListingImplicitlyPopped(int line, bool on) =>
        _listingSwitches.Add(new TextListingSwitch(TextListingSwitchCause.ImplicitPop, on, null, line));

    /// <summary>The library text of the inclusion now open ended, and the state returned to <paramref name="on"/>, the state at
    /// its COPY statement (§2023:7.2.3.4 GR5).</summary>
    internal virtual void ListingRestoredAtInclusionEnd(bool on) =>
        _listingSwitches.Add(new TextListingSwitch(TextListingSwitchCause.InclusionEnd, on, _open.Peek().Id, 0));

    /// <summary>The PAGE directive at physical <paramref name="at"/> of the text now processed (an emitting branch only).</summary>
    internal virtual void PageDirective(SourceOrigin at) => _pages.Add(new TextDirectiveSite(Current, at.Line));

    /// <summary>The DISPLAY directive at physical <paramref name="at"/> of the text now processed transferred
    /// <paramref name="text"/> to the listing.</summary>
    internal virtual void Display(SourceOrigin at, string text) => _displays.Add(new TextDisplayTransfer(Current, at.Line, text));

    /// <summary>A REPLACE statement's replacing action matched at 0-based line <paramref name="outputLine"/> of the merged
    /// driver's output frame, on physical <paramref name="origin"/> (<c>CopyProcessor.ApplyReplaceStatements</c>); carried to
    /// the resultant frame by <see cref="ToResultantFrame"/>.</summary>
    internal virtual void ReplacedByReplace(int outputLine, SourceOrigin origin) => _replacedInOutput.Add((outputLine, origin));

    /// <summary>Carry every placed inclusion and every REPLACE match from the merged driver's output frame to the resultant
    /// frame: <paramref name="resultantLine"/>[i] is the 0-based resultant line of output line i (<c>CopyProcessor.ReplaceLineMap</c>),
    /// the only stage after the driver that changes the line count.</summary>
    internal virtual void ToResultantFrame(int[] resultantLine)
    {
        if (_open.Count != 0) throw new InvalidOperationException($"{_open.Count} inclusion(s) still open after the text stages");
        foreach (var o in _inclusions)
        {
            if (o.OutputFirst < 0 || o.OutputCount == 0) continue;
            o.ResultantFirst = resultantLine[o.OutputFirst] + 1;
            o.ResultantLast = resultantLine[o.OutputFirst + o.OutputCount - 1] + 1;
        }
        foreach (var (outputLine, origin) in _replacedInOutput)
            _replaced.Add(new TextReplacedLine(null, origin, resultantLine[outputLine] + 1));
        _replacedInOutput.Clear();
    }

    private sealed class NoTrace : TextStageTrace
    {
        public override bool Records => false;
        internal override void BeginInclusion(int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
            string? libraryName, string? file, TextInclusionOutcome outcome, bool suppress, IReadOnlyList<SourceOrigin> replacedLines)
            => throw NotRecording();
        internal override void PlaceInclusion(int outputFirst, int lineCount) => throw NotRecording();
        internal override void EndInclusion() => throw NotRecording();
        internal override void ListingDirective(SourceOrigin at, bool on) => throw NotRecording();
        internal override void ListingPopped(SourceOrigin at, bool on) => throw NotRecording();
        internal override void ListingImplicitlyPopped(int line, bool on) => throw NotRecording();
        internal override void ListingRestoredAtInclusionEnd(bool on) => throw NotRecording();
        internal override void PageDirective(SourceOrigin at) => throw NotRecording();
        internal override void Display(SourceOrigin at, string text) => throw NotRecording();
        internal override void ReplacedByReplace(int outputLine, SourceOrigin origin) => throw NotRecording();
        internal override void ToResultantFrame(int[] resultantLine) => throw NotRecording();

        private static InvalidOperationException NotRecording() => new(
            "a text stage recorded into TextStageTrace.None: every recording call asks TextStageTrace.Records first, so a "
            + "compilation without a listing evaluates none of its arguments (DESIGN-listing.md section 9)");
    }
}
