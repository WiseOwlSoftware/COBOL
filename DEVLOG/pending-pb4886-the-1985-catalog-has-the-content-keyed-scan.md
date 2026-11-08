## PB4886: the 1985 rule catalog has the content-keyed scan, and its denominator is complete

The 1985 catalog (kb/Work PB4886) typed its rule blocks by their headings, and a paragraph of numbered rules under a
prose heading (§1985:VI.6.4.2 'The ON SIZE ERROR Phrase') was outside the denominator without a finding. The 2023
catalog closed the same hole with the content-keyed scan of kb/Work PB29, and the 1985 arm now runs that mechanism with
the edition as its parameter, not a copy of it.

**The mechanism.** `extract_rule_catalog.py` now holds the scan's parts once, for both editions: `ordinal_shape` measures a
body (ordinals, lists, longest run), `admitted_kinds`, `rule_kind` and `kinds_of` read a manifest kind (one kind, a
per-item map, a per-sub-list map whose entry may itself be a per-item map), `unharvested_report` compares the measurement
with the committed manifest, and `UNHARVESTED_OF` names each edition's manifest. The 2023 arm calls them; its `--check`
output and its catalog are unchanged apart from one header line. `rule_catalog_1985.parse` measures every paragraph a
heading does not type, in every section and appendix, against the new
`docs/rearchitecture/spec-unharvested-rule-blocks-1985.json`, keyed by citation (the base text's Appendix A and Amendment
1's Section A both have an `A.2.1`). The paragraphs it admits are rule blocks of their declared kind. `BLOCK_KINDS` is
deleted: its eleven locator adjudications are entries in that manifest, so one table adjudicates. `RULE_SHAPED` is
imported from the 2023 arm instead of copied (review lead R1-1 of the previous wave). The appendices' paragraphs now carry
their own locator (`C.2`), and a rule-block heading in the Index or an appendix is reported.

**The re-probe and the reading.** The predecessor's `measure_unharvested.py` reported 94 paragraphs and 799 items on this
tree, an overcount: it filed Addendum 1's front matter under Section XVIII. The scan as the reader sees it measures 90
paragraphs and 756 ordinals. I read each of them against §1985:IV.3.1 and IV.3.2, together with the 2023 manifest's
disposition of the same text. 53 are admitted (254 rows, 196 of them new). 37 are recorded as not rules, each with its
reason: the appendices' history and element lists (Appendix C.2 points at rules counted where they are stated, and unlike
2023's Annex A.1 states no documentation requirement), the glossary, worked examples, indexes, a technique the text
calls one, guidance, the Function paragraph of SET, and II.6.4.3 and II.6.4.4, which restate X.1.3.6 and X.1.3.7 word for
word.

**The citation lead.** Rows `1985-SR-XIII.3.20.3-5` to `-7` (the TYPE clause's syntax rules after the scan gap) cited
`§1985:XIII.3.20.3`, a paragraph whose heading is on a missing page, so `cite.py` refused them. They now cite
`§1985:p.XIII-56`, the printed page that owns them, as an amendment's added rule cites its own page. `cite_check` makes
any row whose citation `citation.validate` refuses a finding: it reported those three before the fix and none after.

**The counts.** Before: 2,057 rows from 622 blocks (SR 597, GR 1,105, AR 65, RV 80, FMT 210). After: 2,253 rows from 664
blocks (SR 645, GR 1,253, AR 65, RV 80, FMT 210) and FINDINGS 0. The new rows are in Nucleus (+22 level 1, +8 level 2),
Sequential I-O (+12), Relative I-O (+19), Indexed I-O (+19), Inter-Program Communication (+12, +11), Segmentation (+17),
Intrinsic Function (+3), Section II (+10) and Section IV (+63). `audit_batch plan --pass map` plans all 2,253 rows.

**Tests.** `rule_catalog_1985.py --self-test` plants an admitted paragraph with a per-item map, one whose map misses a
rule, an undeclared prose list, a declared paragraph the text lacks, a moved count, an appendix list and an appendix rule
heading, the page citation of a resumed paragraph, the shared measure and the nested kind map. No compiler change, so
no golden; no inventory row changes (no 1985 inventory exists yet).

**Self-review.** One mechanism per job: one measure, one kind reader and one drift report for both editions, and one
1985 adjudication table, the manifest. Phases: the reader takes the manifest as data, and only `main_1985` touches the
file system. Duplication: the deleted copies are `BLOCK_KINDS`, the 1985 `RULE_SHAPED` and the 2023 arm's inline
kind resolution and drift printing. The cost is one linear pass, as before. Leads not fixed here: the 1985 Section I
conformance requirements are prose with no row (2023's `paragraph-blocks` shape); a rule of a second sub-list cites the
same `GR1` as the first list's; the transcription's "control fopting" on PDF page 634.
