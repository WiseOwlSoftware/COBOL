// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE V59 IMAGE-PREDICATE INVENTORY. Two predicates decide "does this group have a whole-group image?":
/// <list type="bullet">
///   <item><c>DataItem.IsImageCapable</c> — V59's. A BINARY/PACKED leaf HAS a pinned byte image, so a group
///         containing one qualifies. Only float / COMP-5 / INDEX leaves are genuinely imageless. This is the
///         predicate <c>RecordStructEmitter</c> uses to decide whether to EMIT <c>AsImage()</c>/<c>FromImage()</c>
///         at all.</item>
///   <item><c>DataItem.IsCharacterImage</c> — the PRE-V59 one. A COMP/binary leaf qualifies only via
///         <c>StoreAsImage</c> promotion, so a plain COMP group does NOT.</item>
/// </list>
/// <para>
/// Why this test exists: V59 migrated SOME emit guards to <c>IsImageCapable</c> and left others on
/// <c>IsCharacterImage</c> — <c>MoveEmitter</c> ended up using BOTH. A guard left on the stricter predicate
/// loud-stages a construct whose codec <c>RecordStructEmitter</c> actually generated, which is how
/// <c>CALL "SUB" USING G</c> came to throw "no whole-group character image" for a group whose
/// <c>FUNCTION BYTE-LENGTH</c> answered 5. That is rejecting conforming source (§14.2.3 GR8 — a BY REFERENCE
/// formal "occupies the same storage area as the argument", which COBOL.NET realizes through that very image
/// round-trip).
/// </para>
/// <para>
/// ⛔ THE REMAINING SITES ARE NOT AUTOMATICALLY BUGS. V59's own governing lesson is BYTES ARE NOT TEXT: a COMP
/// leaf's image is radix-2 bytes, not its digits, so an operation defined over CHARACTER POSITIONS (STRING,
/// UNSTRING, INSPECT) may be CORRECT to refuse it — feeding it those bytes as text would be a silent wrong
/// answer, which is worse than a loud stage. Each remaining site needs its own spec derivation. This test does
/// not prejudge them; it makes the inventory EXPLICIT so none can be forgotten, and forces a deliberate decision
/// when one is added or removed.
/// </para>
/// </summary>
public sealed class V59ImagePredicateDriftTests
{
    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "CobolSharp.sln"))) d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray()));

    /// <summary>Count occurrences in CODE only. These predicates are discussed at length in comments (that is the
    /// point of the comments), so a raw text count is a false positive — the first cut of this test failed on its
    /// own explanatory comment. Strips <c>///</c> doc comments, whole-line <c>//</c> comments, and trailing
    /// <c>//</c> tails.</summary>
    private static int CodeOccurrences(string src, string identifier)
    {
        var code = new List<string>();
        foreach (string raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string t = line.TrimStart();
            if (t.StartsWith("///") || t.StartsWith("//")) continue;      // doc / whole-line comment
            int i = line.IndexOf("//", StringComparison.Ordinal);
            code.Add(i >= 0 ? line[..i] : line);                          // drop a trailing comment tail
        }
        return Regex.Matches(string.Join("\n", code), $@"\b{Regex.Escape(identifier)}\b").Count;
    }

    /// <summary>CALL's two guards are the READ and WRITE halves of ONE image round-trip, so they must test the
    /// SAME predicate. If they diverged, a group could cross in through <c>FromImage</c> and back out through a
    /// raw write, or be admitted by one half and refused by the other.</summary>
    [Fact]
    public void CallEmitter_ReadAndWriteHalves_UseTheSamePredicate()
    {
        string src = Read("src", "Cobol.Net.Compiler", "CodeGen", "Verbs", "CallEmitter.cs");
        int capable = CodeOccurrences(src, "IsImageCapable");
        int character = CodeOccurrences(src, "IsCharacterImage");
        Assert.True(capable >= 2,
            $"Expected both CALL group guards on IsImageCapable; found {capable}.");
        Assert.True(character == 0,
            "CallEmitter must not use the pre-V59 IsCharacterImage: a group whose only non-character leaf is "
            + "BINARY/PACKED has a whole-group image (V59), and RecordStructEmitter emits its codec. Guarding on "
            + "the stricter predicate rejects conforming source — see §14.2.3 GR8. Found "
            + $"{character} occurrence(s).");
    }

    /// <summary>The EXPLICIT inventory of emit guards still on the pre-V59 predicate. Each is either a correct
    /// text-context guard (STRING/UNSTRING/INSPECT are defined over character positions) or an unmigrated V59
    /// residue — that judgement is per-site spec work, tracked as queue item DA5. This test pins the SET so a
    /// site cannot be added or silently migrated without a decision being recorded here.</summary>
    [Fact]
    public void RemainingPreV59Guards_AreTheKnownInventory()
    {
        var expected = new Dictionary<string, int>
        {
            ["CodeGen/Emit/NumericRenderer.cs"] = 1,
            ["CodeGen/Verbs/AcceptDisplayEmitter.cs"] = 2,
            ["CodeGen/Verbs/InspectEmitter.cs"] = 1,
            // ⛔ MoveEmitter.cs:144 is NOT a Tier-C guard and must NOT be "migrated". It selects a STRATEGY: when
            // the receiver is not a character image it first tries the memberwise leaf-copy fast path (used when
            // the source and receiver leaf LAYOUTS are positionally identical), and only the fall-through reaches
            // the real capability guard at line 168 — which is ALREADY on IsImageCapable. Verified: MOVE into a
            // COMP-containing group works in all three shapes (aligned memberwise, non-aligned image
            // redistribution, and from an alphanumeric source). Counted here so the inventory is complete, with
            // this note so nobody "fixes" a working fast path.
            ["CodeGen/Verbs/MoveEmitter.cs"] = 1,
            ["CodeGen/Verbs/StringEmitter.cs"] = 2,
        };

        var actual = new Dictionary<string, int>();
        string root = Path.Combine(RepoRoot(), "src", "Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            // The predicate's own definition and the StorageForm/pass plumbing that mirrors it are not emit guards.
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            if (rel.StartsWith("Binding/Model/") || rel.StartsWith("Binding/Passes/")) continue;
            int n = CodeOccurrences(File.ReadAllText(f), "IsCharacterImage");
            if (n > 0) actual[rel] = n;
        }

        Assert.Equal(
            expected.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray(),
            actual.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}").ToArray());
    }
}
