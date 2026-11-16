**2026-11-15, the row `1985-GR-VII.4.7.4-13` is NOT DECIDED and waits for the fix of this note (the Mythos lane's batch-7 code reads, mechanism 6; source `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-7.md`).**

**Ruling: NOT DECIDED, harm wrong_answer under one reading.** CLOSE then OPEN EXTEND and WRITE AFTER ADVANCING 1 LINE writes one more line end than the same two WRITE statements under one OPEN OUTPUT (`0A 41 41 0A 0A 42 42 0A` against `0A 41 41 0A 42 42 0A`). The row stays open and is filed beside this note so the fix cannot miss it. The checker's confidence: medium.

**Mechanism 6, not decided, and what decides it** (the ruling's paragraph, unchanged). `§1985:VII.4.7.4 GR13` says an EXTEND file's WRITE "will add
records to the end of the file as though the file were open in the output mode" (cite.py OK). Read one way, that
covers the line position, and the extra line is wrong. Read the other way, the records are in order and the page
position across a CLOSE and a new OPEN is the implementor's. The fix of PB5815 decides it: if a print file then
reads back by its lines, the empty line is a record no WRITE released (`§1985:VII.4.7.4 GR12`) and the row is
contradicted under both readings. If PB5815 instead refuses the OPEN, CLAUDE.md rule 1's order decides, and GnuCOBOL
is measured first (it is not installed on this host; the checker did not measure it and neither did I). The row
stays open and is filed beside PB5815 so the fix cannot miss it.

**The checker's evidence and its two readings.** A sibling of this note, not documented by `docs/conformance/7/DOC-A.1-146.md` (c'), which says how lines are advanced but nothing of CLOSE and EXTEND. `p7ext`: F OUTPUT AA, BB (AFTER 1) gives `0A 41 41 0A 42 42 0A`; G OUTPUT AA, CLOSE, EXTEND BB (AFTER 1) gives `0A 41 41 0A 0A 42 42 0A`: one more line feed, so on a printed page BB lands two lines below AA instead of one. Reading 1: GR13 covers the line positioning, so EXTEND must continue from the last line and the extra line is wrong (UPHELD (b), wrong_answer). Reading 2: `§1985:VII.4.7.4 GR16` makes the medium a representation of a printed page whose form is the implementor's; GR13's records (AA then BB) are in order, and the page position across CLOSE and a new OPEN is not fixed by the text: conforms. The output at `--std 2023` is the same (`p7ext23.cob`: `0A 41 41 0A 0A 42 42 0A`).

**Where** (the checker's reading, not re-read by it): `src/WiseOwl.Cobol.Runtime/IO/SequentialConnector.cs#OpenCore` (the EXTEND arm does not account for the line end CLOSE wrote).

**Row.** `1985-GR-VII.4.7.4-13` (open). `work.py check` refuses a 1985 rule id in `inventory_rows` (kb/Work PB5094), so the row is named here. The 2023 row the checker names for the same question: `GR-14.9.51.4-18`.

**Rules (each citation checked again with `python scripts/spec/cite.py --check`, verdict OK).**
- `§1985:VII.4.7.4 GR13`: "will add records to the end of the file as though the file were open in the output mode"
  `OK  §1985:VII.4.7.4 GR13  (4.7.4 General Rules)  the text is in GR13`
- `§1985:VII.4.7.4 GR16`: "allow control of the vertical positioning of each line on a representation of a printed page"
  `OK  §1985:VII.4.7.4 GR16  (4.7.4 General Rules)  the text is in GR16`

**Repro** (`E:/COBOL-coord/scratch/my-20261115/contradictions-b7/src/p7ext.cob`; `--std 85`: exit 0, `DONE 00 00`; `p7out.dat` is `0A 41 41 0A 42 42 0A` and `p7ext.dat` is `0A 41 41 0A 0A 42 42 0A`):

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P7EXT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p7out.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT G ASSIGN TO "p7ext.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD F LABEL RECORDS ARE STANDARD.
       01 F-R PIC X(2).
       FD G LABEL RECORDS ARE STANDARD.
       01 G-R PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT F.
           MOVE "AA" TO F-R. WRITE F-R AFTER ADVANCING 1 LINE.
           MOVE "BB" TO F-R. WRITE F-R AFTER ADVANCING 1 LINE.
           CLOSE F.
           OPEN OUTPUT G.
           MOVE "AA" TO G-R. WRITE G-R AFTER ADVANCING 1 LINE.
           CLOSE G.
           OPEN EXTEND G.
           MOVE "BB" TO G-R. WRITE G-R AFTER ADVANCING 1 LINE.
           CLOSE G.
           DISPLAY "DONE " FS " " GS.
           STOP RUN.
```
