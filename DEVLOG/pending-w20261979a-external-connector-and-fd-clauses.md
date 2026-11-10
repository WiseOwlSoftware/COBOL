## The run unit's external file connector rules at every edition, and the obsolete COBOL-85 file description clauses' operands

Wave 20261979 group A finished the binder-driver group: it replayed three unlanded branches onto main (wave 20261973 B: PB5074,
PB5072, PB5087, part of PB5084; E: PB5076, PB5077; D: PB5078, PB5079, PB5090), answered their three review findings, and fixed
PB5069 and PB5070. The replay's only conflicts were append-only: two kb/Work notes, the diagnostic catalog, the code list of
the documented facts, and docs/CONFORMANCE.md, where main's new 3.4 (MULTIPLE FILE TAPE) made PB5076/PB5077's determination 3.5.

**Review findings.** PB5076 R1-1: `BinderDriver.ContainerUsesOf` counted every declarative of a containing program as
"applicable" to a contained program's READ; §1985:X.5.5.4 GR1 b) selects outside the statement's own program only "The
declarative in which the GLOBAL phrase is specified", as the run time's outward walk does, so it now yields GLOBAL ones only (the
unit test that pinned the wrong reading is replaced by two theories). PB5079 R1-1: the SOURCE control-data rule
(§1985:XIII.3.20.3 SR5 c) skipped every table element, because `StorageExtent.OfItem` has no one extent under OCCURS. A new
`StorageExtent.OfEveryOccurrence` gives the occurrence whose subscripts are all 1 and each OCCURS level's stride and count, with an
exact, pruned overlap test, on the record layout's own walk (`RecordLayout.OffsetInRootWithEveryIndexOne`). R1-2: the
SOURCE rule's private RENAMES span computation is gone; `StorageExtent.OfItem` already places a level-66 alias where the layout
does. Goldens `pb5079-source-table-redefines-control` (refused 85), its legal side, and `pb5079-source-table-disjoint-legal`.

**PB5069.** Re-probe: a PADDING CHARACTER item that is not external, and two programs whose external relative file differs in
access mode and names a non-external RELATIVE KEY, all compiled at `--std 85` (the access mode at 2023 too). The compiler compared
only FILE STATUS and RELATIVE KEY, and only at 2023. `FileEntryIdentity.Segments` is now the one list of what a file control entry
specifies, one segment per GR1 item with PADDING CHARACTER added; `ExternalFileConnectorRules.Check` compares it for the entries of
one compilation group at every edition (COBOL6847), and the run time's EC-EXTERNAL-FILE-MISMATCH compares the same segments joined.
RELATIVE KEY must be the same external item at every edition (§1985:VIII.2.4.4 GR8), PADDING CHARACTER's data-name an external item
at 85 and 2002 (§1985:VII.2.7.4 GR6). `ExternalItemIdentity` moved onto `DataItem` (part of PB2418), so the emitter no longer calls
the binder driver. The three run-time EC-EXTERNAL-FILE-MISMATCH goldens now build each called program as its own unit
(wiseowl/cases); the one golden that asserted a non-external RELATIVE KEY compiles at 2014 is a refusal there now.

**PB5070.** Re-probe: `VALUE OF "F1DATA"`, `LABEL RECORDS ARE IGNORED`, a two-character padding literal or item, an index item or
File Section item in VALUE OF, and a DATA RECORD naming an 02 item all compiled at `--std 85`. The grammar is now the printed
format (LABEL takes STANDARD or OMITTED; VALUE OF is a list of implementor-name IS operand pairs), and the syntax rules are
COBOL6848 (PADDING CHARACTER) and COBOL6849 (DATA RECORDS, VALUE OF). The NIST X-cards XXXXX075-079, the VALUE OF object, became
literals: as data-names they would name nothing the programs describe.

**Not done.** PB5084's remainder (the SORT, MERGE and PERFORM range rules of segmentation and the independent segment's initial
state) has no diagnostic code left in this wave's allocation and its second half needs an owner decision; the file DESCRIPTION
entry's own run-unit agreement rules (LABEL RECORDS, BLOCK CONTAINS, CODE-SET, RECORD, VALUE OF) need an FD identity beside
`FileEntryIdentity`.
