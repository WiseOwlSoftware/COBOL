## PB4858: LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER are USE FOR DEBUGGING targets, and DEBUG-ITEM is referenced only inside a debugging section

Wave 20261905 A took the two open items of kb/Work PB4858 and re-probed PB4855.

**Counter registers as identifier-1.** The last staged COBOL1571 of the data-name trigger is gone. The three registers are special-register data
items the 1985 general format does not exclude (ISO/IEC 1989:2023 8.4.3.14.4 GR1 and 8.4.3.15.4 GR1 make each "a temporary unsigned integer data item"),
but they are runtime-sourced and no `Place`, so the reach walk had nothing to match a target against. The structure now has two kinds of reference:
`BoundReferences.Of` answers a `Reach` (the places, plus the register reads, `BoundLinageCounterRef` and `BoundReportCounterRef`), a
`BoundDebugTarget` carries the register it watches (`Register`, matched by `NamesRegister`), and DEBUG-CONTENTS is the register's storage image
(`DebugContents.Register`, the image CONVERT already uses). PAGE-COUNTER's target also carries the report's counter item, so a store through
`ReportPageCounterPlace` matches like any item; LINAGE-COUNTER and LINE-COUNTER are changed by the I-O and report engines alone, so their bare form
never fires. The table builder runs before any statement binder exists, so the one resolution of "which file or report does this counter name"
moved out of `ExpressionBinder.LinageFileOf` and `ReportWriterBinder.CounterReportOf` into `CounterResolver` (`LinageFileOf`, `ReportOf`), with
`ReportScope.Named` as the one report-name lookup; no forwarder was left behind. Golden `pb4858-debug-counter-registers` (accept 85) computes
LINAGE-COUNTER from 13.18.34.4 GR7 c 2 and d (OPEN OUTPUT sets one, WRITE AFTER ADVANCING 2 adds two: "03"); its twin
`pb4858-debug-counter-registers-removed-2002` is refused COBOL0902.

**Syntax rule 11.** "References to the special register DEBUG-ITEM are restricted to references from within a debugging section"
(§1985:XV.3.2.3) was not enforced: `DEBUG-CONTENTS` in ordinary code compiled. The statement funnel now asks each statement's own references
(`StatementBinder.ReferencesDebugItem`, the same `Reach` the planner reads) and refuses one outside a debugging section, COBOL6442. Golden
`pb4858-debug-item-outside-section`; determination D-DEBUG-ITEM-REFERENCE in docs/CONFORMANCE.md.

**PB4855 stays open.** Re-probed: `SORT T ASCENDING` (Format 2, `--std 2002 --permissive`) still runs no section for `ALL REFERENCES OF T`. Making the
reach see the table is the easy half; rule 24 c's DEBUG-CONTENTS is the whole table's storage image, which needs a whole-table place kind and its image
arms (typed array, shared REDEFINES area, dynamic table), a redesign beyond this slice; the note carries the finding.

Codes used: COBOL6442. COBOL6443 and COBOL6444 returned.
