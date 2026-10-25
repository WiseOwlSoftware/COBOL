## Entry 2496 — 2026-10-24 18:22 PDT — PB3670, the owner's top priority: partition on the members a group changes, not on whole files and subsystems

The owner asked why two groups cannot change separate regions of one source file, "Developers do this all the time".
The operator found the planner stricter than that in two ways. Groups meet on any shared file, and a group whose notes
declare no site block reserves its whole subsystem. The loop's fix-lane wave w20261032 recorded group A (seven
SYNCHRONIZED and SIGN notes in `DataBinder.cs`) with all 234 files of `Binding/`, and group B (PB547) with 222 files. So
the owner-approved fuzz fixes PB3664 (`CobolEdit.cs`) and PB3665 (`StringUnstringBinder.cs`) waited behind work that
never touches their code.

The owner made the fix "top priority over all else". PB3670 (PB1957, tier 1) asks for five changes. Groups meet only on
an overlapping MEMBER, from the member index's line ranges. A note with no site block reserves nothing and has its sites
located at plan time. Only a moved or re-signed member reserves its callers. The landing lane dispatches the conflict
finisher the moment a merge conflicts. The effect is measured over a day of waves. PB3640 had already shown that no
partition removes conflicts. The operator dispatches PB3670 by hand beside the PB1957 groups that hold its files.

