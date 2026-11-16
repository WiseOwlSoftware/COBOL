# Rulings on the non-agreeing rows of the 1985 code reads, batch 7 (the Mythos lane, Sunday 2026-11-15 23:58 PST)

**The batch.** The operator's run wf_7bd10c9e-26e: slices 1985-125 to 1985-144 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 of the second population, read on the code at
`--std 85` (main 1df234fa72). 112 agree with the inherited verdict. Eight do not
(`coderead-1985/non-agreeing-125-144.json`).

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main d7d3f99539:
`my-20261115/contradictions-b7/rulings.json`, programs under `contradictions-b7/src/`. I ran two myself at `--std 85`
with the tracked probe runner: `p2nest.cob` printed `OUTER-NAMED-LOCAL 35`, `OUTER OPEN 35`, `OUTER-GLOBAL-INPUT 35`,
`INNER OPEN 35`, and `p6dead.cob` read three records back from a file two WRITE statements made.

**Ruling: six mechanisms. Three UPHELD (one new note, two existing), one REJECTED, two not decided.**
Cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-VII.4.6.4-4` | NOT DECIDED | wrong_answer under one reading | The question of the open kb/Work PB5812, in the sequential module: a containing program's USE GLOBAL on the file against the contained program's own USE ON INPUT (`INNER-INPUT 35`). The sequential text matches the indexed text word for word, so it settles nothing more. | joins PB5812 |
| 2 | `1985-GR-VIII.4.8.4-4`, `1985-GR-VIII.4.8.4-5` | UPHELD (b) | wrong_answer | A containing program holds USE GLOBAL ... ON INPUT and USE ... ON FG. A contained program with no declaratives fails to open FG, and the compiler runs the container's GLOBAL INPUT declarative. No declarative qualifies: see below. Not PB5812's question, because both USE statements are in one program. | PB5821 |
| 3 | `1985-GR-VIII.2.3.4-2` | REJECTED | none | U+00FF is written and read with status 00 and comes back as HIGH-VALUE. That is the storage-byte law `docs/conformance/7/DOC-A.1-31.md` documents, ruled the same way for the indexed rows in batch 5. | (none) |
| 4 | `1985-GR-VIII.3.2.4-5`, `1985-GR-XI.3.2.4-2` | UPHELD (b) | under_rejects, wrong_answer | The mechanism of kb/Work PB5810 in two more files: a relative file's WRITE with RECORD VARYING DEPENDING ON a table element gives status 00 where the length 9 requires 44, and a sort file's RETURN delivers all 8 characters and leaves the length item `00`. | joins PB5810 |
| 5 | `1985-GR-VII.4.7.4-12` | UPHELD (b) | wrong_answer | The mechanism of kb/Work PB5815 by another trigger: one WRITE ... ADVANCING that never executes makes the file a print file, and two plain records then read back as three wrong ones. I ran it. Lead for the fix: the compiler classifies the file from the source text, while `docs/conformance/7/DOC-A.1-146.md` (c') says "written with an ADVANCING phrase". | joins PB5815 |
| 6 | `1985-GR-VII.4.7.4-13` | NOT DECIDED | wrong_answer under one reading | CLOSE then OPEN EXTEND and WRITE AFTER ADVANCING 1 LINE writes one more line end than the same two WRITE statements under one OPEN OUTPUT (`0A 41 41 0A 0A 42 42 0A` against `0A 41 41 0A 42 42 0A`). See below. | beside PB5815 |

**Mechanism 2, ruled.** `§1985:VIII.4.8.4 GR5b` runs the INPUT procedure for any file open or being opened in
the input mode "except those files referenced by file-name-1 in another USE statement specifying the same
condition" (cite.py OK). FG is named in the container's other USE statement, so the INPUT procedure does not apply to
FG. `§1985:X.5.5.4 GR1` says: "The declarative which is selected for execution must satisfy the rules for execution
of that declarative" (cite.py OK). So for the contained program's failed OPEN of FG the container's GLOBAL INPUT
declarative is not a qualifying declarative, the declarative that names FG has no GLOBAL phrase, and GR1c ends the
search: none is executed. The compiler prints `OUTER-GLOBAL-INPUT 35`. The checker found the same at 2023 (rows
`GR-14.9.49.4-5` and `GR-14.9.49.4-6`).

**Mechanism 6, not decided, and what decides it.** `§1985:VII.4.7.4 GR13` says an EXTEND file's WRITE "will add
records to the end of the file as though the file were open in the output mode" (cite.py OK). Read one way, that
covers the line position, and the extra line is wrong. Read the other way, the records are in order and the page
position across a CLOSE and a new OPEN is the implementor's. The fix of PB5815 decides it: if a print file then
reads back by its lines, the empty line is a record no WRITE released (`§1985:VII.4.7.4 GR12`) and the row is
contradicted under both readings. If PB5815 instead refuses the OPEN, CLAUDE.md rule 1's order decides, and GnuCOBOL
is measured first (it is not installed on this host; the checker did not measure it and neither did I). The row
stays open and is filed beside PB5815 so the fix cannot miss it.

**What follows.**
- Rows of mechanisms 2, 4 and 5 do not close on their inherited verdict. Row 3 is confirmed. Rows 1 and 6 wait.
- The 2023 rows of mechanisms 2, 4 and 5 are re-recorded non-conforming by a registrar (the checker's
  `rulings.json` names them).
- Rate at 1985 so far: 924 rows read (the sample 60 and 864 of the second count); 69 contradicted, about 7.5 %.
  Open readings: the six notes of before (PB5789, PB5799, PB5800, PB5805, PB5812, PB5817), one more row on PB5812,
  and the EXTEND question beside PB5815.
