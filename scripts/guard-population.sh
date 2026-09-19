#!/bin/bash
# guard-population.sh — THE populations the NIST guards run, DERIVED from tests/nist/corpus.tsv.
#
# ⛔ WHY THIS FILE EXISTS (kb/Work PB898). `scripts/guard.sh` carried the divergent set as a hand-written string
# and `scripts/guard-fast.sh` `sed`-extracted THAT string, so the manifest — which the header of corpus.tsv
# itself calls the ONE source of truth, and which says it "folds … scripts/guard.sh LEGACY_DIVERGENT" — had a
# second copy nobody compared it to. The copy had drifted: the manifest declared THIRTEEN divergent programs and
# the string named TWELVE. `SQ212A` was the missing one, so under `GUARD_DIVERGENT=1` its EXPECTED legacy
# difference was scored as a REGRESSION.
#
# ⭐ And the tie-break needs no new evidence, because a THIRD reader already derives this very set from the
# manifest: `scripts/guard-nist-audit.sh` computes `expect[name] = (status == "divergent" && compiler ==
# "legacy") ? "LEGACY DIVERGENT" : …` straight out of corpus.tsv. The runner and its auditor disagreed about
# SQ212A while reading the same fact from two places. Deriving here makes them one reader, which is the fix
# CLAUDE.md rule 5 asks for — never a hand-maintained list where a structure belongs.
#
# Sourced, never executed, except for `--self-test`:
#     . "$(dirname "$0")/guard-population.sh"
#     LEGACY_DIVERGENT="$(guard_legacy_divergent)" || exit 1
#
# `GUARD_CORPUS_TSV` overrides the manifest path — for the self-test, and for nothing else.

GUARD_POPULATION_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GUARD_CORPUS_TSV="${GUARD_CORPUS_TSV:-$GUARD_POPULATION_ROOT/tests/nist/corpus.tsv}"

# guard_legacy_divergent [corpus.tsv] — the space-joined names of every `divergent` row, on stdout.
#
# ⛔ AN EMPTY ANSWER IS A FAILURE, NOT A POPULATION. Returning "" for an unreadable or malformed manifest would
# silently un-exempt every divergent program and report a wall of regressions that mean nothing — a missing
# observation is not a negative one (DESIGN-test-build-ci.md §3.10). Both failure arms print to stderr and
# return non-zero, and every caller stops.
guard_legacy_divergent() {
    local corpus="${1:-$GUARD_CORPUS_TSV}"
    if [ ! -f "$corpus" ]; then
        echo "guard-population: the NIST manifest is missing: $corpus" >&2
        echo "guard-population: the divergent set is DERIVED from it and cannot be guessed." >&2
        return 1
    fi
    local names
    # Column 3 is status(green|divergent|pending); a leading # is a comment (corpus.tsv's own header).
    names=$(awk -F'\t' '$1 !~ /^#/ && $3 == "divergent" { printf "%s ", $1 }' "$corpus" | sed 's/[[:space:]]*$//')
    if [ -z "$names" ]; then
        echo "guard-population: $corpus declares NO divergent rows." >&2
        echo "guard-population: that is either a broken manifest or a broken reader — either way the guard must" >&2
        echo "guard-population: not proceed as though every legacy divergence were a regression." >&2
        return 1
    fi
    printf '%s\n' "$names"
}

# ── SELF-TEST ─────────────────────────────────────────────────────────────────────────────────────────────
# A check that has never been observed failing is not evidence (feedback_green_gates_arent_evidence), and BOTH
# loud arms above exist precisely because their silent versions would look green.
if [ "${1:-}" = "--self-test" ]; then
    gp_checks=0; gp_fail=0
    gp_assert() {   # gp_assert <what> <condition-rc>
        gp_checks=$((gp_checks + 1))
        if [ "$2" -eq 0 ]; then echo "  ok   $1"; else echo "  FAIL $1"; gp_fail=$((gp_fail + 1)); fi
    }
    gp_tmp="$(mktemp -d)"
    trap 'rm -rf "$gp_tmp"' EXIT

    # (1) The real manifest yields a non-empty set that matches an independent awk over the same column.
    real="$(guard_legacy_divergent)"; rc=$?
    gp_assert "the real manifest derives a divergent set" "$rc"
    independent=$(awk -F'\t' '$3=="divergent"{print $1}' "$GUARD_CORPUS_TSV" | tr '\n' ' ' | sed 's/[[:space:]]*$//')
    [ "$real" = "$independent" ]; gp_assert "the derived set equals the manifest's divergent column" $?

    # (2) ⛔ A MISSING MANIFEST IS LOUD, not an empty exemption list.
    guard_legacy_divergent "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "a missing manifest returns non-zero" $?

    # (3) ⛔ A manifest with no divergent rows is LOUD too — the shape that would un-exempt everything.
    printf '# header\nNC101A\tNC\tgreen\t-\tvalid\t-\n' > "$gp_tmp/nodiv.tsv"
    guard_legacy_divergent "$gp_tmp/nodiv.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "a manifest with no divergent rows returns non-zero" $?

    # (4) A comment line whose first field would otherwise match is ignored.
    printf '# ZZ999A\tZZ\tdivergent\t-\tvalid\t-\nNC101A\tNC\tdivergent\t-\tvalid\t-\n' > "$gp_tmp/cmt.tsv"
    [ "$(guard_legacy_divergent "$gp_tmp/cmt.tsv")" = "NC101A" ]; gp_assert "comment rows are not population" $?

    if [ "$gp_fail" -eq 0 ]; then
        echo "=== GUARD POPULATION SELF-TEST: PASS ($gp_checks checks) ==="; exit 0
    fi
    echo "=== GUARD POPULATION SELF-TEST: FAIL ($gp_fail of $gp_checks checks) ==="; exit 1
fi
