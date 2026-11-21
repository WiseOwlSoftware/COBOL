// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

/// <summary>
/// The construct registry + the ONE gating entry point (P2.5): every edition gate — validator override or
/// binder-side — routes through <see cref="Check"/>, which evaluates availability at the targeted
/// <see cref="EditionInfo"/> and reports the verdict to an <see cref="IDiagnosticSink"/> at the severity the
/// ONE <see cref="EditionSeverityPolicy"/> dictates (one policy, several emit sites; feedback_one_mechanism_per_job).
/// As of rearch PHASE 02 the registry lives BELOW the frontend, so both Frontend and Compiler consume it.
/// </summary>
public static partial class ConstructRegistry
{
    // Entries (the construct catalogue rendering) is the generated partial half — ConstructRegistry.g.cs.
    private static Dictionary<string, ConstructDialectStatus>? _byId;

    /// <summary>Look up a registry entry by its construct catalogue row id.</summary>
    public static ConstructDialectStatus? Find(string id) =>
        (_byId ??= Entries.ToDictionary(e => e.Id, StringComparer.Ordinal)).GetValueOrDefault(id);

    /// <summary>True when the construct <paramref name="id"/> is in force at <paramref name="edition"/> — the quiet
    /// twin of <see cref="Check"/> for a site that must BRANCH on the edition fact instead of reporting it (a binder
    /// route that is inert below the edition that introduced its declaration surface, kb/Work PB2589). It reads the
    /// same row <see cref="Check"/> reports from, so the branch and the diagnostic cannot name different editions.</summary>
    public static bool IsAvailable(EditionInfo edition, string id) =>
        (Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id)))
            .IsInForceAt(edition.Year);

    /// <summary>The code <see cref="Check"/> reports construct <paramref name="id"/> under at <paramref name="edition"/> when that
    /// report is an ERROR, else null (in force, obsolete, or removed under a permissive edition, all of which report a warning
    /// or nothing). The quiet twin for a binder that asks the construct's edition gate itself (it calls <see cref="Check"/>)
    /// and also refuses the construct by its own rule: where the gate's report is an error, that error is the one report and
    /// the binder adds none of its own (kb/Work PB5538, the ENTER statement's binder). It reads the one verdict <see cref="Check"/>
    /// reports (<see cref="VerdictAt"/>), so the two cannot disagree on whether the gate's report is an error.</summary>
    public static string? GateErrorCodeAt(EditionInfo edition, string id) =>
        VerdictAt(edition, id) is { Severity: EditionSeverity.Error } verdict ? verdict.Code : null;

    /// <summary>⛔ THE ONE VERDICT of construct <paramref name="id"/> at <paramref name="edition"/>, or null when it is in force and unflagged (an obsolete construct is in force and drawn a warning):
    /// the severity <see cref="EditionSeverityPolicy"/> dictates, the code (NotYetIntroduced: the 0900 band for a row whose
    /// availability is a WINDOW, whose <see cref="ConstructDialectStatus.DiagnosticCode"/> names the removal edge, else the row's
    /// pinned code; Removed: the row's code; Obsolete: COBOL0903), and the edition flag of an obsolete or removed construct
    /// (<see cref="EditionFlag.Of(ConstructDialectStatus, ConstructAvailability)"/>; DESIGN-listing.md section 6.11). <see cref="Check"/>
    /// reports it, and a stage that reports the construct with words of its own (the reference-format, COPY and directive-word
    /// stages) takes its severity, code and flag from here, so no stage spells a flag code of its own.</summary>
    public static EditionVerdict? VerdictAt(EditionInfo edition, string id)
    {
        var c = Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id));
        var verdict = c.StatusAt(edition.Year);
        if (verdict == ConstructAvailability.Available) return null;
        string code = verdict switch
        {
            ConstructAvailability.NotYetIntroduced => c.RemovedIn is null ? c.DiagnosticCode : EditionCodes.Introduction,
            ConstructAvailability.Removed => c.DiagnosticCode,
            ConstructAvailability.Obsolete => EditionCodes.ObsoleteFlag,
            _ => throw new ArgumentOutOfRangeException(nameof(id), verdict, "an availability with no code"),
        };
        return new EditionVerdict(verdict, EditionSeverityPolicy.For(verdict, edition), code, EditionFlag.Of(c, verdict));
    }

    /// <summary>
    /// THE gating entry point: evaluate <paramref name="id"/> at <paramref name="edition"/> and report the
    /// verdict to <paramref name="sink"/> at the severity <see cref="EditionSeverityPolicy"/> dictates —
    /// NotYetIntroduced ⇒ error on both axes (0900 band unless a single-edge pinned code); Removed ⇒ error
    /// strict / warning permissive; Obsolete ⇒ 0903 warning. <paramref name="where"/> localizes the diagnostic
    /// ("FD OUT-FILE", "paragraph P1", …). Layer-neutral: the frontend and the compiler (through the
    /// <c>EditionContext</c> adapter, which IS the sink) call this identical funnel. The text/codes are
    /// byte-identical to the pre-PHASE-02 direct <c>edition.Error/Removed/Warning</c> emission.
    /// </summary>
    public static void Check(EditionInfo edition, IDiagnosticSink sink, string id, string where)
    {
        if (VerdictAt(edition, id) is not { } verdict) return;
        var c = Find(id)!;
        // A relaxation row cites, at COBOL-85, the 1985 rule the program breaks (ConstructDialectStatus.Rule1985; kb/Work PB6010).
        string citation = verdict.Availability == ConstructAvailability.NotYetIntroduced
            ? edition.Cite(c.Rule1985 ?? c.Citation, c.Citation) : c.Citation;
        string message = verdict.Availability switch
        {
            ConstructAvailability.NotYetIntroduced =>
                $"{c.Display} requires COBOL-{c.IntroducedIn} (targeting COBOL-{edition.Year}) — {where} ({citation})",
            ConstructAvailability.Removed =>
                $"{c.Display} was removed in COBOL-{c.RemovedIn} (targeting COBOL-{edition.Year}) — {where} ({citation})",
            _ => $"{c.Display} is obsolete as of COBOL-{c.ObsoleteIn} — {where} ({citation}{edition.Cite("; §1985:I.1.5.2.3", "")})",
        };
        sink.Report(new EditionDiagnostic(verdict.Code, verdict.Severity, c.Id, message, where, citation, verdict.Flag));
    }
}
