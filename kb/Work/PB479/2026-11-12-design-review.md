## 2026-11-12 — the design's adversarial review: thirteen findings, each answered in the design and the notes

An adversarial reviewer re-ran the measurement of the dated file `2026-11-12-measured-and-designed.md` at head
dbc567859a of branch `design-pb479`, took its own paragraph sample and found thirteen findings, six blocking (F1, F2, F3,
F5, F6, F7); its verdict: the unit, ruling on every unit and the step order hold. Each finding's command was re-run on
this tree before acting; the review's probes are kept in `E:/COBOL-coord/scratch/pb479/review/`. The design text is
"Phase A, continued" of section 3 of `docs/rearchitecture/DESIGN-spec-conformance-review.md` (rewritten, not appended
to); the figures of the measured file's "Already implemented elsewhere" and "What the owner is told" paragraphs are
replaced by the ones below and in decision PB5448.

- **F1 (carving copied the parent's verdict and test).** Reproduced: `cite.py --check "§2023:14.7.5 GR5" "If no size
  error condition occurs ..."` fails under GRL3.5; `RV-15.4.1-4` is CONFORMS with no `test-ref`, on
  `docs/CONFORMANCE.md#DRV-RV-15.4.1-4`; the 57 parents are 55 CONFORMS with a test, 1 on a derivation, 1 DNS. Changed:
  a carved row is minted OPEN with no record and never a derivation; it closes by its own adjudication, refuter and
  test (design point 7; PB5451; PB5455 names the three known unwitnessed paragraphs). Decided beyond the direction: the
  PARENT keeps its record only through a recheck of its remaining text, since its test may have witnessed only the
  carved text (design point 7, PB5449 item 6, PB5451). "Closed in the same landing" is gone from the design, PB5448,
  PB5451 and the numbers.
- **F2 (the first pass of 293).** Reproduced (`review/fp_quality.py`): 19 same-clause, 274 other clauses; best record
  covers 90 % or more for 23. Changed: no mechanical pass closes anything; the quotations order PB5455 (the 23, then the
  19) and seed candidate sites ("What the tree already shows" in the design; PB5451 loses the first pass; PB5455).
- **F3 (`not-rule` cheaper than a derivation).** Reproduced: 76 function-type sentences (`grep -c`), none in a row.
  Changed (design point 3, PB5449 item 4, PB5450): a normative unit is a ROW and one that obliges nothing closes through
  section 1.1's `definitional` arm under PB5373; `not-a-rule` has three classes with mechanical tests (`lead-in`,
  `cross-reference`, `data`); each `restates` and `not-a-rule` names an UPHELD refuter record (PB5374's form) holding the
  digest it read, with per-class counts audited; the verbal forms are widened and a SHAPE guard derived from every
  row's sentence openings refuses a `not-a-rule` on a rule-shaped unit, a drift test planting the function-type case.
  The sibling one level up, the manifests' block disposition `not-rules` (4 in 2023, 37 in 1985, free-text reasons),
  takes the same refuter record, refuted in PB5450 and PB5452. Decided differently from the direction's example list: an unmarked example or NOTE is a transcription repair, not a
  class (rule 4: the transcription is the root); `overview` is `restates`; `definition` and `frame` are gone.
- **F4 (the estimate).** Reproduced the review's arithmetic; a 1985 paragraph sample was added (seed 1985, 20
  paragraphs, `review/sample1985para.py`, judged in `out/sample-1985-para.txt`: 3 fencing residue of the measuring
  script, 1 list item, 8 rows of 16 units). Restated: 2023 about 500 to 1,850 (paragraphs about 705, 375 to 1,055;
  carved up to 121; tables unmeasured, about 250 to 450; at most 5 data clauses); 1985 paragraphs about 350 (195 to
  500), tables and glued text unmeasured; zero closed at minting; the exact count is PB5450's output, which starts with a
  stratified table sample (design consequence table, PB5448, PB5450, PB5452).
- **F5 (the page-furniture join).** Reproduced: `grep -c "^## *Page "` on the 2023 text gives 0; `review/tails.py` gives
  42 lowercase continuations across blank lines only, 0 across furniture. Changed (design point 1, PB5449 item 1): a
  unit continues across blank lines (and furniture) when the text before it ends no sentence and it opens in lowercase
  or with a continuation word; the 42 are its `--self-test` population.
- **F6 (the text key).** Reproduced: `sublist_key` over the measured population collides on 18 keys covering 43
  paragraphs (the review counted 17 and 41 on a slightly narrower population). Changed (design point 3, PB5449 item 4):
  the key is clause + digest of the whole normalized text + order among equal digests; a repair that changes a unit's
  text leaves an orphaned disposition and an undisposed unit, and the audit fails on both until decided again.
- **F7 (PB5449's done-condition).** Reproduced: `Edition.outside` reads the catalog's `outside-denominator`; `cite.py`
  today answers "it lies in GR13" for the relation-condition sentence and OK for `1985-GR-VI.6.3.1.1.2-L2.1`'s quotation
  under GR13. Changed (PB5449 rewritten): PB5449 changes only each catalog's `outside-denominator` block (the `rules`
  arrays byte-identical), keeps `rule_book`'s extents, answers `(unnumbered prose, pending)` only for prose in no row,
  and names which files change bytes; the extent change, the GR13 answer and the re-pointing of stored citations move
  to PB5451.
- **F8 (missed readers; numbered prose items).** Reproduced: `paragraph-blocks` is keyed by clause with a positional
  `kinds`; `audit_catalog_coverage.py` reads its keys as clause labels; `extract_annex_a2.rule_ids_by_item()[11]` is
  empty. Changed: `paragraph-blocks` stays keyed by clause, `kinds` removed (PB5449); `audit_catalog_coverage.py`,
  `inventory_schema`, `gen_ledger.py`, `rendered_views.py`, `inert_lane.py` are PB5449 sites; `extract_annex_a2.py`,
  `audit_annex_a1.py`, `lineage.py`, `DerivedVerdictDriftTests`, `EcRaiseSiteDriftTests` and `EcRaiseSite.cs` are PB5451
  sites. A numbered item of a prose clause is a unit keyed by its printed number, id `<kind>-<clause>-<n>` (design point
  1 and 2; A.2 items expected `restates` of what their own citation resolves to, PB5450). The 226 count is the review's.
- **F9 (tables).** Reproduced: 36 tables in rule blocks (`out/tables-2023.json`); Table 16 inside `SR-14.9.25.3-10`'s
  text. Not reproduced as stated: Table 12 has 28 rows with both cells empty, not 24 (plus PERFORM with a terminator
  only, and 21 with a phrase). Changed (design point 1): three table kinds (entries: a unit per body row; matrix: a unit
  per row line; data: whole, or `not-a-rule: data` when a row consumes it), proposed by a shape test, keyed by digest
  of the cells; a table a row holds or names is `held-by` that row and never minted again, the audit refusing a table
  held twice. Table 12 decided: one unit per statement, the 22 non-empty rows expected rows, the 28 empty rows expected
  `restates` of the statement's general format; the fourteen 1985 records target them (PB5450, PB5454).
- **F10 (a list's closing tail).** Changed (design point 1, PB5449 item 1): a colon unit owns its list and the list's
  closing tail, decided by the measured level and the continuation test; §2023:14.7.5's tail is the example.
- **F11 (PB5319's FMT digests).** Reproduced the per-row counts (12, 17, 3, 3, 4, 3, 0). Changed: PB5451 names the
  re-stamp of those 1985 records with the reason-quotes-the-new-tail check (design point 8, PB5451).
- **F12 (figures and tiers).** Reproduced 63 (frame) and 142 (1985 Section II); both corrected in the design. PB5453's
  tier is now `PB479:4`. Not changed: PB5452 and PB5456 keep `PB479:2`, since a tier is the dependency depth and both
  need only step 1. The 55 is corrected (F1).
- **F13 (`1985-SR-X.4.6.3-2`).** Reproduced: no file in `inventory-1985/`; the row exists since a2b9c17e42
  (2026-11-07). Changed: PB5454 says it writes that record; PB5148's dated file and PB5448 say the 1985 half's residue
  is PB5454's.
