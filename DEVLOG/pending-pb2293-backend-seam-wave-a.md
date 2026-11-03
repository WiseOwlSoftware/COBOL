## The backend seam, wave 20261847 group A: the repository records are model data written at bind time (PB4319), and no parse context rides a unit or the compilation (PB2398)

**PB4319 — the external repository's records cross the seam as data.** The re-probe on this tree matched the note: the
architecture walk measured the tolerated row `WiseOwl.Cobol.CodeGen → WiseOwl.Cobol.Binding.Repository` at 26 type pairs
(`HostEntryEmitter`, `OoEmitter` and `RepositoryAttributeRenderer` named the record family and called `RepositoryCodec.Of`
and `RepositoryCodec.KindOf` at emit time). The fix moves the encode, not only the types:

- `Binding/Repository/RepositoryRecord.cs` is `Binding/Model/RepositoryRecord.cs` (namespace `WiseOwl.Cobol.Binding.Model`),
  every type unchanged; the codec's two constants are the records' own vocabulary and moved with them
  (`RepositoryRecord.CobolEntryConvention`, `OccursRecord.OutsideTheRecord`).
- `RepositoryPublication` (new, `Binding/Repository`) holds the two publication decisions: WHICH units publish
  (`EntryUnitsOf`, moved from `HostEntryEmitter.EntryUnits`, the Annex C pairing of kb/Work PB3872) and, under
  `--repository-update on`, each published unit's record, written by the same `RepositoryCodec.Of` over the finished bound
  data at the end of `BinderDriver.Bind`. The bound compilation carries the results: `BoundUnit.Record`,
  `OoClassUnit.Record`, `BoundCompilation.InterfaceRecords`, `EntryUnits` and `PublishesSignatures`, the one fact the
  module's `HasSignatures` and each skeleton's `[CobolSkeleton]` read. `RepositoryCodec.KindOf` is `BoundUnit.RepositoryKind`;
  `RepositoryAttributeRenderer.EmittedHere` is `ExpansionPlacements.EmittedHere`, so the binder publishes a record for exactly
  the classes and interfaces the backend emits.
- The option rides `EditionContext.RepositoryUpdate`, as `--sign-encoding` and `--numeric-display` do;
  `BackendOptions.RepositoryUpdate` and the `repositoryUpdate` parameters of `EmitBound`, `ProgramEmitter.Emit`, `OoEmitter`
  and `HostEntryEmitter` are deleted (no forwarder). A check-only compile writes no module, so the driver binds it with the
  option off and it encodes nothing.
- The record members are now on the backend input, so `BackendContract.cs` classifies them (`PicRecord.SignKind` joins
  `PicInfo.SignKind` on PB3252's L9 row).

Neutrality: `python scripts/arch/compare_oracle.py` = `=== ARCH-ORACLE: IDENTICAL — 16435 cases (baseline 0fa5f7201acd) ===`.
The ratchet emptied the row's 26 walked pairs; the landing's census deletes the census-only row (DESIGN-architecture-review
§8.1 "The landing of a remover"). New witness: `RepositoryCodecTests.TheBoundCompilation_CarriesEachPublishedUnitsRecord_OnlyWhenItUpdatesTheRepository`
(every entry unit, class and interface carries exactly the codec's record under the option, none without it). Docs:
DESIGN-codegen-backend §2.2, DESIGN-backend-abstraction §1.3 L6 and §2, DESIGN-external-repository §6.1 and its slice rows,
DESIGN-architecture-review §8.1.

**PB2398 — no parse context crosses the seam on a unit or the compilation.** The re-probe found less than the note named: the
walk measured three pairs, `BoundCompilation → CobolParserCore` (`Tree`, which nothing outside the binder read),
`BoundUnit → CobolParserCore` (`Ctx`, read by `BinderDriver` eight times and `FlagConformancePass` once) and
`CSharpEmitter → CobolParserCore` (the `CSharpEmitter.Bind` facade, "a thin shim over BinderDriver.Bind"); BoundOo.cs's half
was already gone (an unused `using` and alias remained, deleted) and `ProgramEmitter`/`StatementEmitter` read no parse
context. `BoundCompilation.Tree` is deleted; `BoundUnit.Ctx` is the binder-side `SourceMap` on `BindSession` (`Of`, and
`NameOf`, which replaced `BinderDriver.NameCtx`), built by `CollectUnits` as each unit is made; the facade is deleted and the
23 unit-test fixtures that bound through it bind with `BinderDriver.Bind` (rule 4: no shim). The facade's deletion also cut
`CSharpEmitter → BinderDriver`, `→ EditionContext` (PB2293's `CodeGen → Binding` row) and `→ DirectiveResults` (all of
PB2408's row). Oracle: IDENTICAL, 16435 cases. The census-only `Binding.Bound → Frontend.Parsing` pair left PB2398's
Removes line (its source record, the R1 targets file), as PB2417's finisher asked.
