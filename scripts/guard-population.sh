#!/bin/bash
# guard-population.sh — THE populations the NIST guards run, DERIVED from tests/nist/corpus.tsv.
#
# ⛔ WHY THIS FILE EXISTS (kb/Work PB898). A fact about the NIST rows that more than one guard script needs is read
# from the manifest HERE, once, never copied into a script as a hand-written name list: the retired `scripts/guard.sh` once
# carried the divergent set as a string that `scripts/guard-fast.sh` `sed`-extracted, and the copy drifted a
# program (SQ212A) behind the manifest while the audit, reading the manifest itself, disagreed with both.
#
# ⛔ THE ONE FACT THE GUARDS STILL NEED: WHICH `divergent` ROWS TERMINATE (kb/Work PB1955). A `divergent` row whose
# note begins `TERMINATES EC-<name>` has a golden that records a run that CONTINUED past a fatal I-O status nothing
# covers, which WiseOwl COBOL's documented choice under ISO §9.1.13.1 ("The implementor may either continue or
# terminate the execution of the run unit"; Annex A.1 item 103, docs/CONFORMANCE.md DOC-A.1-103) ENDS, so the run
# must end naming that exception. Every other `divergent` row is compared with its golden like a green one (the
# legacy engine's exemption list for them is retired with the engine, kb/Work PB2109). The marker is written down
# here ONCE for the shell (the two runners and the audit all ask this file) and once for C#
# (CorpusRow.ExpectedTermination in tests/Cobol.Net.Tests.Conformance/CorpusManifest.cs); CorpusManifestTests holds
# the two to the same grammar.
#
# Sourced, never executed, except for `--self-test`:
#     . "$(dirname "$0")/guard-population.sh"
#     GUARD_TERMINATES="$(guard_terminating)" || exit 1
#
# `GUARD_CORPUS_TSV` overrides the manifest path — for the self-test, and for nothing else.

GUARD_POPULATION_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GUARD_CORPUS_TSV="${GUARD_CORPUS_TSV:-$GUARD_POPULATION_ROOT/tests/nist/corpus.tsv}"

# The TERMINATES marker, as an awk ERE over the note column (column 6). The C# reader's pattern is the same
# grammar (CorpusRow.ExpectedTermination), and CorpusManifestTests asserts this literal against it.
GUARD_TERMINATES_MARKER='^TERMINATES EC-[A-Z0-9-]+'

# guard_manifest_present corpus.tsv — loud when the manifest is missing; every reader below starts here.
guard_manifest_present() {
    if [ ! -f "$1" ]; then
        echo "guard-population: the NIST manifest is missing: $1" >&2
        echo "guard-population: the guard's populations are DERIVED from it and cannot be guessed." >&2
        return 1
    fi
}

# guard_terminating [corpus.tsv] — `NAME=EC-…` for every TERMINATES row, space-joined, on stdout. Empty is a real
# answer (no program is declared to terminate); a missing manifest is still loud. "TERMINATES " is 11 characters,
# so the exception-name starts at column 12 of the note and runs to the end of the marker's match.
guard_terminating() {
    local -
    set -o pipefail   # an awk that could not read the manifest is a failure, never "no TERMINATES rows"
    local corpus="${1:-$GUARD_CORPUS_TSV}"
    guard_manifest_present "$corpus" || return 1
    awk -F'\t' -v m="$GUARD_TERMINATES_MARKER" '
        $1 !~ /^#/ && $3 == "divergent" && match($6, m) { printf "%s=%s ", $1, substr($6, 12, RLENGTH - 11) }' \
        "$corpus" | sed 's/[[:space:]]*$//' || return 1
    echo
}

# guard_declared_termination NAME — the exception-name GUARD_TERMINATES declares NAME terminates on, or nothing.
# Each runner sets GUARD_TERMINATES from guard_terminating before it scores any program.
guard_declared_termination() {
    local pair
    for pair in ${GUARD_TERMINATES:-}; do
        if [ "${pair%%=*}" = "$1" ]; then printf '%s\n' "${pair#*=}"; return 0; fi
    done
    return 0
}

# guard_chain_preds NAME [corpus.tsv] — NAME's declared chain predecessors (the chain-preds column, column 4) in run
# order, space-joined; nothing for a program that declares none or has no row. The group runner runs them, in order,
# in the program's own clean directory before the program, as NistRunner does (guard-run-group.sh; kb/Work PB3334).
guard_chain_preds() {
    local corpus="${2:-$GUARD_CORPUS_TSV}"
    guard_manifest_present "$corpus" || return 1
    awk -F'\t' -v t="$1" '$1 == t && $1 !~ /^#/ { if ($4 != "-" && $4 != "") print $4; exit }' "$corpus"
}

# guard_nist_population [corpus.tsv] — THE NIST GUARD'S POPULATION, one name per line, sorted (kb/Work PB925, PB3383):
# every row the golden suite asserts (status `green` or `divergent`, NistDifferentialTests' CorpusManifest.Green()),
# plus every program such a row names as a chain predecessor, at any depth. A predecessor is in the population because
# its consumer's directory runs it (guard-run-group.sh) from the program it compiled, so a `pending` predecessor (ST102A,
# a NO_OUTPUT producer) is compiled, run and reported NO BASELINE like any golden-less program, and nothing else
# `pending` is. The population was a hand-written NIST_TESTS block in the retired serial guard, whose ORDER carried
# the chains because its programs shared one directory; each program now runs in its own, so order carries nothing.
guard_nist_population() {
    guard_closure asserted "${1:-$GUARD_CORPUS_TSV}" < /dev/null
}

# guard_with_predecessors [corpus.tsv] — the names on stdin (whitespace-separated) plus every program they declare as a
# chain predecessor, at any depth, one per line, sorted: the programs a run of those names must COMPILE, since each
# one's directory runs its predecessors (guard-fast.sh under a GUARD_TESTS subset, and its re-observation step).
guard_with_predecessors() {
    guard_closure stdin "${1:-$GUARD_CORPUS_TSV}"
}

# guard_closure SEED corpus.tsv — the one walk of the chain-preds column both answers above share. SEED `asserted`
# starts from the rows the golden suite asserts; `stdin` from the names on standard input. A predecessor with no row
# is loud (exit 1): the run could never compile it.
guard_closure() {
    local -
    set -o pipefail   # an awk that could not read the manifest is a failure, never an empty answer
    local seed="$1" corpus="$2"
    guard_manifest_present "$corpus" || return 1
    awk -F'\t' -v seed="$seed" '
        FNR == NR {
            if ($1 ~ /^#/ || $1 == "") next
            row[$1] = 1; preds[$1] = ($4 == "-" ? "" : $4)
            if (seed == "asserted" && ($3 == "green" || $3 == "divergent")) add($1)
            next
        }
        seed == "stdin" { for (i = 1; i <= NF; i++) if ($i != "") add($i) }
        function add(t) { if (!(t in want)) { want[t] = 1; queue[++n] = t } }
        END {
            for (i = 1; i <= n; i++) {              # the closure: a predecessor of a member is a member
                k = split(preds[queue[i]], p, " ")
                for (j = 1; j <= k; j++) {
                    if (!(p[j] in row)) {
                        print "guard-population: " queue[i] " declares predecessor " p[j] ", which has no corpus.tsv row" > "/dev/stderr"
                        bad = 1
                    }
                    add(p[j])
                }
            }
            for (t in want) print t
            exit bad
        }' FS='\t' "$corpus" FS='[ \t]+' - | LC_ALL=C sort || return 1
}

# ── SELF-TEST ─────────────────────────────────────────────────────────────────────────────────────────────
# A check that has never been observed failing is not evidence (feedback_green_gates_arent_evidence), and the loud
# arm above exists precisely because its silent version would look green.
# Only when EXECUTED: a sourcing script's own `--self-test` argument (guard-nist-audit.sh has one) is not this one's.
if [ "${BASH_SOURCE[0]}" = "$0" ] && [ "${1:-}" = "--self-test" ]; then
    gp_checks=0; gp_fail=0
    gp_assert() {   # gp_assert <what> <condition-rc>
        gp_checks=$((gp_checks + 1))
        if [ "$2" -eq 0 ]; then echo "  ok   $1"; else echo "  FAIL $1"; gp_fail=$((gp_fail + 1)); fi
    }
    gp_tmp="$(mktemp -d)"
    trap 'rm -rf "$gp_tmp"' EXIT

    # (1) The real manifest yields the set an independent awk over the same column derives.
    real="$(guard_terminating)"; rc=$?
    gp_assert "the real manifest derives a TERMINATES set" "$rc"
    independent=$(awk -F'\t' '$1 !~ /^#/ && $3=="divergent" && index($6, "TERMINATES EC-") == 1 {
                      split($6, w, " "); printf "%s=%s ", $1, w[2] }' "$GUARD_CORPUS_TSV" | sed 's/[[:space:]]*$//')
    [ "$real" = "$independent" ]; gp_assert "the derived set equals the manifest's TERMINATES rows" $?

    # (2) A comment line whose fields would otherwise match is ignored.
    printf '# ZZ999A\tZZ\tdivergent\t-\tvalid\tTERMINATES EC-I-O\nNC101A\tNC\tdivergent\t-\tvalid\tTERMINATES EC-I-O\n' > "$gp_tmp/cmt.tsv"
    [ "$(guard_terminating "$gp_tmp/cmt.tsv")" = "NC101A=EC-I-O" ]; gp_assert "comment rows are not population" $?
    printf '# header\nNC101A\tNC\tgreen\t-\tvalid\t-\n' > "$gp_tmp/nodiv.tsv"

    # (3) ⛔ THE PB1955 SHAPE. A TERMINATES row enters the TERMINATES set with its exception-name; a plain divergent
    #     row beside it is untouched; a note that only MENTIONS the word, or spells the marker wrong, is a plain
    #     divergent row (CorpusManifestTests refuses the misspelling).
    printf 'NC401M\tNC\tdivergent\t-\tvalid\tTERMINATES EC-I-O-PERMANENT-ERROR - ISO 9.1.13.1: the run unit ends\n' > "$gp_tmp/term.tsv"
    printf 'ST146A\tST\tdivergent\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - ISO 9.1.13.1: status 47\n' >> "$gp_tmp/term.tsv"
    printf 'NC201A\tNC\tdivergent\t-\tvalid\tISO 14.9.28.4 CCVS-DEFECT: the golden carries one failure\n' >> "$gp_tmp/term.tsv"
    printf 'SQ212A\tSQ\tdivergent\t-\tvalid\tISO 9.1.13.1: another compiler TERMINATES EC-I-O where this does not\n' >> "$gp_tmp/term.tsv"
    printf 'SQ213A\tSQ\tgreen\t-\tvalid\tTERMINATES EC-I-O-LOGIC-ERROR - a green row is never a TERMINATES row\n' >> "$gp_tmp/term.tsv"
    [ "$(guard_terminating "$gp_tmp/term.tsv")" = "NC401M=EC-I-O-PERMANENT-ERROR ST146A=EC-I-O-LOGIC-ERROR" ]
    gp_assert "guard_terminating names each TERMINATES row with its exception-name" $?
    [ -z "$(guard_terminating "$gp_tmp/nodiv.tsv")" ]; gp_assert "no TERMINATES row is an empty answer" $?
    guard_terminating "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "guard_terminating on a missing manifest returns non-zero" $?
    [ "$(GUARD_TERMINATES="$(guard_terminating "$gp_tmp/term.tsv")" guard_declared_termination ST146A)" = "EC-I-O-LOGIC-ERROR" ]
    gp_assert "guard_declared_termination finds a declared program's exception-name" $?
    [ -z "$(GUARD_TERMINATES="$(guard_terminating "$gp_tmp/term.tsv")" guard_declared_termination NC201A)" ]
    gp_assert "guard_declared_termination says nothing for an undeclared program" $?
    [ -z "$(GUARD_TERMINATES="" guard_declared_termination NC401M)" ]
    gp_assert "an empty GUARD_TERMINATES declares nothing" $?

    # (4) THE CHAIN PREDECESSORS (kb/Work PB3334 review round 2): a consumer's declared list, in run order, a chain
    #     head's nothing, a comment row's nothing, and a missing manifest is loud.
    [ "$(guard_chain_preds IX115A)" = "IX109A" ]; gp_assert "guard_chain_preds reads IX115A's one declared predecessor" $?
    [ "$(guard_chain_preds ST103A)" = "ST101A ST102A" ]; gp_assert "guard_chain_preds keeps the declared run order" $?
    [ -z "$(guard_chain_preds IX109A)" ]; gp_assert "a chain head declares no predecessor" $?
    printf '# C1\tZZ\tgreen\tP9\tvalid\t-\nC1\tZZ\tgreen\tP1 P2\tvalid\t-\n' > "$gp_tmp/chain.tsv"
    [ "$(guard_chain_preds C1 "$gp_tmp/chain.tsv")" = "P1 P2" ]; gp_assert "a comment row declares no chain" $?
    guard_chain_preds C1 "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "guard_chain_preds on a missing manifest returns non-zero" $?

    # (5) THE POPULATION (kb/Work PB925, PB3383): the asserted rows, plus their declared predecessors at any depth, and
    #     nothing else. The real manifest's answer equals an independent derivation over the same columns, and holds
    #     every asserted row; on a synthetic manifest a `pending` predecessor is in, a `pending` non-predecessor and a
    #     comment row are out, a predecessor's own predecessor is in, and a predecessor with no row is loud.
    real_pop="$(guard_nist_population)"; rc=$?
    gp_assert "the real manifest derives a population" "$rc"
    independent_pop="$(awk -F'\t' '$1 !~ /^#/ && ($3 == "green" || $3 == "divergent") {
                           print $1; if ($4 != "-") { n = split($4, p, " "); for (i = 1; i <= n; i++) print p[i] } }' \
                       "$GUARD_CORPUS_TSV" | LC_ALL=C sort -u)"
    [ -n "$real_pop" ] && [ "$real_pop" = "$independent_pop" ]
    gp_assert "the derived population equals the asserted rows plus their declared predecessors" $?
    printf '%s\n' "$real_pop" > "$gp_tmp/real_pop.txt"
    missing_asserted="$(awk -F'\t' '$1 !~ /^#/ && ($3 == "green" || $3 == "divergent") { print $1 }' "$GUARD_CORPUS_TSV" \
                        | LC_ALL=C sort | LC_ALL=C comm -23 - "$gp_tmp/real_pop.txt")"
    [ -z "$missing_asserted" ]; gp_assert "every asserted row is in the population" $?
    printf '# CX9A\tZZ\tgreen\t-\tvalid\t-\nC3\tZZ\tgreen\tP2\tvalid\t-\nP2\tZZ\tpending\tP1\tnone\t-\n' > "$gp_tmp/pop.tsv"
    printf 'P1\tZZ\tpending\t-\tnone\t-\nQ1\tZZ\tpending\t-\tnone\t-\nD1\tZZ\tdivergent\t-\tvalid\tISO 1.1\n' >> "$gp_tmp/pop.tsv"
    [ "$(guard_nist_population "$gp_tmp/pop.tsv" | tr '\n' ' ')" = "C3 D1 P1 P2 " ]
    gp_assert "a pending predecessor (and ITS predecessor) is in, a pending non-predecessor and a comment row are out" $?
    printf 'C3\tZZ\tgreen\tP9\tvalid\t-\n' > "$gp_tmp/orphan.tsv"
    guard_nist_population "$gp_tmp/orphan.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "a declared predecessor with no manifest row is loud" $?
    guard_nist_population "$gp_tmp/absent.tsv" >/dev/null 2>&1
    [ $? -ne 0 ]; gp_assert "guard_nist_population on a missing manifest returns non-zero" $?
    [ "$(echo " C3  Q1" | guard_with_predecessors "$gp_tmp/pop.tsv" | tr '\n' ' ')" = "C3 P1 P2 Q1 " ]
    gp_assert "guard_with_predecessors adds a subset's predecessors at any depth, and only theirs" $?
    [ "$(echo "IX115A" | guard_with_predecessors | tr '\n' ' ')" = "IX109A IX115A " ]
    gp_assert "guard_with_predecessors over the real manifest: IX115A compiles with IX109A" $?

    if [ "$gp_fail" -eq 0 ]; then
        echo "=== GUARD POPULATION SELF-TEST: PASS ($gp_checks checks) ==="; exit 0
    fi
    echo "=== GUARD POPULATION SELF-TEST: FAIL ($gp_fail of $gp_checks checks) ==="; exit 1
fi
