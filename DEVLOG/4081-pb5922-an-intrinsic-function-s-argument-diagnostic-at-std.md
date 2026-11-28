## Entry 4081 — 2026-11-27 19:35 PST — PB5922: an intrinsic function's argument diagnostic at --std 2002 and 2014 cites that edition's clause

**Mechanism.** The argument rules of the intrinsic functions are written once, as §2023:15.n.3 clauses on the schema rows of `IntrinsicArgumentRules`;
`IntrinsicCitations.Clause` chose the clause by edition only for COBOL-85, so a program compiled at `--std 2002` or `--std 2014` was told a 2023 rule
number (COBOL1627 alone was 145 lines at 2002 and 134 at 2014 in the edition-citation ratchet).

**What the re-probe showed.** The sweep (`WISEOWL_COBOL_RECORD_CITATION_SWEEP`, the net's measure) listed COBOL1627 as the largest family left; the
named COBOL1560/COBOL1707 (screen-module licence text, PB5581's wording), COBOL1688's run-time reasons and the EC-REPORT-INACTIVE run-time text are
not intrinsic-function rules and stay in the note.

**The fix.** `IntrinsicCitations.Clause` now reads every §2023:15.n.2 / §2023:15.n.3 citation in a clause text and renders it in the edition in force:
the function from `IntrinsicCatalog.FunctionAt(2023, n)`, its clause in the edition from `IntrinsicCatalog.Paragraph(year, name)` (the functions in
ordinal name order from 15.6 at 2002 and 15.7 at 2014, derived and read back from the headings of all three transcriptions), the subclause one
earlier, and the 42 rules an edition numbers differently or lacks in `IntrinsicCitations.Edition2002` / `Edition2014` (each found by matching the
2023 rule text against the edition's rules and read). The argument types (§2002:15.2, §2014:15.3), the integer type (AR5 of each), the floating-point
usage rule (§2002:13.16.58.3 GR12, §2014:13.18.60.3 GR12) and the integer-operand bar (§2002:8.4.2.2.2 SR9, §2014:8.4.2.2.2 SR11) cite their own
editions. Sibling sites of the same mechanism moved onto it: the zero-length-literal and NUMVAL-C currency-string messages, DISPLAY-OF / NATIONAL-OF's
argument rules, the locale-name rule of the LOCALE functions, the arity and ALL-subscript messages. A clause text of another shape throws at 2002 and
2014, so a form the reader does not know fails the drift test, never citing 2023 at 2002.

**Tests.** `IntrinsicEditionClausesDriftTests` (function headings of 2002, 2014 and 2023 against the derived numbers; every renumbered rule names a rule
the edition prints; every 2023 rule has a counterpart or is said to have none; sample citations). Refusal expectations that spelled a 2023 clause for
all editions (`pb305-numval-c-edited-category`, `pb59-*-substitution`, `stat-*-zero-length-literal-argument`) now name the shared prefix, and
`pb6174-constant-length-integer-function-alphanumeric-argument` names `§2002:15.34.2 AR1`. COBOL1627 and COBOL1645 left the ratchet: 508 codes at 2002,
517 at 2014.

**Rows.** None re-verdicted: PB5922 stays open on COBOL1560/1707, COBOL1688's run-time reasons, EC-REPORT-INACTIVE and the intrinsic messages that
still spell a 2023 literal (PB6118's sweep calls `IntrinsicCitations.ClauseSince2002`).

