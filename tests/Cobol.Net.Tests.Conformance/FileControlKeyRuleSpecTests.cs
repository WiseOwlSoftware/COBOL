// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The file control entry's key syntax rules, screened AT THE ENTRY (ISO/IEC 1989:2023 §12.4.5.1 Format 1 ·
/// §12.4.5.2 SR10 · §12.4.5.6.3 · §12.4.5.12.3 · §12.4.5.13.3; kb/Work PB699).
/// <para>⛔ THE DISCRIMINATOR THESE TESTS EXIST FOR is the one the negative corpus cannot state on its own: the
/// SAME entry, once with a keyed verb and once with nothing but OPEN and CLOSE, has to produce the SAME
/// diagnostics. The rules used to be screened by <c>KeyedIoBinder.KeyedValidateFile</c> on the first keyed verb
/// naming the file, so the verb-bearing half was green and the entry-only half compiled clean — a green test
/// that held the gap open. Every case below is therefore run BOTH ways and the two results compared.</para>
/// <para>They also pin the two properties the move changed: the diagnostic is positioned at the CLAUSE (not at
/// whatever statement happened to touch the file first), and a file is reported ONCE however many statements
/// name it — the one-report-per-file property that used to be a <c>HashSet</c> memo and is now a consequence of
/// running the screen once per entry.</para>
/// </summary>
public sealed class FileControlKeyRuleSpecTests
{
    private static readonly int[] AllEditions = [85, 2002, 2014, 2023];

    private static IReadOnlyList<string> Diagnostics(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Fckr_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CobolNet.CompilerDriver.Compile(
                new CobolNet.CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"), DialectLevel: edition));
            return [.. r.Errors];
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>The procedure division that names the file WITHOUT any keyed verb — the shape that used to
    /// escape every one of these rules.</summary>
    private const string EntryOnlyBody = """
                PROCEDURE DIVISION.
                MAIN.
                    OPEN INPUT F
                    CLOSE F
                    STOP RUN.
                """;

    /// <summary>The same, plus the keyed verb that used to be the only thing that triggered the screen.</summary>
    private const string WithKeyedVerbBody = """
                PROCEDURE DIVISION.
                MAIN.
                    OPEN INPUT F
                    READ F NEXT AT END CONTINUE END-READ
                    CLOSE F
                    STOP RUN.
                """;

    private static string Indexed(string keyClauses, string record, string working, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRIX.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-ix.dat"
                ORGANIZATION IS INDEXED
                ACCESS MODE IS DYNAMIC
        {keyClauses}
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {record}
        {working}
        {body}
        """;

    private static string Relative(string access, string keyClause, string record, string working, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRRL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-rl.dat"
                ORGANIZATION IS RELATIVE
                ACCESS MODE IS {access}
        {keyClause}
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {record}
        {working}
        {body}
        """;

    /// <summary>Every rule of the table, at every edition it applies to (all four — the RECORD KEY, ALTERNATE
    /// RECORD KEY and RELATIVE KEY clauses and these syntax rules all predate COBOL-85), reported for the
    /// ENTRY-ONLY program and for the verb-bearing one ALIKE. The expected substring is the §, so a rule that
    /// starts reporting under a neighbour's citation fails here.</summary>
    public static TheoryData<string, string, string> Cases()
    {
        var d = new TheoryData<string, string, string>();

        // §12.4.5.12.3 SR1 — the prime key under OCCURS.
        d.Add("ix-record-key-occurs", "§12.4.5.12.3 SR1", "        RECORD KEY IS F-KEY.|"
            + "01 F-REC.\n   05 F-KG.\n      10 F-KEY PIC X(3) OCCURS 2 TIMES.\n   05 F-D PIC X(4).|");
        // §12.4.5.6.3 SR1 — an alternate key under OCCURS, the prime key legal.
        d.Add("ix-alternate-key-occurs", "§12.4.5.6.3 SR1",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-AG.\n      10 F-ALT PIC X(2) OCCURS 2 TIMES.|");
        // §12.4.5.12.3 SR2 — the prime key names an item outside the file's records.
        d.Add("ix-record-key-outside-record", "§12.4.5.12.3 SR2", "        RECORD KEY IS W-OUT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).|WORKING-STORAGE SECTION.\n01 W-OUT PIC X(3).");
        // §12.4.5.6.3 SR2 — an alternate key names an item outside the file's records (silently dropped before).
        d.Add("ix-alternate-key-outside-record", "§12.4.5.6.3 SR2",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS W-OUT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).|WORKING-STORAGE SECTION.\n01 W-OUT PIC X(3).");
        // §12.4.5.1 Format 1 — the RECORD KEY clause is unbracketed, so it is required for an indexed file.
        d.Add("ix-no-record-key", "ISO §12.4.5.1 Format 1", "        .|01 F-REC.\n   05 F-KEY PIC X(3).|");
        return d;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IndexedEntryRule_ReportsAtEveryEdition_WithOrWithoutAKeyedVerb(string name, string citation, string parts)
    {
        string[] p = parts.Split('|');
        foreach (int edition in AllEditions)
        {
            int entryOnly = Diagnostics(Indexed(p[0], p[1], p[2], EntryOnlyBody), edition)
                .Count(e => e.Contains(citation, StringComparison.Ordinal));
            int withVerb = Diagnostics(Indexed(p[0], p[1], p[2], WithKeyedVerbBody), edition)
                .Count(e => e.Contains(citation, StringComparison.Ordinal));
            // ⛔ THE DISCRIMINATOR: the entry rule is reported EXACTLY ONCE whether or not a keyed verb names
            // the file. Before PB699 the entry-only count was 0 and the verb-bearing one was 1.
            Assert.True(entryOnly == 1, $"{name} @ {edition}: entry-only program reported {citation} {entryOnly}×, expected 1");
            Assert.True(withVerb == 1, $"{name} @ {edition}: verb-bearing program reported {citation} {withVerb}×, expected 1");
        }
    }

    /// <summary>§12.4.5.2 SR10 — <i>"The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of
    /// the ACCESS clause is specified."</i> ⚠ THE CITATION IS THE ASSERTION: this rejection used to cite
    /// §12.4.5.13, which has no syntax rules at all, and §12.4.5.1 Format 2 BRACKETS the RELATIVE KEY clause, so
    /// neither the clause nor the format requires it — SR10 is the one sentence that does.</summary>
    [Theory]
    [InlineData("RANDOM")]
    [InlineData("DYNAMIC")]
    public void RelativeKeyRequiredByAccessMode_CitesSr10_AtEveryEdition(string access)
    {
        foreach (int edition in AllEditions)
        {
            var entryOnly = Diagnostics(Relative(access, "        .", "01 F-REC PIC X(8).", "", EntryOnlyBody), edition);
            Assert.Contains(entryOnly, e => e.Contains("§12.4.5.2 SR10", StringComparison.Ordinal));
            Assert.DoesNotContain(entryOnly, e => e.Contains("§12.4.5.13 —", StringComparison.Ordinal));
        }
    }

    /// <summary>ACCESS SEQUENTIAL does NOT require the clause: §12.4.5.2 SR10 names only DYNAMIC and RANDOM, and
    /// the Format 2 diagram brackets it. The rule's own boundary, measured — not merely the violation.</summary>
    [Fact]
    public void RelativeKeyOmitted_UnderAccessSequential_IsLegal()
    {
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(Relative("SEQUENTIAL", "        .", "01 F-REC PIC X(8).", "", EntryOnlyBody), edition);
            Assert.DoesNotContain(d, e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
        }
    }

    /// <summary>§12.4.5.13.3's three syntax rules, each on an entry-only program: SR1 (OCCURS), SR2 (an unsigned
    /// integer without 'P'), SR3 (not defined in a record description entry subordinate to the file-name).
    /// SR2 and SR3 were cited as "§12.4.5.13 SR2/SR3" before PB699 — a clause that carries no syntax rules.</summary>
    [Theory]
    [InlineData("§12.4.5.13.3 SR1", "01 F-REC PIC X(8).", "WORKING-STORAGE SECTION.\n01 W-T.\n   05 W-RK PIC 9(4) OCCURS 3 TIMES.")]
    [InlineData("§12.4.5.13.3 SR2", "01 F-REC PIC X(8).", "WORKING-STORAGE SECTION.\n01 W-RK PIC S9(4).")]
    [InlineData("§12.4.5.13.3 SR3", "01 F-REC.\n   05 W-RK PIC 9(4).\n   05 F-D PIC X(4).", "")]
    public void RelativeKeyOperandRule_ReportsOnAnEntryOnlyProgram_AtEveryEdition(string citation, string record, string working)
    {
        foreach (int edition in AllEditions)
        {
            var entryOnly = Diagnostics(
                Relative("DYNAMIC", "        RELATIVE KEY IS W-RK.", record, working, EntryOnlyBody), edition);
            Assert.Contains(entryOnly, e => e.Contains(citation, StringComparison.Ordinal));
            Assert.DoesNotContain(entryOnly, e => e.Contains("§12.4.5.13 SR", StringComparison.Ordinal));
        }
    }

    /// <summary>THE OVER-REJECTION GUARD. A legal indexed entry — a GROUP prime key (§13.18.29.4 GR3 makes a
    /// group with no GROUP-USAGE clause an alphanumeric group item, which §12.4.5.12.3 SR2 admits), reached
    /// through an IN qualifier (§8.4.2.2), plus an alternate key WITH DUPLICATES — draws no key diagnostic at
    /// any edition. The screen's within-a-record test walks to the 01 root, and a qualified reference has to
    /// survive it.</summary>
    [Fact]
    public void LegalIndexedEntry_WithAGroupKeyAndAQualifiedReference_IsAccepted()
    {
        string source = Indexed(
            "        RECORD KEY IS F-KEY IN F-REC\n        ALTERNATE RECORD KEY IS F-ALT WITH DUPLICATES.",
            "01 F-REC.\n   05 F-KEY.\n      10 F-K1 PIC X.\n      10 F-K2 PIC X.\n   05 F-ALT PIC X(2).",
            "", WithKeyedVerbBody);
        foreach (int edition in AllEditions)
            Assert.DoesNotContain(Diagnostics(source, edition), e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
    }
}
