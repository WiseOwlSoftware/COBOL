## Entry 2186 — 2026-10-18 01:43 PDT — PB2849: the PB1951 table-SORT goldens now observe the capacity, and the carrier-with-ODO abort does not exist

Wave 1115 group E, the SORT and file I/O campaign (PB3353). A tests-only change; no compiler or runtime source moved.

**Re-probe.** The note's two claims, on this build. (1) The two PB1951 goldens
(`pb1951_table_sort_external_variable_element`, `pb1951_table_sort_external_pointer_dynamic_table`) never observed the
capacity of a moved element: confirmed. (2) "A carrier element holding an OCCURS DEPENDING table aborts at run time" does
not reproduce, and the shape cannot be written: 13.18.38.3 SR1 b forbids the OCCURS clause on an entry that has an
occurs-depending table subordinate to it, and the binder says so (COBOLNET0854). The shapes that can meet a depending
table were probed: a depending SUBJECT whose elements are carriers (two dynamic-length items, a dynamic-capacity table,
a fixed inner table of dynamic-length items, a dynamic-capacity pointer table; the count below the maximum), and a fixed
carrier table in a record whose trailing storage is a depending table. All sort correctly; none reached the loud in
`PlaceRenderer.VarGroupImageAt`.

**The observer.** The note asked to DISPLAY ECAP. A capacity register of a table nested under a table sits inside the
outer table (13.18.38.3 SR30) and cannot be subscripted (SR31); the compiler refuses every spelling (COBOLNET2126). The
goldens read the capacity through the element's contiguous extent instead (8.5.1.11.2; 15.50.4 rule 3). Expected values
are derived in the golden headers (lengths 9 6 5 then 6 5 9; pointer capacities 2 1 0 then 1 0 2), not copied from a run.

**Goldens.** The two PB1951 goldens print the capacities before and after. Added in `cases/14.9.40-SORT/`:
`pb2849_table_sort_carrier_odo_subject` covers the success path (count 3 of 4) and the abnormal path the owner's
2026-10-17 test requirement asks for (count 7 above integer-2 under EC-BOUND-ODO checking: the declarative runs, RESUME
NEXT STATEMENT continues after the SORT, nothing is placed), and `pb2849_table_sort_carrier_beside_odo_table`. The
introducing-edition negatives of the shapes already exist (the three `pb1951-...` refusals), so no new negative.

**Found, not fixed (another subsystem).** SEARCH and SEARCH ALL of a dynamic-capacity table nested under another table is
a COBOLNET1756 deferral that aborts at run time (`SearchBinder.cs`, "a later increment"); reported as a lead.

**Review findings (wave 1116 finisher).** R1-1: the headers of the two goldens predicted the wrong output for a capacity that
stays in its position while the items move (9 6 5 / "would print 5"); the arithmetic, 1 + the dynamic-length items +
capacity, gives LEN 7 6 7, and the headers now say so (the 9 6 5 pattern is a whole-image move that ignores the
components). R1-2: `pb2849_table_sort_carrier_beside_odo_table` ran only the success path; it now runs under
`>>TURN EC-BOUND-ODO CHECKING ON` with a USE AFTER EXCEPTION CONDITION EC-BOUND-ODO declarative and sorts E descending
with the record count 7 (above integer-2, 4): E is a sibling of the subject T, neither subordinate nor superordinate to
it, so 13.18.38.4 GR7 raises nothing and the carriers sort whole (the expected output has no RAISED line); N is put back
to 2 before T is read, since reading T under 7 would itself raise the condition.

Gate: see the wave report.

