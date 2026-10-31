## PB3767: an ambiguous reference that a report entry also spells is told its ambiguity

**Mechanism.** `ReferenceResolver.ReportUnidentified` asked `DataBinder.ReportEntryReferenceRefused` before any
ambiguity arm. A procedure division reference to a name declared under two groups (COBOL1639, ISO §8.4.2.2.3 SR1) that a
REPORT SECTION entry also spells drew COBOL5092 ("names the report group description entry", §13.8.6.2.3) and never the
ambiguity.

**Re-probe.** `PV` under G1 and G2 plus `05 PV` under a DETAIL group, `MOVE PV TO W`: COBOL5092 before the fix, COBOL1639
("2 declarations share the name") after.

**Fix shape.** The note proposed a `!Symbols.TryResolve` guard like the constant-name and condition-name arms. That would
have broken the opposite case: `PV OF DET-1` reaches the report entry and neither data item, and must still draw
COBOL5092, not "declared, but not under the given qualifier". The guard is the count the data division's clause operands
(`DataBinder.ResolveClauseOperand`) and OCCURS DEPENDING ON (`DataBinder.Odo`) already take before they ask
`ReportEntryReferenceRefused`: `QualifiedCandidates(name, qualifiers, scope).Count <= 1`. Those two siblings were swept
and already count first, so `ReportUnidentified` was the only arm out of order.

**Goldens.** `pb3767-ambiguous-reference-sharing-a-report-entry-spelling` (COBOL1639) and its counterpart
`pb3767-qualified-reference-reaching-only-a-report-entry` (COBOL5092), both in `13.8.6.2.3-Report-Items`, each refused at
all four editions through `conformance-suite/wiseowl/diagnostics/`.

**Rows.** None claimed, none closed: a misleading diagnostic on source refused either way.

**Review round 1.** The reviewer found the count depth-blind (R1-1): with two GLOBAL `PV` items in a container and a
report entry `PV` in the contained program, 8.4.6.2.1 3) a) makes the nearer report entry the referenced item and
COBOL5092 must stand, but the first fix said COBOL1639. The order is now one method,
`DataBinder.DataItemsAmbiguousBeforeReportEntry` (ambiguity first unless a report entry of a nearer source element
exists), read by `ReportUnidentified` and `ResolveClauseOperand`; the golden is
`pb3767-local-report-entry-nearer-than-ambiguous-global-data-items`. The ambiguity text now counts the report entries of
the data items' source element (R1-2, `ReportEntriesInTierOf`). Probing the clause-operand sibling found that a contained
program's `FILE STATUS IS` a container's GLOBAL data item is refused as undefined (COBOL1639): the contained data
division binds before the globals are inherited, the PB3766 mechanism, reported as a lead.
