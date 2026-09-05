// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE OPTIONAL-WORD DRIFT GATE — ISO §8.3.2.4.3: "Within each format, uppercase words that are not underlined
/// are called optional words and may be specified at the user's option WITH NO EFFECT ON THE SEMANTICS OF THE
/// FORMAT." §5.2.3 says the same of the words themselves. This test takes that literally: for each format below
/// it writes EVERY SUBSET of the format's optional words and demands that all of them compile and produce
/// BYTE-IDENTICAL output. A word made required again fails immediately, and so does a word whose presence quietly
/// changes what the statement binds to.
/// </summary>
/// <remarks>
/// WHY A SUBSET SWEEP AND NOT TWO SPELLINGS. kb/Work PB332: <c>useStatement</c> accepted STANDARD and ON as
/// optional because "the CCVS suite and mainstream compilers write both" spellings — the optional-word set was
/// derived from what a witness corpus happened to contain, so the two words no witness omitted (AFTER, PROCEDURE)
/// stayed required and rejected legal COBOL for as long as nobody wrote them. A pair of hand-picked spellings
/// would have passed the whole time. The POWER SET cannot: it writes the combination nobody thought of.
///
/// Adding a format is one row in <see cref="Formats"/>. The optional-word list is measured from the printed page
/// (<c>scripts/spec/figure_extract.py</c>), never from the transcription and never from the corpus, and
/// <c>scripts/spec/audit_grammar_optional_words.py</c> is the tool that finds the ones still missing.
/// </remarks>
public sealed class OptionalWordSubsetDriftTests
{
    /// <param name="Name">Test identity, and the PROGRAM-ID stem.</param>
    /// <param name="Clause">The general format whose underlining was measured.</param>
    /// <param name="Edition">The ISO edition to compile as — the earliest at which the format exists.</param>
    /// <param name="OptionalWords">The words printed WITHOUT an underline in that format.</param>
    /// <param name="ProgramId">The template's PROGRAM-ID stem — rewritten per spelling so no name is compiled twice.</param>
    /// <param name="Template">Source carrying one <c>{n}</c> slot per optional word.</param>
    private sealed record FormatCase(string Name, string Clause, int Edition, string[] OptionalWords,
                                     string ProgramId, string Template);

    // ── USE Format 1 (§14.9.49.2): USE [GLOBAL] AFTER STANDARD {EXCEPTION | ERROR} PROCEDURE ON {…} ──────────
    // Underlined on printed folio 774: USE, GLOBAL, EXCEPTION, ERROR, INPUT, OUTPUT, I-O, EXTEND. Plain: AFTER,
    // STANDARD, PROCEDURE, ON. Neither file is ever opened, so the CLOSE is unsuccessful with I-O status 42
    // (§14.9.6.4 GR1) and the declarative selected by §14.9.49.4 GR6 a) reports it.
    private const string UseFormat1 = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWA.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F1 ASSIGN TO "opwa.dat"
                   ORGANIZATION IS SEQUENTIAL
                   ACCESS MODE IS SEQUENTIAL
                   FILE STATUS IS ST1.
           DATA DIVISION.
           FILE SECTION.
           FD F1.
           01 R1 PIC X(8).
           WORKING-STORAGE SECTION.
           01 ST1 PIC XX.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H-SECT SECTION.
               USE {0} {1} ERROR {2} {3} F1.
           H-PARA.
               DISPLAY "HANDLER=" ST1.
           END DECLARATIVES.
           MAIN-SECT SECTION.
           MAIN.
               CLOSE F1
               DISPLAY "AFTER=" ST1
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── USE Format 3 (§14.9.49.2): USE AFTER {EXCEPTION CONDITION | EC} … ─────────────────────────────────────
    // Only AFTER is plain. EC is synonymous with EXCEPTION CONDITION (§14.9.49.3 SR12), so the two spellings of
    // the keyword pair are covered by the two rows below rather than by the subset sweep.
    private const string UseFormat3 = """
          >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWB.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 G.
              05 T PIC 9(2) OCCURS 3 TIMES.
           01 IDX PIC 9(2) VALUE 5.
           01 R  PIC 9(2) VALUE 0.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H SECTION.
               USE {0} EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
           H-P.
               DISPLAY "HANDLED".
               RESUME AT NEXT STATEMENT.
           END DECLARATIVES.
           MAIN SECTION.
           MAIN-P.
               MOVE T (IDX) TO R.
               DISPLAY "AFTER".
               STOP RUN.
    """;

    private const string UseFormat3Ec = """
          >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWC.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 G.
              05 T PIC 9(2) OCCURS 3 TIMES.
           01 IDX PIC 9(2) VALUE 5.
           01 R  PIC 9(2) VALUE 0.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H SECTION.
               USE {0} EC EC-BOUND-SUBSCRIPT.
           H-P.
               DISPLAY "HANDLED".
               RESUME AT NEXT STATEMENT.
           END DECLARATIVES.
           MAIN SECTION.
           MAIN-P.
               MOVE T (IDX) TO R.
               DISPLAY "AFTER".
               STOP RUN.
    """;

    // ── START … [WITH] LENGTH (§14.9.41.2) ───────────────────────────────────────────────────────────────────
    // Printed folio 754 underlines LENGTH and leaves WITH plain. §14.9.41.4 GR17 b) makes the temporary key area
    // 2 characters, so both spellings must position at AA01 and read it back identically.
    private const string StartWithLength = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWD.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT IXF ASSIGN TO "opwd.dat"
                   ORGANIZATION IS INDEXED
                   ACCESS MODE IS DYNAMIC
                   RECORD KEY IS IX-KEY
                   FILE STATUS IS ST1.
           DATA DIVISION.
           FILE SECTION.
           FD IXF.
           01 IX-REC.
              05 IX-KEY PIC X(4).
              05 IX-VAL PIC X(4).
           WORKING-STORAGE SECTION.
           01 ST1 PIC XX.
           PROCEDURE DIVISION.
           MAIN.
               OPEN OUTPUT IXF
               MOVE "AA01" TO IX-KEY MOVE "ONE " TO IX-VAL WRITE IX-REC
               MOVE "BB01" TO IX-KEY MOVE "TWO " TO IX-VAL WRITE IX-REC
               CLOSE IXF
               OPEN INPUT IXF
               MOVE "AAZZ" TO IX-KEY
               START IXF KEY IS >= IX-KEY {0} LENGTH 2
                   INVALID KEY DISPLAY "INVALID"
               END-START
               READ IXF NEXT RECORD
                   AT END DISPLAY "EOF"
               END-READ
               DISPLAY "KEY=" IX-KEY "|" IX-VAL "|" ST1
               CLOSE IXF
               DISPLAY "DONE"
               STOP RUN.
    """;

    private static readonly FormatCase[] Formats =
    [
        new("use-format-1", "14.9.49.2 Format 1", 85, ["AFTER", "STANDARD", "PROCEDURE", "ON"], "OPWA", UseFormat1),
        new("use-format-3", "14.9.49.2 Format 3", 2023, ["AFTER"], "OPWB", UseFormat3),
        new("use-format-3-ec", "14.9.49.2 Format 3 (EC)", 2023, ["AFTER"], "OPWC", UseFormat3Ec),
        new("start-with-length", "14.9.41.2", 2002, ["WITH"], "OPWD", StartWithLength),
    ];

    public static IEnumerable<object[]> Cases() => Formats.Select(f => new object[] { f.Name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryOptionalWordSubset_CompilesAndRunsIdentically(string name)
    {
        FormatCase f = Formats.Single(c => c.Name == name);
        int slots = f.OptionalWords.Length;
        string? baseline = null;
        string baselineSpelling = "";

        for (int mask = 0; mask < (1 << slots); mask++)
        {
            // Slot i is written when its bit is set; the all-bits-set spelling is the fully-written form.
            object[] words = [.. Enumerable.Range(0, slots).Select(i => (mask & (1 << i)) != 0 ? f.OptionalWords[i] : "")];
            string spelling = string.Join(" ", words.Cast<string>().Where(w => w.Length > 0));
            if (spelling.Length == 0) spelling = "none written";
            // ⛔ A UNIQUE PROGRAM-ID AND ASSEMBLY NAME PER SPELLING. Every subset compiles inside ONE test
            // process, and a same-named assembly is served from the load context that already holds it — the
            // run would then compare a spelling against ITSELF and pass whatever the grammar did.
            string unit = f.ProgramId + mask.ToString("00");
            string source = string.Format(f.Template, words).Replace(f.ProgramId + ".", unit + ".", StringComparison.Ordinal);
            Assert.Contains("PROGRAM-ID. " + unit + ".", source, StringComparison.Ordinal);

            string dir = CutRunner.NewTempDir("optword");
            try
            {
                string src = Path.Combine(dir, unit + ".cob");
                File.WriteAllText(src, source);
                string dll = Path.Combine(dir, unit + ".dll");
                var r = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, DialectLevel: f.Edition));
                Assert.True(r.Success,
                    $"ISO §{f.Clause}: the optional words [{spelling}] must compile at --std {f.Edition} " +
                    $"(§5.2.3 — they are printed WITHOUT an underline): {string.Join("\n", r.Errors)}");

                var (ran, stdout, detail) = CutRunner.Run(dll, dir);
                Assert.True(ran, $"[{f.Name}] with [{spelling}] must run: {detail}");
                string normalized = CutRunner.Normalize(stdout);
                if (baseline is null)
                {
                    baseline = normalized;
                    baselineSpelling = spelling;
                    continue;
                }
                Assert.True(baseline == normalized,
                    $"ISO §8.3.2.4.3: an optional word is specified \"with no effect on the semantics of the " +
                    $"format\", but §{f.Clause} written [{spelling}] differs from [{baselineSpelling}].\n" +
                    $"  [{baselineSpelling}] -> {baseline}\n  [{spelling}] -> {normalized}");
            }
            finally { CutRunner.TryDelete(dir); }
        }

        Assert.NotNull(baseline);
    }
}
