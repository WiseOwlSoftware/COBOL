## Entry 2885 — 2026-10-31 15:14 PDT — A SELECT entry with no ASSIGN clause is refused: the unbracketed members of the four general formats are one table (PB4236)

**The defect.** `SELECT F.` with an FD compiled at every edition and ran, the file bound to an invented `<file-name>.txt`
(`FileModel.AssignTarget` was initialised to the file-name). The re-probe on this build reproduced it at `--std 85` and
`--std 2023` with no diagnostic, and the same for an SD, a relative entry and an indexed entry with a RECORD KEY.

**The spec.** ISO §12.4.5.1, rendered from PDF p342-344 (the OCR'd diagrams were read against the page, not trusted): the
ASSIGN clause opens its line with no bracket in Formats 1, 2, 3 and 4, and the figure notes say every stacked group is a
plain brace (exactly one) or a plain bracket (zero or one), so an unbracketed clause is a required member. The prime RECORD
KEY is the only other one (Format 1); every other clause opens inside a bracket. GnuCOBOL refuses a SELECT without ASSIGN
at every `-std`, which agrees (it is the net, not the authority).

**The fix shape.** The compiler already screened ONE required member, Format 1's RECORD KEY, as a special row of the
key-operand table (the only row with no rule id), so the next unbracketed clause had no place to go and ASSIGN, the one
printed in all four formats, was screened by nothing. The root is a missing concept, not a missing `if`:
`FileControlKeyRules.RequiredClauses` is a table of `RequiredClause(Face, Printed, Applies, Written)`, `Printed` the set
of formats that print the clause unbracketed and the kinds of file it is required of derived from it. `Screen` runs it
first and reports COBOL5701 at the entry. The RECORD KEY requirement moved from the key-operand table into it (it left
COBOL0863, whose subject is a key clause that was written; `FileControlKeyRule.RuleId` is no longer nullable because the one
null-id row is gone). `FileModel.AssignWritten` is the fact (`AssignTarget` is empty for bare `ASSIGN USING`, so it cannot
say), `FileModel.HasSelect` keeps the screen off the model the binder builds for an FD whose file-name no SELECT names
(§13.4.5.3 SR1 reports that one), and `AssignTarget` is no longer defaulted to the file-name, the invention itself.

**The drift test.** `FileControlKeyRuleDriftTests.EveryFormat_PrintsExactlyTheRequiredClausesOfTheTableUnbracketed` replaces
the single-clause diagram test: it reads the four printed diagrams, takes EVERY line that opens with no bracket (SELECT
aside) and requires exactly one table row for it, and requires each row to open a line in exactly the formats it names. The
next unbracketed clause a later edition prints is a failing test until it is a row. `EveryScreenedRuleInTheInventory_IsARowInTheTable`
now counts the table as the implementation of the format row FMT-12.4.5.1.

**Goldens.** Accepted at 85: `12.4.5-File-control/pb4236_assign_clause_forms` (a Format 3 entry with ASSIGN after ORGANIZATION and
TO omitted, a Format 2 entry with ASSIGN in the middle, and a Format 4 SD's SELECT, writing and reading back and sorting).
Refused at all four editions: `pb4236-select-without-assign`. `FileControlKeyRuleSpecTests.ASelectEntryWithoutAnAssignClause_IsRefusedInEveryFormatAtEveryEdition`
covers the indexed, relative, record-sequential, line-sequential, bare-SELECT and sort-merge kinds at every edition, with
controls that write the clause in each form.

**Rows.** FMT-12.4.5.1 (CONFORMS before, on the grammar and the closed-format pass alone) now cites the table and the drift test;
`closes_rows: [FMT-12.4.5.1]`.

**Design.** `docs/FILES_DESIGN.md` gains the required-members paragraph beside the EntryClauses one.

**Self-review.** Architecture: one table, one screen, the fact on the model, no C# text in a bound node. Code: the message names
the cited rule; sibling arms (SD, relative, indexed) probed one by one, and the FD-without-SELECT model does not hear a second
error. Performance: two predicates per entry, no allocation. Duplication: the RECORD KEY requirement no longer has a second home.

