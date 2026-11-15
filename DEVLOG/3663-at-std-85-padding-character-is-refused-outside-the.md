## Entry 3663 — 2026-11-15 00:42 PST — At --std 85 PADDING CHARACTER is refused outside the sequential entry, and a sort-merge ASSIGN admits a figurative constant and a list (PB5565, PB5587); PB5587 review round: a numeric literal is the sort-merge ASSIGN's literal-1 at 85, and the golden cites the SORT rule

### At --std 85 PADDING CHARACTER is refused outside the sequential entry, and a sort-merge ASSIGN admits a figurative constant and a list (PB5565, PB5587)

Two findings of the 1985 re-read 5 shared one table's neighbourhood, the file control entry's per-format clause rules, and were
fixed as one wave group.

**PB5565.** `SELECT F ... ORGANIZATION IS INDEXED ... PADDING CHARACTER IS "*"` (and the relative twin) compiled and ran at
--std 85, because `FileControlKeyRules.EntryClauses`, the one table saying which format prints each clause, had no row for the
clause. The 1985 indexed and relative entries (§1985:IX.2.3.2, §1985:VIII.2.3.2) print no PADDING CHARACTER, which "specifies the
character which is to be used for block padding on sequential files" (§1985:VII.2.7.4 GR1); §2002:12.3.4.1 prints it in Format 3
alone. The note's repros printed no
diagnostic at its pinned compiler (I did not re-run them before the edit; after it each prints COBOL2912). The fix is a Format 3 row, `a PADDING CHARACTER clause`, so the clause is refused by the same SR11 row that refuses
RECORD DELIMITER on a keyed file (COBOL2912), at 1985 and 2002. The clause is deleted from 2014 and the version gate refuses it
there, so the row reads `PaddingCharacterSpec.InEdition`, set by the binder below COBOL-2014: a 2023 entry writes no clause of any
format and gets only the removal diagnostic, never a second one citing a format the clause is not in. The drift test that
re-derives every row from the printed 2023 diagrams could not hold this row (no 2023 diagram prints it), so a second test,
`PaddingClause_IsCarriedBySequentialFormatAlone`, re-derives it from the 1985 general format and the 2002 Formats 1 to 4.

**PB5587.** `SELECT SF ASSIGN TO SPACE` and `ASSIGN TO "W1" "W2"` on an SD were refused at 85 (COBOL2645, COBOL2256) by the 2023
rules of `AssignTargetRule`, which reads the TO phrase of every entry at every edition. §1985:XI.2.3.2 prints
`ASSIGN TO {implementor-name-1 | literal-1} ...` and §1985:XI.2.3.3 carries no restriction of literal-1 (the other 1985 entries
each add SR3), so a figurative constant stands where literal appears (§1985:IV.4.2.2.2.3 GRL2.3). The difficulty was ordering: the
TO phrase is read at the SELECT, and the file is known to be an SD only when its SD entry is read later. The fix keeps the
operand reader pure and records the two format-level refusals on `FileModel.EntryShapeFindings`; `FileControlKeyRules.Screen`,
which knows the kind and the edition, reports them unless the file is an SD below COBOL-2002. The zero-length, non-alphanumeric
and NULL refusals stay immediate (they are lexical, not a format's). COBOL-2002 keeps both refusals for an SD
(§2002:12.3.4.2 SR4). `docs/conformance/7/DOC-A.1-71.md` states the 1985 basis for the sort-merge entry.

**Goldens.** `pb5587-sort-merge-assign-figurative-and-list` (accepted at 85: two sorts, one `ASSIGN TO SPACE`, one
`ASSIGN TO "W1" "W2"`; output computed from the ascending key), and the refusals `pb5565-padding-on-indexed-85` and
`pb5565-padding-on-relative-85` (COBOL2912 at 85). **Rows.** `1985-FMT-IX.2.3.2`, `1985-FMT-VIII.2.3.2` and `1985-FMT-XI.2.3.2`
PARTIAL to CONFORMS (record_verdicts, GAP 63 to 60).

**Left open, seen on the way.** A 1985 FD's `ASSIGN TO SPACE` is still refused with a message citing §2023:12.4.5.2 SR4 where
§1985:VII.2.3.3 SR3 is the rule that applies; the outcome is right, the citation names the wrong edition.

### PB5587 review round: a numeric literal is the sort-merge ASSIGN's literal-1 at 85, and the golden cites the SORT rule

The branch review found the row 1985-FMT-XI.2.3.2 flipped to CONFORMS while a numeric literal as literal-1 (`SELECT SF1 ASSIGN TO 123` on an SD) was still refused at --std 85, by the grammar (COBOL5506), though §1985:XI.2.3.3 has no SR3 making literal-1 nonnumeric as the other entries do. Root fix: `assignTarget` in CobolIO.g4 admits `numericLiteral`; `AssignTargetRule.Text` records the SR4 refusal (COBOL2645) on `FileModel.EntryShapeFindings` so `FileControlKeyRules.Screen` reports it unless the file is an SD below 2002. The numeric literal is now refused by name at 2002+ (it was COBOL5506 at every edition and entry). The golden gained a third SD (`ASSIGN TO 123`, sorts AAA BBB CCC; runs verified), a 2002-and-later negative `pb5587-sort-merge-assign-numeric-2002` (COBOL2645), and its ascending-order citation now names §1985:XI.4.4.4 GR2a.

