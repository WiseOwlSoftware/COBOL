# Rulings on the non-agreeing rows of the 1985 code reads, batch 3 (the Mythos lane, Sunday 2026-11-15 20:20 PST)

**The batch.** The operator's run wf_998ea5bc-3ff: slices 1985-041 to 1985-060 of
`E:/COBOL-coord/scratch/coderead-1985/`, 120 KEPT rows of 1985 read on the code at `--std 85` (main 5e49d46678). 106
agree with the inherited verdict. Fourteen do not (`coderead-1985/non-agreeing-041-060.json`): the refuter
overturned six and doubted two, and the reader found six non-conforming.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) derived each from the
1985 text and ran its own fixed-form programs at `--std 85` and `--std 2023`:
`my-20261115/contradictions-b3/rulings.json`, programs under `contradictions-b3/src/`. I ran two of them myself at
`--std 85`: `sep1.cob` was refused (`COBOL0001: cannot parse construct near ', '`) and `if1.cob` compiled and
printed `NEXT`.

**Ruling: eleven mechanisms. Eight UPHELD, one REJECTED by my reading of the text, two left OPEN.**
Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-SR-IV.4.2.1-2` | UPHELD (b) | rejects_legal_source | A comma or semicolon followed by a space is refused inside a subscript list where a space may stand (`W-E (I +, 1)`), against `§1985:IV.4.2.1 SR2`. I ran it. 2023 row `SR-8.3.5-2`. | PB5793 |
| 2 | `1985-SR-IX.4.5.3-2`, `1985-SR-IX.4.7.3-4` | UPHELD (b) | under_rejects | A `PIC 9(4)` item at the position of the `X(4)` record key is accepted as the KEY of READ and START: the match compares offset and width only. 2023 rows `SR-14.9.30.3-11`, `SR-14.9.41.3-6`. Lead, not ruled: an alphanumeric item of the same width is accepted for READ too. | PB5794 |
| 3 | `1985-SR-VI.4.3.3-1`, `1985-SR-VI.4.4.3-1` | UPHELD (a) | under_rejects (minor) | `SOURCE-COMPUTER. CONSOLE.` runs in a program that also uses CONSOLE as an implementor-name; 1985 makes the computer-names and the implementor-names of an implementation disjoint sets. The checker rates this medium: the sentence is about the implementation's name sets, not a syntax rule on programs. | PB5795 |
| 4 | `1985-SR-VI.5.10.3-5` | OPEN | (under_rejects under one reading) | A REDEFINES pair inside an OCCURS DEPENDING ON table compiles. Whether SR5 ("Neither the original definition nor the redefinition can include a variable occurrence data item") bars it turns on whether "include" covers the item itself, which the glossary makes a variable occurrence data item. See below. | PB5799 |
| 5 | `1985-SR-VI.6.13.3-4` | UPHELD (b) | under_rejects | EVALUATE accepts `WHEN SPACE THRU 9` and `1 THRU HIGH-VALUE`: the same-class check skips a figurative constant. 2023 row `SR-14.9.13.3-4`. | PB5796 |
| 6 | `1985-SR-VI.5.11.3-4` | UPHELD (a) | under_rejects | The mechanism of kb/Work PB5772 (a level 66 renames a level 66 at 85); this row is added to it. | joins PB5772 |
| 7 | `1985-SR-VI.5.8.3-8`, `1985-SR-VI.5.8.3-9` | UPHELD (b) | under_rejects | A contained program's EXTERNAL or GLOBAL table may depend on a counter declared in the containing program. 2023 rows `SR-13.18.38.3-18`, `SR-13.18.38.3-21`. | PB5797 |
| 8 | `1985-SR-VI.6.16.3-3` | UPHELD (a) | under_rejects | `IF A = 1 NEXT SENTENCE END-IF` compiles at 85 against `§1985:VI.6.16.3 SR3` ("If the END-IF phrase is specified, the NEXT SENTENCE phrase must not be specified"). I ran it. The acceptance at 2023 is kb/Work PB396's recorded decision and is not touched. | PB5798 |
| 9 | `1985-SR-VI.6.18.3-6` | UPHELD (b) | under_rejects, misleads_diagnosis | `INSPECT S TALLYING CN (1:2) FOR ALL "A"` is not refused; the compiler's C# backend then fails (CS1503, exit 70). No exception and no stack: the same symptom kb/Work PB5734 records for STRING and UNSTRING. 2023 row `SR-14.9.22.3-5`. | joins PB5734 |
| 10 | `1985-SR-IV.4.3.8.1-L2.9` | OPEN | (under_rejects under one reading) | Whether "exists in a program" counts a contained program's report descriptions and a GLOBAL report's counters when LINE-COUNTER must be qualified. The checker's confidence is low. | PB5800 |
| 11 | `1985-SR-IX.2.5.3-5` | REJECTED | none | The compiler conforms at 85. See below. | (none) |

**Mechanism 11, ruled.** `§1985:IX.2.5.3 SR5` keeps each alternate key "within the first x character positions of
the record, where x equals the minimum record size specified for the file". For a Format 3 file the checker asks
whether that is integer-4 or the smallest record description. `§1985:VII.3.8.4 GR14` answers it: integer-4 and
integer-5 "refer to the minimum number of characters in the smallest size data record" and the largest, "However,
in this case, the size of each data record is completely defined in the record description entry" (cite.py OK). The
record descriptions define the sizes, so the minimum record size is the smallest description's, and a key inside it
is legal. The compiler accepts the program at 85: it conforms. This is the same reading kb/Work PB5731 and the
PB5692 adjudication rest on. What 2002 and 2023 require is their own text's business.

**Mechanism 4, left open, and how it closes.** The glossary makes every item subordinate to an OCCURS DEPENDING ON
entry a variable occurrence data item, so an item of the table that is redefined IS one; SR5 forbids a definition
that "includes" one. Both readings are honest: "include" as "contain within it" (then the pair is legal, and the
rule keeps redefined areas a fixed size, which they are), or "include" as covering the item itself (then no
redefinition may stand inside such a table). The text does not decide it. CLAUDE.md rule 1 then gives the order:
what GnuCOBOL does, measured; failing that IBM Enterprise COBOL or Micro Focus, from their manuals; and the choice
recorded in `docs/conformance/` for 1985. The same glossary sentence is the hinge of the sample's cause (c) row
`1985-SR-VI.5.8.3-1` (kb/Work PB5692's result): one determination must cover both.

**What follows.**
- Rows of mechanisms 1, 2, 3, 5, 6, 7, 8 and 9 do not close on their inherited verdict: each gets a `contradicted`
  record in the code-read store naming its note. Row 11 is `confirmed` by this ruling. Rows 4 and 10 stay
  `undecided`.
- The 2023 rows of mechanisms 1, 2, 5, 7 and 9 are re-recorded non-conforming by a registrar.
- Rate at 1985 so far: sample 6 of 60, batch 1 9 of 120, batch 2 10 of 120, batch 3 11 of 120: 36 of 420 rows
  contradicted, about 9 %; 3 more undecided.
