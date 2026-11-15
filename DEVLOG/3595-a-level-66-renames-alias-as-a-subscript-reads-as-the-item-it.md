## Entry 3595 — 2026-11-14 03:59 PST — A level-66 RENAMES alias as a subscript reads as the item it renames (PB5477)

`66 RN RENAMES GA` (GA PIC 9) used as `ELT (RN)` produced C# naming the alias as a member of its record (`GR.RN`) plus an undeclared profile, so the backend refused legal source (CS1061, CS0103). The re-probe on this build reproduced it exactly. Everywhere else the alias already resolved to the renamed item through the one item-to-place builder; only the subscript and reference-modification position reader, `SubscriptBinder.ResolveSubscriptName`, built its own field read from the alias item.

Fix: a no-THROUGH alias is replaced by the new `DataItem.AttributeItem` (the renamed item; `RenamesPlaceBuilderDriftTests` forbids the resolver from reading the descriptor itself, so the reading lives in the model) before the class screen and the position read (§1985:VI.5.11.4 GR2, §2023:13.18.45.4 GR1: all of data-name-2's attributes and its storage), and `PositionRead` declines any level-66 item (`DataItem.OwnsNoField`) so a THROUGH span reroutes to the D18 place route instead of naming a field it does not own. Probes: `ELT (RN)`, `ELT (RN - 1)` and `TBL (RN:2)` print the spec values; a THROUGH alias under `--permissive` no longer fails in the backend.

Witness: `1985-SR-IV_4_3_8_2_3-5-renames-alias` (accept 85; no edition below 1985 exists, so no negative). The row `1985-SR-IV.4.3.8.2.3-5` is re-verdicted PARTIAL to CONFORMS (record_verdicts, GAP 229 to 228).

