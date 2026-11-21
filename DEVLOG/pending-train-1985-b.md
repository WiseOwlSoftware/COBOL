## 1985 train B: eleven fix branches in one landing; the mint's 195 rows open, the import's 131 closed, every 1985 refusal cites the 1985 rule

Wave 20262131, one train (owner: one gate and one landing for the group). Merged in order: `train85b-seg` (PB6007,
PB6008), `train85b-arm` (PB6012), `train85b-editions` (PB5995), `train85b-ext` (PB5996), `train85b-cat` (PB6001),
`train85b-mint` (PB5452), `train85b-rows` (PB5453, PB6016), `train85b-small` (PB942, PB382, PB5139, PB4955, PB672,
PB5548), `train85b-areaa` (PB6011), `train85b-cite` (PB5993, PB6013, PB5978, PB5530's 1985 half, and group c's PB6009,
PB6010), `train85b-intarg` (PB5889).

**One reach walk (PB6007, PB6008).** COBOL-85's segmentation restrictions and its RELEASE/RETURN placement rule both
ask which paragraphs control reaches from a range. The segmentation walk dropped a path that left the named procedures
and came back, and the sort-merge range let a GO TO's destination flow on to the end of the program. `ProcedureReach.Walk`
is now the one walker: each step carries the return point of its range, and fall-through ends there, at an
unconditional GO TO or at STOP RUN (§1985:XI.4.1.4 GR9, §1985:XI.4.4.4 GR8/GR11). Only --std 85 changes.

**The register and the derivations.** A seventh derivation arm, `informative`, closes a row whose every sentence informs
the reader of the standard (PB6012); the eleven 1985 presentation rows closed on it. Every open work note declares the
editions whose completion it blocks (`editions:`, PB5995): the backfill wrote 1548 notes (the 1985 open-note count 120
by the old patterns and 120 by the field), then a reader and a refuter narrowed 103 of them and nine stale notes closed
on evidence checked on the merged tree; `edition_status.py` reads the field and nothing else.

**EXTERNAL (PB5996).** Two programs of one compilation group that describe one EXTERNAL record differently are refused,
COBOL7735: at 85 by size (§1985:X.4.5.4 GR2), at 2002 and 2014 by size and the record name's VALUE clause, and from 2002
the records of an EXTERNAL file description; 2023 keeps its run-time EC-EXTERNAL-FORMAT-CONFLICT.

**The 1985 catalog (PB6001, PB5452).** A numbered list is held by its marker, an HTML table is read as a table and a
caption below it numbers it, and a refuter record is matched by its unit's clause, digest and order. §1985:III.2 and
§1985:B.2.1 take the block disposition `units`. The prose-holes measure reads the dispositions and their UPHELD
records: 357 holes become 16 (the Examples of §1985:VI.6.18.5). The 85w campaign's 505 dispositions are imported and
minted: 195 new rows, 290 `restates`, 20 `not-a-rule`, no unit pending.

**The rows (PB5453).** 129 of the 131 rows the import left GAP closed (74 refusal probes with accepting controls written
from the 1985 text, 14 probes staged with library texts by `run_probe.py --with <place>=<file>` (PB6016), 11 on the
informative arm), and on the train the last two (1985-GR-IV.4.1-p2, 1985-SR-IV.4.1-p3) on `unpopulatable-antecedent`
(slice s-101). That import first met PB6020: group e's catalog fix renumbered five rows the import had recorded; their
store records moved through `record_verdicts.py` MOVE records, but a move left no tracked record and the sealed read
records still named the old ids, so `audit_batch.py import-reads` refused every later import. Moves and splits now keep
one tracked copy (`docs/rearchitecture/spec-moves-<year>/`), each move naming the slices that read its old id, and
`lineage_reads.load` reads those slices under the new ids.

**Diagnostics at 85.** Every 1985 refusal cites the 1985 rule and names no ISO standard for it: the designation comes from
`EditionInfo.Designated` (PB6013), the citation from `EditionInfo.CiteRule`, and the catalog says for every descriptor
that cites a later edition either its 1985 rule or why the 1985 text has none (PB5993: 494 of 669 moved, 175 on a ratchet
that only shrinks). A comment-entry paragraph is quoted by its header (PB6009); seven refusals cite their 1985 clause
(PB6010). A separator period forgotten after a data entry is reported once, where it is missing: the parser knows which
tokens open their fixed-form line in area A (PB6011; the procedure-division twin is PB6018). A level-88 range whose
endpoints both break one VALUE rule draws one diagnostic (PB942); `01 CLASS PIC X.` names the reserved word (PB382).

**Integer arguments (PB5889).** The intrinsic integer-argument screen refuses only on a counterexample (COBOL1627 names
the operand values), accepts a proven form, and checks an undecided one at run time (EC-ARGUMENT-FUNCTION from 2002;
termination at 85).

The integration fixed three seams: the five moved adjudications and their two cases' `rules:` tags, the stale derivation
refutation file of slice s-12, and two later groups' messages (COBOL7735, COBOL1627's refuted-expression text) routed
through the edition's designation. `edition_status.py --edition 1985`: `=== EDITION 1985: NOT COMPLETE — 195 inventory
GAP · 8 code reads · 52 open work notes · 16 catalog prose holes ===`.
