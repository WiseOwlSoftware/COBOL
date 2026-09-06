// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// THE roster of ISO §7.3 compiler directives, keyed by the compiler-directive word that heads the line —
/// DERIVED from <see cref="ConstructRegistry.Entries"/>, never hand-written (kb/Work PB725).
///
/// <para>ISO/IEC 1989:2023 §7.3.2 gives one general format for every directive — <c>&gt;&gt;compiler-instruction</c>
/// — and §7.3.3 SR6 says compiler-instruction opens with a compiler-directive word (§8.12). So "which word may
/// head a <c>&gt;&gt;</c> line" and "from which edition" is ONE fact per directive, and it belongs in ONE table.
/// Before PB725 it lived in four places at once: a flat <c>KnownIgnoredDirectives</c> HashSet with no edition
/// column (11 words silently accepted at <c>--std 85</c>, where COBOL has no compiler directives at all), the
/// conditional-compilation switch's own arms (7 more), two hand-rolled <c>if (dialectLevel &lt; 2002)</c> tests
/// with bespoke diagnostic codes, and the five rows that did it correctly through
/// <see cref="ConstructRegistry.Check"/>. That is <c>feedback_one_mechanism_per_job</c> failing four ways on one
/// rule, and it is why half the §E.2 item 5 family was gated and half was not.</para>
///
/// <para>Now a directive is ONE <c>tests/version-matrix/constructs.json</c> row carrying
/// <c>directiveWords</c> plus a regen: the word becomes recognized, its introducing edition becomes enforced by
/// the one COBOLNET0900 producer, and the version matrix compiles its <c>source</c> at all four editions
/// automatically. <c>CompilerDirectiveCatalogDriftTests</c> keeps the roster honest against §7.3 itself.</para>
/// </summary>
public static class CompilerDirectiveCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, ConstructDialectStatus>> Map = new(() =>
    {
        var map = new Dictionary<string, ConstructDialectStatus>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in ConstructRegistry.Entries)
            foreach (string w in e.DirectiveWords)
                if (!map.TryAdd(w, e))
                    throw new InvalidOperationException(
                        $"constructs.json: directive word '{w}' is claimed by both '{map[w].Id}' and '{e.Id}' — "
                        + "one word, one row (ISO §7.3.3 SR6: a compiler-instruction opens with ONE "
                        + "compiler-directive word)");
        return map;
    });

    /// <summary>Every compiler-directive word the compiler recognizes, upper-case, ordered.</summary>
    public static IEnumerable<string> Words => Map.Value.Keys.Order(StringComparer.Ordinal);

    /// <summary>The registry row gating <paramref name="word"/>, or <see langword="null"/> when the word heads no
    /// recognized directive — an unrecognized <c>&gt;&gt;</c> word is LEFT IN the text so it surfaces downstream
    /// as a syntax error (catching typos like <c>&gt;&gt;IFF</c>) rather than being silently swallowed.</summary>
    public static ConstructDialectStatus? Find(string word) => Map.Value.GetValueOrDefault(word);

    /// <summary>True when <paramref name="word"/> heads a recognized ISO §7.3 compiler directive.</summary>
    public static bool IsDirective(string word) => Map.Value.ContainsKey(word);

    /// <summary>
    /// The ONE edition gate for a compiler-directive line (§7.3.3 SR6 + §7.3.2): reject <paramref name="word"/>
    /// below its introducing edition and after its removing one, through
    /// <see cref="ConstructRegistry.Check"/> — COBOLNET0900 for the introduction edge (error on both severity
    /// axes: no conforming source of the older edition can contain the line), COBOLNET0902 for a removal,
    /// COBOLNET0903 for an obsolete use. A no-op for an unrecognized word and for every edition at which the
    /// directive is available.
    /// </summary>
    public static void Check(string word, EditionInfo edition, IDiagnosticSink sink)
    {
        if (Map.Value.GetValueOrDefault(word) is { } row)
            ConstructRegistry.Check(edition, sink, row.Id, row.Display);
    }

    /// <summary>
    /// The same gate addressed by ROW ID rather than by word — for the one stage that must gate a directive it
    /// consumes before the shared recognition point ever sees it (<c>&gt;&gt;SOURCE FORMAT</c>, whose line the
    /// reference-format normalizer removes so it can switch the following segment's format). Keyed on
    /// <c>Constructs.X</c> so an unregistered id is a COMPILE error, never a string that quietly stops matching.
    /// </summary>
    public static void CheckRow(string constructId, EditionInfo edition, IDiagnosticSink sink)
    {
        var row = ConstructRegistry.Find(constructId)
            ?? throw new ArgumentException($"unregistered construct id '{constructId}'", nameof(constructId));
        ConstructRegistry.Check(edition, sink, row.Id, row.Display);
    }
}
