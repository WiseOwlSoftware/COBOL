## PB4111 and PB4488 — two determinations become defects

**PB4111.** The adjudication (wave 20261857) found that under STANDARD-DECIMAL the 34-digit VARIANCE, STANDARD-DEVIATION and PRESENT-VALUE are right, and that under native arithmetic
§8.8.1.3 and §15.4.1 leave an implementor-defined approximation, so CLAUDE.md rule 1 step 2 (GnuCOBOL, exact) decides. Deciding citation: `OK  §15.4.1` "When native arithmetic is in effect
and an equivalent arithmetic expression is specified, the value returned is an implementor-defined approximation of the value of that expression." The note became a `wrong_answer`, `silent`
defect that also folds in the 35-38-digit intake and 34-digit mean siblings, and its 10^-10 is corrected to 10^10. New notes: PB4568 (a wide argument beside an SDIDI or binary64
argument enters as its image), PB4569 (native `2 ** 130` keeps 16 digits), PB4570 (MEAN's 34-digit value and the binary64 carrier against GnuCOBOL's exact decimal).

**PB4488.** The adjudication found the note's question a false dichotomy: a COBOL-WORDS directive cannot affect COPY or REPLACE, so refusing `CPY BOOK.` is correct. Deciding citation:
`OK  §7.3.10.4 6)  (General rules)` "A COBOL-WORDS directive does not affect any Compiler directing statements or Compiler directives." What is wrong is that a synonym of a reserved word
with no lexer token (COPY, REPLACE, LOCALE, NESTED, ANYCASE, SYSTEM-DEFAULT, USER-DEFAULT, EXCEPTION-OBJECT, COMMA, MINUS) is accepted as a user-defined word, and that the refusal of
`CPY BOOK.` carries a period-missing hint. The note became a MAJOR `under_rejects` and `misleads_diagnosis` defect with `rejects_legal_source: false`.

Also filed: PB4571, from re-queuing PR #1101 by hand while the operator lane was idle. Merge-group run 41779240040 lost Windows shard 6/7 to a Conformance step that hung until the 20-minute job limit, and the hang left no test results to name a case, because the step has no timeout of its own and no blame-hang collector.
