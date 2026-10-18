## PB284: the catalog denominator — "General forms", the printed band as a field, prose concept clauses as rows, and the `formats` selector arm

Wave 1115 A (kb/Work PB284, cluster PB3353). The note was written 2026-09-02 against four ways `extract_rule_catalog.py`
lost part of the denominator; the wave re-probed each on a fresh build before touching anything.

**What the re-probe showed.** Part (2), thirty rules truncated by eight `## FORMAT n` headings, no longer reproduces: PB689
(2026-09-05) made the group label a non-boundary and `audit_catalog_coverage.py` counts printed ordinals per clause; the four
named clauses hold 10, 18, 25 and 23 GR rows, equal to what the transcription prints, and no SR/GR text ends in a `FORMAT n`
(176 did). Discharged with that evidence. Parts (1), (3) and (4) reproduced.

**(1) FMT-14.9.7.2.** The scan keyed on `general formats?`; §14.9.7.2 is "General forms", the only one against 321. Fixed at
the title (`GENERAL_FORMAT_TITLE`, both nouns) and, because no earlier guard could see a variant spelling, a new guard
(`is_unrecognised_general`) fails `--check` on any heading that opens with "General" and names no known block. The re-probe
found a second layer: the transcription printed the format as an inline backtick, so the fixed regex alone would have made it
`fmt_undrawn`. The printed page (PDF p.631, rendered) shows the underlined word, so §14.9.7.2 now has the `<pre><u>COMMIT</u></pre>`
form of §14.9.36.2. The row is CONFORMS (probed: bare COMMIT is a statement at `--std 2023`, refused at 2014) and
`pb137_commit_inert` lists it in its `rules:`.

**(3) §9.1.16–§9.1.18.** The mechanism, unidentified in the note: these clauses carry no numbered rule and no "General rules"
heading, so the numbered scan never opened them. The repair is a row shape, not a regex: the manifest key `paragraph-blocks`
admits a prose clause with its kind and reason, and `prose_paragraphs` emits one row per normative paragraph
(`GR-9.1.18.7-p1`, `shape: paragraph`; NOTEs stay inside their paragraph, a page-break tail joins it). 18 rows. §9.1.16 p6 and
§9.1.18.6 are SR (§5.3.2, what may be written), the rest GR. `audit_catalog_coverage.py` holds an admitted clause's rows to its
text sentence for sentence, and `CatalogCoverageDriftTests` re-derives the same from the transcription. Verdicts: the §9.1.18.x
rows, §9.1.17 and the two COMMIT/ROLLBACK paragraphs of §9.1.16 derive DOCUMENTED-NON-SUPPORT from two new arms of
`commit-and-rollback-only` (A.4.3 item 1 names §9.1.18 whole) and stay GAP until the module's witness lands; the six
record-locking paragraphs and the LOCK/UNLOCK prohibition are CONFORMS on the numbered rules they restate. PB479 (the general
class of unnumbered normative prose) keeps the measurement and the other §9.1.x clauses; its note records the shape now exists.

**(4) The printed band.** Rows carry `band` (the label as printed) and `band-formats` (the numbers it names). The `formats`
arm selects a rule only when every format its band names is the arm's, and is refused without `sections` (a format number is its
clause's own). Both engines read it (Python `DerivedSelector`, C# `DerivedVerdictDriftTests.Select`), and a self-test case per
failure mode (missing clause gate, intersection instead of subset, band read as text) is declared by name in both. Eight
rows joined their selectors: five in `validate-only`, three in `screen-handling-only` (one by operand name, because §14.9.1.3
prints no band). All eight already carried DOCUMENTED-NON-SUPPORT by hand, so no verdict moved. `$format-band-residue` is deleted
and `DESIGN-spec-conformance-review.md` states the mechanism.

**Rows.** Catalog 4,195 → 4,214 (+18 paragraph rows, +FMT-14.9.7.2); GAP 75 → 67 after the batch `pb284-new-rows` (8 CONFORMS
closed at birth, 11 DNS awaiting witnesses). Editions: record locking 2002,2014,2023 on its decided siblings; commit and
rollback 2023 (E.3.2 item 2, `cite.py --check` OK).

**Side finding, not widened.** 315 of 322 FMT rows carry a stray `<a id=…>` anchor in their text (the FMT scan does not skip
page furniture); repairing it changes 315 `rule-digest` values under live verdicts, so it is its own landing.

**Owner test requirement (SORT and file I/O abnormal paths).** This group touches no SORT or file I/O behavior (catalog and
selector tooling only), so there is no exception-path test to add; the rows it mints for record locking point at the existing
`51`/`53`/`54` and `IGNORING LOCK` witnesses.
