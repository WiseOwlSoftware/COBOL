## Entry 3740 — 2026-11-16 01:43 PST — PB5801: a level 66 entry named by a file or report clause has a place, and the compiler no longer crashes on it

**The defect (BLOCKER, every edition).** A level 66 entry renaming one item, named as FILE STATUS, a LINAGE operand, RECORD
VARYING DEPENDING ON, RELATIVE KEY or a report CONTROL operand, ended the compiler with an unhandled
`InvalidOperationException` ("no Place was built for data item") during C# emission. Each use is legal source: a 66 entry
has the attributes of what it renames (§1985:VI.5.11.4 GR2, §2002:13.16.43.3 GR1, §2023:13.18.45.4 GR1). The re-probe on
this branch reproduced all five, at `--std 85` and `--std 2023`.

**The root and its fix.** The place table's item set, `DataBinder.PlaceableItems`, walked the storage tree, and a 66
entry rides its owner's `Renames66`, never `Children`. The one item-to-place builder already places a 66 entry, so the fix
is the walk: `DataItem.SelfDescendantsAndAliasesOf` (every item a data-name can reference, each item followed by its 66
entries), which `PlaceableItems` now walks. Every by-item reader (FILE STATUS, RELATIVE KEY, LINAGE, DEPENDING ON, ASSIGN
USING, CONTROL, the SELECT WHEN condition parent, GLOBAL file operands in a contained program) asks the one table, so no
call site changed.

**A sibling, fixed, and its review (R1-1).** The 1985 CONTROL witness then hit §1985:XIII.3.20.3 SR5's USE half, which asked `Place.Item` (attributes and storage), so `DISPLAY RX` of the control RX in its own footing's USE procedure was refused as a reference to the item RX renames. The first answer asked `Place.DenotedItem`, which is null for a reference-modified slice, so the branch review found `DISPLAY WS-KEY (1:1)` in a page heading's USE procedure and `DISPLAY WS-REC (1:2)` in a footing's compiling while the SOURCE half refused the same operands. The question is a third one beside those two: which data-name the reference is written with, under any decoration. `Place.NamedItem` answers it (the recorded level-66 alias at any layer, else the item under the decorations), and every reader that asks it now asks that one accessor: SR5's USE half, INITIALIZE's RENAMES screen (which carried its own decorator unwrap), the USE FOR DEBUGGING reference match and its store match (`BoundStores.DirectStoreOf`, whose visitor now matches on a place), and the DEBUG-ITEM reference check. `PlaceNamedItemDriftTests` pins the answer and refuses a second hand-written one. A debugging section on a 66 alias, which never ran before (references were matched on the renamed item), now runs on a statement that changes the alias by name.

**Sweep.** Probed at 85, 2002 and 2023: LINAGE FOOTING/TOP/BOTTOM, OCCURS DEPENDING ON, RECORD KEY and ALTERNATE RECORD
KEY with READ and START KEY, THROUGH aliases as RECORD KEY and FILE STATUS, PADDING CHARACTER, CONTROLS ARE CX RX, SOURCE,
SUM, ACCEPT, DISPLAY, SEARCH VARYING, CALL USING, a SORT key, ASSIGN USING and a GLOBAL file's FILE STATUS: each compiles
and runs, or is refused with its edition diagnostic; none throws.

**Witnesses.** Fourteen goldens: two 1985 refusals (`14.9.49-USE/pb5801-use-refmod-control-in-page-heading`, `pb5801-use-refmod-group-of-control-in-footing`, COBOL6825), the legal alias control in its own footing (`pb5801_use_alias_control_in_own_footing`), the debugging target (`XV.3.2@1985/pb5801_debug_level66_target`), and ten more, each value derived from the cited rules: five fixed-form at 85 (`VII.2.5@1985`,
`VII.3.7@1985`, `VII.3.8@1985`, `VIII.2.4@1985`, `XIII.3.7@1985`) and five at 2002/2014/2023 (`12.4.5.8`, `13.18.34`,
`13.18.43`, `12.4.5.13`, `13.18.16`; the FILE STATUS one through a THROUGH alias, the CONTROL one as `CONTROLS ARE
WS-KEY RX`, which §2023:13.18.16.3 SR6 permits). `RenamesEntryPlaceDriftTests` pins the invariant (every referenceable
item, 66 entries included, has a table entry) and each by-item clause operand's place; it fails on the old walk (checked).

**Rows.** The five 2023 rows were stored CONFORMS while the crash stood; they now are, with the new witnesses (GAP 10 to
10). The 1985 rows gained their 85 witnesses (GAP 102 to 102). `1985-SR-XIII.3.7.3-2` keeps an open reading (are a 66
entry and its renamed item two data items?) and gained none.

**Lead.** No top-level guard turns an unhandled exception in a compiler pass into an internal-error diagnostic with a
source position: the .NET runtime prints its own stack (reported, not added here).

