## Entry 2994 — 2026-11-02 05:37 PST — A bare operand takes the nearer data item that hides a container's GLOBAL condition-name (PB4433)

Wave 20261824, group C. PB4219's review fix made `ConditionBinder.ReportMisqualifiedCondition` word a condition-only
position whose word a nearer data item hides ("names a data item, not a condition-name", ISO 8.4.6.2.1 3) a)). The bare-operand
classifier (`AnalyzeBareOperand`, IF / EVALUATE / PERFORM UNTIL) asks the same method whenever `Probe` fails, and a bare operand
admits a data item. A subscripted scalar `RELF (1)` in a contained program that hides a container's GLOBAL `88 RELF` therefore
drew COBOL1639 ("this position requires a condition-name") where the same statement with no 88 of the spelling draws COBOL2096
(8.4.2.3.3 SR2), and EVALUATE added a false COBOL1634 (Table 15) about a condition the operand never was.

Re-probe: both statements reproduced on the build (COBOL1639 twice and COBOL1634); the no-88 control gave COBOL2096.
Fix: one predicate, `ConditionBinder.NamesHidingDataItem`, asked by `ReportMisqualifiedCondition` (the condition-only positions,
unchanged) and by the bare-operand arm, which now leaves a word it answers true for to the value arm. SEARCH ALL's bare WHEN
still reports through its own call. Review lead R2-2 (the stale verdict list in `SearchAllFormat2Rules`' comment) corrected.

Goldens: two refusals at 85/2002/2014/2023 (`pb4433_if_...`, `pb4433_evaluate_...`, COBOL2096) in
`8.4.6.2-Local-and-global-names`, and `HiddenConditionNameTests.ABareOperand_...` (IF, IF NOT, PERFORM UNTIL, EVALUATE; no
COBOL1639 / COBOL1634). Row GR-8.4.6.2.1-3 gains the witnesses (CONFORMS, GAP 19 to 19, batch `w20261824c-pb4433`).
No codes used (COBOL5941-5943 returned).

