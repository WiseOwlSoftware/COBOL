using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Version-conformance corpus runner — the NIST-equivalent suite for the post-1985 standards (2002/2014/2023).
/// Auto-discovers every <c>tests/conformance/&lt;version&gt;/*.cob</c> program that has a sibling <c>.out</c>
/// (expected stdout), compiles it under that version's <c>--standard</c> dialect, runs it, and asserts the
/// output. Adding a conformance test is just dropping a <c>.cob</c> + <c>.out</c> in the right version directory
/// — no test code changes. See tests/conformance/README.md. Runs as part of scripts/guard.sh.
/// </summary>
public sealed class ConformanceTests : EndToEndTestBase
{
    private static readonly string ConformanceRoot = Path.GetFullPath(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "tests", "conformance"));

    private static readonly (string Dir, DialectMode Dialect)[] Versions =
    {
        ("2002", DialectMode.Cobol2002),
        ("2014", DialectMode.Cobol2014),
        ("2023", DialectMode.Cobol2023),
    };

    /// <summary>Discover (version, program-name) for every conformance program with an expected-output file.</summary>
    public static IEnumerable<object[]> Cases()
    {
        foreach (var (dir, _) in Versions)
        {
            string versionDir = Path.Combine(ConformanceRoot, dir);
            if (!Directory.Exists(versionDir)) continue;
            foreach (string cob in Directory.GetFiles(versionDir, "*.cob").OrderBy(p => p, StringComparer.Ordinal))
            {
                if (File.Exists(Path.ChangeExtension(cob, ".out")))
                    yield return new object[] { dir, Path.GetFileNameWithoutExtension(cob) };
            }
        }
    }

    /// <summary>Programs whose .out files are ISO-CONFORMING baselines the LEGACY engine cannot reproduce —
    /// the same divergence protocol as scripts/guard.sh's LEGACY_DIVERGENT list (per-program ISO citations).
    /// Both entries are the legacy DISPLAY trailing-space TRIM (non-conforming per ISO §14.9.11.4 GR6 — the
    /// greenfield emits the full field; the .out files were re-baselined at the W3 corpus audit, DEVLOG 597):
    /// the trimmed spaces sit INTERIOR to the expected lines ("DF=[   ]…"), so no whitespace normalization can
    /// bridge them. The greenfield CorpusRunnerTests asserts these baselines; the legacy runner SKIPS the
    /// output comparison (compile+run still asserted) until the G8 cut-over retires it.</summary>
    private static readonly HashSet<(string, string)> LegacyDivergent =
    [
        ("2002", "initialize_phrases"),
        ("2002", "table_value_occurs"),
        // Phase-4 track (a), M2-DATA-3: the .out was re-baselined to FULL-WIDTH DISPLAY (trailing national
        // spaces shown — PAD=P·· etc.) per the same §14.9.11.4 GR6 posture; the legacy's trailing-trim
        // cannot reproduce it. (boolean_data needs no entry — its values are exact-width, no trim exposure.)
        ("2002", "national_data"),
    ];

    /// <summary>Programs exercising features the FROZEN legacy engine never implemented (the OO deep-dive's
    /// "Never landed in legacy" list): greenfield-ONLY coverage — CorpusRunnerTests compiles, runs, and
    /// byte-compares them under the 2002 manifest run contract; the legacy runner skips them ENTIRELY (the
    /// legacy is kept only as a differential oracle until the G8 cut-over — no new legacy features, the
    /// DEVLOG-457 owner directive).</summary>
    private static readonly HashSet<(string, string)> GreenfieldOnly =
    [
        ("2002", "oo_factory"),         // the FACTORY paragraph (ISO §11.4) — net-new in the greenfield (DEVLOG 604)
        ("2002", "oo_override_final"),  // METHOD-ID OVERRIDE/IS FINAL attributes (§11.7) — net-new (DEVLOG 605)
        // These three gained the SR4a-required OVERRIDE attribute (the strict §11.7 wave) — the frozen legacy
        // grammar cannot parse it; the greenfield CorpusRunnerTests byte-compares them:
        ("2002", "oo_inherit"),
        ("2002", "oo_super"),
        ("2002", "oo_self_polymorphic"),
        // The INTERFACE/PROPERTY wave (§11.5/§11.6/§13.18.42) — net-new in the greenfield (DEVLOG 606):
        ("2002", "oo_interface"),
        ("2002", "oo_interface_covariant"),
        ("2002", "oo_property"),
        ("2002", "oo_property_methods"),
        // The property-REFERENCE increment (§8.4.3.9.4 GR1–GR3 desugar) — net-new (DEVLOG 607):
        ("2002", "oo_property_ref"),
        ("2002", "oo_property_explicit_ref"),
        ("2002", "oo_property_factory_ref"),
        // The UNIVERSAL wave (D10, §13.18.60.4/§14.9.23 GR7c) — net-new (DEVLOG 608):
        ("2002", "oo_universal"),
        ("2002", "oo_universal_name"),
        ("2002", "oo_universal_inherit"),
        ("2002", "oo_universal_relation"),
        // The EC-OO wave (§14.9.29 RAISE identifier / §14.9.49 F4 / §14.6.13.1.5) — net-new (DEVLOG 609):
        ("2002", "oo_ec_raise_object"),
        ("2002", "oo_ec_goback_raising"),
        // Phase-4 track (e), DEVLOG 611: ARITHMETIC IS STANDARD routes fixed-point through the standard
        // DECIMAL intermediate (§8.8.1.2/§8.8.1.4 — 2/7*7 = 2.00000); the FROZEN legacy engine does not
        // implement that routing (it gives the native-clipped 1.99997), so this is greenfield-only.
        ("2014", "options_paragraph"),
        // Phase-4 track (d), DEVLOG 612: DELETE FILE (§14.9.10 Format 2) is a 2023 construct the frozen
        // legacy grammar cannot parse; the greenfield CorpusRunner byte-compares the sequential leg.
        ("2023", "delete_file"),
        ("2023", "delete_file_absent"),
        // Phase 5 intrinsics, DEVLOG 628: the 2023 CONCAT (§15.18) + BASECONVERT (§15.12) intrinsics — the frozen
        // legacy has CONCAT but crashes on BASECONVERT (InvalidCastException); the greenfield CorpusRunner
        // byte-compares intrinsics_string_2023.
        ("2023", "intrinsics_string_2023"),
        // Phase 5 intrinsics, DEVLOG 629: FUNCTION TRIM (§15.96) with the 2023 argument-2 char-set form — the
        // frozen legacy trims only spaces (ZERO=0042, ignoring the "0" arg-2) where the greenfield gives ZERO=42;
        // the greenfield CorpusRunner byte-compares intrinsics_trim.
        ("2023", "intrinsics_trim"),
        // Phase 5 intrinsics, DEVLOG 630: FUNCTION FIND-STRING (§15.37) with the LAST / START AFTER / ANYCASE
        // phrase keywords is a 2023 construct the frozen legacy grammar cannot bind; the greenfield CorpusRunner
        // byte-compares find_string.
        ("2023", "find_string"),
        // Phase 5 intrinsics, DEVLOG 631: FUNCTION SUBSTITUTE (§15.87) with per-pair ANYCASE/FIRST/LAST phrase
        // keywords is a 2023 construct the frozen legacy grammar cannot bind; the greenfield CorpusRunner
        // byte-compares substitute.
        ("2023", "substitute"),
        // Phase-4 track (c), DEVLOG 615/616: the frozen legacy's partial UDF support runs the simple
        // invocation goldens but lacks EXIT FUNCTION's control transfer (§14.9.14 — it falls through to the
        // trailing MOVE, X=9999 not X=0014) and the nested-args legs (GR5a by-ref argument mutation /
        // intrinsic-in-UDF); the greenfield CorpusRunner byte-compares both.
        ("2002", "udf_exit_function"),
        ("2002", "udf_nested_args"),
        // Phase-4 track (c) residue, DEVLOG 624: FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) is a construct the
        // frozen legacy grammar cannot parse; the greenfield CorpusRunner byte-compares udf_prototype.
        ("2002", "udf_prototype"),
        // Phase-4 track (c) residue, DEVLOG 626: the §8.4.3.2 SR2 FUNCTION-keyword-omitted reference form (via
        // REPOSITORY FUNCTION ALL INTRINSIC) is net-new in the greenfield — the frozen legacy treats MAX/MIN/MOD
        // without FUNCTION as undefined data-names (CBL3128); the greenfield CorpusRunner byte-compares it.
        ("2002", "udf_keyword_omitted"),
        // Phase-4 track (a) increment 2, DEVLOG 621: the boolean operators B-AND/B-OR/B-XOR/B-NOT (§8.7.2)
        // are net-new in the greenfield — the frozen legacy grammar has no boolean-expression support at all;
        // the greenfield CorpusRunner byte-compares boolean_ops.
        ("2002", "boolean_ops"),
        // Phase-4 track (d), DEVLOG 623: the file-sharing / record-locking subsystem (SHARING / LOCK MODE /
        // RETRY / WITH LOCK / IGNORING LOCK / UNLOCK — §14.9.27/.30/.47) is net-new in the greenfield; the
        // frozen legacy binder has only the by-name CLOSE-WITH-LOCK/38 primitive and cannot bind these clauses.
        // The greenfield CorpusRunner byte-compares file_sharing (two connectors → 61/51/00 in one run unit).
        ("2002", "file_sharing"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Conformance(string version, string name)
    {
        if (GreenfieldOnly.Contains((version, name))) return;   // never landed in legacy — greenfield coverage only
        DialectMode dialect = Versions.First(v => v.Dir == version).Dialect;
        string source = File.ReadAllText(Path.Combine(ConformanceRoot, version, name + ".cob"));
        string expected = Normalize(File.ReadAllText(Path.Combine(ConformanceRoot, version, name + ".out")));

        var (ok, stdout, stderr) = CompileAndRun(source, dialect);
        Assert.True(ok, $"[conformance {version}/{name}] compile/run failed:\n{stderr}");
        if (LegacyDivergent.Contains((version, name))) return;   // ISO-adjudicated divergence — see the list
        Assert.Equal(expected, Normalize(stdout));
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
}
