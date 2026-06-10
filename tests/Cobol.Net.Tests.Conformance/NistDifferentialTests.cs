// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Drives real NIST CCVS programs through COBOL.NET end-to-end and compares the produced output to the NIST golden
/// (<c>tests/nist/valid/&lt;TEST&gt;.txt</c>) on the guard's acceptance basis (drop CR, strip per-line trailing spaces,
/// and mask the volatile COMPUTED= operand). The golden is the authoritative oracle — it was validated against the
/// legacy byte engine over the whole 364-program corpus — so a match here proves COBOL.NET runs the program correctly,
/// not merely that it agrees with the legacy. This is the harness the G5 corpus drive runs through: each NC/SM/IC/…
/// program that goes green becomes a permanent regression test by adding its name here.
/// </summary>
public sealed class NistDifferentialTests
{
    [Theory]
    [InlineData("NC101A")]   // the first full NC program: MULTIPLY/DIVIDE + the CCVS print-file report
    [InlineData("NC110M")]
    [InlineData("NC111A")]
    [InlineData("NC112A")]
    [InlineData("NC113M")]
    [InlineData("NC127A")]
    [InlineData("NC136A")]
    [InlineData("NC211A")]   // the first NC conditional program: abbreviated/compound conditions, OCCURS-group image,
                             // signed→alphanumeric de-sign, ALL "literal", IS NUMERIC over alphanumeric (DEVLOG 506–511)
    // Additional nucleus programs that already byte-match the golden — located by the compile/run/diff corpus sweep
    // and locked in as permanent regressions (DEVLOG 513). They exercise the MOVE / PERFORM / GO TO / ADD /
    // SUBTRACT / IF surface already built for the eight above; no new feature was needed to green them.
    [InlineData("NC118A")]   // nucleus arithmetic + data movement (MOVE / PERFORM / ADD)
    [InlineData("NC119A")]   // nucleus arithmetic (MOVE / SUBTRACT / ADD)
    [InlineData("NC177A")]   // nucleus arithmetic, ADD/MOVE heavy
    [InlineData("NC205A")]   // nucleus conditional + data movement
    // Greened by the PERFORM-range control fix (DEVLOG 514): PERFORM proc-1 THRU proc-2 N TIMES now iterates the
    // range N times (§14.9.28 GR9) instead of once, so the COMP ON SIZE ERROR drain-loops reach overflow.
    [InlineData("NC106A")]   // SUBTRACT + COMP ON SIZE ERROR, driven by a PERFORM THRU … TIMES loop
    [InlineData("NC176A")]   // ADD + COMP ON SIZE ERROR, driven by a PERFORM THRU … TIMES loop
    [InlineData("NC134A")]   // nucleus arithmetic exercising PERFORM THRU … TIMES ranges
    // Greened by group-level SIGN clause inheritance (ISO §13.18.52 GR1–3, DEVLOG 525): a group SIGN applies to
    // every subordinate signed numeric DISPLAY item, nearest enclosing clause wins (GF-17's SIGN LEADING SEPARATE
    // group overrides the 01's SIGN TRAILING; the separate '+' is readable through a REDEFINES view).
    [InlineData("NC116A")]   // SIGN clause precedence (GF-16/17/18) + signed data movement
    // Greened by the SET statement + index machinery (ISO §14.9.39 Formats 1-2 + §13.18.60 USAGE INDEX, DEVLOG 526):
    // SET index/index-item/numeric TO …, SET UP/DOWN BY, index-names in relations and subscripts.
    [InlineData("NC121M")]   // table handling via SET + index subscripting
    [InlineData("NC123A")]   // SET + GO TO DEPENDING over table paragraphs
    [InlineData("NC131A")]   // SET across index-names, USAGE INDEX items (incl. a USAGE INDEX group), numeric receivers
    [InlineData("NC137A")]   // SET + relative index subscripting
    [InlineData("NC141A")]   // SET + multi-dimensional table indexing
    [InlineData("NC248A")]   // SET + table relation conditions
    // Greened by sections-as-procedure-targets + qualified procedure-names (ISO §14.4.3/§14.9.17/§14.9.28/§8.4.2.2)
    // + the PERFORM TIMES once-evaluated count (§14.9.28 GR7) — DEVLOG 527.
    [InlineData("NC102A")]   // PERFORM/GO TO torture: sections, inverted THRU ranges, TIMES with body-modified counts
    [InlineData("NC138A")]   // GO TO section-name
    [InlineData("NC139A")]   // SET + GO TO section-name
    [InlineData("NC140A")]   // SET + GO TO section-name
    [InlineData("NC245A")]   // SET + GO TO section-name (table series)
    // Greened by PERFORM VARYING (ISO §14.9.28 Format 4 GR12-13, all levels/TEST modes) + ALL-"literal"
    // repeat-to-group-width (§8.3.3.6.4 GR2) + benign out-of-range subscripts (§8.4.2.3.4 GR2, CobolTable.At) —
    // DEVLOG 528.
    [InlineData("NC239A")]   // PERFORM VARYING + SET over tables
    [InlineData("NC240A")]   // PERFORM VARYING (pure)
    [InlineData("NC241A")]   // PERFORM VARYING + SET
    [InlineData("NC242A")]   // PERFORM VARYING + ALL-literal table seeding
    [InlineData("NC243A")]   // VARYING up to 7 AFTER levels over a 7-dim table + past-end FAIL-path reads
    [InlineData("NC244A")]   // SET multi-receiver + PERFORM VARYING
    // Greened by the numeric-edited receiver stack (CobolEdit, ISO §13.18.40.4 + §14.7.7 ROUNDED-before-editing) —
    // DEVLOG 530.
    [InlineData("NC117A")]   // DIVIDE into numeric-edited receivers
    [InlineData("NC120A")]   // MULTIPLY GIVING edited ROUNDED ($ZZ9.99CR etc.)
    [InlineData("NC220M")]   // SET + PERFORM VARYING + DIVIDE REMAINDER combined (greened by the union of waves)
    // Greened by the wave-4 resolver work (DEVLOG 533): B2 Tier-B offset×OCCURS accounting + inner-REDEFINES
    // target offsets (ISO §13.18.44 GR1), GAP-1 subscripted Tier-B views (computed-offset RedefViewPlace), and
    // NEXT SENTENCE (§14.9.19 GR6, sentence-boundary labels).
    [InlineData("NC125A")]   // tables REDEFINING a picture item, subscripted views
    [InlineData("NC132A")]   // subscripted Tier-B views (ENTRY-B-2(1) style)
    [InlineData("NC210A")]   // subscripted views + data-name subscripts
    [InlineData("NC231A")]   // SEARCH F1 + PERFORM VARYING + NEXT SENTENCE
    [InlineData("NC232A")]   // SEARCH F1 + NEXT SENTENCE
    [InlineData("NC234A")]   // SEARCH F1 + VARYING + NEXT SENTENCE over REDEFINES tables
    // Greened by GAP-2 qualified subscripts (§8.4.2.3.2), qualifier-aware 88 selection (§8.4.2.2 F2), the
    // CobolTable.Occ bind-time/emit-time storage-form bridge, and alphanumeric figurative-vs-numeric comparisons
    // (§8.8.4.2.1) — DEVLOG 534.
    [InlineData("NC206A")]   // qualified base + qualified subscript (AX-2 IN AX(CX-SUB OF CX))
    [InlineData("NC246A")]   // 5-deep qualification + qualified 88s over multi-level tables
    // Greened by ALPHANUMERIC-EDITED pictures (X/A/9 with B 0 / insertion, ISO §13.18.40) + the all-symbol
    // numeric-edited classification fix (PIC ****/$$$$ are numeric-edited even with zero '9's) — DEVLOG 535.
    [InlineData("NC126A")]   // level-number torture incl. XBXBXBX / 9090900 / **** edited receivers
    // Greened by Int128 radix alignment in the division kernel (Divide/RoundDiv/DivisionLosesPrecision — an
    // 18-digit dividend scaled by the receiver's fraction digits exceeded long MID-computation, DEVLOG 536).
    [InlineData("NC171A")]   // DIVIDE INTO with 18-significant-digit operands
    // Greened by SEARCH ALL (ISO §14.9.37 F2 — from-start scan, GR9 technique-implementor-specified) and the
    // sole-numeric-literal comparison operand (§8.8.4.2.1 written character form vs groups) — DEVLOG 537.
    [InlineData("NC233A")]   // SEARCH ALL over 3-dim tables
    [InlineData("NC238A")]   // SEARCH ALL + SET
    [InlineData("NC237A")]   // 3-dim table build + group-vs-literal comparisons + SEARCH ALL
    [InlineData("NC103A")]   // group-vs-numeric-literal comparisons + NEXT SENTENCE
    [InlineData("NC133A")]   // SET + multi-index tables (greened by the union of the resolver waves)
    // Greened by the §14.9.12 GR6c subsidiary-quotient fix (REMAINDER from the GIVING-scale-truncated quotient)
    // and the print-file plain-WRITE line advance (§14.9.46) — DEVLOG 538.
    [InlineData("NC203A")]   // DIVIDE REMAINDER across scales (S9(6)V9(6) dividends, edited remainders)
    [InlineData("NC251A")]   // DIVIDE REMAINDER + END-DIVIDE scope
    [InlineData("NC135A")]   // 3-dim table build via sections + plain WRITE in a print stream
    // Greened by EVALUATE (ISO §14.9.13 — bind-time chained selection): value/expression/literal subjects,
    // THRU ranges, NOT groups, ALSO multi-subject AND composition, ANY, TRUE/FALSE subjects↔condition objects,
    // and CONDITIONAL subjects (class tests + level-88 condition-names) paired with TRUE/FALSE — DEVLOG 542.
    [InlineData("NC225A")]   // the EVALUATE feature program (29 GF tests incl. 6-subject ALSO matrices)
    // Greened by the SIX-FAMILY VERB WAVE (DEVLOG 543 — INSPECT §14.9.22, STRING §14.9.43 / UNSTRING §14.9.48,
    // INITIALIZE §14.9.20, ACCEPT §14.9.1, MOVE/ADD/SUBTRACT CORRESPONDING §14.7.6, ALTER (85-only) + SPECIAL-NAMES
    // switches §12.3.7 / SET F3 §14.9.39 / switch conditions §8.8.4.6) plus the integration fixes: JUSTIFIED
    // §13.18.34, user-defined classes §12.3.7/§8.8.4.1.4, sign-aware zoned IS NUMERIC on character-backed views
    // §8.8.4.4 r3, level-66 RENAMES places §13.18.45, de-editing §14.9.25.4 GR5, AN-edited figurative INITIALIZE
    // §14.9.25.4 GR5, and the VARYING AFTER augment-then-reinit order §14.9.28 GR13e.
    [InlineData("NC115A")]   // INSPECT TALLYING
    [InlineData("NC216A")]   // INSPECT REPLACING/CONVERTING (BEFORE+AFTER combos)
    [InlineData("NC221A")]   // INSPECT TALLYING+REPLACING combined
    [InlineData("NC122A")]   // INSPECT over signed/edited senders
    [InlineData("NC217A")]   // STRING (POINTER, DELIMITED, overflow)
    [InlineData("NC218A")]   // UNSTRING (DELIMITER/COUNT/TALLYING IN, JUSTIFIED receivers)
    [InlineData("NC223A")]   // INITIALIZE (REPLACING categories, AN-edited receivers)
    [InlineData("NC201A")]   // INITIALIZE + PERFORM VARYING AFTER FROM-outer-var ordering
    [InlineData("NC109M")]   // ACCEPT device reads (stdin .dat piped by the harness)
    [InlineData("NC204M")]   // ACCEPT FROM mnemonic (SPECIAL-NAMES device registry)
    [InlineData("NC202A")]   // ADD CORRESPONDING (ROUNDED, SIZE ERROR, qualified, tables)
    [InlineData("NC207A")]   // ADD/SUBTRACT CORR over 5-deep identical-name subtrees
    [InlineData("NC208A")]   // MOVE CORRESPONDING (partial matches)
    [InlineData("NC209A")]   // MOVE CORR qualified/subscripted + level-66 RENAMES references
    [InlineData("NC222A")]   // MOVE CORR tables + the de-editing MOVE (NE sender → numeric)
    [InlineData("NC253A")]   // SUBTRACT CORRESPONDING suite
    [InlineData("NC174A")]   // SPECIAL-NAMES switches + SET F3 + user-defined classes + zoned IS NUMERIC
    [InlineData("NC254A")]   // switch conditions inside compound/abbreviated conditions
    [InlineData("NC302M")]   // ALTER (no PROCEED) inside PERFORM THRU
    // Greened by the ARITHMETIC WAVES (DEVLOG 544): single evaluation for multi-receiver arithmetic (§14.7.7 GR4 +
    // NOTE 3 — sender snapshots), SIZE ERROR capacity checks for numeric-edited resultants (§14.7.5 case 3 +
    // storing rule 2, CobolEdit.TryFormat), P-scale sending-image zeros (§13.18.40.3 / §14.9.25.4 GR6a),
    // breadth-first sign-condition operand binding (§8.8.4.3), and bare figurative words in string 88 VALUEs
    // (§8.3.1.2 + §8.3.3.6.4 GR2).
    [InlineData("NC172A")]   // multi-result DIVIDE INTO with a dividend-aliasing receiver + 27-digit self-division
    [InlineData("NC173A")]   // multi-result DIVIDE BY with a dividend-aliasing receiver
    [InlineData("NC170A")]   // MULTIPLY GIVING into edited receivers under ON SIZE ERROR
    [InlineData("NC175A")]   // SUBTRACT GIVING into edited receivers under ON SIZE ERROR ($.** / $**.**CR)
    [InlineData("NC114M")]   // MOVE S9P(17) SIGN LEADING SEPARATE to X(18)/edited (P zeros + de-sign)
    [InlineData("NC250A")]   // IF torture: multi-term sign conditions + 88 VALUE QUOTE/SPACE
    // Greened by the MOVE/PICTURE editing fixes (DEVLOG 545): PIC 9(5)CR classified numeric-edited (§13.18.40.4),
    // GROUP-level VALUE distribution (§13.18.63), BLANK WHEN ZERO (§13.18.8), AN→NE editing moves (§14.9.25.4
    // GR5), and P scaling in EDITED masks (ZZZPP — §13.18.40.3, mask scale −P, no output position).
    [InlineData("NC104A")]   // the MOVE feature program (141 tests: every Table-16 pairing incl. CR/BWZ/group-VALUE)
    [InlineData("NC124A")]   // the PICTURE feature program (169 tests incl. P-scaled edited masks)
    // Greened by the PROGRAM COLLATING SEQUENCE subsystem (DEVLOG 546): ALPHABET literal phrases (§12.3.7 GR7
    // k1–k6 incl. THRU either-direction, ALSO shared positions, the k3 distinct-ascending unspecified tail),
    // PCS-collated relation/figurative/88 comparisons via CobolString.Compare(a,b,__COLLATE) (§12.3.6 GR11 /
    // §8.8.4.2.7), and PCS-derived HIGH-/LOW-VALUE character identity (§8.3.3.6 GR6/7 + §12.3.7 GR8/9 tie rules).
    [InlineData("NC215A")]   // THE-WILD-ONE alphabet: THRU+ALSO matrix, mixed numeric/alphanumeric collated compares
    [InlineData("NC219A")]   // figuratives INSIDE the alphabet (GR10 native), HIGH=0xFE re-derivation, VALUE LOW-VALUE
    // Greened by reference modification over NUMERIC-DISPLAY items + the parsed refModSpec form (ISO §8.4.2.4 —
    // the unique result is an elementary ALPHANUMERIC item; NumericImagePlace formats/decodes the character image
    // around the slice, and a ref-mod result never takes the numeric render path) — DEVLOG 548.
    [InlineData("NC224A")]   // the reference-modification feature program (numeric/edited/group senders+receivers)
    // Greened by the null-table benign chain in CobolTable.At (an out-of-range OUTER subscript's zeroed scratch
    // struct carries null nested OCCURS arrays; every deeper level now resolves benignly too, §8.4.2.3.4 GR2) —
    // DEVLOG 549.
    [InlineData("NC401M")]   // obsolete-elements program: ALTER + CORR + ACCEPT DAY-OF-WEEK + 5-deep OOR chain
    // Greened by the RENAMES/REDEFINES layout closeout (DEVLOG 550): no-THRU RENAMES forwards to the renamed
    // item (§13.18.45 GR1), THRU spans expand OCCURS leaves + numeric leaves via the StoreDisplay storage-form
    // bridge, nested REDEFINES classes dissolve into the outer class's ONE backing, and group width/image
    // exclude redefiner children (§13.18.44 — an overlay adds no storage).
    [InlineData("NC252A")]   // the REDEFINES + RENAMES feature program
    // Greened by OCCURS DEPENDING ON (ISO §13.18.38 — DEVLOG 551): max allocation (§8.5.1.8), the GR8
    // sending-slice / receiving direction-split via OdoGroupPlace + CobolTable.OdoExtent, SEARCH bounded by the
    // CURRENT count (§14.9.37.4 GR4/GR9). NC235A additionally EXCEEDS the legacy golden (spec-pinned).
    [InlineData("NC247A")]   // INSPECT/UNSTRING/STRING over ODO groups (the string[] CS0029 program)
    // Greened by SORT/MERGE/RELEASE/RETURN (ISO §14.9.40/§14.9.24/§14.9.32/§14.9.34 — DEVLOG 552): SD binding,
    // the three phases with implicit USING/GIVING transfers, INPUT/OUTPUT PROCEDURE dispatch ranges, stable
    // DUPLICATES, algebraic numeric keys, the GR5 collating precedence over the PCS table. The 14 below are the
    // STANDALONE-deterministic ST programs; chain consumers (ST103A/105A/111A… read a predecessor's output file)
    // need harness chaining and stay swept-only.
    [InlineData("ST101A")]
    [InlineData("ST104A")]
    [InlineData("ST106A")]
    [InlineData("ST118A")]
    [InlineData("ST119A")]
    [InlineData("ST125A")]
    [InlineData("ST132A")]
    [InlineData("ST135A")]
    [InlineData("ST136A")]
    [InlineData("ST137A")]   // NATIVE collating via X-card
    [InlineData("ST139A")]   // COLLATING keyword omitted (leniency)
    [InlineData("ST140A")]
    // ST146A swept-only: its golden encodes the LEGACY's LOW-VALUE fill in the spec-UNDEFINED tail of a
    // max-length RETURN INTO record area (the byte tail beyond the returned record is undefined).
    [InlineData("ST147A")]   // NATIVE COLLATING SEQUENCE checks
    // Greened by the KEYED I/O subsystem (DEVLOG 553 — RELATIVE §14.9.30 GR25/GR29 + INDEXED organizations,
    // DELETE §14.9.10, START §14.9.41, INVALID KEY routing §9.1.14, RelativeFile slot store + IndexedFile
    // prime/alternate keys; the record AREA is the largest record description §13.4.2).
    [InlineData("RL101A")]
    [InlineData("RL105A")]
    [InlineData("RL106A")]
    [InlineData("RL107A")]
    [InlineData("RL108A")]
    [InlineData("RL116A")]
    [InlineData("RL117A")]
    [InlineData("RL118A")]
    [InlineData("RL119A")]
    [InlineData("RL201A")]
    [InlineData("RL205A")]
    [InlineData("RL209A")]
    [InlineData("RL210A")]
    [InlineData("RL211A")]
    [InlineData("RL302M")]
    [InlineData("IX101A")]
    [InlineData("IX105A")]
    [InlineData("IX107A")]
    [InlineData("IX108A")]
    [InlineData("IX111A")]
    [InlineData("IX201A")]
    [InlineData("IX208A")]
    [InlineData("IX209A")]
    [InlineData("IX211A")]
    [InlineData("IX212A")]
    [InlineData("IX213A")]
    [InlineData("IX302M")]
    public void NistProgram_MatchesGolden(string testName)
    {
        string root = RepoRoot();
        string goldenPath = Path.Combine(root, "tests", "nist", "valid", testName + ".txt");
        Assert.True(File.Exists(goldenPath), $"golden not found: {goldenPath}");

        var (ok, output, detail) = RunNist(root, testName);
        Assert.True(ok, detail);
        Assert.Equal(Normalize(File.ReadAllText(goldenPath)), output);
    }

    /// <summary>Compile a NIST program (with CCVS X-card preprocessing) and run it in an isolated temp directory,
    /// returning the program's output read from its print file (the CCVS report) — or stdout for a DISPLAY-only
    /// program — normalized to the NIST acceptance basis.</summary>
    private static (bool ok, string output, string detail) RunNist(string root, string testName)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Nist_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(root, "tests", "nist", "programs", testName + ".cob");
            if (!File.Exists(src)) return (false, "", $"source not found: {src}");
            string dll = Path.Combine(dir, testName + ".dll");

            var result = CompilerDriver.Compile(new CompilerDriver.Options(src, dll, NistTestName: testName, DialectLevel: 85));
            if (!result.Success)
                return (false, "", $"[compile] {result.Status}: {string.Join("\n", result.Errors)}");

            string dat = Path.Combine(root, "tests", "nist", "data", testName + ".dat");
            // Guard parity (scripts/guard.sh:120): the CCVS-85 switch programs (NC174A/NC254A) run with external
            // SWITCH-1 ON, SWITCH-2 unset — their goldens assume exactly that.
            var env = new Dictionary<string, string> { ["COBOL_SWITCH_1"] = "ON" };
            var (runOk, stdout, runDetail) = CutRunner.Run(dll, dir, File.Exists(dat) ? dat : null, env);
            if (!runOk) return (false, "", $"[run] exit non-zero: {runDetail}");

            // The CCVS report lands in the print file (assign target → <lowercased>.txt in the run dir); a
            // DISPLAY-only program produces no print file and is read from stdout — exactly the guard's discovery order.
            string printFile = Path.Combine(dir, testName.ToLowerInvariant() + ".txt");
            string raw = File.Exists(printFile) ? File.ReadAllText(printFile)
                : Directory.EnumerateFiles(dir, "*.txt").FirstOrDefault() is { } any ? File.ReadAllText(any)
                : stdout;
            return (true, Normalize(raw), runDetail);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>The NIST acceptance basis (exactly <c>scripts/guard.sh</c>'s <c>normalize()</c>): drop CR, strip
    /// per-line trailing spaces, and mask the COMPUTED= operand (a value some CCVS programs print that is not part of
    /// the pass/fail decision). Applied identically to the golden and the produced output.</summary>
    private static string Normalize(string s)
    {
        var lines = s.ReplaceLineEndings("\n").Split('\n')
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(' '), "COMPUTED=  [0-9]*", "COMPUTED=  XXXXXXXXX"));
        return string.Join("\n", lines).TrimEnd('\n');
    }

    /// <summary>Walk up from the test assembly to the repository root (the directory holding <c>tests/nist</c>).</summary>
    private static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, "tests", "nist"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("repo root (with tests/nist) not found");
    }
}
