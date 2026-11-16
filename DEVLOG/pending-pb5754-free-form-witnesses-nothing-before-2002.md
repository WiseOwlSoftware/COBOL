## PB5754: a 1985 witness is 1985 source text; one predicate, the free-form converter, and 45 rows closed (1985 GAP 780 to 736)

**The decision** (kb/Work PB5754, Mythos lane 2026-11-15). A conformance-suite case witnesses a row at an edition only
when its text is in a reference format that edition defines. Before 2002 that is fixed form alone ("each compiler
accepts source programs written in reference format", §1985:IV.7.1), for an accepted case and a refusal test, in
`cases/` and `wiseowl/`, for a KEPT row's own witness and an own verdict's. A free-form refusal test may still be
refused at 85 as a regression test of the `--source-format free` extension; it just witnesses no 1985 row.

**The re-probe** (measured on the base, main 30b21e7d87, with PB5730 landed). 1985 rows with a test-ref: 1,508; the
strongest evidence of 102 was a free-form refusal test and of 5 a free-form accepted WiseOwl test. The rows named 134
free-form refusal tests in `cases/` (182 references; two of them refused only from 2002) and 4 accepted tests in
`wiseowl/` (15 references). The decision's own figures (128 and 4, main 5e49d46678) grew with PB5730's widening.

**The predicate, one function.** `lineage_reads.format_problem` over the one fact `generate.format_defined_at` (fixed
at every edition, free from `FIRST_FREE_FORM`), which `generate.check_editions` now asks too. It reads the case's
`source-format` as the generator does (`Case.source_format`; for one header, the new `generate.source_format_of`, which
`Reader.read_case` uses). The askers: the audit (`Witnesses.own_problems` for a KEPT row's references, the new
`Witnesses.format_problems` from `stored_errors` for every other row's, which nothing checked before), the writer
(`ref_format_problem`, asked by `record_verdicts.validate` of every conformance reference an older edition's record
names; a case it cannot read fails the record) and the planner (`Witnesses.at` and `accepted_at`, so `search`,
`witness-batch` and `widen` never count such a case; `case_status` gives it the status `format`, reason
`reference-format`). Self-tests: `case_12` of `scripts/selftests/spec/lineage_reads/test_reads.py`.

**The converter is the design's `reformat.py`** (DESIGN-conformance-suite.md §13 S8 planned it under that name; §3.6
now describes it). `conformance-suite/harness/reformat.py` rewrites a free-form case and its library texts in place in
the 1985 layout (§1985:IV.7.2 to IV.7.6: headers, paragraph-names, FD/SD/CD/RD, 01 and 77 in area A; sentences,
entries and continued lines in area B; other data entries where they were; comment lines with the asterisk in column
7; an inline comment moved to its own line; the free mark to `<<`; margin R at 72 with re-wrapping between separators).
It reads the text with the harness's own reader (`cobol_source`, which gained `floating_comment`), refuses a directive,
a tab, a continued literal, an undecidable line, a comment-entry, a line it cannot re-wrap, a shared library text and a
compound mark, then proves its result twice: the tokens equal the original's, and each line of the result, placed again
by the same classifier, sits in its area. Cases: `conformance-suite/harness/selftests/reformat/test_reformat.py`.

**The data.** Of the 171 cases (the 138 the rows named and the 33 refused at 85 for free form alone), 170 converted and
one was refused (`pb485-level-number-missing`: its subject is a data entry with no level-number, whose area cannot be
decided). Re-running the final converter over the 171 original texts reproduces every committed conversion (170 the
same, 0 different, 1 refused). Each converted refusal test compiled at every edition of its `refuse-at` (517
compilations) gave its expected diagnostic. The refused case's reference left `1985-SR-VI.5.7.3-1` (batch
`pb5754-retire-unconvertible-1985`); `witness-batch` and `widen` found no other 1985 witness for it, so it is open
again (`kb/Work/PB5754/2026-11-15-rows-reopened.md`). The four `wiseowl/` cases are about file control, organization,
symbolic characters and relations, not free form, so all four were converted.

**The 33.** `widen --edition 1985 --apply` planned 31 of them (the other two, `float_edited` and
`pb51_compute_figurative_zero`, support targets that already have a 1985 witness) and proved all 31 at 85 through
run.py; it proved none of the other 77 candidates. `witness-batch` then gave 45 stored KEPT rows a witness at every
target, exactly the 45 rows the decision listed; `record_verdicts.py --edition 1985` accepted 45 of 45 and the 1985 GAP
went from 780 to 736 (781 after the retirement). None of the 11 partly covered rows closed: each still has a target
whose only cases are free form (`reference-format`), a diagnosis for a rule that needs a run, or a case not proven at 85.
On the merged tree no 1985 row names a free-form case, and `lineage_reads.py --check` is GREEN for 1985 and 2002.

**What it does not do.** The free-form refusal tests no 1985 row names (about 1,250) stay free form; `check_editions`
still refuses a free-form `cases/` case accepted or flagged before 2002; the "witness is about the row" predicate is a
separate decision.
