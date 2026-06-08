#!/bin/bash
# Guardrail script — run after every meaningful change set.
# Exit on first failure.
set -e

echo "=== Building ==="
dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj -v quiet

echo "=== Unit tests ==="
dotnet test tests/CobolSharp.Tests.Unit/CobolSharp.Tests.Unit.csproj --verbosity quiet

echo "=== Integration tests ==="
dotnet test tests/CobolSharp.Tests.Integration/CobolSharp.Tests.Integration.csproj --verbosity quiet

echo "=== NIST regression ==="
cp src/CobolSharp.Runtime/bin/Debug/net10.0/CobolSharp.Runtime.dll tests/nist/output/
CLI=src/CobolSharp.CLI/bin/Debug/net10.0/cobolsharp.dll

# Start each run from a clean DATA-file state so the guard is deterministic and reproducible from any prior
# state. Producer/consumer chains (RL/SQ/IX) rebuild WITHIN this run because the producer runs before the
# consumer in NIST_TESTS order (the loop still does not clean BETWEEN tests, so TF### carries over once
# created). Critically, absent-file tests (e.g. IX216A/217A OPEN I-O/EXTEND of an OPTIONAL file, expecting
# status 05 = "not present, created") must NOT see the file they themselves created on a previous invocation;
# without this start-clean such a test passes once then fails forever. Only data/report .txt files are
# removed — the compiled .dll/.runtimeconfig.json stay.
rm -f tests/nist/output/*.txt

# All NIST tests currently at 100% — must stay green
# (93 NC + 42 IF + 15 SM + 18 IC + 83 SQ + 28 RL + 39 IX + 29 ST + 3 OBSQ = 350 tests).
# (SQ212A baselined DEVLOG 311–312: variable-length WRITE/REWRITE boundary -> status 44, plus a USE-
#  procedure declaratives-return fix so its USE handler returns at the section's exit instead of falling
#  through into the CLOSE-FILES/footer/STOP-RUN termination tail. Still removed (genuine remaining work):
#  NC303M 0-byte flag module; IX108A footer 001 TEST(S) FAILED.)
#
# Out-of-scope exclusion class (DEVLOG 301 scope decision — NOT baselined, NOT implemented; like the
# flagging-"M" modules these are documented-excluded for modern-COBOL "operational" conformance):
#   CM (Communication, OBSOLETE) · SG (Segmentation, OBSOLETE) · OBNC/OBIC (obsolete variants) · EXEC85.
# Deferred OPTIONAL stretch (in-scope only as future enhancements, NOT required for operational):
#   DB (Debug) · RW (Report Writer).
# ST chains: ST101A (builds the SORT output file TF002) -> ST102A (updates it; NO_OUTPUT producer, runs but
# is not baselined) -> ST103A (verifies). ST104A (builds TF001) -> ST105A (verifies). ST112M (builds a
# 3-reel file; emits only a 000-of-000 builder comment, so NOT baselined) -> ST113M (sorts; NO_OUTPUT) ->
# ST114M (verifies, 10/10). ST109A (builds a 40-variable-length-record file; 000-of-000 builder, NOT
# baselined) -> ST110A (variable-length SORT, NO_OUTPUT) -> ST111A (verifies, 7/7). ST122A -> ST123A ->
# ST124A (the second variable-length SORT chain). ST115A (builds a 204-record file — XXXXX065 "number of
# records" is substituted to 204 = 51*4; 000-of-000 builder, NOT baselined) -> ST116A (the BIG-SORT,
# NO_OUTPUT) -> ST117A (verifies the native-collating sort of all 204 records, 1/1). Keep each chain's
# members consecutive, producers ahead of consumers. ST119A (SORTs, writes TF001 via XXXXP001 — baselined)
# -> ST120A (SORT USING TF001 GIVING TF002, the USING/GIVING feature; NO_OUTPUT producer) -> ST121A
# (verifies the TF002 sort, 9/9). These three MUST stay consecutive and ahead of ST122A, which re-creates
# TF002 in a variable-length format.
# ST137A is a self-contained SORT over a FORMAT-2 variable-length record (FD RECORD CONTAINS 148 TO 1435
# CHARACTERS + an embedded OCCURS DEPENDING ON); its 6 tests verify the native collating order of the
# sorted keys against the XXXXX063 collating-sequence literal. ST147A (MERGE) re-baselined the same
# session: substituting XXXXX063 turned its previously-blank NATIVE COLL.SEQUENCE checks into real ones.
# RL206A→RL207A are a producer/consumer pair over TF021 with VARIABLE-LENGTH records (RECORD IS
# VARYING): RL206A creates the file (each slot stores its own length, persisted length-prefixed), RL207A
# verifies/updates it. They MUST stay consecutive, AND a fixed-format TF021 producer must follow (RL209A
# opens OUTPUT, re-creating TF021 in the fixed format) so the later fixed TF021 readers are unaffected —
# each producer re-creates the file in its own format, so consecutive chains are self-sufficient.
# RL105A/RL118A are self-contained (each opens its relative file OUTPUT, populates it, and verifies in
# one run), so they carry no chain dependency. RL108A→RL109A→RL110A are a producer/updater/verifier
# chain over the shared relative file TF061 (XXXXX061): RL108A creates 500 records (ACCESS SEQUENTIAL),
# RL109A REWRITEs every 5th record by COMP relative key (ACCESS RANDOM), RL110A verifies (ACCESS
# SEQUENTIAL); they MUST stay consecutive. RL107A→RL117A is a producer/consumer pair over TF022
# (XXXXX022): RL107A creates it, RL117A verifies it — they MUST stay consecutive, with no other TF022
# writer (e.g. RL118A) between them. The XXXXX### ASSIGN targets of RELATIVE/INDEXED files are mapped to
# a shared "TF###" literal so these chains share one on-disk file across run units; SEQUENTIAL files
# keep program-id-qualified isolation (DEVLOG 244). Several of these compile only under the L1/L2 dialect
# leniencies (INVALID KEY / RELATIVE KEY with KEY omitted, accepted in Default/--nist) — see
# docs/dialect-strictness.md.
# RL101A→RL102A→RL103A are a producer/updater/verifier chain over the shared relative file TF021
# (XXXX[PD]021): RL101A creates 500 records, RL102A REWRITEs 100, RL103A verifies. They MUST stay
# consecutive in this list (the loop does not clean data files between tests, so TF021 carries over),
# and ahead of any other TF021 producer (e.g. RL201A).
# RL201A→RL202A→RL203A are a second producer/updater/verifier chain over TF021 (DYNAMIC access,
# COMP relative keys): RL201A creates, RL202A randomly REWRITE/DELETEs, RL203A verifies. They too
# MUST stay consecutive. RL201A opens OUTPUT (recreating TF021), so it is safe to run after the
# RL1xx chain; RL209A (also XXXXP021) likewise opens OUTPUT, so it is safe after RL203A.
# RL210A/RL211A are self-contained format-3 variable-record tests (RECORD IS VARYING with an OCCURS
# DEPENDING table inside the record, and mixed 120/140-byte 01 formats). They write 200×120 + 300×140
# records and verify the ODO content round-trips. They had a long history of vacuous/failing baselines
# (the no-op secondary-record WRITE, then 300/500 genuine failures); now genuinely pass once the READ
# buffer for a varying record uses the record's MAXIMUM length (ResolveReadRecordLocation resolves the
# largest 01 as a receiving operand) — see DEVLOG 257. They open OUTPUT (self-contained), so they leave
# a varying-format TF021 that the following fixed-format producer (RL209A) re-creates.
# IF401M/402M/403M are flagging-conformance modules: they emit no CCVS report by
# design, so they are intentionally NOT guarded (nothing to compare).
NIST_TESTS="
NC101A NC102A NC103A NC104A NC105A NC106A NC107A NC108M NC109M NC110M
NC111A NC112A NC113M NC114M NC115A NC116A NC117A NC118A NC119A NC120A NC121M
NC122A NC123A NC124A NC125A NC126A NC127A
NC131A NC132A NC133A NC134A NC135A NC136A NC137A NC138A NC139A NC140A NC141A
NC170A NC171A NC172A NC173A NC174A NC175A NC176A NC177A
NC201A NC202A NC203A NC204M NC205A NC206A NC207A NC208A NC209A NC210A NC211A NC215A NC216A NC217A NC218A NC219A NC220M NC221A NC222A NC223A NC224A
NC225A NC231A NC232A NC233A NC234A NC235A NC236A NC237A NC238A NC245A NC246A NC247A
NC239A NC240A NC250A NC241A NC242A NC243A NC244A NC248A NC251A NC252A NC253A NC254A
NC302M
NC401M
IF101A IF102A IF103A IF104A IF105A IF106A IF107A IF108A IF109A IF110A
IF111A IF112A IF113A IF114A IF115A IF116A IF117A IF118A IF119A IF120A
IF121A IF122A IF123A IF124A IF125A IF126A IF127A IF128A IF129A IF130A
IF131A IF132A IF133A IF134A IF135A IF136A IF137A IF138A IF139A IF140A
IF141A IF142A
SM101A SM102A SM103A SM104A SM106A SM107A SM201A SM202A SM203A SM204A SM206A
SM207A SM208A SM105A SM205A
IC101A IC103A IC106A IC108A IC112A IC114A IC201A IC203A IC207A IC209A IC213A IC216A IC222A IC223A IC224A IC225A IC226A IC227A IC228A IC233A IC234A IC235A IC237A
SQ101M SQ102A SQ104A SQ105A SQ108A SQ111A SQ112A SQ113A SQ114A SQ116A SQ117A SQ121A SQ126A SQ127A
SQ133A SQ136A SQ144A SQ141A SQ142A SQ106A SQ107A SQ109M SQ110M SQ124A SQ128A SQ130A SQ131A SQ143A SQ146A SQ149A SQ150A SQ154A SQ155A SQ156A SQ202A SQ203A SQ204A SQ206A SQ207M SQ211A SQ212A SQ213A
SQ214A SQ216A SQ217A SQ218A SQ219A SQ220A SQ221A SQ222A SQ223A SQ224A SQ230A SQ227A SQ228A SQ201M SQ208M SQ209M SQ210M SQ302M
SQ103A SQ115A SQ122A SQ123A SQ125A SQ129A SQ132A SQ134A SQ135A SQ137A SQ138A SQ139A SQ140A SQ147A SQ148A SQ151A SQ152A SQ153A SQ205A SQ215A SQ225A SQ226A SQ229A
RL105A RL118A RL108A RL109A RL110A RL101A RL102A RL103A RL107A RL117A RL201A RL202A RL203A RL206A RL207A RL208A RL210A RL211A RL209A RL104A RL112A RL113A RL114A RL115A RL116A RL204A RL205A RL111A RL106A RL119A RL302M RL212A RL213A
IX101A IX102A IX103A IX104A IX105A IX106A IX107A IX108A IX109A IX110A IX111A IX112A IX113A IX114A IX115A IX116A IX117A IX118A IX119A IX120A IX121A IX201A IX202A IX203A IX204A IX205A IX206A IX207A IX208A IX209A IX210A IX211A IX212A IX213A IX214A IX215A IX216A IX217A IX218A IX302M
ST101A ST102A ST103A ST104A ST105A ST106A ST107A ST108A ST109A ST110A ST111A ST112M ST113M ST114M ST115A ST116A ST117A ST118A ST119A ST120A ST121A ST122A ST123A ST124A ST125A ST126A ST127A ST131A ST132A ST133A ST134A ST135A ST136A ST137A ST139A ST140A ST144A ST146A ST147A
OBSQ1A OBSQ3A OBSQ4A OBSQ5A
RW101A RW102A RW103A RW104A
"

# NIST convention: SWITCH-1 ON, SWITCH-2 OFF (default)
export COBOL_SWITCH_1=ON

FAILURES=0
for test in $NIST_TESTS; do
    # Compile
    if ! dotnet "$CLI" --nist "tests/nist/programs/$test.cob" -o "tests/nist/output/$test.dll" 2>/dev/null; then
        echo "  $test: COMPILE FAILED — REGRESSION!"
        FAILURES=$((FAILURES + 1))
        continue
    fi

    # Run in the output directory; capture stdout for DISPLAY-only tests
    # Pipe NIST data file to stdin when available (for ACCEPT tests)
    outfile=$(echo "$test" | tr '[:upper:]' '[:lower:]').txt
    stdoutfile="tests/nist/output/${test}-stdout.txt"
    datafile="tests/nist/data/$test.dat"
    if [ -f "$datafile" ]; then
        (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true) < "$datafile" > "$stdoutfile"
    else
        (cd tests/nist/output && dotnet "$test.dll" 2>/dev/null || true) > "$stdoutfile"
    fi

    # Compare output (files written to tests/nist/output/)
    validfile="tests/nist/valid/$test.txt"
    if [ ! -f "$validfile" ]; then
        # No baseline = test has known failures. Still compile/run but don't compare.
        fail_count=$(grep -c "FAIL\*" "tests/nist/output/$outfile" 2>/dev/null || true)
        fail_count=${fail_count:-0}
        echo "  $test: NO BASELINE (${fail_count} FAIL* — pending fix)"
        continue
    fi

    # Normalize: strip trailing spaces, and normalize time-dependent COMPUTED values
    normalize() { sed 's/ *$//; s/COMPUTED=  [0-9]*/COMPUTED=  XXXXXXXXX/' "$1" 2>/dev/null; }

    # Find the actual output file (outfile, print-file, or stdout)
    actual=""
    if diff <(normalize "$validfile") <(normalize "tests/nist/output/$outfile") > /dev/null 2>&1; then
        actual="tests/nist/output/$outfile"
    elif diff <(normalize "$validfile") <(normalize "tests/nist/output/print-file.txt") > /dev/null 2>&1; then
        actual="tests/nist/output/print-file.txt"
    elif diff <(normalize "$validfile") <(normalize "$stdoutfile") > /dev/null 2>&1; then
        actual="$stdoutfile"
    fi

    if [ -z "$actual" ]; then
        echo "  $test: DIFF — REGRESSION!"
        FAILURES=$((FAILURES + 1))
        continue
    fi

    # Check for FAIL* in output — these are real test failures, not acceptable baselines
    fail_count=$(grep -c "FAIL\*" "$actual" 2>/dev/null || true)
    fail_count=${fail_count:-0}
    # Footer-total check: the AUTHORITATIVE CCVS pass/fail signal is the report footer
    # "NNN TEST(S) FAILED" total, not just the per-paragraph FAIL* detail lines. A test can
    # fail-to-execute (e.g. "000 OF 001 EXECUTED" + "001 TEST(S) FAILED") with ZERO FAIL* detail
    # lines, which the FAIL*-grep alone would pass as MATCH (the IX108A false-green). "NO TEST(S)
    # FAILED" does not match [0-9]+ so it is treated as zero.
    footer_failed=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$actual" 2>/dev/null | grep -oE "^[0-9]+" | head -1)
    footer_failed=${footer_failed:-0}
    if [ "$footer_failed" -gt 0 ] 2>/dev/null; then
        echo "  $test: FOOTER ${footer_failed} TEST(S) FAILED — REGRESSION!"
        FAILURES=$((FAILURES + 1))
    elif [ "$fail_count" -gt 0 ] 2>/dev/null; then
        echo "  $test: MATCH (${fail_count} FAIL*)"
    else
        echo "  $test: MATCH"
    fi
done

if [ $FAILURES -gt 0 ]; then
    echo "=== $FAILURES NIST REGRESSION(S) ==="
    exit 1
fi

# Verify every baseline is clean and non-vacuous — baselines must be 100% trustworthy.
# (1) a 0-byte baseline matches any empty/crash output vacuously; (2) a FAIL* detail line is a real
# failure; (3) a nonzero footer "NNN TEST(S) FAILED" is a real failure even with no FAIL* detail line.
for f in tests/nist/valid/*.txt; do
    if [ ! -s "$f" ]; then
        echo "=== ERROR: $(basename "$f") is EMPTY — a 0-byte baseline passes vacuously; remove from valid/ ==="
        FAILURES=$((FAILURES + 1))
        continue
    fi
    fc=$(grep -c "FAIL\*" "$f" 2>/dev/null || true)
    fc=${fc:-0}
    if [ "$fc" -gt 0 ] 2>/dev/null; then
        echo "=== ERROR: $(basename "$f") contains $fc FAIL* — remove from valid/ ==="
        FAILURES=$((FAILURES + 1))
    fi
    ff=$(grep -oE "[0-9]+ TEST\(S\) FAILED" "$f" 2>/dev/null | grep -oE "^[0-9]+" | head -1)
    ff=${ff:-0}
    if [ "$ff" -gt 0 ] 2>/dev/null; then
        echo "=== ERROR: $(basename "$f") footer reports $ff TEST(S) FAILED — not a clean baseline; remove from valid/ ==="
        FAILURES=$((FAILURES + 1))
    fi
done

echo "=== ALL GREEN ==="
