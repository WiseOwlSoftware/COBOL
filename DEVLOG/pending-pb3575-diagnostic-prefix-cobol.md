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
adjudication/, the frozen evidence under docs/rearchitecture/evidence/, the decision note that quotes the owner, the
script and its drift test. Its run changed 3,626 files with 23,560 substitutions and renamed 804 distinct codes (src
249 files, tests 302, the conformance suite 1,726 including `wiseowl/diagnostics.json`, kb 1,278, docs 54, scripts 12,
.claude 4, README.md). It is idempotent: a branch cut before this landing converts itself after merging main, and the
finisher brief's CONFLICT arm now says so. `DiagnosticCodePrefixDriftTests` (a tree sweep, tier 0c) refuses any
code-shaped use of the old prefix outside `EXCEPT`, holds its scope's exceptions equal to the script's list, and fires
on every planted shape.

**The inventory's witnesses.** Fourteen witnesses of the traceability inventory carried a code inside a test name or
an anchor (`ZeroWhereNoRulePermitsIt_IsCOBOL2386`, six `PictureCompositionTests` methods, `BinderDriver.cs#COBOL1507`).
The first gate stopped on WITNESS LOSS (14 unexcused against the base): the rename had re-spelled them in the JSON by
hand. They now go through `record_verdicts.py` as one batch (14 records, 14 rows changed, GAP 31 → 31) that retires
each old spelling and records the new one, and the script never re-spells a `retired-witness:` mark, the record that
excuses the loss (its self-test and the drift test both pin that).

**By hand**, where the rules cannot decide: `alloc.py` prints `COBOL####` for the code kind (the reservations are bare
numbers, so `alloc.json` needs no conversion) and scans whole words only; `test_alloc.py`; the adapter drift test
now checks the three series of the two prefixes by number (`SeriesOf`); `ConditionNameSubjectClassDriftTests` reads a
code as a whole `COBOL####` word; `DiagnosticPositionTests` pins COBOL1625 where it matched any non-parser error;
`OoClassTablePositionTests`, `PerformFormat3Tests`, `ApplyCommitSubjectRuleTests` (now every error must be the
rule's code, parser errors included); `docs/COBOLNET_DESIGN.md` §14.10 and its open question 12 state the one scheme;
the other design docs, DOC_INDEX and the notes lose "a COBOLNET diagnostic". Left as they are: the
`docs/COBOLNET_*.md` file names, the `Cobol.Net.*` assemblies and `CobolNet` namespaces, a COBOL program named
COBOLNET in two tests, and PB424's quoted measurement of a past grep.

**Self-review.** Architecture: one rename mechanism, one exception list read by its drift test. Full code: every
regex that matched the old prefix was re-read for what the one prefix now admits (the parser series, prose such as
"COBOL2002", now "COBOL 2002" in `IntrinsicCatalog.cs`). Performance: the sweep reads each tracked text file once per
test assembly. Duplication: none added; `semgrep/verify.py` counts unchanged.

**Gate.** GATE_LINE
