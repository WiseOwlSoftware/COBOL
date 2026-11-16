# Rulings on the re-reads of batch 10's five refused slices (the Mythos lane, Monday 2026-11-16 06:17 PST)

**The batch.** The store's importer refused five of batch 10's thirty slices as defective READS (two held records
with empty required fields, one a refuter's case with no corrected verdict, one a citation that does not parse, one
a row the store already held): `my-20261115/registrar-code-reads-2/refused-slices-b10.md`. A read is never edited,
so their rows were read again by other agents as slices 1985-220 to 1985-224 of
`E:/COBOL-coord/scratch/coderead-1985/` at main d92742fe62 (the Mythos lane's runs wf_ae3049de-719 and
wf_70197d54-d76; `my-20261115/store-import/reread-refused-b10.json` maps each new slice to the one it replaces; the
row `1985-SR-VI.5.8.3-1` was left out, because the store's record of it is undecided under the open adjudication
kb/Work PB5737 and a second read in which nobody raised that question does not settle it).
Of the 29 rows 20 agree with the inherited verdict. Nine do not (`coderead-1985/non-agreeing-220-224.json`). Six of
the nine are rows batch 10's rulings already decide, and those rulings stand whatever the new read says
(`rulings-code-reads-batch-10.md`: the five RECORD DELIMITER rows under kb/Work PB5844 and the open reading kb/Work
PB5849). Three are NEW: the first readers of the same rows did not find them.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
programs at `--std 85` and `--std 2023` on a compiler built at main d92742fe62:
`my-20261115/contradictions-b10r/rulings.json`, programs beside it. I ran three myself at `--std 85` with the tracked
probe runner and re-ran every one of the checker's citations through `cite.py --check` (all OK). `adv39.cob` printed
`OPEN-IN 39 39 00` and `R1 47 00 [          ] 47 00 [          ] 00 04 [AB        ]`. `nn.cob` ended
`error COBOL0001: cannot parse construct near 'NOT'` on both of its `NOT NOT <` lines. `nin.cob` (the same
conditions written `NOT IS NOT <`) printed `C1 T`, `C2 F`, `U1 T`.

**Ruling: two mechanisms, both UPHELD, each a new note; one row REJECTED.** Cause (a) = wrong at 85 only; cause
(b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-VII.2.8.4-4`, `1985-GR-VII.4.3.4-8`, `1985-GR-VII.1.3.5-L2.3` | UPHELD (b) | wrong_answer | A variable-length record sequential file that a program WRITES with ADVANCING, or under LINAGE, cannot be read back through the file description that wrote it: OPEN INPUT answers 39 and the READ 47. The same file written with no print control reads back. See below. The checker's confidence for the first row: medium. | PB5860 |
| 2 | `1985-GR-VII.2.8.4-1` | REJECTED | none | The rule speaks of what a successful READ shows of the method, and every successful read conforms; the failure of mechanism 1 is that no read succeeds, which is the other rows'. | (none) |
| 3 | `1985-SR-VI.6.3.3-2` | UPHELD (a) | rejects_legal_source | At `--std 85` `IF A > 1 AND NOT NOT < 5` is refused (COBOL0001), where the 1985 text reads the first NOT as the logical operator and `NOT <` as the relational operator. The same condition written `NOT IS NOT < 5` compiles and gives the right answer. At 2023 the refusal is right. | PB5861 |

**Mechanism 1: why 39 is wrong, and what the fix must hold to.** Status 39 is for a conflict "between the fixed file
attributes and the attributes specified for that file in the program" (`§1985:VII.1.3.5`, cite.py OK), and here
the program's own description created the file. Nothing in 1985 makes a file written with
ADVANCING unreadable: the phrase is there to "allow control of the vertical positioning of each line on a
representation of a printed page" (`§1985:VII.4.7.4 GR16`, cite.py OK), and "The execution of the WRITE statement
releases a logical record to the operating system" (`§1985:VII.4.7.4 GR7`, cite.py OK). The code decides the 39 by
asking whether the file's length prefixes parse, and the print controls its own WRITE put between the records make
them not parse; the read side has no case for those controls either, so removing the 39 alone would not make the
READ right. The register does not cover it: A.1 item 146 states the write form only, and for a LINAGE file it
contradicts A.1 item 151.
DECIDED, as a bound on the fix and not as its design: every file a program can write through a file description it
can read back through the same description, record for record, at every combination the description admits
(ADVANCING, LINAGE, each record format); the method that does it is the implementor's (`§1985:VII.2.8.4 GR4`), is
chosen by the precedence of CLAUDE.md rule 1 (what GnuCOBOL does for a record sequential file written with
ADVANCING or LINAGE is read from its source first), and is stated ONCE in the register, so that items 146 and 151
agree. The checker did not probe the fixed-length sibling (a fixed-length file written with ADVANCING, whose
control characters may be read into the record area): the fix sweeps it and says what item 146 documents of it.

**Mechanism 3: the text.** `§1985:VI.6.3.3 GRL2.1` tests the WORD that follows NOT against six words: when it is one
of them "then the NOT participates as part of the relational operator" (cite.py OK); otherwise, by GRL2.2, "The NOT
is interpreted as a logical operator" (cite.py OK). In `AND NOT NOT < 5` the word after the first NOT is NOT, which
is none of the six, so the first NOT is the logical operator and the second, followed by `<`, is part of the
relational operator. The expansion is `A > 1 AND NOT (A NOT < 5)`, which the table of permissible sequences admits:
its bar on the pair NOT NOT is a bar on two LOGICAL operators (`§1985:VI.6.3.2.3`; the checker's quotation, cite.py
OK). The landed kb/Work PB5123 made the refusal and its landing file calls it correct; for 1985 it is not. At 2023
the text itself bars the form (the note under `§2023:8.8.4.12.4 GR1`: "NOT cannot immediately precede an
extended-relational operator", cite.py OK). A lead beside it: at 2023 this form gets the generic COBOL0001 where
its sibling `NOT IS NOT <` gets the specific COBOL0902.

**What the re-reads say about the method.** Twenty-nine rows were read twice by independent reader and refuter
pairs. The second pair found two mechanisms the first pair missed on the same rows (1 and 3 above), and missed one
the first pair found (`1985-SR-VI.5.6.3-1`, JUSTIFIED on a report group entry with subordinates, kb/Work PB5850,
which a checker confirmed with its own program). A confirmed read is therefore not proof of conformance, and the
contradiction rate the campaign reports is a floor.

**What follows.**
- Rows of mechanisms 1 and 3 do not close on their inherited verdict (four rows; two of mechanism 1's were read
  and confirmed in earlier slices and are adjudicated in the store). The row of mechanism 2 is confirmed.
- The 2023 rows of mechanism 1 (`GR-12.4.5.11.4-5`, `GR-14.9.27.4-10`, `GR-9.1.13.6-7`) are re-recorded
  non-conforming by a registrar where they still read conforming.
