## Entry 3272 — 2026-11-07 14:08 PST — PB4886: the 1985 audit's tooling — the rule catalog takes an edition, and one batch tool judges the mapping

The 1985 compiler is to be audited against the 1985 standard itself, the way 2023 is, and that audit waits for the
reconciled 1985 transcription (PB4850). This landing builds its tooling beside that work, and runs no batch.

**The design** is one new section of the one audit's design doc (DESIGN-spec-conformance-review.md §10): the edition is a
parameter of the 2023 audit, not a second audit. It fixes the 1985 row key (the Roman-section locator, since 1985 restarts
its paragraph numbers in every section; the module; the level from the level-2 box; the amendment), the SAME/DIFFERS/NONE
mapping onto the 2023 rows (a SAME row carries the 2023 verdict by reference, never a copy), the 1985 rows in the same
inventory store, and the 1985 GAP count beside 2023's.

**The catalog.** `extract_rule_catalog.py` takes `--edition`; its 2023 arm is unchanged, and an edition with no arm (2002)
exits naming the table. The 1985 arm, `rule_catalog_1985.py`, reads the document's own structure through `citation.py`'s
readers, so the catalog and `cite.py --check` agree on what a paragraph is. It harvests the Syntax Rules, General Rules,
Arguments and Returned Values blocks and the general formats, marks each row's box and level, and reads the two amendments'
change tables: a change that names a rule records its amendment on that row, and a change that adds a rule becomes a row of
its amendment, cited by the amendment's own page. On the current, unreconciled file, `--check` counts 1,988 rows from 614
blocks and reports 49 findings. It writes no catalog. Among the findings are 18 duplicate ids, because the transcription
prints Section X's paragraphs 2.5 and 3.1 twice, on PDF pages 526 to 529. Another 11 are ordinals that break their lists,
which is the reconciliation's to check. The other 13 are rule-shaped blocks ("Editing Rules", "Presentation Rules") whose
kind the 1985 text does not type, which the audit must adjudicate.

**The batch tool.** `audit_batch.py` runs the four passes (map, verify, claims, drafts) through the Message Batches API.
Its restartable state is not a copy: `message_batches.py` now holds the batch state `transcribe_pdf.py` had (the intent
before every create, the batch after, a crash adopted from the server's list), and both tools use it. The self-tests share
one fake client. A full submit is refused until the pass has a collected canary with a valid answer. `--max-usd` guards
every submit, priced by the canary's measured cost. A prompt carries only the tracked text, and a stamp shape refuses the
submit. A dry plan of the map pass on the current file estimates about $34. No API call was made, and no test opens a key
file.

**Found on the way (fixed here).** An amendment's part heading did not end the 1985 sections in `citation.py`, so
`cite.py --check "§1985:XVIII.8"` validated Amendment 1's addendum paragraph as a paragraph of the Index. The heading now
ends them, with a planted case.

**Self-review** found two silent wrong rows in the amendment arm before the gate. A change that adds a rule but names
no paragraph was read as a change to the base rule of that number. An added rule with no new text took the change's own
wording as its text. Both are now unresolved changes. The first gate was red on six text-mode writes in the new
self-test that did not state `newline=`. The re-gate of the tooling at 0e712ea read `=== BUILD-LOCAL GATE: GREEN —
Conformance 22,482/22,482 · Unit 40,428/40,428 · Characterization 37/37 cases ran (skipped 0)` (run
000002-20261107T210023Z).

**Branch review** (round 1) found that the batch state keyed each reply's usage by its custom id alone. A retried item
has the same custom id, so the retry's usage overwrote the earlier billed reply, a cut-off one usually the costliest. The
reviewer's probe showed $0.0140 spent where $0.1000 was billed, so `--max-usd` checked a spend that was too low. Usage is
now keyed by batch and custom id (`BatchRun.usage_key`). The probe now reads $0.1000, and three new self-test cases cover
the retried reply, the cut-off reply and a repeated collect.

PB4886 stays open: the audit itself, and the edition parameter of `build_inventory.py` and the ledger, wait for PB4850.

