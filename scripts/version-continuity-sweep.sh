#!/usr/bin/env bash
# INV-1 CONTINUITY SWEEP (VERSION_TEST_MATRIX_DESIGN.md §3 / Phase 1a): every NIST CCVS-85 program that COMPILES
# at --std 85 must still compile at 2002 / 2014 / 2023 — a break is either a VERSION_CHANGE_REFERENCE-documented
# removal/reserved-word collision (expected, must be cited) or a REGRESSION in the greenfield's edition gating.
#
# Usage: scripts/version-continuity-sweep.sh [parallelism]   (default 12)
# Output: one line per 85-compiling program — "NAME OK" or "NAME BREAKS@<edition>[,<edition>…]";
#         programs that do not compile at 85 are listed as "NAME SKIP85" (not yet in the witness set).
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CLI="$ROOT/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll"
PAR="${1:-12}"

sweep_one() {
  NAME="$1"
  ROOT="$2"
  CLI="$3"
  SRC="$ROOT/tests/nist/programs/$NAME.cob"
  DIR="$(mktemp -d)"
  trap 'rm -rf "$DIR"' RETURN
  if ! dotnet "$CLI" "$SRC" -o "$DIR/p.dll" --nist "$NAME" --std 85 >/dev/null 2>&1; then
    echo "$NAME SKIP85"
    return
  fi
  breaks=""
  for ED in 2002 2014 2023; do
    if ! dotnet "$CLI" "$SRC" -o "$DIR/p$ED.dll" --nist "$NAME" --std $ED >/dev/null 2>&1; then
      breaks="$breaks,$ED"
    fi
  done
  if [ -z "$breaks" ]; then echo "$NAME OK"; else echo "$NAME BREAKS@${breaks#,}"; fi
}
export -f sweep_one

ls "$ROOT"/tests/nist/programs/*.cob | sed 's|.*/||; s|\.cob$||' \
  | xargs -P "$PAR" -I{} bash -c 'sweep_one "$@"' _ {} "$ROOT" "$CLI"
