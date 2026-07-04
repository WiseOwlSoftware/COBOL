#!/usr/bin/env bash
# INV-1 CONTINUITY SWEEP (VERSION_TEST_MATRIX_DESIGN.md §3; RESTATED at the P2.7 flip — the §10 #1 migration
# posture): every NIST CCVS-85 program that COMPILES at --std 85 must still compile at 2002 / 2014 / 2023
# **UNDER --permissive** — under strict, later editions legitimately REJECT the removed '85 elements every
# NIST program carries (LABEL RECORDS in every FD; gate live since DEVLOG 588), and every strict failure must
# trace to a recognized edition-band diagnostic code. A PERMISSIVE break is a REGRESSION, full stop.
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
    if ! dotnet "$CLI" "$SRC" -o "$DIR/p$ED.dll" --nist "$NAME" --std $ED --permissive >/dev/null 2>&1; then
      breaks="$breaks,$ED"
    fi
  done
  if [ -z "$breaks" ]; then echo "$NAME OK"; else echo "$NAME BREAKS@${breaks#,}"; fi
}
export -f sweep_one

ls "$ROOT"/tests/nist/programs/*.cob | sed 's|.*/||; s|\.cob$||' \
  | xargs -P "$PAR" -I{} bash -c 'sweep_one "$@"' _ {} "$ROOT" "$CLI"
