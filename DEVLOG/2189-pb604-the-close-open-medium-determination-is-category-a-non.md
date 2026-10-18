## Entry 2189 — 2026-10-18 02:46 PDT — PB604: the CLOSE/OPEN medium determination is category (a) Non-unit, and two inventory rows stop calling it single-unit

kb/Work PB604 named two follow-ups to PB235's CLOSE landing. Both were prose in the traceability inventory; no compiler
change was needed or made.

**Row GR-14.9.27.4-19 (the OPEN current volume pointer).** Re-probed on a fresh build: `pb141_close_forms` at `--std 85`
prints `REEL=07 UNITR=07 NOREW=07`, so the compiler reports the status only the Non-unit column of Table 14 produces
(§9.1.13.2 item 6, §14.9.6.4 GR2 a)). The row's CONFORMS derivation placed every file in category (b) ("exactly ONE
reel/unit"), which carries no symbol g and so could never print `07`. Its verdict was right a fortiori; its ground and a
stale warning were wrong (GR11's `07` arm landed with PB317, and docs/CONFORMANCE.md no longer states the opposite). The row
was re-recorded through `record_verdicts.py`: sequential files are category (a), relative and indexed (d), a category (a)
file has no unit for the §9.1.10 pointer to designate, and the verdict holds under either reading. The placement test
`CloseTable14Tests.Category_PlacesEverySupportedConnectorKind` was added as a witness. All citations were run through
`cite.py --check`.

**Sibling sweep.** A scan of every inventory row's notes for the single-unit wording found `GR-14.9.30.4-20` (READ PREVIOUS
needs a single reel/unit mass storage file) with the same phrase and a second stale claim, that sequential `READ ... PREVIOUS`
is silently read as NEXT. PB334 fixed that; re-probed here, the first PREVIOUS after `OPEN INPUT` returns the first record
(GR21 b) and the second ends with status `10`. Its prose was re-recorded; its signed derivation is unchanged.

**Row DOC-A.1-24.** Not reproduced: the row has been CONFORMS and OK, with code-location and four test refs, since the
doc-rows-1 batch (PB1522), and `audit_annex_a1.py --check` reports no remaining obligation. Only its notes still called PB604
open; that sentence was re-recorded.

**Rows.** No GAP moved (66 before and after): `closes_rows` is empty with its reason. The self-review looked for other rows
that put a file in category (b) or (c) and for notes that cite a landed defect as open; the two above were the only hits.

