# Rulings on the non-agreeing rows of the 1985 code reads, batch 4, the last of the first population (the Mythos lane, Sunday 2026-11-15 20:40 PST)

**The batch.** The operator's run wf_09a98daf-3a0: slices 1985-061 to 1985-084 of
`E:/COBOL-coord/scratch/coderead-1985/`, 144 KEPT rows of 1985 read on the code at `--std 85` (main 5e49d46678). 129
agree with the inherited verdict. Fifteen do not (`coderead-1985/non-agreeing-061-084.json`): the refuter
overturned eight and doubted four, and the reader found three non-conforming.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) derived each from the
1985 text, rendered page 605, and ran its own fixed-form programs at `--std 85` and `--std 2023`:
`my-20261115/contradictions-b4/rulings.json`, programs under `contradictions-b4/src/`. I ran the most serious one
myself at `--std 85`, compile only: `ren_fs.cob` ends with `Unhandled exception:
System.InvalidOperationException: no Place was built for data item 'FS66' when binding finished` and a .NET stack
through `PlaceTable.FileStatusOf` and `SequentialIoEmitter.EmitStoreFileStatus` (exit 1).

**Ruling: nine mechanisms. Seven UPHELD, one joins an existing note, one left OPEN.**
Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-SR-VII.2.5.3-2`, `1985-SR-VII.3.7.3-1`, `1985-SR-VII.3.8.3-4`, `1985-SR-VIII.2.4.3-2`, `1985-SR-XIII.3.7.3-2` | UPHELD (b) | **crashes** | THE COMPILER CRASHES ON LEGAL SOURCE. A level 66 item that renames one elementary item, named as FILE STATUS, LINAGE, DEPENDING ON, RELATIVE KEY or a report CONTROL, ends the compiler with an unhandled `System.InvalidOperationException` ("no Place was built for data item"), at 85 and at 2023, with no COBOL diagnostic. Each use is legal: "all of the data attributes of data-name-2 become the data attributes for data-name-1" (`§1985:VI.5.11.4 GR2`). I ran the FILE STATUS case. Not probed: SORT and MERGE keys, SUM operands, RECORD KEY. For `1985-SR-XIII.3.7.3-2` the row's own check is right for every operand that compiles. | PB5801 |
| 2 | `1985-SR-VII.2.3.3-3` | UPHELD (a) | rejects_legal_source, misleads_diagnosis | `ASSIGN TO NULL` is refused at 85 (COBOL1639 "not defined"); NULL is not a reserved word in 1985, so there it is an implementor-defined file name, as `ASSIGN TO NULLS` already is. | PB5802 |
| 3 | `1985-SR-XII.3.3-3`, `1985-SR-XII.3.3-4` | UPHELD (a) | rejects_legal_source, misleads_diagnosis | REPLACE and COPY REPLACING pseudo-text that begins a line with `>>` is refused at 85 (COBOL2572, COBOL2697): the scanner looks for compiler directive lines at every edition, and 1985 has none, so `>>XYZ` is an ordinary text word there. | PB5803 |
| 4 | `1985-SR-XVI.2.2.3-1` | UPHELD (a) | rejects_legal_source | CLASS-ID, INTERFACE-ID, FUNCTION-ID and REPOSITORY are refused as the computer-name of OBJECT-COMPUTER and SOURCE-COMPUTER at 85; none is a reserved word in 1985. | PB5804 |
| 5 | `1985-SR-VII.4.7.3-5` | OPEN | (depends on the reading) | WRITE ADVANCING accepts `+3` and refuses `-0`. `§1985:VII.4.7.3 SR5` says integer-1 "may be positive or zero, but must not be negative"; the glossary says an integer is "neither signed nor zero unless explicitly allowed". Whether SR5 allows a written sign is not decided by the text. Settled by CLAUDE.md rule 1's order (GnuCOBOL measured, then the vendor manuals) and a 1985 conformance entry. Accepting ZERO is right either way. | PB5805 |
| 6 | `1985-SR-XIII.3.13.3-1` | UPHELD (a) | under_rejects | GROUP INDICATE on an entry with no COLUMN clause compiles at 85; the 1985 rule requires a printable item, and a printable item has a COLUMN clause. The checker rates it medium; the fix reverses half of the landed kb/Work PB5243, so the implementer reads that note's reasoning first. | PB5806 |
| 7 | `1985-SR-XIII.3.18.3-2` | UPHELD (b) | wrong_answer | A group item named in a report SOURCE clause prints `047` where a MOVE of the same group gives `4X7`. 2023 row `GR-13.18.53.4-1`. | PB5807 |
| 8 | `1985-SR-XIII.4.9.3-4` | UPHELD (b) | under_rejects | A USE BEFORE REPORTING procedure may change a control data item through a REDEFINES of it; the direct store is refused (COBOL2378). 2023 row `SR-14.9.49.3-11`. A leftover the landed kb/Work PB363 recorded. | PB5808 |
| 9 | `1985-SR-XIII.4.9.3-2`, `1985-SR-XIII.3.18.3-1` | UPHELD (b) | under_rejects | The mechanism of kb/Work PB5775 (a report name that is also a Working-Storage name); `SOURCE DFLD` silently taking the Working-Storage item is its other direction. | joins PB5775 |

**What follows.**
- Rows of mechanisms 1 to 4 and 6 to 9 do not close on their inherited verdict: each gets a `contradicted` record
  in the code-read store naming its note. Row 5 stays `undecided`.
- The 2023 rows of mechanisms 1, 7, 8 and 9 are re-recorded non-conforming by a registrar. Mechanism 1's 2023 rows:
  `SR-12.4.5.8.3-2`, `SR-13.18.34.3-2`, `SR-13.18.43.3-6`, `SR-12.4.5.13.3-2`, `SR-13.18.16.3-6`.
- **The first population is done: 504 KEPT rows read at 1985 (plus the sample's 60).** Of the 564: the sample 6,
  batch 1 9, batch 2 10, batch 3 11, batch 4 14 contradicted, 50 in all, about 9 %; 4 more undecided on open
  readings (PB5789, PB5799, PB5800, PB5805); 4 rejected or decided as conforming.
