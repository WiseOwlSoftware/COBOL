// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO TRACKED TEXT FILE UNDER <c>src/</c>, <c>tests/</c> OR <c>conformance-suite/</c> CONTAINS A NUL BYTE, because a
/// file with one is BINARY to every sweep this project runs, and a file a sweep cannot read has been exempt from every sibling sweep ever run
/// (<c>kb/Work/PB927</c>). The instance: <c>tests/Cobol.Net.Tests.Unit/CobolPtrTests.cs</c> held
/// <c>CobolPtr.Allocate(3, '\0', …)</c> written with a LITERAL NUL character, so <c>grep -rn UpByReal tests/</c>
/// answered <c>Binary file … matches</c> instead of the lines, and the arm that file tests was never re-read against
/// the emitter. The cause is mechanical and recurs: the harness that writes these files decodes a backslash-u escape in
/// a tool input into the RAW character, so an agent writing <c>'\u0000'</c> writes a NUL
/// (<c>feedback_tool_input_unicode_escapes_decoded</c>). A memory that says "byte-check after scripted writes" is not a
/// gate; this test is.
/// <para>
/// ⭐ The population is the COMMITTED tree (<see cref="TrackedTree.Files"/>), narrowed to the text kinds the repo's
/// sweeps read (<see cref="TextKinds"/>). Binary assets (<c>.bin</c>, <c>.zip</c>, <c>.dat</c>) legitimately hold NULs and
/// are not in the list; a NEW text kind is a deliberate addition here, not a silent exemption. Binary data that carries a
/// text kind's name (a file a NIST program wrote is named for its ASSIGN target, <c>&lt;name&gt;.txt</c>) is an
/// <see cref="Exempt"/> entry with its reason, held to still needing it. The BOM half of the same hygiene lives in
/// <c>SourceEncodingDriftTests</c>.
/// </para>
/// <para>
/// ⭐ THE SCAN IS A FUNCTION so its FAILURE branch can be fired: <see cref="TheSweepFiresOnAPlantedNul"/> aims
/// <see cref="Sweep"/> — the identical code path the tree sweep uses — at a file holding the very shape that caused
/// PB927, in a GUID-named scratch directory (<c>kb/Work/PB376</c>), and requires it back
/// (<c>feedback_green_gates_arent_evidence</c>). <see cref="TheSweepActuallyReadsTheTrackedTextTree"/> requires the
/// sweep to have read thousands of files, so a broken enumeration cannot pass for a clean tree
/// (<c>feedback_verdict_evidence_invariant</c>).
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NulByteDriftTests
{
    /// <summary>The files swept (kb/Work PB3531: declared, so the drift-rule index names this class for each of them).
    /// The trees are the compiler, its tests, and the conformance suite, which holds the corpus's <c>.cob</c>/<c>.out</c>
    /// text since the move (kb/Work PB3177, slice S2); the class summary states the same trees, and
    /// <see cref="TheSweepFiresOnAPlantedNul"/> plants an offender in the suite and one outside every tree. The kinds
    /// are the text kinds the repo's greps and drift sweeps read, compared case-insensitively: COBOL sources and
    /// copybooks, the C# the compiler is made of, the grammar, the registers and project files, and the golden
    /// <c>.out</c>/<c>.err</c> fixtures (expected output is text: a program whose output holds a control character is
    /// asserted through a relation, not by pinning the raw byte).</summary>
    private static readonly TreeScope TextKinds = new(Under: ["src/", "tests/", "conformance-suite/"],
        Kinds: [".cs", ".cob", ".cbl", ".cpy", ".g4", ".json", ".md", ".txt", ".csproj", ".props", ".ps1", ".tsv", ".out", ".err"]);

    /// <summary>The one sweep of the committed tree the three tree tests judge, read once per test assembly (a read of
    /// the text tree costs 15-60 s under a gate's load, and the gate runs this class in leg 1 for a change to any file
    /// it sweeps, tier 0d).</summary>
    private static readonly Lazy<SweepResult> s_tree = new(() => Sweep(TestRepo.Root, TrackedTree.Files(TextKinds)));

    /// <summary>The tracked text-kind files that legitimately hold a NUL, each with why: a key is a path, or a
    /// directory (a key ending in <c>/</c>) that covers every file beneath it. A file is here only when no agent's
    /// tool write produced its bytes: UPSTREAM DATA this repository does not own, or RECORD DATA a program wrote and
    /// the harness recorded byte for byte. <see cref="EveryExemptionStillHoldsItsNul"/> fails when an entry no longer
    /// covers a file holding a NUL, so the list cannot rot into a blanket exemption. (A file ENCODED as UTF-16 or
    /// UTF-32 needs no entry: its byte-order mark says so, and the sweep sets it aside structurally.)</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        ["tests/nist/valid/NC107A.txt"] =
            "the NIST CCVS85 source's own information comment (line 35) spells the value LOW-VALUE out as literal NUL "
            + "bytes; it is the upstream distribution's text, a comment, and the test is what the suite expects",
        ["tests/nist/valid-files/"] =
            "the NIST differential's written-file goldens (kb/Work PB804): the bytes a CCVS program WROTE to its "
            + "sequential, relative, indexed and sort files, recorded by the harness (NistOutputs.UpdateVariable) and "
            + "compared byte for byte (NistOutputs.Canonical). A relative or indexed store's header and slots, a "
            + "variable-length record's length prefix and a COMP field are binary by the file's own definition; each "
            + "golden is named for the file the program wrote (its ASSIGN target, <name>.txt), which the comparison "
            + "requires; and .gitattributes declares the directory byte-exact (-text) for the same reason",
    };

    /// <summary>The <see cref="Exempt"/> key that covers <paramref name="path"/>: the path itself, or the directory
    /// key it lies beneath; null when none does.</summary>
    private static string? ExemptionOf(string path) =>
        Exempt.ContainsKey(path) ? path
        : Exempt.Keys.FirstOrDefault(k => k.EndsWith('/') && path.StartsWith(k, StringComparison.Ordinal));

    /// <summary>What a sweep saw: the offenders AND the population they were drawn from.</summary>
    internal sealed record SweepResult(
        IReadOnlyList<string> Offenders, int FilesScanned, int FilesMissing, int FilesWideEncoded, IReadOnlyList<string> Exempted);

    /// <summary>No tracked text file under <c>src/</c>, <c>tests/</c> or <c>conformance-suite/</c> contains a NUL
    /// byte.</summary>
    [Fact]
    public void NoTrackedTextFileContainsANulByte()
    {
        SweepResult r = s_tree.Value;

        Assert.True(r.Offenders.Count == 0,
            $"{r.Offenders.Count} tracked text file(s) contain a NUL byte ({r.FilesScanned} files scanned) — grep reports "
            + "each one as BINARY and silently skips it, so every sibling sweep over the repo misses it:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, r.Offenders.Select(o => "    " + o))
            + Environment.NewLine
            + "A NUL inside a C# literal is the backslash-zero (or backslash-u0000) escape written as a RAW character: the "
            + "tool that wrote the file decoded the escape. Replace the byte with the escape sequence written as ordinary "
            + "characters, verified with `grep -c -a -P '\\x00' <file>` (without -a, grep reads a file holding a NUL as "
            + "binary and counts 0). A golden .out whose program displays a NUL is re-expressed so every byte it pins "
            + "is printable (a national character whose UTF-16 bytes are both printable, NX\"5831\").");
    }

    /// <summary>The sweep read the tree it claims to have swept — a missing git or an empty index would otherwise make
    /// <see cref="NoTrackedTextFileContainsANulByte"/> pass by looking at nothing.</summary>
    [Fact]
    public void TheSweepActuallyReadsTheTrackedTextTree()
    {
        SweepResult r = s_tree.Value;

        Assert.True(r.FilesScanned >= 5000,
            $"the NUL sweep read only {r.FilesScanned} text file(s) of the tracked tree under {TestRepo.Root} "
            + $"({r.FilesMissing} tracked path(s) missing from the working tree) — the enumeration is broken, so the "
            + "sweep is scanning nothing.");
        Assert.True(r.FilesMissing * 100 <= r.FilesScanned,
            $"{r.FilesMissing} tracked path(s) are absent from the working tree against {r.FilesScanned} read — "
            + "the tree is not the one git describes, and the sweep is not looking at it.");
    }

    /// <summary>An exemption is for a file that still needs it: an entry that no longer covers a file holding a NUL
    /// (the file, or every file of the directory, clean or no longer tracked) must leave <see cref="Exempt"/>, or the
    /// list becomes a blanket exemption nobody re-derives (<c>feedback_a_dead_lookup_is_also_unverified</c>).</summary>
    [Fact]
    public void EveryExemptionStillHoldsItsNul()
    {
        SweepResult r = s_tree.Value;

        Assert.Equal(Exempt.Keys.Order(StringComparer.Ordinal), r.Exempted.Order(StringComparer.Ordinal));
    }

    /// <summary>The sweep fires: a file with the PB927 shape — a literal NUL inside a C# char literal — is reported, a
    /// clean one beside it is not, a UTF-16 file (a NUL in every other byte by construction) is set aside by its
    /// byte-order mark, an exempt path is reported as exempt, a file beneath an exempt directory is reported as that
    /// directory's exemption (once, however many files it covers) while a sibling directory sharing its name's prefix
    /// is still swept, a binary asset kind is out of scope, and a suite golden is swept while the same bytes outside
    /// every tree of <see cref="TextKinds"/> are not.</summary>
    [Fact]
    public void TheSweepFiresOnAPlantedNul()
    {
        string scratch = Path.Combine(Path.GetTempPath(), "cobolnet-nul-byte-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(scratch, "src"));
        Directory.CreateDirectory(Path.Combine(scratch, "tests", "nist", "valid"));
        try
        {
            // The NUL is built, never written as a literal, so THIS FILE is not an offender either.
            string nul = new((char)0, 1);
            File.WriteAllBytes(Path.Combine(scratch, "src", "Planted.cs"),
                Encoding.UTF8.GetBytes("var cell = CobolPtr.Allocate(3, '" + nul + "', out _);\n"));
            File.WriteAllText(Path.Combine(scratch, "src", "Clean.cs"), "var cell = CobolPtr.Allocate(3, '\\0', out _);\n");
            File.WriteAllBytes(Path.Combine(scratch, "src", "Wide.cpy"), [0xFF, 0xFE, 0x41, 0x00]);
            File.WriteAllBytes(Path.Combine(scratch, "src", "Asset.bin"), [0, 1, 2]);
            File.WriteAllBytes(Path.Combine(scratch, "tests", "nist", "valid", "NC107A.txt"), [0x2A, 0x00]);
            // Record data a program wrote, beneath the exempt directory: two stores, one exemption. A directory whose
            // name merely STARTS with the exempt one's is not beneath it.
            Directory.CreateDirectory(Path.Combine(scratch, "tests", "nist", "valid-files", "IX101A"));
            Directory.CreateDirectory(Path.Combine(scratch, "tests", "nist", "valid-files-x"));
            File.WriteAllBytes(Path.Combine(scratch, "tests", "nist", "valid-files", "IX101A", "tf024.txt"), [0x43, 0x00]);
            File.WriteAllBytes(Path.Combine(scratch, "tests", "nist", "valid-files", "IX101A", "tf025.txt"), [0x43, 0x00]);
            File.WriteAllBytes(Path.Combine(scratch, "tests", "nist", "valid-files-x", "tf024.txt"), [0x43, 0x00]);
            // A suite golden pinning a raw NUL (a DISPLAY of LOW-VALUE) is an offender: the suite is a swept tree. The
            // same bytes outside every swept tree are out of scope.
            Directory.CreateDirectory(Path.Combine(scratch, "conformance-suite", "cases", "_unassigned"));
            Directory.CreateDirectory(Path.Combine(scratch, "docs"));
            File.WriteAllBytes(Path.Combine(scratch, "conformance-suite", "cases", "_unassigned", "lowvalue.out"), [0x41, 0x00]);
            File.WriteAllBytes(Path.Combine(scratch, "docs", "Outside.md"), [0x41, 0x00]);

            // The planted tree goes through the class's one scope, as the tracked tree does (TrackedTree.Files).
            string[] planted =
                ["src/Planted.cs", "src/Clean.cs", "src/Wide.cpy", "src/Asset.bin", "src/Gone.cs", "tests/nist/valid/NC107A.txt",
                 "tests/nist/valid-files/IX101A/tf024.txt", "tests/nist/valid-files/IX101A/tf025.txt",
                 "tests/nist/valid-files-x/tf024.txt", "conformance-suite/cases/_unassigned/lowvalue.out", "docs/Outside.md"];
            SweepResult r = Sweep(scratch, planted.Where(TextKinds.Holds));

            Assert.Equal(
                new[] { "src/Planted.cs", "tests/nist/valid-files-x/tf024.txt", "conformance-suite/cases/_unassigned/lowvalue.out" },
                r.Offenders);
            Assert.Equal(new[] { "tests/nist/valid/NC107A.txt", "tests/nist/valid-files/" }, r.Exempted);
            // Planted, Clean, the exempt file, the two stores, the prefix sibling and the suite golden; Wide and Asset
            // are set aside, and docs/ is no swept tree.
            Assert.Equal(7, r.FilesScanned);
            Assert.Equal(1, r.FilesWideEncoded);
            Assert.Equal(1, r.FilesMissing);
        }
        finally
        {
            try { Directory.Delete(scratch, recursive: true); }
            catch (IOException) { /* a scratch directory left behind is not a test failure */ }
        }
    }

    /// <summary>Scan <paramref name="paths"/> (repo-root-relative, already narrowed to <see cref="TextKinds"/>) beneath
    /// <paramref name="root"/> for a NUL byte.</summary>
    internal static SweepResult Sweep(string root, IEnumerable<string> paths)
    {
        var offenders = new List<string>();
        var exempted = new List<string>();
        int scanned = 0, missing = 0, wide = 0;
        foreach (string path in paths)
        {
            string full = Path.Combine(root, path);
            if (!File.Exists(full)) { missing++; continue; }

            (bool holdsNul, bool wideEncoded) = Probe(full);
            if (wideEncoded) { wide++; continue; }

            scanned++;
            if (!holdsNul) continue;
            if (ExemptionOf(path) is { } exemption)
            {
                if (!exempted.Contains(exemption)) exempted.Add(exemption);
            }
            else offenders.Add(path);
        }

        return new SweepResult(offenders, scanned, missing, wide, exempted);
    }

    /// <summary>Whether <paramref name="file"/> holds a NUL byte, and whether it is a UTF-16 or UTF-32 file by its
    /// byte-order mark (FF FE, FE FF, 00 00 FE FF) — encodings in which a NUL is part of every character, so they are
    /// not the text a byte-oriented sweep can read at all and are a legitimate fixture kind (the source-encoding
    /// tests). A file is wide-encoded on the mark alone.</summary>
    private static (bool HoldsNul, bool WideEncoded) Probe(string file)
    {
        using var stream = File.OpenRead(file);
        byte[] buffer = new byte[64 * 1024];
        int n = stream.Read(buffer, 0, buffer.Length);
        if (IsWideBom(buffer.AsSpan(0, n))) return (false, true);

        bool nul = false;
        while (n > 0 && !nul)
        {
            nul = buffer.AsSpan(0, n).IndexOf((byte)0) >= 0;
            n = nul ? 0 : stream.Read(buffer, 0, buffer.Length);
        }

        return (nul, false);
    }

    private static bool IsWideBom(ReadOnlySpan<byte> head) =>
        head.StartsWith(new byte[] { 0xFF, 0xFE }) || head.StartsWith(new byte[] { 0xFE, 0xFF })
        || head.StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF });
}
