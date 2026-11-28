// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure;

/// <summary>
/// ⛔ THE STATIC RANGE OF A PROCEDURE TRANSFER — which paragraphs execute UNDER a set of procedure ranges, written
/// once over the bound procedures (kb/Work PB434).
///
/// <para><b>The rule it models.</b> ISO §2023:14.9.28.4 GR1: "The range includes all statements that are executed as
/// the result of a transfer of control in the range of the PERFORM statement". A range is therefore not the text
/// between two procedure-names: a PERFORM of another procedure from inside it, a SORT or MERGE whose INPUT or
/// OUTPUT PROCEDURE runs from inside it, and a handler of an exception-checking PERFORM inside it all execute
/// their own procedures and RETURN, so those procedures are in the range too, transitively.</para>
///
/// <para><b>What it follows, and what it does not.</b> It follows every RETURNING transfer
/// (<see cref="ReturningTransfersIn"/>) and starts from each seed's specified set read LEXICALLY
/// (<see cref="PcRange.Spans"/>, both physical orders of a GR6 inverted range). It does NOT follow a GO TO or a
/// declarative: where a GO TO leads after it leaves a range, and whether an I/O failure invokes a USE procedure,
/// are run-time facts, and a syntax rule that followed them statically would refuse statements no execution
/// ever reaches under the PERFORM.</para>
///
/// <para>A SYNTAX rule scoped to "under" or "within the range of" asks this model, never its own walk:
/// §2023:14.9.28.3 SR8 (<see cref="UntilExitPlacement"/>) does, and kb/Work PB812 records the SORT/MERGE-procedure
/// bans (<c>VersionConformancePass.GateSortMergeProcedures</c>) as the next reader.</para>
/// </summary>
internal static class ProcedureReach
{
    /// <summary>Every procedure range a statement list transfers to AND RETURNS FROM, at any nesting depth: an
    /// out-of-line PERFORM's range (§2023:14.9.28.4 GR4/GR5), a SORT's INPUT and OUTPUT PROCEDURE and a MERGE's OUTPUT
    /// PROCEDURE (§2023:14.9.40.4 GR10/GR11, §2023:14.9.24.4), and each handler body of an exception-checking PERFORM, which
    /// the binder appends as its own one-paragraph pc range (§2023:14.9.28.4 GR17).</summary>
    public static IEnumerable<PcRange> ReturningTransfersIn(IEnumerable<BoundStatement> statements)
    {
        foreach (var s in statements)
        {
            if (s is BoundOutOfLinePerform perform) yield return perform.Range;
            if (s is BoundSort { InputProcedure: { } input }) yield return input;
            if (s is BoundSort { OutputProcedure: { } sortOutput }) yield return sortOutput;
            if (s is BoundMerge { OutputProcedure: { } mergeOutput }) yield return mergeOutput;
            if (s is BoundExceptionPerform f3)
            {
                foreach (var when in f3.Whens) yield return PcRange.At(when.Imp2Pc);
                if (f3.OtherPc is { } other) yield return PcRange.At(other);
                if (f3.CommonPc is { } common) yield return PcRange.At(common);
            }
            foreach (var nested in ReturningTransfersIn(s.StatementChildren())) yield return nested;
        }
    }

    /// <summary>Every paragraph a GO TO in a statement list transfers to, at any nesting depth: GO TO (format 1 and its DEPENDING
    /// form) and an alterable GO TO's written target, plus every paragraph an ALTER ANYWHERE in the program makes that GO TO proceed
    /// to (<paramref name="alteredTargets"/>, by the GO TO's paragraph-name: an ALTER executed before the range was entered changes
    /// where the range's GO TO leads just as one executed inside it — §1985:XI.4.4.4 GR7). Each is the transfer's own destination
    /// paragraph; where control goes from there is the caller's walk.</summary>
    public static IEnumerable<int> GoToTargetsIn(IEnumerable<BoundStatement> statements, ILookup<string, int> alteredTargets)
    {
        foreach (var s in statements)
        {
            switch (s)
            {
                case BoundGoTo g: yield return g.TargetPc; break;
                case BoundGoToDepending d: foreach (int t in d.Targets) yield return t; break;
                case BoundGoToAlterable a:
                    if (a.DefaultPc >= 0) yield return a.DefaultPc;
                    foreach (int t in alteredTargets[a.AlterField]) yield return t;
                    break;
            }
            foreach (int nested in GoToTargetsIn(s.StatementChildren(), alteredTargets)) yield return nested;
        }
    }

    /// <summary>Every ALTER destination the program holds, keyed by the alterable GO TO's C# alter field (a field
    /// name the binder made, never the COBOL word: one paragraph's field is one spelling).</summary>
    private static ILookup<string, int> AlteredTargetsOf(IReadOnlyList<BoundParagraph> paragraphs)
    {
        var entries = new List<BoundAlterEntry>();
        void Collect(IEnumerable<BoundStatement> statements)
        {
            foreach (var s in statements)
            {
                if (s is BoundAlter al) entries.AddRange(al.Entries);
                Collect(s.StatementChildren());
            }
        }
        foreach (var p in paragraphs) Collect(p.Statements);
        return entries.ToLookup(e => e.AlterField, e => e.NewPc, StringComparer.Ordinal);   // a C# field name, no COBOL word
    }

    /// <summary>The paragraph pcs executed under <paramref name="seeds"/>, each mapped to the origin of the FIRST
    /// seed found to reach it: the seeds' own specified sets, then — breadth-first, so a pc is reached by its
    /// shortest chain — every range a reached paragraph transfers to and returns from.
    /// <para><paramref name="followGoTo"/> is the reading of a RANGE the 1985 sort-merge rules use (§1985:XI.4.4.4 GR7: "The range includes all
    /// statements that are executed as the result of a transfer of control by CALL, EXIT, GO TO, and PERFORM statements in the range of the
    /// input procedure"; kb/Work PB5104): control is also followed as it flows (<see cref="Walk{TOrigin}"/>: every GO TO destination, an
    /// alterable one's ALTER destinations, and the fall-through to the next paragraph until the range's return mechanism), so a statement this
    /// walk does not reach is one no execution of the range can reach. False keeps the model above, which a syntax rule about "under" needs (it
    /// does not follow a GO TO, so it never refuses a statement no execution reaches under the PERFORM).</para></summary>
    public static Dictionary<int, TOrigin> Closure<TOrigin>(
        IReadOnlyList<BoundParagraph> paragraphs, IEnumerable<(PcRange Range, TOrigin Origin)> seeds, bool followGoTo = false) =>
        Walk(paragraphs, seeds, fromEntry: false, followReturning: true, followFlow: followGoTo);

    /// <summary>The paragraphs execution of <paramref name="range"/> reaches from its FIRST paragraph by the flow of control alone: the
    /// fall-through and every GO TO (<see cref="Walk{TOrigin}"/>), wherever it leads, out of the range's procedures and back included, and no
    /// PERFORM, SORT or MERGE range inside it (each of those is its own statement's question). The question the COBOL-85 segmentation
    /// restrictions ask of the procedures a statement names (<c>Segmentation.CheckProcedures</c>, kb/Work PB5442, PB6007).</summary>
    public static IReadOnlyCollection<int> FlowFromEntry(IReadOnlyList<BoundParagraph> paragraphs, PcRange range) =>
        Walk(paragraphs, [(range, 0)], fromEntry: true, followReturning: false, followFlow: true).Keys;

    /// <summary>⛔ THE ONE REACH WALK (kb/Work PB6007, PB6008): every question "which paragraphs does control reach from a range" is answered
    /// here, each reader choosing what it follows. A range is entered at its first paragraph (<paramref name="fromEntry"/>) or at every
    /// paragraph of its lexical span (<see cref="PcRange.SpannedPcs"/>, either physical order); <paramref name="followReturning"/> enters
    /// every range a reached paragraph transfers to and returns from (<see cref="ReturningTransfersIn"/>).
    /// <para><paramref name="followFlow"/> follows control as it runs: each GO TO in a reached paragraph reaches its destination ("control is
    /// transferred to procedure-name-1", §1985:VI.6.15.4 GR1; an alterable GO TO also each paragraph any ALTER of it names), and a reached
    /// paragraph falls through to the next one unless one of its own statements transfers control away for certain (an unconditional GO TO, or
    /// STOP RUN, by which "execution of the run unit ceases", §1985:VI.6.24.4 GR1) or it is the last paragraph of the range whose execution
    /// reached it: there the compiler-inserted return mechanism ends the range ("When control passes the last statement in the output
    /// procedure, the return mechanism provides for termination of the merge", §1985:XI.4.1.4 GR9; §1985:XI.4.4.4 GR8 and GR11 for the
    /// SORT's procedures; §1985:VI.6.21.4 GR11 for a PERFORM). Each walk step carries that return point, so a path that leaves the range and
    /// comes back stops where the range ends, and a procedure written after the range is reached only if some path really flows into it, never
    /// because of where it stands in the text (kb/Work PB6008).</para></summary>
    private static Dictionary<int, TOrigin> Walk<TOrigin>(IReadOnlyList<BoundParagraph> paragraphs,
        IEnumerable<(PcRange Range, TOrigin Origin)> seeds, bool fromEntry, bool followReturning, bool followFlow)
    {
        var reached = new Dictionary<int, TOrigin>();
        var work = new Queue<(int Pc, int Exit)>();
        var walked = new HashSet<(int Pc, int Exit)>();
        var altered = followFlow ? AlteredTargetsOf(paragraphs) : null;
        void Reach(int pc, int exit, TOrigin origin)
        {
            if ((uint)pc >= (uint)paragraphs.Count) return;
            reached.TryAdd(pc, origin);
            if (walked.Add((pc, exit))) work.Enqueue((pc, exit));
        }
        void Enter(PcRange range, TOrigin origin)
        {
            if (range.IsEmpty) return;
            if (fromEntry) Reach(range.Start, range.End, origin);
            else foreach (int pc in range.SpannedPcs()) Reach(pc, range.End, origin);
        }
        foreach (var (range, origin) in seeds) Enter(range, origin);
        while (work.TryDequeue(out var step))
        {
            var (pc, exit) = step;
            var origin = reached[pc];
            var paragraph = paragraphs[pc];
            if (followReturning)
                foreach (var range in ReturningTransfersIn(paragraph.Statements)) Enter(range, origin);
            if (!followFlow) continue;
            foreach (int destination in GoToTargetsIn(paragraph.Statements, altered!)) Reach(destination, exit, origin);
            if (pc != exit && !TransfersAway(paragraph)) Reach(pc + 1, exit, origin);
        }
        return reached;
    }

    /// <summary>Does one of <paramref name="paragraph"/>'s own statements (not one nested in a conditional) transfer control away for
    /// certain, so the paragraph never falls through: a GO TO without the DEPENDING phrase, written or alterable (§1985:VI.6.15.4 GR1), or
    /// STOP RUN (§1985:VI.6.24.4 GR1). A GO TO DEPENDING whose identifier is out of range falls through, and so does every other
    /// statement.</summary>
    private static bool TransfersAway(BoundParagraph paragraph) =>
        paragraph.Statements.Any(s => s is BoundGoTo or BoundGoToAlterable or BoundStop);
}
