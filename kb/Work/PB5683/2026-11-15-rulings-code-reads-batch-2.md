# Rulings on the non-agreeing rows of the 1985 code reads, batch 2 (the Mythos lane, Sunday 2026-11-15 19:45 PST)

**The batch.** The operator's run wf_d1ad4982-478: slices 1985-021 to 1985-040 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 read on the code at `--std 85` (main 5e49d46678). 107
agree with the inherited verdict. Thirteen do not (`coderead-1985/non-agreeing-021-040.json`): the refuter
overturned seven, doubted three, and the reader found three non-conforming.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) derived each from the
1985 text, rendered pages 287, 293, 589, 605, 619 and 620, and ran its own fixed-form program per mechanism at
`--std 85` and `--std 2023`: `my-20261115/contradictions-b2/rulings.json`, programs under `contradictions-b2/src/`.
I ran two of its programs myself at `--std 85`: `nn1.cob` was refused (`COBOL0001: cannot parse construct near
'NOT'`) and its twin `nn2.cob` printed `T2`; `xfile.cob` printed `A FX=LOCL`.

**Ruling: eleven mechanisms. Seven UPHELD, two REJECTED, one decided as the implementor's classification, one left
open for want of a printed page.** Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-VI.6.3.3-L2.1` | UPHELD (a) | rejects_legal_source | `IF A > B OR NOT NOT < C` is refused at 85. By `§1985:VI.6.3.3 GRL2.1` the second NOT, followed by `<`, is part of the relational operator and the first is the logical operator, so the condition is `A > B OR NOT (A NOT < C)`; the same condition written `NOT IS NOT <` compiles. I ran both. | PB5784 |
| 2 | `1985-GR-VII.3.8.4-14` | UPHELD (a) | wrong_answer | The mechanism of the open kb/Work PB5731 (a format 3 record with an OCCURS DEPENDING ON table is sized at the table's minimum at 85); this row adds a run-time consumer: WRITE puts out a 4-character record where `§1985:VII.3.8.4 GR15` makes it 10. | joins PB5731 |
| 3 | `1985-GR-VII.3.8.4-3` | UPHELD (b) | wrong_answer | READ INTO where every 01 record is shorter than RECORD CONTAINS gives status 00 and a short move. The text supports two results (the full record with 00, or truncation with 04) and the compiler gives neither. WHICH result is right is not ruled here: it is decided before the fix. 2023 row `GR-13.18.43.4-6`. | PB5785 |
| 4 | `1985-GR-X.1.3.8.2-1`, `1985-GR-X.1.3.8.2-3` | UPHELD (b) | under_rejects, wrong_answer | A contained program's own file-name does not hide its container's GLOBAL data-name of the same word (and the reverse): `MOVE "LOCL" TO FX` in the contained program, where FX is its file, changes the container's item. `§1985:X.1.3.8.2 GR3a`. I ran it. 2023 rows `GR-8.4.6.2.1-1`, `GR-8.4.6.2.1-3`. | PB5786 |
| 5 | `1985-GR-X.4.2.4-1` | UPHELD (a) | rejects_legal_source, misleads_diagnosis | An unqualified LINAGE-COUNTER in a contained program is refused at 85 (COBOL5997, citing §2023), while the containing program's own unqualified reference is accepted. An unswept sibling of the landed kb/Work PB5166. | PB5787 |
| 6 | `1985-GR-XIII.3.10.10-2`, `1985-GR-XIII.3.10.7-2` | UPHELD (a) | under_rejects, wrong_answer | The report footing prints on line 13 of a 12-line page, and the page heading's last line lands on FIRST DETAIL under the detail line (`§1985:XIII.3.8.4 GR1c`). Whether the 2023 row `GR-13.18.57.4-8` is wrong too was not read. | joins PB1003 |
| 7 | `1985-GR-XIII.3.10.7-1` | OPEN | (under_rejects if the first reading holds) | Whether an absolute page-heading LINE after a report heading with NEXT GROUP is an error turns on `§1985:XIII.3.10.7 GR1`, whose text is RECONSTRUCTED: the scan lacks folio XIII-30 (kb/Work PB4952, landed). Not ruled until the printed page is read. | PB5789 |
| 8 | `1985-GR-XIII.3.6.4-2` | UPHELD (b) | wrong_answer (minor) | With a CODE clause and RECORD CONTAINS 2 or 1 the compiler writes `ABW`; the 2-character record is `AB`. 2023 row `GR-13.18.12.4-2`. Lead in the same note: a report file with RECORD VARYING compiles at 85 (row `1985-FMT-XIII.3.2.2`, page 589). | PB5788 |
| 9 | `1985-GR-XIII.3.2.4-2` | REJECTED | none | The compiler conforms: 1985's report writer logical record includes its vertical positioning, so the lines that only space the page need no CODE characters. Lead: the conformance entry `DOC-A.1-159` does not say how unoccupied lines are written. | (none) |
| 10 | `1985-GR-XIII.3.9.4-2` | REJECTED | none for this row | The compiler treats the report entry and the Working-Storage entry alike, as the row requires. Lead, not ruled: the printed Table 1 (page 605) permits SIGN SEPARATE with BLANK WHEN ZERO, which the compiler refuses at 85 (COBOL2422); rows `1985-SR-XIII.3.17.3-1`, `1985-SR-XIII.3.9.3-11`. | PB5790 (the lead) |
| 11 | `1985-GR-XIII.4.2.4-2` | DECIDED | none | The CLOSE twin of kb/Work PB5779. See below. | joins PB5779 |

**Mechanism 11, decided.** The 1985 CLOSE rules sort files into three categories and define the first as a file
"whose input or output medium is such that the concepts of rewind and reels/units have no meaning"
(`§1985:VII.4.2.4 GR2a`, cite.py OK; the report writer's CLOSE has the same sentence); `§1985:VII.1.3.5`
gives status 07 to a CLOSE with NO REWIND, REEL/UNIT or FOR REMOVAL, and to an OPEN with NO REWIND, when "the
referenced file is on a non-reel/unit medium". The checker reads the CLOSE preamble ("Treatment of sequential mass
storage files is logically equivalent to the treatment of a file on tape") as putting a disk file in the second
category, with status 00. I do not: which media have reels, units and a rewind is a fact about the implementation's
devices, the standard lists the phrase as hardware dependent, and for the host files this compiler uses those
concepts have no meaning. So the files are of the first category, 07 is the conforming status, and the rules for
the reel and unit categories have nothing to apply to. That is the same classification kb/Work PB5779 rests on for
OPEN; this row joins it, and the one conformance entry for 1985 that PB5779 owes must state the classification for
OPEN and CLOSE both, with what GnuCOBOL does measured, not remembered (CLAUDE.md rule 1).

**What follows.**
- Rows of mechanisms 1 to 6 and 8 do not close on their inherited verdict; each gets a `contradicted` record in the
  code-read store naming its note. Rows 9 and 10 are `confirmed` by adjudication. Row 7 stays `undecided`. Row 11 is
  confirmed as conforming by PB5779's classification once its conformance entry exists.
- The 2023 rows of mechanisms 3, 4 and 8 are re-recorded non-conforming by a registrar.
- Rate at 1985 so far: the sample 6 of 60, batch 1 9 of 120, batch 2 10 of 120 upheld (13 non-agreeing): 25 of 300
  rows contradicted, about 8 %.
