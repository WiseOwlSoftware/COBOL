## PB5652: a bare SUM in a report entry is the clause only where a clause can begin

**The mechanism.** At `--std 2002` a REPOSITORY-declared SUM intrinsic written without FUNCTION inside a report clause
operand (`SUM OF WA + SUM(WB WC)`, `SOURCE WA + SUM(WB WC)`, `SOURCE SUM(1 2 3)`) was COBOL5506. The note named
`CobolReportWriter.g4`; the root was the lexer. PB924 made every bare SUM in the REPORT SECTION the SUM clause keyword
(`IsReportSumClauseKeyword`, §2023:13.18.54.3 SR9), so the '(' after it was a grouping paren and a keyword-omitted call
could not parse. COBOL 2002 has no SR9, and §2002:12.2.7.3 GR13 lets a REPOSITORY-declared intrinsic drop FUNCTION
anywhere in the environment division's scope.

**Re-probe (this build, before the fix).** fsum.cob and fsrc.cob: COBOL5506 near 'WC' at 2002 and 2023. sumsrc.cob:
COBOL5506 near '2' at 2002 and 2023.

**The fix.** A clause keyword can stand only after something has ended. The region exception now applies only where the
entry does not owe an operand: no paren is open and the token before SUM is not in `ReportOperandFollows` (the
arithmetic operators, SOURCE, SOURCES, IS, ARE, SUM, OF, IN, WHEN, FROM, BY and the relational words). There `SUM (` keeps
the reference paren that the keyword-omitted call reads. The lexer reads no edition. At 2002 the tree binds as the
intrinsic. From 2014 (§2014:13.18.54.2 SR9) `IntrinsicBinder.ReportSumClauseWord` refuses it as COBOL7933 (new,
`ReportSumWithoutFunctionKeyword`), which replaces the raw parse error. Where a clause can begin, after a complete operand
or a separator comma, SUM stays the clause keyword at every edition. `SUM WA SUM (WB * WC)` still has two SUM groups.

**After the fix.** fsum and fsrc print `0111` at 2002, and sumsrc prints `0006`. At 2023 all three are COBOL7933.

**Goldens.** `g2sum07_sum_repository_intrinsic_unprefixed` is accepted at 2002 and covers the SUM addend, SOURCE, SOURCE
SUM(1 2 3), SUM OF SUM(..), the repeated keyword, PRESENT WHEN true and false, and SUM inside FUNCTION MAX's argument list.
`g2sum08_sum_unprefixed_undeclared` has no REPOSITORY declaration and is refused COBOL1543 at 85 and 2002.
`pb5652-report-sum-unprefixed-sr9` is refused COBOL7933 at 2014 and 2023. CobolLexerModeDriftTests pins every member of
the operand-owed set, plus the clause-keyword arm.

**Rows.** In the 2002 inventory, 2002-SR-13.16.52.2-1 went from DIVERGES to CONFORMS and 2002-GR-12.2.7.3-13 went from
GAP to CONFORMS. The 2002 GAP count fell from 1163 to 1161. The 2023 row SR-13.18.54.3-9 now gives editions 2014,2023,
since SR9 is a 2014 rule.

**Docs.** DESIGN-frontend-grammar §1.9 now states where the exception holds. A paragraph of REPORT_WRITER_DESIGN still
called `SUM (A * B)` "staged loud COBOL0899", which PB924 had already fixed. That paragraph now describes the current rule.

**Gate red, fixed.** The first gate was RED at the generator. `pb924_report_sum_paren_addend` names only
SR-13.18.54.3-9 and was accepted from 2002, which is outside the rule's band now that the band is 2014,2023. Its `accept`
moved to 2014. The 2002 parenthesised-addend reading is witnessed by g2sum07 leg E under 2002-SR-13.16.52.2-1.
