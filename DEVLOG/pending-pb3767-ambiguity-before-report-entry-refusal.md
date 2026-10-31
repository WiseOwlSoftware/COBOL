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
