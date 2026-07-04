// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Validation;

/// <summary>A construct's availability at a targeted edition (the registry's verdict shape).</summary>
public enum ConstructAvailability
{
    /// <summary>Valid at the edition, no flag.</summary>
    Available,
    /// <summary>Newer than the edition — introduction gating (COBOLNET0900 band; error on BOTH axes).</summary>
    NotYetIntroduced,
    /// <summary>Removed by the edition — error strict / warning permissive (<see cref="EditionContext.Removed"/>).</summary>
    Removed,
    /// <summary>Obsolete at the edition (ISO §4.2.13 over Annex F.2) — warning always (0903).</summary>
    Obsolete,
}

/// <summary>
/// One construct's per-edition dialect status (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.5):
/// the in-code rendering of a <c>tests/version-matrix/constructs.json</c> row — THE canonical catalogue; the
/// drift test (<c>ConstructRegistryDriftTests</c>) asserts registry↔json equality BOTH directions, so a gate
/// cannot land without its matrix row nor a row without its registry entry.
/// </summary>
/// <param name="Id">The constructs.json row id.</param>
/// <param name="Display">Human name used in diagnostics.</param>
/// <param name="IntroducedIn">First edition that HAS the construct (85/2002/2014/2023).</param>
/// <param name="RemovedIn">First edition that REMOVED it (null = never).</param>
/// <param name="ObsoleteIn">First edition marking it obsolete/archaic (null = never; drives 0903).</param>
/// <param name="DiagnosticCode">The code its gate emits (the TARGET code where surfacing is still a raw
/// parse error today — the W1.5 upgrade wires it).</param>
/// <param name="Citation">ISO § / VCR row / roadmap-D citation.</param>
public sealed record ConstructDialectStatus(
    string Id, string Display, int IntroducedIn, int? RemovedIn, int? ObsoleteIn,
    string DiagnosticCode, string Citation)
{
    /// <summary>The availability verdict at <paramref name="edition"/>.</summary>
    public ConstructAvailability StatusAt(int edition)
    {
        if (edition < IntroducedIn) return ConstructAvailability.NotYetIntroduced;
        if (RemovedIn is { } r && edition >= r) return ConstructAvailability.Removed;
        if (ObsoleteIn is { } o && edition >= o) return ConstructAvailability.Obsolete;
        return ConstructAvailability.Available;
    }
}

/// <summary>
/// The construct registry + the ONE gating entry point (P2.5): every edition gate — validator override or
/// binder-side — routes through <see cref="Check"/>, which maps availability onto the
/// <see cref="EditionContext"/> channels (one policy, several emit sites; feedback_singular_pattern).
/// </summary>
public static class ConstructRegistry
{
    /// <summary>The in-code rendering of constructs.json (drift-tested against it both directions). Pending
    /// rows (not yet implemented) are REGISTERED — their edition metadata is frozen here even before their
    /// owning roadmap phase lands.</summary>
    public static readonly IReadOnlyList<ConstructDialectStatus> Entries =
    [
        new("nucleus-move-display", "nucleus MOVE/DISPLAY", 85, null, null, EditionCodes.Introduction, "edition-invariant baseline"),
        new("read-previous-2002", "READ PREVIOUS", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.30 Format 1; VCR rows 29/108 gate after-OPEN behavior"),
        new("start-first-last-2002", "START FIRST/LAST", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.41"),
        new("delete-file-2023", "DELETE FILE", 2023, null, null, EditionCodes.Introduction, "ISO 2023 §14.9.10 Format 2; Annex E.3.3 item 15"),
        new("allocate-2002", "ALLOCATE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.3"),
        new("free-2002", "FREE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.15"),
        new("invoke-2002", "INVOKE", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.23 (OO)"),
        new("goback-returning-2002", "GOBACK RETURNING", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.18"),
        new("stop-run-status-2002", "STOP RUN WITH status", 2002, null, null, EditionCodes.Introduction, "ISO §14.9.42"),
        new("based-clause-2002", "BASED clause", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.5"),
        new("procedure-returning-2002", "PROCEDURE DIVISION RETURNING", 2002, null, null, EditionCodes.Introduction, "ISO §14.2"),
        new("currency-picture-symbol-2002", "CURRENCY SIGN WITH PICTURE SYMBOL", 2002, null, null, EditionCodes.Introduction, "ISO §12.3.7"),
        new("pic-wide-19-digits-2002", "fixed-point item wider than 18 digits", 2002, null, null, "COBOLNET0802", "ISO §8.3.1.2 / §13.18.40 (the LIVE digit-capacity gate)"),
        new("options-arithmetic-native-2014", "OPTIONS paragraph / ARITHMETIC IS NATIVE", 2014, null, null, EditionCodes.Introduction, "ISO §11.9"),
        new("rounded-mode-is-2014", "ROUNDED MODE IS", 2014, null, null, EditionCodes.Introduction, "ISO §14.7.4"),
        new("arithmetic-standard-decimal-2014", "ARITHMETIC IS STANDARD-DECIMAL", 2014, null, null, EditionCodes.Introduction, "ISO §11.9.5 / §8.8.1.5"),
        new("type-clause-2002", "TYPE clause (TYPEDEF family)", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.58; PROVISIONAL 2002 edge (ISO-validation DEVLOG 582; decision-1 policy)"),
        new("usage-float-short-2002", "USAGE FLOAT-SHORT", 2002, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2002); PENDING (Phase 6)"),
        new("usage-float-binary32-2014", "USAGE FLOAT-BINARY-32", 2014, null, null, EditionCodes.Introduction, "ISO §13.18.59; D16 split (provisional 2014); PENDING (Phase 6)"),
        new("constant-entry-2002", "constant entry (01 … CONSTANT AS)", 2002, null, null, EditionCodes.Introduction, "ISO §13.10 + §13.18.15; D5; PENDING (Phase 6)"),
        new("concat-operator-2002", "concatenation expression (&)", 2002, null, null, EditionCodes.Introduction, "ISO §8.8.3; D6; PENDING (Phase 4g)"),
        // ── Removal gates (P2.6) + reserved-word interval rows (P2.7): RemovedIn drives Removed()/0901 ──
        new("label-records-removed-2002", "the LABEL RECORDS clause", 85, 2002, null, EditionCodes.RemovedConstruct, "obsolete '85 FD element DELETED by ISO 2002; the 2023 FD clause set (§13.18) has no LABEL clause; VCR Table 7"),
        new("user-word-commit-2023", "the word COMMIT as a user-defined word", 85, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable until 2023 reserved it (Annex E.2 item 25 = VCR row 32)"),
        new("user-word-raising-2002", "the word RAISING as a user-defined word", 85, 2002, null, EditionCodes.ReservedWord, "§8.9 interval encoding: user-definable at 85, reserved since 2002 (the EC family — DEVLOG 585 correction)"),
        new("receive-as-user-word", "the word RECEIVE as a user-defined word", 2002, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding of the RE-reservation: 85-reserved (communication) → user-definable 2002/2014 → re-reserved 2023 (Annex E.2 item 25)"),
        new("end-receive-as-user-word", "the word END-RECEIVE as a user-defined word", 2002, 2023, null, EditionCodes.ReservedWord, "§8.9 interval encoding: the THIRD re-reserved communication word — discovered mechanically (DEVLOG 585); same interval as RECEIVE"),
    ];

    private static Dictionary<string, ConstructDialectStatus>? _byId;

    /// <summary>Look up a registry entry by its constructs.json row id.</summary>
    public static ConstructDialectStatus? Find(string id) =>
        (_byId ??= Entries.ToDictionary(e => e.Id, StringComparer.Ordinal)).GetValueOrDefault(id);

    /// <summary>
    /// THE gating entry point: evaluate <paramref name="id"/> at the context's edition and route the verdict
    /// onto the channels — NotYetIntroduced ⇒ error (both axes, 0900 band); Removed ⇒
    /// <see cref="EditionContext.Removed"/> (strict error / permissive warning); Obsolete ⇒ 0903 warning.
    /// <paramref name="where"/> localizes the diagnostic ("FD OUT-FILE", "paragraph P1", …).
    /// </summary>
    public static void Check(EditionContext edition, string id, string where)
    {
        var c = Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id));
        switch (c.StatusAt(edition.DialectLevel))
        {
            case ConstructAvailability.NotYetIntroduced:
                edition.Error(c.DiagnosticCode,
                    $"{c.Display} requires COBOL-{c.IntroducedIn} (targeting COBOL-{edition.DialectLevel}) — {where} ({c.Citation})");
                break;
            case ConstructAvailability.Removed:
                edition.Removed(c.DiagnosticCode,
                    $"{c.Display} was removed in COBOL-{c.RemovedIn} (targeting COBOL-{edition.DialectLevel}) — {where} ({c.Citation})");
                break;
            case ConstructAvailability.Obsolete:
                edition.Warning(EditionCodes.ObsoleteFlag,
                    $"{c.Display} is obsolete as of COBOL-{c.ObsoleteIn} — {where} ({c.Citation})");
                break;
        }
    }
}
