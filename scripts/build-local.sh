#!/usr/bin/env bash
# build-local.sh — the PER-COMMIT wave-local gate, as one command (kb/Work PB108; plan §0 "Gates"):
#   build the SOLUTION (Debug — the same binaries battery.sh PHASE 0 builds; a stale test-bin compiler DLL hides
#   regressions, so no --no-build leg ever runs on an unbuilt tree), then Conformance filtered on the SUBJECT of the
#   change, full Unit (~2 min), Characterization. NOT the comprehensive gate (battery.sh), NOT guard-fast.sh, NOT
#   Release (the Windows CI leg). ⛔ The filter is REQUIRED: the wave-local gate is a filter chosen from what the
#   change TOUCHES ("~Arithmetic|~Inspect"; add "~VersionMatrix" for an edition gate) — a default would re-create the
#   PB36 mistake of filtering on where the new goldens sat. Shorthand terms ("~X|~Y") are expanded to
#   FullyQualifiedName~X|FullyQualifiedName~Y — vstest silently matches NOTHING for a bare "~X" (and exits 0), so a
#   leg with NO verdict line is RED here, never green by absence.
# Usage:  bash scripts/build-local.sh "<filter>"        e.g.  bash scripts/build-local.sh "~Collation|~Locale"
set -u
F="${1:-}"
if [ -z "$F" ]; then echo "usage: $0 \"<conformance filter, e.g. ~Area|~Other>\" — the wave-local gate needs the SUBJECT of the change" >&2; exit 2; fi
cd "$(dirname "$0")/.."
F="$(printf '%s' "$F" | sed -E 's/(^|[|&(])(!=|=|~)/\1FullyQualifiedName\2/g')"
RC=0
# ⛔ THE CITATION AUDITS RUN FIRST, BEFORE THE BUILD — they are a second and cost nothing, and a wrong § is the
# one defect class no test can ever catch (CLAUDE.md rule 1: the failure mode is INHERITING a clause number).
# `audit_code_citations` gates on three checks (clause vs the CONSTRUCT the comment is about) and
# `audit_doc_citations` on one (a QUOTED fragment vs the clause it is filed under); both have a proven-zero
# baseline and a `--self-test` proving each check still fails on a real defect. They need `specs/ISO_COBOL.md`
# for the phantom check and say so loudly when the submodule is absent — which is why they live HERE and in
# battery.sh, and not in CI, where the checkout is `submodules: false`.
python scripts/spec/audit_code_citations.py --check || { echo "=== CITATIONS: RED (see above) ==="; RC=1; }
python scripts/spec/audit_doc_citations.py --check || { echo "=== DOC CITATIONS: RED (see above) ==="; RC=1; }
dotnet build CobolSharp.sln -v quiet || { echo "=== WAVE-LOCAL GATE: BUILD FAILED ==="; exit 1; }
leg() {   # leg <name> <dotnet test args…> — the verdict is the Passed!/Failed! line; none = the filter matched nothing = RED
    local name="$1"; shift
    local out; out="$(dotnet test "$@" 2>&1)"; local rc=$?
    printf '%s\n' "$out" | grep -E "^(Passed!|Failed!)|error|\[FAIL\]" | tail -20
    local v; v="$(printf '%s\n' "$out" | grep -E "^(Passed!|Failed!)" | tail -1)"
    if [ -z "$v" ]; then echo "$name: NO VERDICT LINE — the filter matched no test (a run must assert its population)"; rc=1; fi
    [ "$rc" -eq 0 ] || RC=1
}
leg conformance      tests/Cobol.Net.Tests.Conformance --no-build --filter "$F"
leg unit             tests/Cobol.Net.Tests.Unit --no-build
leg characterization tests/Cobol.Net.Tests.Characterization --no-build
[ "$RC" -eq 0 ] && echo "=== WAVE-LOCAL GATE: GREEN (filter $F) ===" || echo "=== WAVE-LOCAL GATE: RED (filter $F) ==="
exit $RC
