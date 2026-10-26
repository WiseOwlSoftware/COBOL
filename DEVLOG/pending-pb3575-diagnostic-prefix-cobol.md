## PB3575: every diagnostic code is COBOL####, one prefix and one number space

The owner decided on 2026-10-23 that the diagnostic code prefix COBOLNET#### becomes COBOL#### with every number kept
(kb/Work PB3575), and on 2026-10-26 ~10:55 PDT said: "All the error codes are incorrect because days ago I ordered
you to change from COBOLNET#### to COBOL####". The note's `## Timing` deferral was the Mythos lane's own, never the
owner's; it is deleted and the rename landed at once (wave 20261706, group A).

**Re-probe.** On this branch's base (`448eeb9e6`) the compiler printed two prefixes for one program: `error
COBOLNET1985: invalid PICTURE 9L9F9G ...` from the binder and `error COBOL0001: unexpected ...` from the parser. After
the change the same probes print `error COBOL1985`, `error COBOL2616` and `error COBOL0001`. src held 692 distinct
COBOLNET codes, COBOLNET0710 to COBOLNET5092 (`alloc.json` reserved to 5196); the parser's series is COBOL0001,
COBOL0100-0107, COBOL0200-0201 and COBOL0300-0313, so no number collided and nothing is renumbered.

**The mechanism.** `scripts/rename_diagnostic_prefix.py` rewrites the bytes of every tracked file through four rules
(a code; a placeholder such as `nnnn` or `####`; a regular expression that matches a code, with the both-prefix forms
collapsed; the prose word for the family) and sets aside one list, `EXCEPT`: DEVLOG/, the applied batches under
adjudication/, the frozen evidence under docs/rearchitecture/evidence/, the decision note that quotes the owner, and
the script. Its run changed 3,626 files with 23,560 substitutions and renamed 804 distinct codes (src 249 files,
tests 302, the conformance suite 1,726 including `wiseowl/diagnostics.json`, kb 1,278, docs 54, scripts 12, .claude
4, README.md). It is idempotent: a branch cut before this landing converts itself after merging main, and the
finisher brief's CONFLICT arm now says so. Its `--check` is the guard: the DIAGNOSTIC PREFIX audit of the gate driver,
CI's `audits` job and the battery, the same rules asked as a question, RED naming each line a rewrite would change.

**What the gates found.** Gate 1 stopped on WITNESS LOSS: 14 witnesses of the traceability inventory carried a code in
a test name or an anchor (`ZeroWhereNoRulePermitsIt_IsCOBOL2386`, six `PictureCompositionTests` methods,
`BinderDriver.cs#COBOL1507`), and the rename had re-spelled them in the JSON by hand. They now go through
`record_verdicts.py` as one batch (14 records, 14 rows changed, GAP 31 → 31) that retires each old spelling and
records the new one, and the script never re-spells a `retired-witness:` mark, the record that excuses the loss.
Gate 2's Unit leg caught one over-match: collapsing the optional group `COBOL(?:NET)?` had also rewritten
`RuntimeConfigTests`' environment-variable pattern `COBOL(?:NET)?_…`, which must still match `COBOLNET_CLOCK`; that
rule now fires only before a digit atom, and a sweep of the base for every other non-numeric rule match found only
diagnostic uses. Gate 3's Linux half was RED on the inert watch: the guard was first a Unit tree sweep, and a test
that reads every tracked file opened 3,128 inert paths (`kb/`, `.obsidian/`), so the inert lane could skip nothing.
It became the audit, as `conflict_markers.py` did (kb/Work PB2878), and the test is deleted.

**By hand**, where the rules cannot decide: `alloc.py` prints `COBOL####` for the code kind (the reservations are bare
numbers, so `alloc.json` needs no conversion) and scans whole words only; `test_alloc.py`; the adapter drift test
now checks the three series of the two prefixes by number (`SeriesOf`); `ConditionNameSubjectClassDriftTests` reads a
code as a whole `COBOL####` word; `DiagnosticPositionTests` pins COBOL1625 where it matched any non-parser error;
`OoClassTablePositionTests`, `PerformFormat3Tests`, `ApplyCommitSubjectRuleTests` (now every error must be the
rule's code, parser errors included); `docs/COBOLNET_DESIGN.md` §14.10 and its open question 12 state the one scheme;
the other design docs, DOC_INDEX and the notes lose "a COBOLNET diagnostic"; `IntrinsicCatalog.cs`'s prose
"COBOL2002" became "COBOL 2002". Left as they are: the `docs/COBOLNET_*.md` file names, the `Cobol.Net.*` assemblies
and `CobolNet` namespaces, the environment variables `COBOLNET_CLOCK` and `COBOLNET_HARNESS_LOG`, a COBOL program named
COBOLNET in two tests, and PB424's quoted measurement of a past grep.

**Self-review.** Architecture: one rename mechanism whose rules are also its guard, one exception list. Full code:
every regex that matched the old prefix was re-read for what the one prefix now admits (the parser series, prose
such as "COBOL2002"). Performance: the audit reads the tracked tree once, outside the gate slot. Duplication: none
added; `semgrep/verify.py` counts unchanged.

**Gate.** Run `000004-20261026T181708Z` at `865f78263`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515):
GREEN — ... Conformance 12,931/18,832 · Unit 37,735/38,392 · Characterization 7/36 cases ran (skipped 0) in 1 of 2
leg(s) · Linux half (WSL clone of HEAD) GREEN ...`, with `=== DIAGNOSTIC PREFIX: GREEN — 18942 tracked path(s), none
spells a code with the former prefix ===` and `=== WITNESS LOSS vs 448eeb9e6421: 0 unexcused, 14 retired ... GREEN ===`.

**Branch review, round 1.** One blocking finding, R1-1: the audit ran in the gate and in CI's `audits` job, not at
the landing. A branch cut before the rename that merges main cleanly is gated at its own head, whose gate has no such
audit, and its old spellings would meet the audit for the first time in the merge queue's CI, which the CI invariant
forbids. The review finisher's own merge of main showed it: five notes main gained after the reviewed base (PB3830,
PB3831, PB3832, PB3836, PB906; 26 spellings of 5 codes) came in without a conflict and turned the audit RED. The
landing now runs the merged head's own `rename_diagnostic_prefix.py --check` as its fourth check, the same shape as
`work.py check` (kb/Work PB3389): a refusal is exit 6, which a finisher answers by running the rename, and a tree that
cannot be listed is exit 2, NOT RUN. The finisher brief's exit-6 arm, MANDATORY-PRACTICES L1, DOC_INDEX,
DESIGN-test-build-ci.md and the enqueue unit name it. The lead R1-2 sat in the same script, so it is fixed too: the
rules missed code that builds a code (`"<prefix>" + code`, an interpolation hole) or matches the prefix in the
compiler's output (`"error <prefix>"`, `StartsWith("<prefix>")`). A negative assertion built that way would pass
whatever the compiler printed. Four rules now cover those shapes, plus the hyphenated band and the backticked family
word. A bare literal passed to any other call stays, because it is a name (the COBOL program of
`ModuleRegistrationHostTests`). Run over the pre-rename main, the script now converts all nine string sites the first
pass converted by hand. What it still leaves is prose and names.
