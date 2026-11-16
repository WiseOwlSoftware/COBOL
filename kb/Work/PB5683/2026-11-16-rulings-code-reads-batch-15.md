# Rulings on the re-reads of batch 15 (the Mythos lane, Monday 2026-11-16 08:41 PST)

**The batch.** Slices 1985-229 and 1985-230 of `E:/COBOL-coord/scratch/coderead-1985/`: eleven KEPT rows of 1985,
read on a compiler built at main 1712645b47 (the Mythos lane's run wf_dabeb5ce-52d of the operator's workflow).
Eight are rows whose first read was contradicted and whose defect note has landed since (kb/Work PB5854; among
them the rows of kb/Work PB5851 and PB5781); three are the rows whose reads the fix of kb/Work PB5847 withdrew
because it renamed a member they cited (kb/Work PB5863). Nine agree with the inherited verdict: every one of those
fixes is confirmed on the code, the group VALUE of PB5851 and the CLOSE rows of PB5847 among them. Two do not
(`coderead-1985/non-agreeing-229-230.json`): one refuter's OVERTURNED and one DOUBTFUL, each on a shape the first
reads did not try.

**How each was checked.** I ran the refuter's programs myself on the same compiler with the tracked probe runner,
at `--std 85`, and read GnuCOBOL's source where the 1985 text leaves a choice (3.3-dev, `cobc/field.c`; read, not
run). No separate checker ran: the two rows are narrow.
- `out/refute-1985-230/.../r2d.cob` (`01 B PICTURE; 9(3) VALUE 12.`) ended `error COBOL0001: missing token before '9'`.
- `r2e.cob` (`01 B PIC, 999 VALUE 12.`) ended with COBOL2419, which takes the comma for the character-string.
- `r4c.cob` (`66 R-QQ RENAMES A OF G1 THRU A OF G2.`) compiled and printed `QQ=ABCDEF`.

**Ruling: one new note, one row REJECTED.** Cause (b) = wrong the same way at 2023 (the refuter ran both).

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-SR-IV.4.2.1-2` | UPHELD (b) | rejects_legal_source | A comma or a semicolon separator between PICTURE (or PIC, or PICTURE IS) and the character-string is refused: `PICTURE; 9(3)`, `PIC, 999`, `PICTURE IS, X(2)`. The lexer's picture mode skips only spaces before the string. See below. | PB5867 |
| 2 | `1985-SR-VI.5.11.3-4` | REJECTED | none | `RENAMES A OF G1 THRU A OF G2`, two different items of one record that share the word A, is accepted. The row is confirmed. See below. | (none) |

**Mechanism 1, ruled from the text.** `§1985:IV.4.2.1 SR2` says "the punctuation characters comma and semicolon,
immediately followed by a space, are separators that may be used anywhere the separator space is used" (cite.py
OK), and its one exception is a comma "used in a PICTURE character-string". The place between the word PICTURE and
the character-string is a place where the separator space is used. A semicolon is never a PICTURE symbol, so the
exception cannot reach `PICTURE; 9(3)`; and a comma followed by a space before the string is no part of the string
(a string of the comma alone is no PICTURE), so it is the separator too. Everything else the refuter tried with
comma and semicolon separators, across the divisions, is accepted. The source is unusual and legal.

**Mechanism 2, ruled by the precedence.** `§1985:VI.5.11.3 SR4` says data-name-2 and data-name-3 "cannot be the same
data-name" (cite.py OK). The refuter's doubt: read as the WORD, the rule refuses `A OF G1 THRU A OF G2`; read as
the ITEM named, it does not, and a later syntax rule of the same clause already refuses a range that ends where it
begins. The text does not decide it; GnuCOBOL does: `cobc/field.c#cb_validate_renames_item` compares the two
RESOLVED items ("THRU item must be different") and has no check of the words. This compiler does the same:
`RENAMES A THRU A` is refused and two different items that share a word are accepted. A sentence in the
conformance register is owed for the choice.

**What follows.**
- Row 1 does not close on its inherited verdict; its 2023 row is re-recorded non-conforming by a registrar (the
  note names it once a checker of the note's own has read the 2023 text: the refuter ran the programs at 2023 and
  did not cite the 2023 rule). Row 2 is confirmed.
- The re-reads so far, counted from the tallies of slices 218 and 219 (9 rows), 225 to 227 (15), 228 (5) and the
  eight fixed rows of 229 and 230: 37 rows read again after their fix landed. 30 agree outright. Seven do not: a
  fix that went too far (kb/Work PB5858), a form its fix never covered (kb/Work PB5859), a row another open note
  holds (kb/Work PB5810), two new mechanisms (kb/Work PB5866, PB5867), and two doubts ruled rejected.
