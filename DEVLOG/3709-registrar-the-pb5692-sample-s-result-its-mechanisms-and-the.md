## Entry 3709 — 2026-11-15 17:01 PST — Registrar: the PB5692 sample's result, its mechanisms, and the records they correct (8 2023 rows and one 1985 row)

**The 2023 GAP rises from 1 to 9, and that is not a regression in the compiler.** Eight 2023 rows were recorded CONFORMS
while the defects below already existed; the sample's code read found them, and this landing corrects the records to
PARTIAL, each naming the open note that holds its defect. Measured with `python scripts/spec/inventory_view.py --summary` at
this branch's merge of main c50b53fba2, before and after the nine inventory files: 2023 **1 → 9 GAP**, 2002 **1,980 →
1,983 GAP**, 1985 **835 → 844 GAP**. The 2002 and 1985 rises are KEPT rows that follow their 2023 targets through the chain
(`lineage.state_of`): 2002-SR-14.8.21.2-2, 2002-SR-7.2.13.2-2 and 2002-SR-7.2.10.2-4; at 1985 the eight rows named in PB5733 and
PB5734 and 1985-GR-XII.2.4-12's own re-record.

**Filed beside kb/Work PB5692** (its status unchanged; its owner closes it): the sample's result
(`PB5692/2026-11-15-sample-result.md`, from `E:/COBOL-coord/scratch/sample-shared/RESULT.md`) and the Mythos lane's adjudication of
its contradictions, verbatim (`PB5692/2026-11-15-adjudication-of-the-sample.md`).

**One note per mechanism, each repro re-run first** on this job's build of main 95e8e476ad at every edition named (log
`E:/COBOL-coord/scratch/op-20261115/registrar-sample/runs.log`):
- PB5731: at `--std 85` a format 3 record holding an OCCURS DEPENDING ON table is sized at the table's minimum (§1985:VII.3.8.4
  GR15 sizes it at the maximum): `r4-f3odo-clean` RAN (under-rejects), `r4-sdf3odo-clean` COBOL1757 and `r5-f3odo-key` COBOL0874
  (legal source refused); right at 2002 and 2023.
- PB5732: at `--std 85` the nested-COPY gate judges library text before REPLACING: `r4h.cob` COBOL0900 where the produced text
  holds no COPY statement (§1985:XII.2.4 GR12, §1985:XII.2.3 SR10); its control prints X=3.
- PB5733: two describers of one EXTERNAL indexed file agreeing on an alternate key: the second OPEN gets status 39 at 85, 2002 and
  2023 (`FileRegistry.AddAlternateKey` has no `::EXT::` guard). Wrong answer, MAJOR.
- PB5734: a reference-modified integer as UNSTRING COUNT IN, TALLYING IN or WITH POINTER, or STRING WITH POINTER, reaches Roslyn
  (CS1503) with no COBOL diagnostic, every edition (the PB5384 shape in `StrUnstrIsInteger`; PB5384 landed, so a new note).
- PB5735: INSPECT refuses a GROUP-USAGE NATIONAL group operand (COBOL0847) that GR2b treats as elementary national (2002, 2023).
- PB5736: `>>IF 1 = 1;` and `>>WHEN 1;` compile while `>>IF 1 = 1,` (COBOL1619) and `>>ELSE;` / `>>END-IF;` (COBOL1911) are
  refused: the cce fragment lexer hides a semicolon (2002, 2023).
- PB5737 and PB5738 (`kind: adjudication`, nothing decided): the 1985 reach of "variable occurrence data item" under SR1b (an
  OCCURS group inside an ODO table, accepted at every edition), and whether `METHOD-ID m AS literal` is reachable by m through a
  typed reference (COBOL0825 at 2002 and 2023). The text settles neither; each note states both readings with checked citations.

**Records re-recorded** through `record_verdicts.py` (dry run first), witnesses kept, prior sites kept and the defect's site
added, the prior prose kept as the account of the half that holds: 2023 `GR-12.4.5.3-1` (PB5733), `SR-14.9.48.3-5`,
`SR-14.9.48.3-6`, `SR-14.9.43.3-7` (PB5734), `SR-14.9.22.3-2` (PB5735), `SR-7.3.16.3-2`, `SR-7.3.13.3-4` (PB5736), and
`SR-13.18.29.3-3` (PB5711, its repro re-run at `--std 2023`: accepted); 1985 `1985-GR-XII.2.4-12` (PB5732, a CHANGED row with its
own verdict). The landed notes PB1079, PB1364, PB79, PB856 and PB88 claimed those 2023 rows in `closes_rows`; `work.py check`
refused the claims once the rows were PARTIAL, so each row left its `closes_rows` with a reason naming the open note (precedent
PB259).

**Not re-recorded, and why.** `SR-13.18.63.3-37` (PB5713 named it): its DOCUMENTED-NON-SUPPORT licence is VALIDATE's format 5
VALID/INVALID phrase, and PB5713's crash (re-run at 2023: `ArgumentOutOfRangeException` in `SetEmitter.EmitSet`) is a level-88
entry with the table format, so the record's own licence does not cover it and it is not contradicted (`PB5713/2026-11-15-...`).
The four 1985 KEPT rows whose 2023 targets stay right (`1985-SR-XI.4.4.3-10`, `1985-SR-XI.4.4.3-4`, `1985-GR-XII.2.4-8`, and
PB5737's `1985-SR-VI.5.8.3-1`): a KEPT rule holds no verdict of its own (`record_verdicts.py --dry-run --edition 1985` refuses one:
"a KEPT rule holds no verdict of its own (its chain's newest rule does)"), and a CHANGED re-record would assert a lineage relation
no read supports (DESIGN-spec-conformance-review section 10.7, D1). They stay closed through their chains until the lineage switch
(PB5682) or the 1985 own-edition adjudication PB5692 decided; their notes name them. The two 2002 screen rows the sample named
(`2002-SR-13.16.23.2-2`, `2002-SR-13.16.31.2-6`) already carry the screen facility's DOCUMENTED-NON-SUPPORT through their KEPT
targets (`SR-13.18.25.3-2`, `SR-13.18.33.3-6`), so RESULT.md's "stored CONFORMS" was not what the store holds; the sweep of the
stored 2002 KEPT rows whose rule text names the screen facility found 94, none sharing a CONFORMS
(`E:/COBOL-coord/scratch/op-20261115/registrar-sample/sweep.py`).

**Part 4.** The 15 rows whose named note had landed (PB5600 12, PB5609 2, PB5601 1) were re-run at `--std 2002`
(`registrar-sample/runs4.log`): each is now right, so each went to the 2002 read's `dropped-rows.json` as needing a witness.
`2002-RV-15.16.2-1` still prints `+1400` at 2002; no note owned D-UTC-OFFSET, so PB5741 (`kind: decision`, `status: owner`) holds
it for the owner. **Part 5.** PB5739: `buildgraph.py`'s unbounded `git ls-tree` read hung a gate under memory pressure (related
PB5697). `PB2119/2026-11-15-owner-wait.md`: the owner's "Wait" on the Delete program until 2002 and 1985 read zero GAP.

Every citation above went through `python scripts/spec/cite.py --check` (verdict OK; log
`E:/COBOL-coord/scratch/op-20261115/registrar-sample/cites.log`). `work.py check`: 4,825 work items, all well-formed;
`inventory_view.py --check`, `lineage_reads.py --check` and `edition_gates.py --check` GREEN after the re-records. No change
record's kind changed, so no edition-gate proof moved.

