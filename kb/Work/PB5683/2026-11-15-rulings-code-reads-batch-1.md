# Rulings on the non-agreeing rows of the 1985 code reads, batch 1 (the Mythos lane, Sunday 2026-11-15 18:55 PST)

**The batch.** The operator's run wf_c2805f83-26b: slices 1985-001 to 1985-020 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 read on the code at `--std 85` (main 5e49d46678), each
CONFORMS attacked by a refuter. 111 agree with the inherited verdict. Nine do not
(`coderead-1985/non-agreeing-001-020.json`): the refuter overturned seven and the reader found two non-conforming.

**How each was checked before it counts.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`)
derived each from the 1985 text, rendered the printed page for the five general-format rows, wrote its own
fixed-form program and ran it at `--std 85` and `--std 2023`; its evidence is
`my-20261115/contradictions-b1/rulings.json` with the programs under `contradictions-b1/src/` and the pages under
`contradictions-b1/pages/`. I then ran two of its programs myself and checked their citations with `cite.py --check`
(both OK): `m1ren.cob` printed `R2=[ABCD]` at `--std 85`, and `m3ctl.cob` was refused at `--std 85` with COBOL1700.
The seventh mechanism I had ruled an hour earlier from a different lead
(`my-20261115/adjudications-20261115-evening.md`, section 1).

**Ruling: all nine rows are UPHELD, seven mechanisms.** Cause (a) = wrong at 85 and right at the latest edition;
cause (b) = wrong the same way at 2023, so the 2023 row's stored CONFORMS is wrong too.

| # | 1985 rows | Cause | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-FMT-VI.5.11.2`, `1985-GR-IV.4.3.2.1-1` | a | under_rejects | `66 R2 RENAMES R1`, R1 itself a level 66, compiles and runs at 85 against `§1985:VI.5.11.3 SR4` ("A 66 level entry cannot rename another 66 level entry"); refused at 2023 (COBOL1655). I ran it. | PB5772 |
| 2 | `1985-FMT-VI.6.22.2` | b | under_rejects | SEARCH ALL accepts a parenthesised WHEN condition, which Format 2 does not print (page 354 rendered). 2023 row `FMT-14.9.37.2`. | PB5773 |
| 3 | `1985-FMT-XIII.3.7.2` | a | rejects_legal_source | An index data item as a CONTROL is refused at 85 (COBOL1700, citing a 2023 rule); `§1985:XIII.3.7.4 GR3b` provides for exactly that control, and 1985's limit on explicit references (`§1985:VI.5.14.3 SR5`) is about Procedure Division statements (`§1985:IV.4.4.1`). I ran it. | PB5774 |
| 4 | `1985-FMT-XIII.4.3.2`, `1985-FMT-XIII.4.9.2` | b | under_rejects | With a Working-Storage `01 DA` beside a report group `01 DA`, an unqualified `GENERATE DA` and `USE BEFORE REPORTING DA` compile and run; the name is ambiguous (`§1985:IV.4.3.8.1 SRL2.1`). 2023 rows `FMT-14.9.16.2`, `FMT-14.9.49.2`. | PB5775 |
| 5 | `1985-GR-VI.5.8.4-3` | b | wrong_answer | STRING into a group that holds an OCCURS DEPENDING ON table with its count item inside the group overflows and stores short. 2023 row `GR-13.18.38.4-8`. | PB5776 |
| 6 | `1985-GR-VI.6.27.4-13` | b | wrong_answer | UNSTRING with no DELIMITED BY examines such a group receiver (count item outside) at its maximum size and takes the wrong characters. 2023 row `GR-14.9.48.4-11`. | PB5777 |
| 7 | `1985-GR-VI.5.4.4-2` (and `1985-GR-VI.6.3.1.1.2-1`) | a | wrong_answer | A relation on a BLANK WHEN ZERO item holding spaces is evaluated as if the item were zero. | PB5771 |

Mechanisms 2, 4, 5 and 6 I did not run myself; the checker rates 2, 5 and 6 high and the MOVE sibling of 4 medium.
The implementer who takes each note reproduces it first, as every fix does.

**Leads, not ruled** (each belongs in the note of its mechanism):
- `ReceivingStore.CharacterPositions` is also used by the ACCEPT and DISPLAY emitter, so mechanism 6 may reach them.
- Kb/Work PB5188 (landed) refuses an index data item as a report SOURCE at 85 on the reading of SR5 that mechanism 3
  rejects; it may be wrong for the same reason.
- Mechanism 4's site for an ordinary MOVE of the ambiguous name was not read.

**What follows from it.**
- The nine rows do not close on their inherited verdict. Each gets a `contradicted` record in the code-read store
  naming its note (kb/Work PB5759, `code_reads.py adjudicate`, once the store is on main).
- The five 2023 rows of the cause (b) mechanisms are re-recorded non-conforming with the note's id by a registrar:
  the 2023 count rises by five, and that is the honest figure.
- Rate so far at 1985: the sample 6 of 60, this batch 9 of 120; 15 of 180 rows, about 8 %.
