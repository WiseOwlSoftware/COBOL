## Entry 3796 — 2026-11-16 15:16 PST — ASSIGN TO NULL compiles at --std 85 (PB5802)

At `--std 85`, `SELECT F1 ASSIGN TO NULL` was refused with COBOL1639 although NULL is not a 1985 reserved word, so it is
implementor-name-1 (`§1985:VII.2.3.3 SR3`) and `ASSIGN TO NULLS` already compiled. The grammar parses NULL as the predefined
NULL at every edition, and `AssignTargetRule.Text` sent that arm to `PredefinedNullRule.Report` without an edition test.
After the fix `--std 85` exits 0 printing DONE, and `--std 2002`, `2014` and `2023` still exit 65 with COBOL2576.

Fix: in `Text`, a `predefinedNull` token where `UnreservedKeywordReference.IsFreeAt("NULL", edition)` reads through `WordText`
like any word (a constant entry of that name still wins, per the determination at `WordText`); `WordText` now takes the
spelling and an anchor context. Goldens: `pb5802-assign-to-null-85` (accepted at 85) and `pb5802-assign-to-null-2002` (refused
at 2002, 2014, 2023 with COBOL2576). No inventory row closes (the row is a 1985 rule id). Sibling sweep: the other NULL consumers
(ExpressionBinder, CallBinder, SetBinder, OoBinder, PredefinedNullRule) already test `IsFreeAt`.

