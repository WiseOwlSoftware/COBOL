# Rulings on the non-agreeing rows of the 1985 code reads, batch 8 (the Mythos lane, Monday 2026-11-16 00:25 PST)

**The batch.** The operator's run wf_4669deef-ad2: slices 1985-145 to 1985-164 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 of the second population, read on the code at
`--std 85` (main 1df234fa72). 110 agree with the inherited verdict. Ten do not
(`coderead-1985/non-agreeing-145-164.json`).

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main d7d3f99539:
`my-20261115/contradictions-b8/rulings.json`, programs under `contradictions-b8/src/`. I ran two myself at `--std 85`
with the tracked probe runner. `m8sdref.cob` was refused: `error COBOL2973: WRITE 'SW-N'`, citing a rule of the 2023
text. `m6mode.cob` printed `OPEN INPUT SF WHILE OPEN OUTPUT 41` with no declarative before it, then `USE-OUTPUT SF=41`
for the same-mode OPEN, then `OPEN INPUT GF WHILE OPEN EXTEND 41` with no declarative.

**Ruling: eight mechanisms. Six UPHELD as defects (four new notes, two existing), two NOT DECIDED.**
Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-XIII.3.10.8-4`, `1985-GR-XIII.3.15.4-4`, `1985-GR-XIII.3.16.4-4` | UPHELD (a) | wrong_answer | Report Writer. After a body group's absolute NEXT GROUP saves its integer, the next relative group whose LINE integers sum to zero (a control footing with LINE PLUS 0) is sent to the next page. The 1985 fit test keeps it on the page: a body group HAS been presented there, so the saved integer is not consulted. The forced page advance is the 2023 rule, applied at every edition. Narrow: only a relative group of integer sum 0 whose lower limit is at least the FOOTING line. The rules it rests on are printed text (folios XIII-32 to XIII-36), not reconstruction. | PB5823 |
| 2 | `1985-GR-XIII.3.10.7-3` | NOT DECIDED | none shown | The first line of a page heading after a report heading. The code matches folio XIII-30 as RECONSTRUCTED, and only the printed folio settles it (kb/Work PB4952 reconstructed it and is landed, so the question gets a note of its own). | PB5830 |
| 3 | `1985-GR-XIII.3.16.4-3` | NOT DECIDED | wrong_answer under one reading | TERMINATE's control break level. The text supports FINAL (when FINAL is a control) and it supports data-name-1; writing FINAL changes the result at both editions. An open reading: rule 1's order, then a conformance entry. GnuCOBOL was not measured (it is not on this host). | PB5824 |
| 4 | `1985-GR-XIII.3.7.4-2` | UPHELD (b) | wrong_answer | The mechanism of the open kb/Work PB5782: moving negative zero into a numeric control item causes a control break though the item equals ZERO. | joins PB5782 |
| 5 | `1985-GR-XIII.4.8.4-4` | UPHELD (b) | wrong_answer | The mechanism of kb/Work PB5821 in the report module: a container's USE GLOBAL ... ON OUTPUT runs for a file its own non-GLOBAL USE names. | joins PB5821 |
| 6 | `1985-GR-XIII.4.8.4-5` | UPHELD (b) | wrong_answer | OPEN INPUT of a file already open OUTPUT (or EXTEND) fails with status 41 and runs no declarative, though USE ... ON OUTPUT (or ON EXTEND) is declared. See below. I ran it. | PB5825 |
| 7 | `1985-SR-VI.6.22.3-3` | The ROW is confirmed; a DEFECT outside it is UPHELD (a) | under_rejects | `WHEN 1 + 1 = 2` and `WHEN 3 IS POSITIVE` compile clean at 85. SEARCH's SR3 admits any conditional expression and the compiler admits it, so this row stands. What is broken is the 1985 text of the relation and sign conditions, which has no catalog row. See below. | PB5826 |
| 8 | `1985-SR-XI.3.2.3-3` | UPHELD (a) | rejects_legal_source | At 85 the compiler refuses (COBOL2973) a data item of a sort-merge file's record used as an operand of ANOTHER file's statement. 1985 forbids only input-output statements for the sort or merge file itself; the operand restriction first appears in 2002. I ran it. | PB5827 |

**Mechanism 6, ruled.** `§1985:XIII.4.8.4 GR5b` runs the OUTPUT procedure "for any file open in the output mode or
in the process of being opened in the output mode" (cite.py OK). A file open in the output mode on which an OPEN
INPUT fails with 41 is a file open in the output mode, so the OUTPUT procedure runs. The compiler answers with the
ATTEMPTED mode only (`FileConnector.OpenModeView`, since kb/Work PB5200), so with only OUTPUT and EXTEND procedures
declared nothing runs. Which procedure runs when both the current and the attempted mode have one is not stated by
the text and is not part of this ruling. The checker found the same at 2023 (row `GR-14.9.49.4-6`).

**Mechanism 7, ruled.** `§1985:VI.6.3.1.1` says: "The relation condition must contain at least one reference to a
variable" (cite.py OK), and the sign condition's text says the same of its arithmetic expression. The compiler at 85
applies the 2023 rule instead (an operand that is not a literal, `docs/conformance` D-RELLITERAL), which `1 + 1 = 2`
passes. That is a defect at 85 and is filed. It is not a contradiction of `1985-SR-VI.6.22.3-3`: that rule grants
"any conditional expression" and is met. The two sentences are clause text with no row in the 1985 catalog, so no
1985 row could have caught them: the note carries that as a catalog gap.

**Mechanism 8, ruled.** `§1985:XI.3.2.3 SR3` forbids only that "input-output statements may be executed for this
sort or merge file" (cite.py OK on the words "no input-output statements may be executed for this sort or merge
file"). `WRITE OF-REC AFTER ADVANCING SW-N LINES` is a statement for another file, and its operand needs only to be
an integer item. The refusal's own text cites `§2023:13.4.6.3 SR4`; the same words first appear at
`§2002:13.3.5.2 SR4` (cite.py OK). The screen needs an edition gate below 2002.

**What follows.**
- Rows of mechanisms 1, 4, 5, 6 and 8 do not close on their inherited verdict (seven rows). The row of mechanism 7
  is confirmed. The rows of mechanisms 2 and 3 wait.
- The 2023 rows of mechanisms 4, 5 and 6 are re-recorded non-conforming by a registrar (the checker's
  `rulings.json` names them).
- Rate at 1985 so far: 1,044 rows read (the sample 60 and 984 of the second count); 76 contradicted, about 7.3 %,
  and one more defect found outside its row. Open readings: PB5789, PB5799, PB5800, PB5805, PB5812 (two rows),
  PB5817, PB5824, PB5830 and the EXTEND question beside PB5815.
