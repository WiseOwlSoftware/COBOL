# Rulings on the non-agreeing rows of the 1985 code reads, batch 5, the first of the second population (the Mythos lane, Sunday 2026-11-15 23:05 PST)

**The batch.** The operator's run wf_fadf5469-1bf: slices 1985-085 to 1985-104 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 that gained a witness of their own today, read on the
code at `--std 85` (main 1df234fa72). 111 agree with the inherited verdict. Nine do not
(`coderead-1985/non-agreeing-085-104.json`).

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) derived each from the
1985 text and ran its own fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main d7d3f99539:
`my-20261115/contradictions-b5/rulings.json`, programs under `contradictions-b5/src/`. I ran two myself at `--std 85`:
`stopsym.cob` was refused (`COBOL5506 ... expected '&'`, citing a 2023 clause), and `odord.cob` printed
`USE-RDF 34`, `R2 34 CNT 9`, `USE-RDF 34`, `R1 34 CNT 9 T AB`.

**Ruling: six mechanisms. Four UPHELD (one of them an existing note), one REJECTED as a defect of the code, one
left OPEN.** Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-IV.4.2.2.2.3-L2.2`, `1985-GR-IV.4.2.2.2.3-L2.3` | UPHELD (a) | rejects_legal_source, misleads_diagnosis | `STOP STAR.`, STAR a symbolic-character, is refused at 85. A symbolic-character is a figurative constant, and `§1985:VI.6.24.3 SR1` bars from STOP only one "that begins with the word ALL". I ran it. Lead: the other formats that take only `literal` were not swept for the same gap. | PB5809 |
| 2 | `1985-GR-IV.4.3.8.4.4-2` | UPHELD (b) | wrong_answer | The mechanism of kb/Work PB5780 (a reference-modified numeric item is compared as a number); this row joins it. | joins PB5780 |
| 3 | `1985-GR-IX.3.2.4-5` | UPHELD (b) | under_rejects, wrong_answer | `RECORD VARYING ... DEPENDING ON TL`, TL an element of a table, compiles, and at run time the length is ignored (a WRITE gives 00 where an elementary item gives 44). The subscripted spelling is refused. 2023 rows `FMT-13.18.43.2` and the operand rule `SR-13.18.43.3-6`. | PB5810 |
| 4 | `1985-GR-IX.1.3.4-4` | UPHELD (a) | wrong_answer | An indexed READ of a record whose OCCURS DEPENDING ON count is past its maximum sets I-O status 34 at 85, runs the USE procedure, and leaves a permanent error that fails the next valid READ. 1985 defines status 34 for a sequential file's boundary only, and `§1985:IX.4.5.4 GR18` makes a record within the file's sizes a successful READ. The compiler applies the 2023 rule with no edition switch; the same code serves the sequential READ and SORT USING. I ran it. | PB5811 |
| 5 | `1985-GR-IX.2.3.4-2`, `1985-GR-IX.4.9.4-14`, `1985-GR-IX.4.9.4-17` | REJECTED | none | The behaviour (a character above U+00FF refused with 91; U+00FF read back as HIGH-VALUE) is fixed by the owner's decisions R47 and R52 and documented in `docs/conformance/7/DOC-A.1-31.md`; the code does what was decided. Lead: that entry does not state one consequence the checker measured, that two keys differing only in U+00FF against HIGH-VALUE collide (status 21 on a sequential WRITE, 22 on a random one). The rows close on the documented determination, not on a plain CONFORMS. | (none) |
| 6 | `1985-GR-IX.4.8.4-4` | OPEN | (wrong_answer under one reading) | When the containing program has `USE GLOBAL ... ON` the file and the contained program has its own `USE ... ON INPUT`, the contained program's own procedure runs. Whether the 1985 precedence rules reach across programs is not decided by the text. Settled by CLAUDE.md rule 1's order (GnuCOBOL measured, then the vendor manuals) and a 1985 conformance entry. | PB5812 |

**What follows.**
- Rows of mechanisms 1 to 4 do not close on their inherited verdict. Rows of mechanism 5 are confirmed against the
  documented determination. Row 6 stays undecided.
- The 2023 rows of mechanisms 2 and 3 are re-recorded non-conforming by a registrar.
- Rate at 1985 so far: 684 rows read (the sample 60, the first population 504, this batch 120); 56 contradicted,
  about 8 %; 5 undecided on open readings (PB5789, PB5799, PB5800, PB5805, PB5812).
