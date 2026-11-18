## PB5925: the source listing, step 6 - the finished symbol table, the data map and file map, the program attributes and the margin

Step 6 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925; the owner's order of 2026-11-18 to build the full
listing, and his direction that the listing is built from the symbol table plus the source). `--listing-sections data-map`
(and `all`) now adds a DATA MAP section: every data item, condition-name, index-name, constant and file of every program and
class half, in the order written, with its level, offset, length, bytes, usage, picture and the .NET field and type it
became; the source section prints each definition line's offset, length and usage in the margin from column 100; and the
program map gives each bound program's COMMON, INITIAL, RECURSIVE and PROTOTYPE attributes, its externalized name and its .NET
type.

**What was built.**
- The finished symbol table: `WiseOwl.Cobol.Symbols` (a layer of its own, `compiler.symbols`, which depends on nothing):
  `SymbolTableSnapshot`, `SymbolEntry`, `NameClass`, `SymbolPosition` and one attributes record per class. The binder makes
  it once the data binding is frozen (`SymbolTableFinish.Of` and `SymbolTableWriter` in the binder layer, run by
  `BinderDriver.Bind` only when `EditionContext.RecordsSymbols`, which the driver sets only for a listing whose sections read
  it, `ListingFacts.ReadsSymbols`). `Condition88.DeclaredAt` and `BoundUnit.DeclaredAt` record where a condition-name and a unit
  are declared; `ConstantEntryBinder.DeclaredConstants` gives the constant entries, which make no data item.
- The listing: `ListingDataMap` (`ListingDataItem`, `ListingFileEntry`), `ListingMargin`, `ListingProgramAttributes`,
  `ListingSection.DataMap`; `DataMapBuilder` (the data map and the program attributes, each placed through the one line
  index, so a name's program is the innermost program of its line and no name is compared), `LineAnnotationBuilder` (the
  margin, made once and stored on the line), `DataMapText`, `ProgramMapText.AttributesText`, `TextListingRenderer.WithMargin`.
  The schema is regenerated.

**Design corrections** (each marked "(step 6)" in the design): the attributes are read off each symbol when the table is
finished rather than written beside it, which would store every fact twice; the one spelling of a field's .NET type already
existed (`StorageFormPass.StorageElementType`, made internal); the finishing lives in the binder layer, because
`compiler.model` is backend input and may name neither the binder's constant table nor `StorageFormPass` (a first build that
put `SymbolTable.Finish` there was refused by `ArchitectureEdgesDriftTests` and `BackendContractDriftTests`). kb/Work PB5932
splits the one store of names (every name table private to `SymbolTable`, `Identify` the only lookup,
`NameTableOwnershipDriftTests`) into step 6b, landed before step 7: it is a restructuring of the binder's whole name
resolution, which the data map does not stand on and step 7's occurrences do.

**Tests.** `ListingDataMapTests` (Conformance) derive every expected size and offset from the PICTURE character-strings and
the rules they cite (§2023:13.18.40.4 GR14, §2023:13.18.40.3 SR6, §2023:13.18.38.4 GR4, §2023:13.18.44.4 GR1,
§2023:13.18.45.4 GR2, each checked with `cite.py --check`): every name in the order written, the links between entries, the
file map, the program attributes, the margin equal to the data map's facts, a class's OBJECT data, a bind error's
`NotProducible`, and no data map when none is asked. `ListingOffDoesNoWorkDriftTests` holds the `RecordsSymbols` gate. Seven
mutations each turned their test red and were reverted (conditions dropped, the first-occurrence offset lost, margins not
marked, the table finished whatever the request, INITIAL read as COMMON, the class halves not walked, the section dropped
from the text order).

**Measured** (`CompileBenchmarks.ListingMode.All`, the README's step-6 table, with the parent commit run beside it): every
section in both formats is +11.1 to +20.2 % over no listing on a full compile (budget 25 %); no-listing rows within 0.6 MB of
the parent's. The first run finished every declared name for the standard listing, which reads only the program entries, and
put SM201A's standard text listing at +12.6 %; `EditionContext.RecordsNames` now gates the names to the data map. SM201A's
standard row still reads +11.2 % on the second run from a no-listing full compile 2.07 MB under the parent's for the same code
(+5.2 % against the parent's), filed as a lead on the measurement.
