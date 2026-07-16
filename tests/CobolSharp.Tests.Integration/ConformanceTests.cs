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
        // ALLOCATE based-item INITIALIZED: the §14.9.3 GR7 lowering (INITIALIZE … WITH FILLER ALL TO VALUE
        // THEN TO DEFAULT) never landed in legacy — its CIL emitter only zero-fills (CilEmitter.cs), so the
        // GR7-conforming .out (VALUE members honored, numerics ZERO, edited zero, SPACES) is greenfield-only.
        ("2002", "allocate_initialized"),
        ("2002", "oo_factory"),         // the FACTORY paragraph (ISO §11.4) — net-new in the greenfield (DEVLOG 604)
        ("2002", "oo_factory_file"),    // FACTORY-paragraph FILE-CONTROL + FILE SECTION (M2-OO-1i inc 3) — net-new (DEVLOG 646)
        ("2002", "oo_object_file"),     // OBJECT-paragraph per-object file connector (M2-OO-1i inc 4) — net-new (DEVLOG 647)
        ("2002", "oo_object_file_two_instances"),   // per-object connector independence (M2-OO-1i inc 4) — net-new (DEVLOG 647)
        ("2002", "oo_external_file_shared"),   // EXTERNAL FD shared program<->object: one connector + record area (M2-OO-1i inc 5) — net-new (DEVLOG 648)
        ("2002", "oo_object_report"),   // REPORT SECTION in an OBJECT paragraph (M2-OO-1i review — Report Writer wired into the class path) — net-new (DEVLOG 649)
        ("2002", "oo_method_sort"),     // OBJECT-paragraph SD sorted by a method (M2-OO-1i review — SD keeps a static key) — net-new (DEVLOG 649)
        // Phase-6a floats (D16): the raw-float-DISPLAY goldens use CobolFloat.Display (invariant shortest round-trip,
        // §14.9.11 GR1 implementor-defined) — NOT the legacy oracle's form — and float→PIC rounding — so GreenfieldOnly.
        ("2002", "float_move"),         // float→float MOVE, raw-float DISPLAY (DEVLOG 650)
        ("2002", "float_neg"),          // negative/fractional literal → float, raw-float DISPLAY (DEVLOG 650)
        ("2002", "float_rounded"),      // float→PIC 9(2) truncate vs ROUNDED (DEVLOG 650)
        ("2002", "float_compare"),      // float vs fixed/int comparison (DEVLOG 650)
        ("2002", "float_literal"),      // floating-point exponent-form literals 1.5E3 (§8.3.3.3.3, DEVLOG 651)
        ("2002", "float_edited"),       // float source → numeric-edited receiver (DEVLOG 651)
        ("2002", "float_intrinsic"),    // SQRT/÷ into a float receiver, full binary64 precision (DEVLOG 651)
        ("2002", "float_88"),           // fractional level-88 VALUE on a float item (DEVLOG 651)
        ("2002", "oo_override_final"),  // METHOD-ID OVERRIDE/IS FINAL attributes (§11.7) — net-new (DEVLOG 605)
        // These three gained the SR4a-required OVERRIDE attribute (the strict §11.7 wave) — the frozen legacy
        // grammar cannot parse it; the greenfield CorpusRunnerTests byte-compares them:
        ("2002", "oo_inherit"),
        ("2002", "oo_super"),
        ("2002", "oo_self_polymorphic"),
        // The INTERFACE/PROPERTY wave (§11.5/§11.6/§13.18.42) — net-new in the greenfield (DEVLOG 606):
        // DISPLAY-OF/NATIONAL-OF on the greenfield substrate (P10 national wave): the legacy oracle carries
        // its OWN pass-through DISPLAY-OF approximation (no repertoire substitution) — never a match target.
        ("2002", "national_intrinsics"),
        // P10 Step-11 EC-N wave: the national EC twins EXCEPTION-FILE-N/EXCEPTION-LOCATION-N (§15.29/§15.31)
        // and CHAR-NATIONAL/ORD-over-national (§15.16/§15.70.4 r2) — the frozen legacy has no EC model and no
        // national result category; greenfield CorpusRunner byte-compares both.
        ("2002", "exception_file_n"),
        ("2002", "char_national"),
        // P10 Step 14: §8.8.3 concatenation expressions (the & operator) — the frozen legacy grammar has no
        // & token at all; greenfield CorpusRunner byte-compares the golden.
        ("2002", "literal_concat"),
        // P10 Step 15: §13.10 constant entries + §13.18.15 CONSTANT RECORD — the compile-time constant table
        // (DataBinder.Constants.cs) never landed in legacy; greenfield CorpusRunner byte-compares both.
        ("2002", "constant_entry"),
        ("2002", "constant_record"),
        // P10 Step 6: qualified/subscripted ADDRESS OF (§8.4.3.11 GR1 occurrence addressing) — the pointer
        // subsystem never landed in legacy; greenfield CorpusRunner byte-compares it.
        ("2002", "address_of_qualified"),
        // P10 Step 7: USAGE PROGRAM-POINTER (§13.18.60 GR24 — SET TO ENTRY / CALL-through-pointer /
        // relations on the ProgramPointer carrier) — never landed in legacy.
        ("2002", "program_pointer"),
        ("2002", "oo_interface"),
        ("2002", "oo_interface_conformance"),
        ("2002", "oo_class_env"),   // class-level env + object-own env both bind (DEVLOG-738 fix) — OBJECT-paragraph env never landed in legacy   // the §4.2.2 interface conformance leg (P9 Step 12) — interfaces never landed in legacy
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
        // The ANY LENGTH clause (§13.18.2, PHASE-09 Step 11) — never landed in the frozen legacy grammar; the
        // greenfield CorpusRunnerTests byte-compares all three legs (method / contained program / function):
        ("2002", "oo_any_length"),
        ("2002", "any_length_contained"),
        ("2002", "any_length_function"),
        // Phase-4 track (e), DEVLOG 611: ARITHMETIC IS STANDARD routes fixed-point through the standard
        // DECIMAL intermediate (§8.8.1.2/§8.8.1.4 — 2/7*7 = 2.00000); the FROZEN legacy engine does not
        // implement that routing (it gives the native-clipped 1.99997), so this is greenfield-only.
        ("2014", "options_paragraph"),
        // P7 Step 12 (FUNCTION-arg grammar): the golden pins §8.8.1.2 r3 — consecutive same-level operations
        // execute LEFT to right, INCLUDING ** (2 ** 3 ** 2 = 64). The frozen legacy's arithmetic parser folds
        // ** right-associatively (512) — a known legacy non-conformance; greenfield-only.
        ("2014", "func_expr_arg"),
        // Same family (P3 step 9): ARITHMETIC IS STANDARD-DECIMAL routes the fixed-point intermediate through the
        // standard DECIMAL data item (§8.8.1.4 — 2/7*7 = 2.00000); the frozen legacy engine doesn't implement it.
        ("2014", "arithmetic_standard_decimal"),
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
        // Phase 5 intrinsics, DEVLOG 632: FUNCTION CONVERT (§15.19) with the source-format/destination-format
        // keywords (ANY/ANUM/HEX/NAT/BYTE) is a 2023 construct the frozen legacy grammar cannot bind; the
        // greenfield CorpusRunner byte-compares intrinsics_convert.
        ("2023", "intrinsics_convert"),
        // Phase 5 intrinsics, DEVLOG 633: FUNCTION MODULE-NAME (§15.65) with the ACTIVATING/CURRENT/NESTED/
        // STACK/TOP-LEVEL keyword is a 2023 construct the frozen legacy grammar cannot bind; the greenfield
        // CorpusRunner byte-compares module_name (a CALL chain with a contained program).
        ("2023", "module_name"),
        // Phase 5 intrinsics, DEVLOG 634: FUNCTION HIGHEST-/LOWEST-ALGEBRAIC (§15.43/§15.58, 2002) and
        // SMALLEST-ALGEBRAIC (§15.83, 2023) are PICTURE-metadata folds the frozen legacy does not implement;
        // the greenfield CorpusRunner byte-compares them.
        ("2002", "highest_lowest_algebraic"),
        ("2023", "smallest_algebraic"),
        // Phase 5 intrinsics, DEVLOG 635: the COBOL-2014 date/time + number family (FORMATTED-*/COMBINED-DATETIME/
        // INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/TEST-FORMATTED-DATETIME/NUMVAL-F/TEST-NUMVAL-F,
        // §15.17/38-41/48/69/79/92/95) — the frozen legacy oracle predates them; greenfield CorpusRunner byte-compares.
        ("2014", "formatted_datetime"),
        // Phase-4 track (c), DEVLOG 615/616: the frozen legacy's partial UDF support runs the simple
        // invocation goldens but lacks EXIT FUNCTION's control transfer (§14.9.14 — it falls through to the
        // trailing MOVE, X=9999 not X=0014) and the nested-args legs (GR5a by-ref argument mutation /
        // intrinsic-in-UDF); the greenfield CorpusRunner byte-compares both.
        ("2002", "udf_exit_function"),
        ("2002", "udf_nested_args"),
        // Phase-4 track (c) residue, DEVLOG 624: FUNCTION-ID … IS PROTOTYPE (§11.5 Format 2) is a construct the
        // frozen legacy grammar cannot parse; the greenfield CorpusRunner byte-compares udf_prototype.
        ("2002", "udf_prototype"),
        // P10 Step 9 (DEVLOG 859): category-carrying FUNCTION-ID RETURNING (§8.4.3.2.4 GR1) — alphanumeric/
        // group/numeric-edited/national results; the frozen legacy's partial UDF support carries only numeric
        // results (and has no national category at all); the greenfield CorpusRunner byte-compares it.
        ("2002", "udf_returning_categories"),
        // Phase-4 track (c) residue, DEVLOG 626: the §8.4.3.2 SR2 FUNCTION-keyword-omitted reference form (via
        // REPOSITORY FUNCTION ALL INTRINSIC) is net-new in the greenfield — the frozen legacy treats MAX/MIN/MOD
        // without FUNCTION as undefined data-names (CBL3128); the greenfield CorpusRunner byte-compares it.
        ("2002", "udf_keyword_omitted"),
        // Phase-4 track (a) increment 2, DEVLOG 621: the boolean operators B-AND/B-OR/B-XOR/B-NOT (§8.7.2)
        // are net-new in the greenfield — the frozen legacy grammar has no boolean-expression support at all;
        // the greenfield CorpusRunner byte-compares boolean_ops.
        ("2002", "boolean_ops"),
        // M2-OO-1h step 1, DEVLOG 637: level-66 RENAMES in a METHOD's LOCAL-STORAGE — a greenfield method-scope
        // data-model reclamation (was staged COBOLNET0899); the frozen legacy predates it. Greenfield CorpusRunner
        // byte-compares oo_method_renames.
        ("2002", "oo_method_renames"),
        // M2-OO-1h step 2, DEVLOG 638: OCCURS … DEPENDING ON in a METHOD's LOCAL-STORAGE (method-scoped
        // data-name-1) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares oo_method_odo.
        ("2002", "oo_method_odo"),
        // M2-OO-1h step 3, DEVLOG 639: REDEFINES in METHOD data (scope-aware target; Tier-B string backing routed
        // static/method-local) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares both.
        ("2002", "oo_method_redefines_linkage"),
        ("2002", "oo_method_redefines_local"),
        ("2002", "oo_method_redefines_ws"),
        // M2-OO-1h step 4, DEVLOG 640: OCCURS … INDEXED BY in METHOD data (per-method index namespace; §11.7.4 GR5
        // privacy) — greenfield reclamation (was COBOLNET0899). Greenfield CorpusRunner byte-compares both.
        ("2002", "oo_method_indexed_search"),
        ("2002", "oo_method_indexed_two_methods"),
        // Phase-4 track (d), DEVLOG 623: the file-sharing / record-locking subsystem (SHARING / LOCK MODE /
        // RETRY / WITH LOCK / IGNORING LOCK / UNLOCK — §14.9.27/.30/.47) is net-new in the greenfield; the
        // frozen legacy binder has only the by-name CLOSE-WITH-LOCK/38 primitive and cannot bind these clauses.
        // The greenfield CorpusRunner byte-compares file_sharing (two connectors → 61/51/00 in one run unit).
        ("2002", "file_sharing"),
        // Phase 6, TYPEDEF / the TYPE clause (data-model D17, §13.18.58/§13.18.57): a type-declaration template +
        // subtree clone. The frozen legacy has no TYPEDEF model — greenfield CorpusRunner only.
        ("2002", "typedef_weak_elem"),
        ("2002", "typedef_weak_group"),
        ("2002", "typedef_strong_ok"),   // TYPEDEF inc 2: a STRONG type + same-type MOVE/compare (D17, §8.5.3.3) — net-new
        ("2002", "typedef_88"),          // TYPEDEF inc 3: level-88 condition-names cloned per TYPE reference (D17, §13.18.58.4 GR1) — net-new
        ("2002", "typedef_indexed"),     // TYPEDEF inc 4: a single INDEXED-BY type reference (D17, §13.18.38) — net-new
        ("2002", "typedef_nested_strong"), // TYPEDEF review (DEVLOG 664): nested strong types + same-type by nearest anchor — net-new
        ("2002", "typedef_odo"),         // TYPEDEF review (DEVLOG 664): cloned OCCURS DEPENDING binds the clone's own counter — net-new

        // Phase 6, OCCURS DYNAMIC increment 1 (data-model D9, §13.18.38 Format 4 / §8.5.1.9): the
        // dynamic-capacity table declaration + the growable CobolDynTable<T> storage substrate. The frozen
        // legacy binder/emitter has no dynamic-table model (it predates the D9 rewrite), so although the shared
        // grammar now parses OCCURS DYNAMIC, the legacy engine cannot emit it — greenfield CorpusRunner only.
        ("2014", "dyn_declare"),
        // Phase 6, OCCURS DYNAMIC increment 2 (data-model D9, §13.18.38 GR15 / §14.9.39): the CAPACITY register
        // (an implicit view over the table's current capacity) + SET Format 14 (TO / UP BY / DOWN BY). The frozen
        // legacy has no dynamic-table model — greenfield CorpusRunner only.
        ("2014", "dyn_capacity_read"),
        ("2014", "dyn_capacity_set"),
        ("2014", "dyn_capacity_bounds"),
        // Phase 6, OCCURS DYNAMIC increment 3 (data-model D9, §8.5.1.9.2/.9.3): subscripted element access — a
        // receiving subscript past capacity grows-and-seeds (RefReceiving), a sending OOB is benign (RefSending).
        // Greenfield CorpusRunner only.
        ("2014", "dyn_implicit_grow"),
        ("2014", "dyn_initialized"),
        // Phase 6, OCCURS DYNAMIC increment 4 (data-model D9, §14.9.37 / §14.9.20 GR10): SEARCH bounds over the
        // table's current Capacity (EnterSearch/ExitSearch bracket → EC-FLOW-SEARCH); INITIALIZE re-initializes
        // every occurrence up to Capacity with the statement's category defaults. Greenfield CorpusRunner only.
        ("2014", "dyn_search"),
        ("2014", "dyn_initialize"),
        // Phase 6, OCCURS DYNAMIC adversarial review (wf_3f05d472-ad8) regressions — data-model D9. Greenfield only.
        ("2014", "dyn_nested_group_move"),   // #1 a group MOVE nested below a dynamic level grows via RefReceiving
        ("2014", "dyn_corr"),                // #2 CORRESPONDING excludes a dynamic-table member (§14.7.6 rule 4)
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
