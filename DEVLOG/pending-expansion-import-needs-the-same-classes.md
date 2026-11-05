## An existing expansion is imported only when it is compiled against the classes this compilation names (PB4650)

PB4650 (filed from PB4483's report): `ExpansionPlacementPass.Place`'s import arm took any expansion of the search space with the
same definition, actuals, `ExpansionKey` and surface. The key hashes a class's RECORD, not the assembly the class lives in, so an
expansion made over the library's `BOX` matched a compilation that holds a different `BOX` with the same record.

Re-probe on `630b1f2fb`: LIBX (BOX, HOLD), then CLIENT1 (HOLD-BOX over the library's BOX, written to `HOLD-BOX.dll`), then CLIENT2,
which carries an identical BOX of its own and expands HOLD over it, failed with `error CS0012: The type 'BOX' is defined in an
assembly that is not referenced ... 'LIBX'`. The sibling, found on the way and the same arm: two libraries LIBB and LIBB2 holding the
identical BOX, CLIENT1 over LIBB and CLIENT4 over LIBB2, failed the same way; had it not, placement (a) would have written
`HOLD-BOX.dll` over the file CLIENT1 depends on. The reverse order (CLIENT2 first, which puts HOLD-BOX in `CLIENT2.dll`, then CLIENT1)
imported `CLIENT2.dll`'s expansion, typed with CLIENT2's BOX.

Fix, at the import decision: an existing expansion serves a compilation only if its assembly names (AssemblyRef) the assembly of every
actual and resolution the expansion names (`MetadataRepositoryProvider.IsCompiledAgainst`). `ExpansionPlacementPass.Place` computes
each named class's home with `HomeOf`: the assembly an imported class came from (`ImportedFrom` now carries the assembly simple name,
taken from the entry; `PublishedExpansion` lost its duplicate `AssemblyName`), the file that holds a named expansion, or this
compilation's output for a class written here (the output name is `MetadataRepositoryProvider.OutputAssemblyName`). The reference
cycle of DESIGN-external-repository section 10.5 step (3) is the case where the home is this group's own previous build, and still
imports. A same-key copy compiled against other assemblies is neither stale nor another class, so it reports nothing; when it holds
the file placement (a) would write, the expansion is made in the group's assembly (placement (b)) instead of over it, and the
COBOL2240 wording of `ReportForeignState` names which reason applies. DESIGN-external-repository section 10.5 states the rule.

Witnesses (Unit, `ExternalRepositoryExpansionTests`): `AnExpansionCompiledAgainstALibrarysClass_IsNotImported_ByAGroupThatWritesThatClassItself`
(both client orders, each client run: BOX 042 and BOX 007) and `AnExpansionCompiledAgainstAnotherAssemblysClass_IsNotImported_NorWrittenOver`
(the file's timestamp and key unchanged). All three cases were red with `IsCompiledAgainst` forced true. No conformance-suite
golden: the behaviour is the outcome of separate compilations sharing a directory, as for PB4483. No inventory row is claimed or closed.
