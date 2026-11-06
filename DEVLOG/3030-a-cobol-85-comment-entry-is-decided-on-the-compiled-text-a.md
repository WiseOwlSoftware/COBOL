## Entry 3030 — 2026-11-03 05:01 PST — A COBOL-85 comment-entry is decided on the compiled text: a three-valued division, a carried candidate, and a resolver after conditional compilation, COPY, REPLACE and COBOL-WORDS

The operator lane is out of quota for a day, so the Mythos lane took over its work (this branch continues group A's reviewed
head `a99ac480c`). PB4281 was filed as "one clause, no structural change"; the re-probe showed it was a stage-order defect
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

D-CE2 (docs/CONFORMANCE.md) and the front-end design doc describe this.

### The review's blocking finding, and the fixed point

The branch review (round 1) found that the first landing paid for carrying with a regression: a carried candidate that proved to
be a program paragraph (REMARKS in the procedure division) was converted AFTER conditional compilation, COPY and REPLACE, so
a REPLACE in force did not apply to its words and a COPY inside it was not expanded. A fixed-form program with
`>>COBOL-WORDS EQUATE "DISPLAY" WITH "SHOW"` before its identification division printed `7 1` where main printed `7 7`, and its
`REMARKS.` paragraph holding `COPY RGBOOK.` was refused (COBOL0307). Two causes, two changes:

- The directive tainted the rest of the text whatever it named. A `>>COBOL-WORDS` directive now leaves the division NOT KNOWN
  only when its operand names a word the cursor reads (a literal that is DIVISION, IDENTIFICATION, ID or an identification
  paragraph, or a compilation-variable-name standing in a literal slot, whose value only conditional compilation knows,
  §7.3.11.4 1)); `EQUATE "DISPLAY" WITH "SHOW"` leaves it known, as an unrelated REPLACE already did.
- The residual itself, for the cases that really are ambiguous. A late conversion cannot give the stages the text they needed,
  so it is gone: `CommentEntryResolver.Resolve` NAMES a candidate that is program text (`Resolution.ProgramText`, its physical
  line) and `Frontend.RunTextStages` runs the normalizer and `Preprocess` again with that line pinned as program text
  (`CommentEntryPins`, read by the two converters where the division is not known; the COPY processor hands the pins to the
  library text it normalizes), until a run names no new candidate. A program with no candidate pays one `Contains` and runs
  once; every other run adds a pin, so it ends. One case stays converted late: a library text two COPY statements bring in
  that the two decide differently, which a per-line pin cannot serve (a unit test pins that it is named for neither).

Finding R1-2 (a lead) shared the file: a free-form candidate header longer than 255 positions was reported COBOL2653 twice when it
proved to be program text. The first fix left a candidate header's length unreported and had whoever decides the candidate
report it; round 2 (finding R2-1, blocking) showed that regression: a candidate inside an omitted `>>IF` branch is never decided,
so a 308-position line there was accepted while every other over-long line of an omitted branch is COBOL2653 (§6.1 3) a)). The root
is that the gate belongs to the physical line, not to the comment-entry decision: the free-form converter asks it
(`ReportLineLength`) of every physical line once, before conditional compilation, a carried candidate's included, and the conversion of
a carried candidate (`gatesLineLength: false`) never asks it again.

Round 2 golden: `cases/6.5-Logical-conversion/pb4281_long_free_form_line_in_omitted_branch_candidate` (free form, a 308-position `AUTHOR.` line in an
omitted `>>IF` group after a disagreeing group; refused COBOL2653 at 85, 2002, 2014 and 2023, `wiseowl/diagnostics/` file of the same id), and the unit
test `FreeFormLineTooLong_InACandidateOfAnOmittedBranch_IsReported`. The lead R2-2 (a later candidate pinned on a text where an earlier one was
still hidden from COPY) is left for the clerk: it sits in `Frontend.RunTextStages`, which this round does not change.

New goldens, each failing on the reviewed head with COBOL0307 at the COPY statement and passing now with `7 7`:
`cases/6.5-Logical-conversion/pb4281_program_text_paragraph_after_group_is_read_by_copy_and_replace` (2002, 2014, 2023; a
disagreeing `>>EVALUATE`), `cases/7.3.10-COBOL-WORDS/pb4281_unrelated_cobol_words_directive_keeps_remarks_paragraph_in_the_stages`
and `pb4281_respelled_division_paragraph_is_read_by_copy_and_replace` (2023; the directive names no division word / names
DIVISION). Their refusal below 2023 is the directive's own introduction gate, pinned by `pb4281_respelled_division_below_2023`. Unit:
`DivisionCursorTests` (twelve directive operands), `CommentEntryResolverTests` (naming, pinned conversion, the two-inclusion
case, the 255-position gate once in each outcome), `CommentEntryResolutionOrderDriftTests` (the iteration's order, the single
maker and reader of a pin).

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
and in the resolver (decided case), over the one `CommentEntryHeader`. Review round 1 added the fixed point, whose
cost is a second run of the text stages for a program that has a program-text candidate, and none for any other.

Gate: see the report of the final head.

