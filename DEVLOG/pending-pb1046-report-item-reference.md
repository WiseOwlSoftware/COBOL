## PB1046: a report section entry's data-name outside GENERATE and the sum counters is a refused reference (COBOLNET5092), no longer "not defined"

**The question.** kb/Work PB1046 asked whether a printable item's name may be referenced from the procedure division
(then the compiler rejects legal source) or not (then the diagnostic names the wrong rule). The standard answers it:
13.8.6.2.3 allows exactly two accesses to a report item outside the report section, a sum counter in the procedure
division and a DETAIL group by GENERATE, and 13.15.3 SR7 closes the list for the entry's data-name ("The data-name
shall not be referenced in any other way"). Both quotations pass `cite.py --check`. Refusing is right; the message was
the defect.

**The re-probe.** On the w20261501 build, `MOVE PV TO W`, `DISPLAY DL`, `DISPLAY PV OF DL OF RR`, `DISPLAY PV OF RR`
and a contained program's `DISPLAY PV` under `RD RR GLOBAL` all drew COBOLNET1639 "no declaration in this source
element gives the name", which is false: the report section declares it. The sibling arms said the same in their own
words: `FILE STATUS IS PN` and `RELATIVE KEY IS PN` (COBOLNET1639 from `ResolveClauseOperand`) and `OCCURS ... DEPENDING
ON PN` (COBOLNET0851). The control (`ADD 100 TO SC`, `DISPLAY SC` on a sum counter) already worked.

**The fix.** One lookup and one refusal. `ReportModel.NamedEntries` reads the written entries once (name,
qualification innermost first ending at the 01, sum-counter flag); `DataBinder.ReportEntriesNamed` walks the visible
reports (own plus GLOBAL ones of containers, nearest source element wins by 8.4.6.2.1 rule 3) with the same 8.4.2.2.3
SR4 `QualifierWalk` a sum counter's qualification already used, report-name last;
`DataBinder.ReportEntryReferenceRefused` reports the new COBOLNET5092 with both rules quoted. `ReportUnidentified`
(every procedure division identifier), `ResolveClauseOperand` and the data-division ODO lookup ask it before they say
"not defined". The report section's own ODO is exempt (`inReportSection`), since its operand rules are the report
section's. `ReportSectionOnlyNames` and `IsReportSubscriptCounter` now read `NamedEntries` rather than walking the
entries a second and third time. A reference the qualifiers do not reach (`PV OF W`) keeps COBOLNET1639, as does a
report without GLOBAL seen from a contained program.

**Goldens.** `cases/13.8.6.2.3-Report-Items/`: five refusals (procedure statement, report group, contained program
under a GLOBAL report, FILE STATUS, OCCURS DEPENDING ON), each at all four editions in `wiseowl/diagnostics.json`, and
the positive control `pb1046_report_sum_counter_in_contained_program` (a contained program alters a sum counter and
GENERATEs the DETAIL group: prints 0100, derived from 13.18.54.4 GR2 and GR12). `ReportItemReferenceTests` has 22
cases, including a complement scan that names the three arms and the single reporter of the descriptor.

**Rows.** SR-13.15.3-7 PARTIAL -> CONFORMS (`closes_rows`); the two 13.8.6.2.3 rows gain witnesses.

**Not fixed.** REDEFINES and RENAMES naming a report item draw COBOLNET1654 / 1655, which say "does not name a
preceding entry / an item of the record": true, not the false "not defined", so they were left. A contained program's
own data-division clause naming a container's GLOBAL report item still says "not defined", because the container's
reports are inherited after the contained data division binds; the report carries the lead.

**Self-review.** Architecture: one lookup and one refusal, the three arms call it. Code: the message quotes the
standard word for word. Performance: `NamedEntries` is built once per report and only on the failure path. Duplication:
two entry walks removed.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` at head 360313016, Linux half (WSL clone of
HEAD) GREEN. All 24 conformance results of the six new cases and all 22 `ReportItemReferenceTests` passed in leg 1
(`TestResults/build-local/000001-20261025T195328Z/leg-1-Conformance.trx`, `leg-1-Unit.trx`).
