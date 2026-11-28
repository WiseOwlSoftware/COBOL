## Entry 4091 — 2026-11-27 22:42 PST — The data map prints a table of dynamic capacity as OCCURS DYNAMIC, with the phrases it writes

PB6226. The listing's data map printed `05 T OCCURS DYNAMIC CAPACITY IN C FROM 5 TO 100` as `OCCURS 5 TO 0` and a bare `OCCURS DYNAMIC` as
`OCCURS 0`: `OccursSpec` carries `Max = 0` for a Format 4 table (it has no allocated maximum), `SymbolTableFinish.DataAttributesOf` copied
`Min`/`Max` only, and `DataMapText.DetailParts` printed any item with a maximum as a fixed or ODO range. The JSON reader had to know the 0
sentinel, and the expected capacity (integer-5, ISO section 2023:13.18.38.4 GR17) was lost.

Re-probe on this build before the fix is the note's scenario (min 5, max 0 on the model); after it, `cobol dyn.cob --listing-sections data-map`
prints `OCCURS DYNAMIC FROM 5 TO 100` and `OCCURS DYNAMIC`, in the text, HTML and JSON (`dynamicCapacity: { from, to, initialized }`).

Fix shape: `DataAttributes` and `ListingDataItem` gain one nullable `DynamicCapacity` (`DynamicCapacityAttributes` / `ListingDynamicCapacity`: FROM
integer-4, TO integer-5, INITIALIZED, each as written; an absent FROM stays null, GR16 assumes zero). A dynamic table's `OccursMin` and `OccursMax`
are now null, so the 0 sentinel is gone and a table of no occurrences cannot be told from one of dynamic capacity by a magic value. One reader,
`DataMapText.DetailParts`, serves the text and HTML listings. The JSON schema was regenerated (`WISEOWL_COBOL_UPDATE_LISTING_SCHEMA=1`); the schema
version stays 1.0 because the listing JSON has never shipped (no release tag holds `listing-v1.schema.json`), as every earlier added property did.
DESIGN-listing.md section 6.6 names the new fact.

Goldens: `ListingDataMapTests.ATableOfDynamicCapacity_IsDataMapped_AsDynamic_WithThePhrasesItWrites` (bare, FROM, TO, all three phrases, plus a fixed
and an ODO table whose rows keep their forms) and a dynamic item in `JsonListingWriterTests`' round trip. Sweep: the only readers of
`OccursMin`/`OccursMax` are `DetailParts` and the JSON model. No inventory row closes.

