## Entry 2181 — 2026-10-18 00:18 PDT — PB284: the catalog denominator — "General forms", the printed band as a field, prose concept clauses as rows, and the `formats` selector arm

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

**Rows.** Catalog 4,195 → 4,214 (+18 paragraph rows, +FMT-14.9.7.2); GAP 56 → 67 on main's inventory (counted: origin/main at `fbe2d15a7` has 4,195 rows and 56 GAP; this branch 4,214 and 67) after
the batch `pb284-new-rows`: the landing RAISES the burn-down GAP by 11, because the 11 new DOCUMENTED-NON-SUPPORT rows wait for
module witnesses, while 8 new rows were CONFORMS at birth. (An earlier draft said 75 → 67; 75 was an intermediate count of the
working tree that never existed on main.) Editions: record locking 2002,2014,2023 on its decided siblings; commit and
rollback 2023 (E.3.2 item 2, `cite.py --check` OK).

**Branch review round 1: the heading rule had seven copies (R1-1).** Fixing the extractor's title left the same blind spot in
its siblings: `sweep_figures.py` (`MD_GENERAL_FORMAT`), `render_figure.py` (`GENERAL_FORMAT`), `audit_grammar_optional_words.py`,
`audit_figure_structure.py` (a substring test, `"General format" in page text`) and the C# guards `PrintedFormatAlternative`,
`RequiredFormatPhrase` and `IntrinsicRepeatedArgument`. Re-probed: `sweep_figures.py --check --clause 14.9.7.2` reported
`figures generated: 0 / CHECK: clean` for the figure this branch hand-placed. Measuring the whole sweep before and after found a
SECOND clause the copies had never seen: §7.3.23.2 is headed "General format." (period), and its figure was a hand-fenced
blockquote that had never been compared with the printed page; the sweep now locates it (486 figures against 484) and it was
DRIFTED from the generated form, so it is regenerated (`<pre>` with the printed underlining of REF-MOD-ZERO-LENGTH and OFF; the
figure notes are kept). The root fix is one definition per language: `scripts/spec/spec_headings.py`
(`GENERAL_FORMAT_TITLE`, `GENERAL_FORMAT_PRINTED`) imported by all five Python readers, and `SpecClauseText.GeneralFormatNoun` /
`GeneralFormatHeading` composed by all four C# guards; the 322 clauses are the same set in both (`spec_headings.py --self-test`
reads the C# constant and compares), and `CatalogCoverageDriftTests.NoScriptOrTest_SpellsTheGeneralFormatHeadingItself` fails
when any script or test writes the pattern itself again. The sweep after the change: 486 figures, 0 words-differ, 1 documented
correction kept.

**Branch review round 2: an eighth copy survived (R2-1).** `audit_code_citations.py` carried `"general format" in
title.lower()`, a case-folded substring test (False for "General forms"); round 1 had judged it a non-copy, and the guard could
not see it because its substring test was capital-G only and its pattern test took a lower-case spelling only as `\s`. The
audit now imports `GENERAL_FORMAT_TITLE` (no citation verdict moved: 0 findings before and after, self-test PASS), and the
guard sees the case-folded shapes: `MembershipTest` is case-insensitive and covers `startswith`/`endswith`/`find`/`count`/
`Contains`/`IndexOf`, and `PythonLowerPattern` flags a lower-case regex inside a Python `re` call (a C# keyword alternation
stays prose). `TheNoCopySweep_FlagsEachSpellingAndNotAKeywordList` gained four flagged cases, including the exact R2-1 line.

**Side finding, not widened.** 315 of 322 FMT rows carry a stray `<a id=…>` anchor in their text (the FMT scan does not skip
page furniture); repairing it changes 315 `rule-digest` values under live verdicts, so it is its own landing.

**Owner test requirement (SORT and file I/O abnormal paths).** This group touches no SORT or file I/O behavior (catalog and
selector tooling only), so there is no exception-path test to add; the rows it mints for record locking point at the existing
`51`/`53`/`54` and `IGNORING LOCK` witnesses.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` on the branch head after the review round
(run `20261018T064519Z-0c3b6a`). The first review-round gate was RED on the branch's own new no-copy sweep, which flagged a
lower-case keyword list in `BoundDeferralDriftTests` as a copy of the heading pattern; the sweep now takes a lower-case spelling
only as a regex, and a theory proves it flags each spelling of the pattern and passes the keyword list.

