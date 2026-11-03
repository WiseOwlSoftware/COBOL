// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// <c>docs/CONFORMANCE.md</c>'s registers (§7 the Annex A.1 implementor-defined element register, §8 the owner-signed
/// derivations, and the diagnostic codes §2 and §5 name) as the compiler tests read them: THE EXTRACT
/// <c>tests/version-matrix/conformance-register.json</c> (kb/Work PB4453).
/// </summary>
/// <remarks>
/// <para>
/// ⛔ NO COMPILER TEST OPENS A MARKDOWN FILE (owner 2026-11-02: "There is no compelling reason a compiler change needs to
/// read an markdown note"). This class used to parse the register out of the Markdown, a C# twin of
/// <c>scripts/spec/inventory_schema.py</c>'s <c>register_section</c>; now there is ONE parser, the Python one, and it writes
/// the extract (<c>python scripts/doc_audits.py --write-register</c>), which the DOC AUDITS gate audit holds fresh. A change
/// to a register row reaches the tests that depend on it through the extract; a change to the prose around it does not.
/// </para>
/// <para>
/// The extract keeps what the tests ask of a §7 row: its KEY (an item's rule-id, plus a letter on a further determination of
/// the item, kb/Work PB3840, so unique) and the first 64 characters of its determination with leading emphasis removed, which
/// every schema <c>determination-prefix</c> is shorter than. The prose behind it is not data.
/// </para>
/// </remarks>
internal static class ConformanceRegister
{
    /// <summary>One data row of the §7 table: the key, and the determination's lead.</summary>
    /// <param name="Key">the inventory rule-id the row discharges — <c>DOC-A.1-&lt;n&gt;</c></param>
    /// <param name="Determination">the determination's first characters, leading markdown emphasis removed</param>
    internal sealed record Row(string Key, string Determination);

    /// <summary>One data row of the §8 derivation register, by cell — the twin of Python's <c>DerivationRow</c>. The
    /// argument is prose and is not in the extract.</summary>
    /// <param name="Key">the derivation anchor fragment — <c>DRV-&lt;rule-id&gt;</c></param>
    /// <param name="Arm">which of the three §1.1 grounds this determination stands on</param>
    /// <param name="Names">the arm's OBJECT — an A.2 item, the closed set, or the indistinguishable rule-id</param>
    /// <param name="Argument">the derivation itself, written for a reader (empty in the extract)</param>
    /// <param name="Signature">the owner's signature, one of the keys of the schema's <c>derivation.signatures</c></param>
    internal sealed record DerivationRow(string Key, string Arm, string Names, string Argument, string Signature);

    /// <summary>The extract file itself, so a caller can assert it exists before believing an empty parse.</summary>
    internal static string Path => TestRepo.VersionMatrix("conformance-register.json");

    private static readonly Lazy<JsonElement> Extract = new(() => JsonDocument.Parse(File.ReadAllText(Path)).RootElement);

    /// <summary>Every data row of §7, in document order.</summary>
    internal static IReadOnlyList<Row> Rows { get; } =
        [.. Extract.Value.GetProperty("annexA1").EnumerateArray()
            .Select(e => new Row(e.GetProperty("key").GetString()!, e.GetProperty("lead").GetString()!))];

    /// <summary>
    /// <c>row key → determination lead</c>. A row key is UNIQUE (kb/Work PB3840); a repeated key throws — matching
    /// <c>register_determinations</c> on the Python side.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Determinations { get; } = Build();

    /// <summary>A determination cell with its leading markdown emphasis removed — a determination is written for
    /// a human first (<c>**Not provided.** Each usage holds …</c>), so a predicate reading the cell must not be
    /// reading the bold markers. (The extract's lead is already plain; this is idempotent.)</summary>
    internal static string Plain(string cell) => cell.TrimStart('*', '_', ' ', '\t');

    private static Dictionary<string, string> Build()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var r in Rows)
        {
            if (!map.TryAdd(r.Key, r.Determination))
            {
                throw new InvalidOperationException(
                    $"{Path} repeats the row key {r.Key}: a row key names ONE determination " +
                    "(file a further determination of an item under its id plus a letter, kb/Work PB3840)");
            }
        }
        return map;
    }

    /// <summary><c>§8 row key → row</c> for the register of owner-signed DERIVATIONS (<c>kb/Work/PB386</c>).</summary>
    internal static Dictionary<string, DerivationRow> Derivations()
    {
        var map = new Dictionary<string, DerivationRow>(StringComparer.Ordinal);
        foreach (var e in Extract.Value.GetProperty("derivations").EnumerateArray())
        {
            string key = e.GetProperty("key").GetString()!;
            map.TryAdd(key, new DerivationRow(key, e.GetProperty("arm").GetString()!, e.GetProperty("names").GetString()!,
                "", e.GetProperty("signature").GetString()!));
        }
        return map;
    }

    /// <summary>The diagnostic codes the Annex A.3 register (<c>"A3"</c>, §2) or the Annex A.4 register (<c>"A4"</c>, §5) names.</summary>
    internal static HashSet<string> DeclinedCodes(string annex) =>
        Extract.Value.GetProperty("declinedCodes").GetProperty(annex).EnumerateArray()
            .Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
}
