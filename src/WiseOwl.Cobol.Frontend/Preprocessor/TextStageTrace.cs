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
public sealed record TextInclusion(int Id, int? Parent, int Depth, SourceOrigin CopyFirst, SourceOrigin CopyLast,
    string TextName, string? LibraryName, string? File, TextInclusionOutcome Outcome, int ResultantFirst, int ResultantLast)
{
    /// <summary>Whether 1-based resultant <paramref name="line"/> lies in this inclusion's library text.</summary>
    public bool Holds(int line) => ResultantFirst > 0 && line >= ResultantFirst && line <= ResultantLast;
}

/// <summary>
/// ⛔ THE TEXT STAGES' TRACE for the source listing (DESIGN-listing.md section 4.4; kb/Work PB5923): what the COPY stage did,
/// recorded at the one place it acts, so the listing never re-derives an inclusion from text. One trace per run of the text
/// stages, created beside that run's <c>DiagnosticBag</c>; the run whose diagnostics are the compilation's is the run the
/// listing reads (<c>Frontend.ListingTrace</c>).
/// <para>A compilation that asks for no listing passes <see cref="None"/> (DESIGN-listing.md section 9: no listing, no listing
/// work). ⛔ EVERY RECORDING CALL IS ASKED ONLY WHEN <see cref="Records"/> IS TRUE, so a call's arguments (an origin the
/// caller computes, a switch) are never evaluated for a compilation without a listing; <see cref="None"/>'s recording methods
/// THROW, so a call site that forgets the question fails every compilation of a COPY statement without a listing, the
/// no-work drift test (<c>ListingOffDoesNoWorkDriftTests</c>) first (kb/Work PB5923 review R1-3).</para>
/// <para>The inclusions are recorded in the merged driver's OUTPUT frame (<c>CopyProcessor.ExpandCopiesOneLevel</c> begins
/// one, <c>ConditionalCompilationProcessor.Run.RenderCopybook</c> places its library text) and carried through REPLACE to
/// the resultant frame by the one line map REPLACE answers (<c>CopyProcessor.ReplaceLineMap</c>), so a copybook copied
/// twice is two inclusions with two resultant ranges.</para>
/// </summary>
public class TextStageTrace
{
    /// <summary>The trace that records nothing: a compilation without a listing.</summary>
    public static readonly TextStageTrace None = new NoTrace();

    private sealed class Open(int id, int? parent, int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
        string? libraryName, string? file, TextInclusionOutcome outcome)
    {
        public readonly int Id = id;
        public readonly int? Parent = parent;
        public readonly int Depth = depth;
        public readonly SourceOrigin CopyFirst = copyFirst, CopyLast = copyLast;
        public readonly string TextName = textName;
        public readonly string? LibraryName = libraryName;
        public readonly string? File = file;
        public readonly TextInclusionOutcome Outcome = outcome;
        public int OutputFirst = -1;   // 0-based line of the merged driver's output frame, -1 when not placed
        public int OutputCount;
        public int ResultantFirst, ResultantLast;
    }

    private readonly List<Open> _inclusions = [];
    private readonly Stack<Open> _open = new();

    /// <summary>Whether this trace records (false only for <see cref="None"/>).</summary>
    public virtual bool Records => true;

    /// <summary>Every COPY statement processed, in the order met.</summary>
    public IReadOnlyList<TextInclusion> Inclusions => [.. _inclusions.Select(o => new TextInclusion(o.Id, o.Parent, o.Depth,
        o.CopyFirst, o.CopyLast, o.TextName, o.LibraryName, o.File, o.Outcome, o.ResultantFirst, o.ResultantLast))];

    /// <summary>A COPY statement begins its inclusion (<c>CopyProcessor.ExpandCopiesOneLevel</c>, once
    /// <c>ResolveOneCopy</c> has decided it); the inclusion now open is the parent of any COPY met in its library text until
    /// <see cref="EndInclusion"/>.</summary>
    internal virtual void BeginInclusion(int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
        string? libraryName, string? file, TextInclusionOutcome outcome)
    {
        var open = new Open(_inclusions.Count + 1, _open.TryPeek(out var parent) ? parent.Id : null, depth, copyFirst, copyLast,
            textName, libraryName, file, outcome);
        _inclusions.Add(open);
        _open.Push(open);
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

    /// <summary>Carry every placed inclusion from the merged driver's output frame to the resultant frame:
    /// <paramref name="resultantLine"/>[i] is the 0-based resultant line of output line i (<c>CopyProcessor.ReplaceLineMap</c>),
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
    }

    private sealed class NoTrace : TextStageTrace
    {
        public override bool Records => false;
        internal override void BeginInclusion(int depth, SourceOrigin copyFirst, SourceOrigin copyLast, string textName,
            string? libraryName, string? file, TextInclusionOutcome outcome) => throw NotRecording();
        internal override void PlaceInclusion(int outputFirst, int lineCount) => throw NotRecording();
        internal override void EndInclusion() => throw NotRecording();
        internal override void ToResultantFrame(int[] resultantLine) => throw NotRecording();

        private static InvalidOperationException NotRecording() => new(
            "a text stage recorded into TextStageTrace.None: every recording call asks TextStageTrace.Records first, so a "
            + "compilation without a listing evaluates none of its arguments (DESIGN-listing.md section 9)");
    }
}
