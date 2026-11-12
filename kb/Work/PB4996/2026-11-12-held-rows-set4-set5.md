**2026-11-12: held sets 4 and 5 recorded; 10 CONFORMS, 8 DIVERGES and 2 PARTIAL, one CONFORMS overturned to PARTIAL by the new PB5393 (1985 GAP 381 to 372); branch records-85h.**

**The sets.** Set 4 (`E:/COBOL-coord/scratch/held-85/set4/decisions.json`) is ten rows a refuter found DOUBTFUL; a Mythos adjudicator decided all ten CONFORMS, three of them under an implementor choice. Set 5 (`held-85/set5/decisions.json`) is ten rows a refuter OVERTURNED; an Opus adjudicator confirmed every overturn, eight DIVERGES and two PARTIAL, each held by an open note. The refuter had already attacked every row before the adjudicator decided it, so there is no second refute file. The merge is `coderead-85/held_batch_decided.py`, a copy of `held_batch.py` that takes the set names as arguments and records every decision with the adjudicator's code-location and reason. A CONFORMS decided under an implementor choice also names the choice and its `docs/CONFORMANCE.md` section; a non-conforming verdict names its note.

| Outcome | Rows |
|---|---|
| Set 4, recorded CONFORMS (10) | `1985-GR-XIV.3.2.4-3`, `1985-GR-XIV.3.3.4-2`, `1985-GR-XIV.3.2.4-7` and `1985-GR-XIV.3.3.4-6` (implementor choice: the omitted KEY phrase), `1985-GR-XIV.3.6.4-10` (implementor choice: a SEND with no FROM), `1985-SR-XI.2.5.3-5`, `1985-GR-XIV.2.2.4-10` (the choice already recorded under PB5280), `1985-SR-XIV.2.2.3-5`, `-9`, `-14` |
| Set 5, recorded DIVERGES (8) | `1985-GR-VI.5.9.4-8` (PB5375), `1985-GR-XIV.3.2.4-1` (PB5376), `1985-GR-XIV.3.5.4-3`, `-8`, `-9` (PB5377), `1985-GR-XIV.3.6.4-3` (PB5378), `1985-GR-XVI.1.4.3-L2.1` (PB5380), `1985-GR-XIV.2.2.4-31` (PB5381) |
| Set 5, recorded PARTIAL (2) | `1985-GR-XIII.3.2.4-3` (PB5202), `1985-FMT-XVI.2.3.2` (PB5379) |
| Re-adjudicated, CONFORMS to PARTIAL (1) | `1985-GR-XIV.2.2.4-34` (the new PB5393) |

**Recorded.** `record_verdicts.py --edition 1985 held-set4-set5-batch.json` ran as a dry run, then for real: "rows changed : 20", "CONFORMS 10 — 10 closed the GAP, 0 still test-needed", "DIVERGES 8", "PARTIAL 2", "GAP : 381 → 371 (+10 closed of 2274)". The second batch, `held-set4-gr34-batch.json`, keeps the row's six code-locations, adds `MessageStoreState.cs#FindQueue` and `MessageDefinitions.cs#FindQueue`, and appends the overturn to its notes: "1985-GR-XIV.2.2.4-34: CONFORMS → PARTIAL", "GAP : 371 → 372 (-1 closed of 2274)". The decision names no verdict string for the row; PARTIAL is mine, because every other Table 1 association the read probed is right. `inventory_view.py --summary` moved from 670 adjudicated (CONFORMS 519, PARTIAL 136, DIVERGES 11) and 381 GAP to 690 adjudicated (CONFORMS 528, PARTIAL 139, DIVERGES 19) and 372 GAP.

**The new defect, PB5393.** ENABLE INPUT and DISABLE INPUT never answer status 80. Table 1 marks 80 possible for both, and §1985:B.2.2 returns it where conditions 15 and 20 both apply: a known queue level already in the requested state with an unknown level below it. `MessageControlSession.SetPath` answers 20 as soon as `FindQueue` fails. The repro `held-85/set4/p/p1.cob` reproduced on the pinned compiler (`E:/COBOL-wt/coderead85-tree-b`): `ENA Q1/SX fresh 20`, `DIS Q1/SX 20`, `DIS Q1/S2/S9 20`, each expected 80, and `ENA Q1 fresh 15` shows the compiler judges the prefix correctly on its own. The note's Sites name the design doc's sentence "Status 80 appears only for destinations, the one place one statement names several" (`DESIGN-communication-facility.md` §6, "Logical paths" paragraph, line 362), so the fix corrects it. Every citation in the note passed `cite.py --check`.

**Amendments to the set 5 notes**, one dated file each (`<id>/2026-11-12-held-row-adjudication.md`):
- PB5375: names PB5187, whose landing fixed only the comparison arm of the same row; `related:` adds PB5187.
- PB5202: also claims `1985-GR-XIII.3.2.4-3`; the "or not DISPLAY" arm cannot arise in a report group.
- PB5376: `related:` adds PB5306 and PB5315.
- PB5377: the fix's test checks that every position past the transferred text, up to the group's maximum length, is unchanged.
- PB5378: the storage measure is not an alternative; §1985:IV.4.3.4 decides the standard-data-format measure.
- PB5379: confirmed; nothing to amend.
- PB5380: states its reading of the first iteration (the PERFORM's own entry is explicit), on which the verdict does not depend.
- PB5381: takes the lowercase-terminal arm ('t1' after a 'T1' lock is changed content; `p31b` prints LOWER 00, expected 20).

**docs/CONFORMANCE.md section 3.3.**
- "The KEY phrase at COBOL-85" said "CCVS-85 programs omit it". That is false. A parse of every ENABLE and DISABLE statement in the ten CCVS-85 programs that have one (`git grep` of `tests/nist/programs` finds CM101M to CM105M, CM201M, CM202M, CM303M, CM401M and DB205A) found 48 statements, none without KEY. The entry now rests on the text's grounds (§1985:p.XVII-84, obsolete item 11 and the unspecified match rules; §1985:p.XVII-93, items 77 and 78) and says that a written KEY of spaces is still matched while an omitted KEY is honored.
- A new row documents the second implementor choice: a SEND with no FROM carries its indicator as an empty portion, so a lone EGI after an EMI is an empty message that ends the group (§1985:XIV.3.6.4 GR6, GR9, GR10, GR11).
- Every citation added passed `cite.py --check`. `doc_audits.py --check` is GREEN (30 checks), and `work.py check` reports 4500 work items, all well-formed.
