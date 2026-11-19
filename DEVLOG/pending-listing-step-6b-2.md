## PB5932: the source listing, step 6b slice 2 - the tables keyed by no COBOL user word take keys of their own, and the procedure-names move into SymbolTable with their resolution

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 2, from
`listing-step-6b-2`: two whole classes of the progress table, each shown behaviour-neutral by the architecture oracle, and the
baseline lowered from 325 to 308.

**The tables keyed by no COBOL user word** (section 6.7.2's last row). Seven string-keyed tables whose key is no word the programmer
wrote, so they hold no name of the program, are keyed by a type that says what the key is, and `NameTableOwnershipDriftTests` rule
(1) no longer reads them as name tables: `FileModel.FdClauses` by the enum `FdClause` (the X3.23-1985 agreement-rule clauses are a
fixed set; `FileDescriptionIdentity`'s string constants are deleted); `ScreenFacility.ClauseFacts` by the generated grammar rule index
(a renamed rule now breaks the build); `Polynomial._terms` by a private `MonomialKey` ordered as the string was; `ExpansionPlacements.ByName`
by `ClrTypeName`; and the intrinsic catalog, the intrinsic argument schemas and the implementor-name registry by `StandardWord`, a word
the standard or the implementor defines, equal on the basic letters alone (`CobolKeywords`, §2023:8.1.3.2 GR3a). Three of those had
compared with the user-word fold; every key of theirs is basic letters and the fold maps no extended letter to a basic one, so every
lookup answers as before. The tests that read the schema rows as text match the new key form.

**The procedure-names.** `ProcedureNameMap<T>` is deleted: its maps are `SymbolTable`'s private `ProcedureNames<T>`, keyed by
`CobolName`, for the program, each method (keyed by its `OoMethodScope`) and each section's own paragraphs (`SymbolTable.Procedures.cs`).
`ProcedureTableBuilder` declares through `SymbolTable.DeclareParagraph` and `DeclareSection`, and every procedure-name reference (the
statements' operands, the quiet prescan, the USE FOR DEBUGGING subjects) resolves through `SymbolTable.Identify(written,
NameClass.Procedure, report, site)`, whose body is the §2023:8.4.2.2 algorithm `ResolveProcedureWord` was. The builder keeps the pc
space and the messages: a paragraph identified is its pc, whose owning section is read from `ParaSections`, and an ambiguous name's
candidates are described there, each at its source line, as before.

**Design corrections the code proved** (section 6.7.1, "Step 6b, slice 2"): `Identify` keys one word for a name rule, but a
procedure-name is a REFERENCE rule, so the table reads its head and qualifier from the context's children (`procedureName`, the
USE FOR DEBUGGING `dataReference` shapes), never its glued text; a reference's SITE (its method and containing section) is an input
the caller states (`ProcedureSite`), since the ambient cursor is the procedure binder's and the debug subjects resolve after
collection; and a name several procedures carry answers `ProcedureCandidates` in `IdentifiedName.Symbol`, which records no occurrence.
`NameClass` gains `Procedure`.

**Records that followed the move.** `QualifiedNameClasses`' qualified-procedure-name row names `SymbolTable.ProcedureNamed`; seven
version-matrix records' code-locations and notes name the moved members; `ProcedureNameUniquenessDriftTests` scans
`SymbolTable.Procedures.cs`, takes the `Paragraphs.TryAdd` form, and plants the same habit in the store among its pre-fix lines;
CONTROL_FLOW_DESIGN.md D13a, DESIGN.md section 0.5 and DOC_INDEX name the store. No user-visible behaviour changed, so the user guide
does not.

**Proof.**
- `python scripts/arch/compare_oracle.py` after each class: `=== ARCH-ORACLE: IDENTICAL — 22642 cases (baseline eac451c185f2, new
  eac451c185f2-dirty) ===` (the keyword tables) and `... new 8b75f594cac5-dirty) ===` (the procedure-names).
- `NameTableOwnershipDriftTests` named exactly the moved entries STALE after each move (7 tables and 4 sites; 2 tables, 2 lookups and
  2 sites) and no new finding; with the baseline lowered it is green, 9 of 9.
- Section 9: the `None` row of the front end and bind, twice on the branch and twice on the parent eac451c185, alternating, is
  within the parent's spread on all four programs, at most 0.07 MB above it (IX113A 69.20, 69.13 MB against 69.18, 69.22 MB;
  tests/WiseOwl.Cobol.Benchmarks/README.md, step 6b slice 2).

**What remains** (kb/Work/PB5932/2026-11-19-step-6b-slice-2.md): the data-name unit, the report class, constants, the
CONFIGURATION SECTION names, programs and functions, and the OO classes; 308 baseline entries.
