# Rulings on the non-agreeing rows of the 1985 code reads, batches 11 and 12 (the Mythos lane, Monday 2026-11-16 05:24 PST)

**The batches.** Slices 1985-216 to 1985-219 of `E:/COBOL-coord/scratch/coderead-1985/`, read on the code at
`--std 85` at main d92742fe62 (the Mythos lane's two runs of the operator's workflow: wf_8905bb16-dd6 and
wf_c4645b74-119). Batch 11 (slices 216 and 217) is the twelve KEPT rows that were closed after batch 10 was built.
Batch 12 (slices 218 and 219) is NINE RE-READS: rows whose first read was contradicted and whose defect note has
landed since (kb/Work PB5854), read again on the fixed code. Of the 21 rows 17 agree with the inherited verdict:
seven of the nine re-reads now confirm their row. Four do not (`coderead-1985/non-agreeing-216-219.json`).

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
programs at `--std 85` and `--std 2023` on a compiler built at main d92742fe62:
`my-20261115/contradictions-b11/rulings.json`, programs under `contradictions-b11/src/`. I ran three myself at
`--std 85` with the tracked probe runner, and re-ran every one of the checker's citations through `cite.py --check`
(all OK). `my-20261115/accept-comp/acc4.cob` with the input line `12345678` printed `D4=[1234]`, `C4=[5678]`,
`S4=[+2345]` and `X4=[1234]`. `unsn.cob` printed `N OVERFLOW`. `insr.cob` ended
`error CS1503: Argument 4: cannot convert from 'long' to 'string?'`, exit 70.

**Ruling: four mechanisms, all four UPHELD, each a new note.** Cause (a) = wrong at 85 only; cause (b) = wrong the
same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1985-GR-VI.6.5.4-4` | UPHELD (b) | wrong_answer | An ACCEPT from the device into a binary or packed numeric item converts the whole 80-character record, so characters the rule says are ignored decide the value: the line `12345678` into `PIC 9(4) COMP` stores 5678, where a `PIC 9(4)` DISPLAY item stores 1234. See below: decided. | PB5856 |
| 2 | `1985-GR-VIII.2.4.4-8` | UPHELD (b) | under_rejects | Two programs that describe one external relative file may name RELATIVE KEY items of the same name at DIFFERENT character positions of the external record, and the compiler accepts it: it identifies the item by its name path alone. See below: decided. | PB5857 |
| 3 | `1985-GR-VI.6.27.4-17` (a re-read) | UPHELD (a) | wrong_answer | At `--std 85` an UNSTRING with NO POINTER phrase from a zero-size sender runs ON OVERFLOW, where the 1985 text gives NOT ON OVERFLOW. The fix of kb/Work PB5816 carried the ruling made for WITH POINTER to the form with no POINTER, and leg C of its witness expects the wrong answer. | PB5858 |
| 4 | `1985-SR-VI.6.18.3-6` (a re-read) | UPHELD (b) | under_rejects, misleads_diagnosis | `INSPECT X TALLYING N1 (1:2) FOR ALL "A"`: a reference-modified numeric counter passes the binder, which reads the base item's PICTURE, and the generated C# fails to compile (CS1503, exit 70), at every edition. The fix of kb/Work PB5734 covered STRING and UNSTRING only; the finding file that named INSPECT reached main three minutes after that fix merged. | PB5859 |

**Mechanism 1, decided: the code is wrong and one register sentence with it.** `§1985:VI.6.5.4 GR4b` says "only the
leftmost characters of the transferred data are stored in the receiving data item" (cite.py OK), and the size it
compares is the item's: `§1985:IV.4.3.4` says "The size of an elementary data item or a group item is the number of
characters in standard data format of the item" (cite.py OK), four for `PIC 9(4)` whatever its usage. What
`§1985:VI.6.5.4 GR1` leaves to the implementor is the conversion ("Any conversion of data required between the
hardware device and the data item referenced by identifier-1 is defined by the implementor", cite.py OK), not which
characters are converted: a value decided by characters five to eight is a breach of GR4b, not a conversion. The
register already says so in A.1 item 5 (the receiving size is "the digit count plus a SIGN SEPARATE position for a
fixed-point numeric one"); the sentence of A.1 item 1 that gives a binary or packed item one whole record is wrong
and is corrected by the fix. kb/Work PB3513 (landed) took the whole-record read from GnuCOBOL's behaviour; the ISO
text controls here, so GnuCOBOL does not decide (CLAUDE.md rule 1). The signed result is right as it is: the five
characters `12345` into `S9(4) SIGN LEADING SEPARATE` give `+2345`.

**Mechanism 2, decided: the same external data item is the same storage.** `§1985:VIII.2.4.4 GR8` says "the
RELATIVE KEY phrase in each associated file control entry must reference that same external data item in each case"
(cite.py OK). The checker found the text open between an item identified by its place in the external record and
an item identified by its name, and leaned to the first. I decide the first. An external data item is storage the
programs of a run unit share; two descriptions that put the key at different character positions describe two
different items of the record whatever they are called, and two programs that share one file connector and take the
relative record number from different bytes is the fault the rule exists to exclude. The compiler's agreement check
for RECORD KEY on an external indexed file already identifies the item by its position; the RELATIVE KEY check uses
the same identity (one mechanism, CLAUDE.md rule 4), and a mismatch is refused as the access-mode mismatch is.
The 2023 text reaches the same result in its own words (§2023:14.8.4.2: the relative key data items "shall refer to
the same corresponding storage"; the checker ran that citation, cite.py OK), so the 2023 row is contradicted too.

**Mechanism 3: what the fixer must also rule.** The checker left one point at medium confidence: whether, at 1985,
an UNSTRING with no POINTER from a zero-size sender also space-fills the first receiving item and adds 1 to the
TALLYING item (`§1985:VI.6.27.4 GR13` and its item g). Today the compiler leaves both unchanged. The fix decides it
from the text and its witness says which.

**What follows.**
- All four rows do not close on their inherited verdict. The 2023 rows of mechanisms 1, 2 and 4
  (`GR-14.9.1.4-4`, `GR-12.4.5.3-1`, `SR-14.9.22.3-5`) are re-recorded non-conforming by a registrar.
- The re-reads did what kb/Work PB5854 wants of them: seven contradictions are answered by their fixes, one fix
  (PB5816) is shown to have gone too far, and one finding (INSPECT) is shown never to have been in its fix.
- This batch adds two rows never contradicted before (mechanisms 1 and 2) and keeps two contradicted (the
  re-reads of mechanisms 3 and 4, each under a new note). The store's own counts after the import are the figure
  to quote for the whole; no total is restated here.
