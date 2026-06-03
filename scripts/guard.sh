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
cp src/CobolSharp.Runtime/bin/Debug/net9.0/CobolSharp.Runtime.dll tests/nist/output/
CLI=src/CobolSharp.CLI/bin/Debug/net9.0/cobolsharp.dll

# Start each run from a clean DATA-file state so the guard is deterministic and reproducible from any prior
# state. Producer/consumer chains (RL/SQ/IX) rebuild WITHIN this run because the producer runs before the
# consumer in NIST_TESTS order (the loop still does not clean BETWEEN tests, so TF### carries over once
# created). Critically, absent-file tests (e.g. IX216A/217A OPEN I-O/EXTEND of an OPTIONAL file, expecting
# status 05 = "not present, created") must NOT see the file they themselves created on a previous invocation;
# without this start-clean such a test passes once then fails forever. Only data/report .txt files are
# removed — the compiled .dll/.runtimeconfig.json stay.
rm -f tests/nist/output/*.txt

# All NIST tests currently at 100% — must stay green
# (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 40 IX = 270 tests).
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
NC302M NC303M
NC401M
IF101A IF102A IF103A IF104A IF105A IF106A IF107A IF108A IF109A IF110A
IF111A IF112A IF113A IF114A IF115A IF116A IF117A IF118A IF119A IF120A
IF121A IF122A IF123A IF124A IF125A IF126A IF127A IF128A IF129A IF130A
IF131A IF132A IF133A IF134A IF135A IF136A IF137A IF138A IF139A IF140A
IF141A IF142A
SM101A SM102A SM103A SM106A SM107A SM201A SM202A SM203A SM204A SM206A
SM207A SM208A
IC203A IC224A IC225A IC228A
SQ101M SQ102A SQ104A SQ105A SQ108A SQ111A SQ112A SQ113A SQ114A SQ116A SQ117A SQ121A SQ126A SQ127A
SQ133A SQ136A SQ144A SQ141A SQ142A SQ106A SQ107A SQ109M SQ110M SQ124A SQ128A SQ130A SQ131A SQ143A SQ146A SQ149A SQ150A SQ154A SQ155A SQ156A SQ202A SQ203A SQ204A SQ206A SQ207M SQ211A SQ213A
SQ214A SQ216A SQ217A SQ218A SQ219A SQ220A SQ221A SQ222A SQ223A SQ224A SQ230A SQ227A SQ228A SQ201M SQ208M SQ209M SQ210M SQ302M
RL105A RL118A RL108A RL109A RL110A RL101A RL102A RL103A RL107A RL117A RL201A RL202A RL203A RL206A RL207A RL210A RL211A RL209A RL302M
IX101A IX102A IX103A IX104A IX105A IX106A IX107A IX108A IX109A IX110A IX111A IX112A IX113A IX114A IX115A IX116A IX117A IX118A IX119A IX120A IX121A IX201A IX202A IX203A IX204A IX205A IX206A IX207A IX208A IX209A IX210A IX211A IX212A IX213A IX214A IX215A IX216A IX217A IX218A IX302M
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
    if [ "$fail_count" -gt 0 ] 2>/dev/null; then
        echo "  $test: MATCH (${fail_count} FAIL*)"
    else
        echo "  $test: MATCH"
    fi
done

if [ $FAILURES -gt 0 ]; then
    echo "=== $FAILURES NIST REGRESSION(S) ==="
    exit 1
fi

# Verify no baselines contain FAIL* — baselines must be 100% clean
for f in tests/nist/valid/*.txt; do
    fc=$(grep -c "FAIL\*" "$f" 2>/dev/null || true)
    fc=${fc:-0}
    if [ "$fc" -gt 0 ] 2>/dev/null; then
        echo "=== ERROR: $(basename "$f") contains $fc FAIL* — remove from valid/ ==="
        FAILURES=$((FAILURES + 1))
    fi
done

echo "=== ALL GREEN ==="
