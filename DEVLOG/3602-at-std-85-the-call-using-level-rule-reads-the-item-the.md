## Entry 3602 — 2026-11-14 06:11 PST — At --std 85 the CALL USING level rule reads the item the reference denotes (PB5383)

The call-using-group-any-level-2002 gate (VersionConformancePass, kb/Work PB4305) asked `arg.Place.Item.IsGroup` and the item's level. That item is the storage and attributes half of a place, and two shapes read it wrong. A reference-modified operand `WG(1:2)` reported its base group at level 05 and was refused COBOL0900, although §1985:IV.4.3.8.4.4 GR5 makes it an elementary item (rejected legal source). A `66 W66G RENAMES WA THRU WB` item carries no children in the model, so it was no group and was accepted, although §1985:VI.5.11.4 GR1 makes it a group item at level 66 (under-rejection).

Re-probe on this build before the fix: `v01.cob` refused lines 15 (the non-THROUGH alias, by luck, through the renamed group's level 05) and 17 (the reference-modified group) and accepted line 14 (THROUGH alias). After: line 14 and 15 refused as groups at level 66, line 17 accepted and prints `IN SUBP [CD]`.

Fix shape: a new `Place.DenotesGroupItem` answers "is the denoted item a group" once (null `DenotedItem`, the reference-modified case, is no group; a THROUGH alias, `RenamesPlace`, is one by §2023:13.18.45.4 GR2; otherwise the described item's `IsGroup`), and the gate reads the level from `DenotedItem`, the identity half. Sweep: the other `Place.Item.IsGroup` readers are in intrinsic result typing, code generation and MOVE, none an edition gate; `MoveClassifier.IsGroupPlace` is the same question under the MOVE rule's plain-group test and was left.

Goldens: `1985-SR-X_5_2_3-3-refmod` (accepted at 85, `.out` computed from the rule) and `1985-SR-X_5_2_3-3-renames` (refused at 85, COBOL0900). Row 1985-SR-X.5.2.3-3 re-verdicted PARTIAL to CONFORMS (GAP 207 to 206) through `record_verdicts --edition 1985`; `closes_rows` stays empty because `work.py check` refuses 1985 ids there (PB5094).

