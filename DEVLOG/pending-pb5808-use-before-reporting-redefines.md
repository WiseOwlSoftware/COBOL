## A USE BEFORE REPORTING procedure can no longer change a control data item through a REDEFINES or RENAMES of it

kb/Work PB5808. `MOVE "22" TO WS-RED`, where WS-RED REDEFINES the record that holds control item WS-GRP, compiled at every
edition and changed the control item (the run printed `GRP=2`), while `MOVE 2 TO WS-GRP` was refused COBOL2378. The store
relation (`ReportWriterBinder.SameStorageLine`) walked the `Parent` chain only. Re-probe on this build before the fix:
exit 0, `GRP=2`; after, at `--std 85`, exit 65 with COBOL2378.

Fix: `SameStorageLine` also asks the layout's `StorageExtent` overlap, the relation the 1985 SOURCE rule
(`ReportSectionBinder.ControlDataReferenceOf`) already asks, so a REDEFINES or RENAMES that shares a byte with the control
item is refused (a table counts every occurrence; an unprovable extent is not asked). The message now cites the edition's own
rule (§1985:XIII.4.9.3 SR4 with "must", §2002:14.8.45.2 SR11, §2014:14.9.45.2 SR11, §2023:14.9.49.3 SR11), each checked with
`cite.py --check`.

Witnesses: conformance case `14.9.49-USE/pb5808-control-item-redefines-altered` refused at 85, 2002, 2014 and 2023; unit
tests for a REDEFINES of the record and a RENAMES at every edition, and the accepted control (a redefinition sharing no byte).
With the overlap line disabled six unit cases go red; restored, 39 of 39 pass. Row `SR-14.9.49.3-11` PARTIAL to CONFORMS.
`1985-SR-XIII.4.9.3-4` keeps its stored PARTIAL read until the code-read campaign reads this tree again.

Review round 1 (R1-1): the overlap was asked of the stored ITEM, so a reference-modified or literal-subscripted store into a
REDEFINES that touches no byte of the control item was refused, legal source that compiled before. `BoundStores.StoresInto` now
hands the checker the Place, and `StorageExtent.OfStore` gives the bytes the store reaches (a literal occurrence of a table, a
literal reference-modification window, a Tier-B view's constant window); only an extent that is a run-time quantity falls back to
the whole item. Unit cases: a window or occurrence over the non-control byte is accepted, over a control byte or by variable
subscript refused.

Round 2 review (R2-1): StorageExtent.OfStore placed a Tier-B view window by its class-relative offset in the 01 record's coordinates, so a REDEFINES below level 01 whose original is not at record offset 0 was mis-placed (an overlay of the control item accepted, a disjoint store refused). The window is now placed by the view item's record position (OfEveryOccurrence) plus the offset past its first occurrence in its class (OfViewWindow). Witness: DeclarativesStructuralRulesTests.AStoreThroughANestedRedefines_IsPlacedInTheRecordNotTheClass, both ways.
