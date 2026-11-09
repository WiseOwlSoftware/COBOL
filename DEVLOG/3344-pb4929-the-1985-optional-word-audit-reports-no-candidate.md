## Entry 3344 — 2026-11-08 16:23 PST — PB4929: the 1985 optional-word audit reports no candidate

PB4929 asked for a verdict on each of the 36 candidates that `audit_grammar_optional_words.py --edition 1985` reported, and
for an audit whose next run is empty. PB4919 had already landed the same lead: it fixed the five real mechanisms and
recorded a verdict for every other candidate. On this branch's build the run still reported 25 candidates. Every PB4919
witness was re-run at `--std 85`. The gates hold: USE without AFTER or PROCEDURE, PERFORM VARYING without BY, and SET with
a second phrase each draw COBOL0900, and `EVALUATE A B` draws COBOL5506. The legal forms compile: bare `EXIT`,
`SYMBOLIC CHARACTERS BELL 8`, `VALUE OF FILE-ID "F.DAT"`, `READ F INVALID …`, `RERUN EVERY REEL OF F` and `PAGE LIMIT IS 10`.
One PB4919 verdict was wrong. The Report Writer SIGN clause on page 629 underlines SEPARATE with no bracket, and
§1985:XIII.3.17.3 SR3 requires it in a report group entry. The compiler already refuses the omission at 85
(`DataBinder.ScreenReportSignClauses`, golden `pb3683-report-sign-without-separate-85`), so only the verdict changed.

Every remaining candidate was a misreading by the audit. There were four mechanisms, and each is fixed in the tool.

- **A required choice is one slot.** `demanded` used to delete a choice's words one at a time. `(IS | ARE)` without IS
  became `(ARE)`, which no sibling alternative spells, so SYMBOLIC CHARACTERS' IS/ARE and PAGE's LINE/LINES were reported
  although a sibling alternative accepts the omission. A choice with an arm that writes no word, such as VALUE OF's
  `(cobolWord | literal | IS)+`, was counted as demanding IS. `_slots` deletes a choice whole, and a choice with a wordless
  arm fills no slot. In the 2023 run this also drops the false SYMBOLIC IS/ARE and LINE ARE/NUMBERS candidates (7 → 3,
  part of PB4110). The only anchoring changes are that `reportPageClause` no longer anchors the LINE clause, and that
  SELF/SUPER and the constant entry each gain a helper rule.
- **A construct is all its printings.** The 1985 standard prints a construct once in each module that uses it, and
  §1985:VIII.2.6.3 GR1 calls the Relative I-O RERUN clause "a subset" of the Sequential one. The audit read each printing
  alone, so the Relative RERUN's underlined ON, Inter-Program's `EXIT PROGRAM`, the Relative READ's INVALID KEY and the
  Report Writer SIGN each raised a candidate. `construct_of` now reads a scan's format with every printing of its
  construct: a statement by its verb, anything else by its whole title. Each candidate is reported once per construct.
- **An edition gate is a later phase that decides one edition's format.** A `decided-by` marker applied to every run,
  and the dead-marker check failed a 2023 run that matched none. The marker now takes `at <edition>`. USE AFTER/PROCEDURE,
  PERFORM VARYING/AFTER BY and the SET repetition carry one, naming the `VersionConformancePass` visitor that refuses each
  at 85.
- **A delimiter pair across lines.** Page 316 types `{{WHEN` and `[ALSO` at the head of a line and closes them lines
  later, the bracket with a drawn stroke. The drawing reader paired only delimiters on the same lines, so the underlined
  ALSO was held by nothing. `figure_reading.spans` pairs these strays in reading order. A `Holder` with
  `open_bottom`/`close_top` holds the text between such delimiters, and it sorts outside the pairs it contains. The 1985
  run's unpaired-delimiter count is now zero.

The 1985 run now reports 0 candidates and 13 DECIDED. The 2023 run's layout is unchanged line for line. The new self-test
arms (audit, `figure_options`, `figure_reading`) each fail with their mechanism removed. `DESIGN-frontend-grammar.md` and
`DOC_INDEX.md` describe the four mechanisms. PB4929 is landed with `closes_rows: []`, because the change is to tooling
and touches no compiler behaviour. PB4110 records which of its causes remain: COLUMN NUMBERS across nesting levels, and
the DYNAMIC LENGTH rule map.

