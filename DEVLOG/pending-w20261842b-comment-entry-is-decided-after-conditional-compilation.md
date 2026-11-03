## A COBOL-85 comment-entry is decided on the compiled text: a three-valued division, a carried candidate, and a resolver after conditional compilation, COPY, REPLACE and COBOL-WORDS

The operator lane is out of quota for a day, so the Mythos lane took over its work (this branch continues group A's reviewed
head `cd0217755`). PB4281 was filed as "one clause, no structural change"; the re-probe showed it was a stage-order defect
and CLAUDE.md rule 5 asked for the restructuring.

### What the re-probe showed (CLI, `--source-format free --permissive`, 2023)

- `>>DEFINE X AS 1 / IDENTIFICATION DIVISION. / >>IF X IS DEFINED / PROGRAM-ID. CE1. / >>ELSE / PROCEDURE DIVISION. / >>END-IF /
  AUTHOR. Pat O'Brien.` drew COBOL5563 and COBOL2633 on the apostrophe. GnuCOBOL 3.2 (`cobc -free -fsyntax-only`) accepts it:
  it scans the identification division's paragraphs after its preprocessor has run. WiseOwl read the paragraph before
  conditional compilation and, the branches disagreeing, took the text for no IDENTIFICATION DIVISION.
- `>>COBOL-WORDS EQUATE "DIVISION" WITH "DIV"` / `PROCEDURE DIV.` / `REMARKS. DISPLAY "HI".` drew `COBOL0001 unexpected '.'`: the
  converter never saw DIV as DIVISION, stayed in the identification division and read the PROCEDURE DIVISION paragraph as a
  comment-entry. A program with no comment-entry at all was refused.
- The same root has a third source the note did not name: `REPLACE ==PD== BY ==PROCEDURE DIVISION==` writes a division word
  the converter, which reads the text as written, never sees (§7.2.4.4 2)).

### The fix

`DivisionCursor.InIdentificationDivision` is a three-valued fact: yes, no, NOT KNOWN. The converter stays exact where it can be
(a certain IDENTIFICATION DIVISION discards the entry as before, a certain other division reads the paragraph as program
text), and where something after it could change what its words mean it does not guess: a `>>IF` / `>>EVALUATE` group whose
branches disagree, a `>>COBOL-WORDS` directive (the converters see the line, and the merged driver tells its own cursor, since its
blocks end at a directive line), a REPLACE or `COPY ... REPLACING` that names a word the cursor reads. It CARRIES the
candidate: each physical line of the header and of the entry becomes an inert comment-like slot of the logical text
(`CommentEntryCarrier`, the `DebugLineCarrier` device with its own noncharacter), so no entry text reaches a scanner.
`CommentEntryResolver` decides it in `Frontend.Preprocess` after the merged driver (conditional compilation, COPY, REPLACE) and
the COBOL-WORDS stage, before the text is lexed, on a cursor that reads the finished text through the group's `CobolWordsMap`.
In an IDENTIFICATION DIVISION the header is written `AUTHOR. .` and the entry dropped; elsewhere the lines are converted as the
ordinary lines they are, through the same walker and the same reference-format diagnostics
(`ReferenceFormatProcessor.ConvertCommentEntryCandidate`). `CommentEntryResolutionOrderDriftTests` pins the order, the single
maker and the single reader of a carrier, and that a carrier is a comment to the scanners.

D-CE2 (docs/CONFORMANCE.md) and the front-end design doc now describe this, including the one cost of carrying: a carried
candidate that proves to be a program paragraph is converted after conditional compilation, COPY and REPLACE, so a COPY or
a directive written inside such a paragraph is not processed by them.

### What the new golden found

The first EQUATE golden had no library text; adding a `COPY` of a copybook whose paragraph is named `SECURITY` failed with
`COBOL0001` at the copybook's `SECURITY. .`. The merged driver's cursor never sees a directive line (its blocks end at one),
so the COPY's library text was converted as if it stood in the IDENTIFICATION DIVISION. The driver now tells its cursor of a
`>>COBOL-WORDS` line it leaves in the text. The golden stays: it is the case that failed.

### Goldens and tests

`cases/6.5-Logical-conversion/pb4281_disagreeing_branches_paragraph_after_group` (2002, 2014, 2023; `>>EVALUATE`, the paragraph
named AUTHOR is the program's) with its `_at_85` refusal (COBOL0900); `pb4281_comment_entry_after_disagreeing_group_is_removed`
(refused COBOL0902 at 2002, 2014, 2023: the comment-entry is found, where the old reading drew COBOL5563 and COBOL2633);
`cases/7.3.10-COBOL-WORDS/pb4281_respelled_division_is_not_a_comment_entry` (2023, a copybook paragraph included) with
`pb4281_respelled_division_below_2023` (COBOL0900 at 85, 2002, 2014). Unit: `CommentEntryResolverTests` (fixed and free
form, both outcomes, the round trip of a line and its column map, a damaged carrier throws), `DivisionCursorTests` (the
three-valued groups, COBOL-WORDS, REPLACE naming a division word, the word map), `CommentEntryResolutionOrderDriftTests`.
The existing `pb4241_*`, `pb3245_*`, `pb1494_*` and `pb1758_*` goldens pass unchanged.

### Siblings

Registered, not fixed: `>>COBOL-WORDS EQUATE "COPY" WITH "CPY"` leaves `CPY BOOK.` no COPY statement (COBOL0307):
`CopyProcessor` finds the statement by the written word, before the COBOL-WORDS stage. Whether text manipulation is subject to
GR2 of 7.3.10.4 is an owner question.

### Self-review

Architecture: one mechanism per job (the cursor knows, the converter carries, the resolver decides); no second copy of the
division rule. Code: the header rendering `AUTHOR. .` is `CommentEntryHeader` in both the converter (certain case) and the
resolver (decided case). Performance: a text with no carrier costs one `Contains`; the per-line `ReadInto` the converter did
before is unchanged. Tests: a blank line inside a fixed-form candidate is a carrier too (a discarded slot before; the same
result). Duplication: the two lines that write a decided header and discard its entry exist in the converter (certain case)
and in the resolver (decided case), over the one `CommentEntryHeader`. Left unfixed on purpose: the cost of carrying, in
D-CE2.

Gate on the code head `7a34960f7` (run `000002-20261103T105031Z`): `=== BUILD-LOCAL GATE: GREEN — Conformance 21,290/21,290 · Unit
39,525/39,525 · Characterization 37/37 cases ran (skipped 0) ===`; the pb4281 goldens and refusals Passed in its
`Conformance.trx`. The commits after it change this entry, `docs/CONFORMANCE.md` D-CE2 (the REPLACE measurement of GnuCOBOL) and
nothing else; the report names the gate of the final head. `python scripts/semgrep/verify.py`: PASS, no rule count up.
