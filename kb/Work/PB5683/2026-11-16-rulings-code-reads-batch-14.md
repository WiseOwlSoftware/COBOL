# Rulings on the re-reads after the fix of kb/Work PB5801, batch 14 (the Mythos lane, Monday 2026-11-16 07:20 PST)

**The batch.** Slice 1985-228 of `E:/COBOL-coord/scratch/coderead-1985/`: five KEPT rows of 1985 whose first read
was contradicted under kb/Work PB5801 (the places of level-66 items), read again after that note's fix landed (pull
request 1769), on a compiler built at main 3fd4a31c0d (the Mythos lane's run wf_204d09be-ed1 of the operator's
workflow; kb/Work PB5854). Two agree with the inherited verdict (`1985-SR-VII.3.7.3-1`, `1985-SR-VIII.2.4.3-2`):
the fix answered them. Three do not (`coderead-1985/non-agreeing-228-228.json`): one reader's PARTIAL and two
refuters' DOUBTFUL, each a question the first reads did not raise.

**How each was checked.** The three are narrow, and I checked them myself on the same compiler with the tracked
probe runner, at `--std 85`. No separate checker ran.
- `out/work-1985-228/src/rd17.cob` (the reader's program) printed `00 00 ABCDEFGHIJ`.
- `my-20261115/b14-check/ctl66.cob` (mine: `CONTROLS ARE B R66` where `66 R66 RENAMES B`) compiled and printed
  `RAN`; its control `ctlbb.cob` (`CONTROLS ARE B B`) was refused with COBOL1699, which names the 1985 rule.
- `out/refute-1985-228/.../fsnum9.cob` (the refuter's: a FILE STATUS item that is a group of two `PIC 9` items)
  compiled and printed `RAN`.
- Where the 1985 text leaves a choice I read GnuCOBOL's source (3.3-dev, `cobc/tree.c` and `cobc/typeck.c`; read,
  not run), by the precedence of CLAUDE.md rule 1.

**Ruling: one row joins an open note, one is a new note, one is REJECTED.** Cause (a) = wrong at 85 only; cause
(b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-SR-VII.3.8.3-4` | UPHELD (b) | under_rejects, wrong_answer | RECORD IS VARYING ... DEPENDING ON a table element written with no subscript compiles, and the length item is then ignored: the WRITE writes the whole record and the READ does not set it. The mechanism of the open kb/Work PB5810, whose title is this sentence. | joins PB5810 |
| 2 | `1985-SR-XIII.3.7.3-2` | UPHELD (a) | under_rejects | `CONTROLS ARE B R66`, where `66 R66 RENAMES B`, compiles at `--std 85`: the screen that refuses `CONTROLS ARE B B` compares the two resolved entries, and a level-66 entry is another entry. See below. | PB5866 |
| 3 | `1985-SR-VII.2.5.3-2` | REJECTED | none | A FILE STATUS item that is a group of two `PIC 9` items is accepted. The row is confirmed. See below. | (none) |

**Mechanism 2, ruled from the text.** `§1985:XIII.3.7.3 SR2` says "Each recurrence of data-name-1 must identify a
different data item" (cite.py OK). A level-66 entry with no THROUGH phrase is the same data item under another
name: `§1985:VI.5.11.4 GR2` says "all of the data attributes of data-name-2 become the data attributes for
data-name-1" (cite.py OK), and it describes the same storage. So `B` and `R66` identify one data item and the
clause is to be refused, as `CONTROLS ARE B B` is. A level-66 entry WITH a THROUGH phrase is a group of its own
(GR1), and an item that REDEFINES another has a description of its own: neither is the same data item, and the
refuter's program that passes those is right. The screen runs below 2002 only (`ReportSectionBinder.cs#ScreenControlItemsDistinct`,
the refuter's reading of the code); what 2002 and 2023 require is not ruled here. GnuCOBOL has no check of this
rule at all (its parser checks only that the CONTROL clause is not repeated), so it settles nothing; the text does.

**Mechanism 3, ruled by the precedence.** `§1985:VII.2.5.3 SR2` asks for "a two-character data item of the category
alphanumeric" (cite.py OK). The refuter's doubt: the 1985 text gives a group item a class ("The class of a group
item is treated at object time as alphanumeric", `§1985:IV.4.3.3`, cite.py OK) and never says which category a
group takes, so a group of numeric items might be held not alphanumeric. The text does not decide it; GnuCOBOL
does: `cobc/tree.c#cb_tree_category` gives every field that has subordinate items the category alphanumeric, and
`cobc/typeck.c#validate_file_status` accepts a two-character item of that category. This compiler does the same,
and a FILE STATUS item described as a group is ordinary practice. A sentence in the conformance register is owed
for the choice (a group item is of the category alphanumeric at 1985), with the other 1985 entries still owed.

**What follows.**
- Rows 1 and 2 do not close on their inherited verdict; row 3 is confirmed. Rows 1's 2023 rows are already held
  open by kb/Work PB5810.
- Of the five rows the fix of PB5801 was read against, the fix answered all five as far as PB5801's own mechanism
  goes: the two contradictions that remain are other mechanisms the second read found.
