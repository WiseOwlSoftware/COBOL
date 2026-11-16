## Entry 3748 — 2026-11-16 04:00 PST — PB5777: UNSTRING with no DELIMITED BY examines an OCCURS DEPENDING ON group receiver at its current size

**Mechanism.** `ReceivingStore.CharacterPositions` answered every group receiver with its static maximum width, so
`UNSTRING S INTO OH R1 WITH POINTER P` (OH holds `OCCURS 1 TO 5 DEPENDING ON NO2`, NO2 = 2, object outside the group)
examined five characters for OH and took the wrong characters for R1 (`R1=[FGH] P=09`). `§2023:14.9.48.4 GR11b` and
`§1985:VI.6.27.4 GR13b` make the examined size the size of the current receiving area, and `§2023:13.18.38.4 GR8` /
`§1985:VI.5.8.4 GR3a` give an occurs-depending group only the part its object's value names.

**Re-probe.** Before the fix the note's program printed `R1=[FGH] P=09`; after it, at `--std 85` and `--std 2023`, the
same program prints `OH=[AB] R1=[CDE] P=06` (run_probe, runs m5uns-001 and m5uns-002).

**Fix.** `CharacterPositions` has a new arm ahead of the group arm: an `OdoGroupPlace` for which
`PlaceRenderer.UsesCurrentExtent(odo, Receiving)` holds is sized by `PlaceRenderer.CharLengthExpr`, the same expression
the receiving store slices at, so the size a statement examines and the size it stores cannot disagree. A group whose
object lies inside it keeps the maximum (GR8b). ACCEPT's device window reads the same function, so it takes the same
current extent.

**Golden.** `conformance-suite/cases/14.9.48-UNSTRING/pb5777_unstring_odo_group_receiver_current_extent` (accept 85, expected
value computed from the rules above). No refused-below program: the rule exists in the oldest edition.

**Rows.** `GR-14.9.48.4-11` re-recorded PARTIAL to CONFORMS (GAP 10 to 9); the 1985 row is kept by its bridge.

Review round 1 (R1-1): the arm sized every occurs-depending group in storage-image bytes, so a GROUP-USAGE NATIONAL group got its byte extent. It now reads `PlaceRenderer.CurrentPositionsExpr`, the one reader that picks character, national or bit positions by channel. Golden `pb5777_unstring_odo_national_group_receiver_current_extent` (2002) pins NG=[AB] R1=[CDE] P=06.

