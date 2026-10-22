## Entry 2350 — 2026-10-22 11:12 PDT — PB3510: an OCCURS DEPENDING table records only an admitted data-name-1, so DEPENDING ON its own record no longer overflows the binder's stack

The second overnight fuzz run's fz01530 (kb/Work PB3510) wrote `OCCURS 1 TO 5 DEPENDING ON` the group that holds the
table. WiseOwl died with "Stack overflow." and no diagnostic; GnuCOBOL refused it. Re-probed on this branch's build:
the note's reduced program and the fz01530 shape both crashed, and so did two shapes the note did not list:
DEPENDING ON an intermediate group above the table, and two records whose tables each depend on the OTHER record.
That last shape breaks only ISO §13.18.38.3 SR17 ("Data-name-1 shall describe an integer"), so refusing SR20
violations alone would not have fixed it.

**Mechanism.** `DataBinder.OdoResolve` recorded `OccursSpec.Depending` before it checked the rules. A group
data-name-1 therefore reached `ReferenceResolver.WrapIfOdoGroup`, the §13.18.38.4 GR8 group-operand slice. That slice
resolves data-name-1's own place, and resolving a group that holds an occurs-depending table wraps the group again,
with no end. A second fault hid the SR20 diagnostic: `DependingInsideTableRange` searched the record's elementary items
for data-name-1 itself, so a group data-name-1 was never found and never failed. DEPENDING ON the table's own entry also
never drew SR2 ("shall not be subscripted"), because the check asked only whether data-name-1's PARENT lies in a table.

**Fix.** `OdoResolve` now computes three predicates once: not an integer (SR17), subscripted (SR2, now
`dep.SubscriptArity`, which counts the table's own entry), and inside the table's range (SR20). It records the link
only when none of them holds, and each rule reports from its predicate. An integer is an elementary item, so a recorded
link never leads back into a group, and every `WrapIfOdoGroup → ResolveItem` chain ends after one step. SR20 now
compares data-name-1's LAST elementary item with the table's first. A refused link leaves `Depending` null, the same
state an undefined data-name-1 (COBOLNET0851) leaves, and the procedure division adds no diagnostic of its own. The
sibling sweep covered every `OccursSpec.Depending` consumer: the extent freeze in `SendingValueTemp` copies an admitted
counter. The report-writer and RECORD VARYING `DEPENDING` arms check their own item's integer rule and resolve it
directly, so they reach only admitted links. `NumericRenderer` names `WrapIfOdoGroup` only in a comment.

**Goldens.** Four refusals in `conformance-suite/cases/13.18.38-OCCURS/`, each refused at 85 through 2023:
`pb3510-depending-on-containing-record` (the note's program; COBOLNET0852 and 0857), `-on-group-with-picture`
(fz01530; COBOLNET2191, 0852 and 0857), `-on-each-others-record` (0852 twice) and `-on-the-table-itself` (0853 and 0857).
One positive, `pb3510_depending_counters_admitted` (accepted at 85), checks that GR8 a) and b) still slice by an
admitted counter. The drift test `OdoDependingAdmissionDriftTests` pins the two code sites that write
`OccursSpec.Depending`. It also binds nine shapes and asserts that every recorded link is an elementary integer outside
its table and that exactly the expected codes are reported. With the admission reverted, the test host dies of the
same stack overflow.

**Rows.** The note claimed no inventory row. The SR17, SR2 and SR20 rows were already OK, and the new goldens become
their witnesses. No diagnostic code was allocated (COBOLNET4730 returned).

**Gate.** `LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261022T174638Z-9d0d64; Conformance 10,742, Unit 34,440
and Characterization 5 cases ran, every new refusal and positive and all ten drift-test cases among them).

