#!/usr/bin/env bash
# INV-1 CONTINUITY SWEEP (VERSION_TEST_MATRIX_DESIGN.md §3; RESTATED at the P2.7 flip — the §10 #1 migration
# posture): every NIST CCVS-85 program that COMPILES at --std 85 must still compile at 2002 / 2014 / 2023
# **UNDER --permissive** — under strict, later editions legitimately REJECT the removed '85 elements every
# NIST program carries (LABEL RECORDS in every FD; gate live since DEVLOG 588), and every strict failure must
# trace to a recognized edition-band diagnostic code. A PERMISSIVE break is a REGRESSION, full stop.
#
# SPEED (DEVLOG 627): the sweep asks only "does this program COMPILE at edition X" — a verdict fully settled in
# the greenfield parse + edition-validate + bind/emit stage, BEFORE the Roslyn backend. So instead of ~1400 cold
# `dotnet` FULL compiles (~350 programs × 4 editions, the former ~29-min CI pole), it emits ONE manifest and runs
# `cobol check-batch` once: a single warm process that parse+bind-checks every (program, edition) entry IN
# PARALLEL with NO Roslyn emit. (Backend/C#-type errors are out of scope for INV-1 — they are not an
# edition-continuity concern and are covered by the greenfield conformance suite.)
#
# Usage: scripts/version-continuity-sweep.sh [parallelism]   (the arg is accepted for back-compat but the
#        in-process check-batch now parallelises across all cores itself).
# Output: one line per 85-compiling program — "NAME OK" or "NAME BREAKS@<edition>[,<edition>…]";
#         programs that do not compile at 85 are listed as "NAME SKIP85" (not yet in the witness set).
set -u
# Run from the repo root and use RELATIVE source paths: the .NET check-batch process resolves them against its
# cwd, so the manifest is portable across Linux CI and a Windows/git-bash checkout (where an absolute MSYS path
# like /e/... is not a path the .NET runtime understands).
cd "$(dirname "$0")/.." || exit 1
CLI="src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll"

MANIFEST="$(mktemp)"
RESULTS="$(mktemp)"
trap 'rm -f "$MANIFEST" "$RESULTS"' EXIT

# Build the manifest: the 85 baseline (strict) + 2002/2014/2023 (permissive) for every program.
# Line format: <source>\t<std>\t<nist-name>\t<permissive 0|1>  (source RELATIVE to the repo root).
for SRC in tests/nist/programs/*.cob; do
  NAME="$(basename "$SRC" .cob)"
  printf '%s\t85\t%s\t0\n' "$SRC" "$NAME" >> "$MANIFEST"
  for ED in 2002 2014 2023; do
    printf '%s\t%s\t%s\t1\n' "$SRC" "$ED" "$NAME" >> "$MANIFEST"
  done
done

# One warm process parse+bind-checks the whole manifest in parallel (no Roslyn emit).
dotnet "$CLI" check-batch "$MANIFEST" > "$RESULTS"

# Aggregate the per-(source,std) PASS/FAIL verdicts into the per-program verdict:
#   85 FAIL            -> SKIP85 (not in the 85 witness set)
#   any later-ed FAIL  -> BREAKS@<eds>
#   else               -> OK
awk -F'\t' '
  { name=$1; sub(/.*\//,"",name); sub(/\.cob$/,"",name); seen[name]=1
    if ($2=="85") p85[name]=$3
    else if ($3=="FAIL") brk[name]=brk[name] "," $2 }
  END { for (n in seen) {
          if (p85[n]=="FAIL") print n " SKIP85"
          else if (n in brk) { b=brk[n]; sub(/^,/,"",b); print n " BREAKS@" b }
          else print n " OK" } }' "$RESULTS" | sort
