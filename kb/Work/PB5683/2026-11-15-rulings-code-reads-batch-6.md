# Rulings on the non-agreeing rows of the 1985 code reads, batch 6 (the Mythos lane, Sunday 2026-11-15 23:45 PST)

**The batch.** The operator's run wf_053488df-a31: slices 1985-105 to 1985-124 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 of the second population, read on the code at
`--std 85` (main 1df234fa72). 108 agree with the inherited verdict. Twelve do not
(`coderead-1985/non-agreeing-105-124.json`).

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) derived each from the
1985 text and ran its own fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main d7d3f99539:
`my-20261115/contradictions-b6/rulings.json`, programs under `contradictions-b6/src/`. I ran two myself at `--std 85`
with the tracked probe runner: `unz.cob` printed `X=[QQQ] P=01` and then `OVERFLOW-B` (neither phrase of the first
UNSTRING ran), and `addc.cob` printed `AA=100 JJ=2 BB=400`.

**Ruling: ten mechanisms. Six UPHELD (two of them existing notes), four REJECTED.**
Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-VI.6.17.4-1`, `1985-GR-VI.6.17.4-2` | UPHELD (b) | wrong_answer | INITIALIZE ... REPLACING treats a reference-modified alphabetic item as alphanumeric, so the wrong REPLACING phrase applies (`M2[aQc]` where `abc` is right). Both editions keep the item's category. Leads: the same "a reference-modified slice is always alphanumeric" assumption is written in `MoveOverlap.cs` and `ExpressionBinder.cs`, not probed. | PB5813 |
| 2 | `1985-GR-VI.6.18.4-1` | REJECTED | none | INSPECT handles the characters inside the current count correctly; what it blanks lies past the DEPENDING ON value, whose content is undefined, so no conforming program can see it. | (none) |
| 3 | `1985-GR-VI.6.6.4-3` | UPHELD (a) | wrong_answer | ADD CORRESPONDING at 85 evaluates the group operand's subscript afresh for each pair (`BB=400`); the 1985 text as amended requires once, at the start (`BB=200`, which 2023 prints). See below: this overturns part of the documented determination D-CORR85. SUBTRACT CORRESPONDING takes the same switch. | PB5814 |
| 4 | `1985-GR-VII.2.6.3-2`, `1985-GR-VII.4.3.4-17` | UPHELD (b) | wrong_answer | A file written with ADVANCING or under LINAGE is opened INPUT or I-O with status 00 and then read as fixed blocks: records that were never written come back, a READ past the last record returns 00 with no AT END, and REWRITE overwrites a line end. The 1985 text allows the records as written or status 37 at OPEN, not this. `docs/conformance/7/DOC-A.1-146.md` (c') does not cover reading such a file back. | PB5815 |
| 5 | `1985-GR-VI.6.27.4-17` | UPHELD (a) | wrong_answer | UNSTRING from a zero-size sender with POINTER 1 runs neither ON OVERFLOW nor NOT ON OVERFLOW at 85. `§1985:VI.6.27.4 GR17` makes a pointer "greater than the size of the data item referenced by identifier-1" an overflow. The 2023 zero-length guard is applied with no edition test. I ran it. | PB5816 |
| 6 | `1985-GR-VII.4.4.4-6` | UPHELD (b) | crashes, rejects_legal_source | The trigger of the open kb/Work PB5729, at 85 as at 2023, with a second symptom that note lacks: a numeric redefined item gives a C# backend error (CS0103, exit 70) and no COBOL diagnostic; an alphanumeric one overflows the stack. | joins PB5729 |
| 7 | `1985-GR-VII.4.4.4-13` | UPHELD (b) | wrong_answer | The mechanism of kb/Work PB5785 in its `RECORD IS VARYING ... DEPENDING ON` form: status 00, the DEPENDING item says 15, and READ INTO moves 10 characters. By the ruling beside PB5785 the RECORD clause sizes the move, here through the item it names: 15 characters. | joins PB5785 |
| 8 | `1985-GR-VII.3.7.4-11` | REJECTED | none | GR11 binds the run unit; 1985 requires no detection of a violation. That the compiler checks one of its three parts is an extra, and the two it does not check are no defect. | (none) |
| 9 | `1985-GR-VI.6.23.4-1` | REJECTED | none for this row | The index association the row covers is right. The swapped index-name reference the refuter built belongs to `1985-SR-IV.4.3.8.2.3-3`, where the text admits two readings: an OPEN question of its own. | PB5817 (the open reading) |
| 10 | `1985-GR-VI.6.4.3-5` | REJECTED | none | The program's `FF`, `HH`, `KK IN G2` are ambiguous references, so it violates GR5 itself and its result is undefined. The checker rates this medium: if "unique" meant unique among the items that meet the CORRESPONDING conditions, FF would pair and the row would be a wrong answer. I take the first reading: GR5 says "each data item which satisfies the above conditions must be unique after application of the implied qualifiers", and these are not. | (none) |

**Mechanism 3, ruled, with what it overturns.** `§1985:IV.4.3.8` as amended reads: "Unless otherwise specified by
the rules for a statement, any subscripting and reference modification are evaluated only once as the first
operation of the execution of that statement." (cite.py OK; the sentence is Amendment 1's, X3.23a-1989, applied in
place in the transcription, kb/Work PB5045.) MOVE CORRESPONDING does specify otherwise: "The results are the same as
if the user had referred to each pair of corresponding identifiers in separate MOVE statements"
(`§1985:VI.6.19.4 GR1`), so its per-pair evaluation at 85 stands. ADD and SUBTRACT CORRESPONDING specify nothing, so
the general sentence governs them: once, at the start. `docs/conformance/3/D-CORR85.md` (kb/Work PB5185) held that
for ADD and SUBTRACT "the standard leaves the choice to the implementor" and took GnuCOBOL's per-pair behaviour.
Its premise is wrong for the amended text, and where the standard controls CLAUDE.md rule 1 never reaches GnuCOBOL.
So: at `--std 85` ADD and SUBTRACT CORRESPONDING evaluate the group operands' subscripts once; D-CORR85 is corrected
for those two verbs and keeps MOVE; and the two cases that pin the per-pair result
(`pb5185_add_corresponding_subscript_per_pair`, `pb5185_subtract_corresponding_subscript_per_pair`) are re-derived
with the fix.

**What follows.**
- Rows of mechanisms 1, 3, 4, 5, 6 and 7 do not close on their inherited verdict. Rows 2, 8, 9 and 10 are confirmed.
- The 2023 rows of mechanisms 1, 4, 6 and 7 are re-recorded non-conforming by a registrar (the checker's
  `rulings.json` names them).
- Rate at 1985 so far: 804 rows read (the sample 60, the first population 504, batches 5 and 6 240); 64
  contradicted, about 8 %; 6 undecided on open readings (PB5789, PB5799, PB5800, PB5805, PB5812, PB5817).
