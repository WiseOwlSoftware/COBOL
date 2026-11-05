## Entry 3120 — 2026-11-04 16:57 PST — An existing expansion is imported only when it was made over the classes this compilation names (PB4650)

PB4650 (filed from PB4483's report): `ExpansionPlacementPass.Place`'s import arm took any expansion of the search space with the
same definition, actuals, `ExpansionKey` and surface. The key hashes a class's RECORD, not the assembly the class lives in, so an
expansion made over the library's `BOX` matched a compilation that holds a different `BOX` with the same record.

Re-probe on `630b1f2fb`: LIBX (BOX, HOLD), then CLIENT1 (HOLD-BOX over the library's BOX, written to `HOLD-BOX.dll`), then CLIENT2,
which carries an identical BOX of its own and expands HOLD over it, failed with `error CS0012: The type 'BOX' is defined in an
assembly that is not referenced ... 'LIBX'`. The sibling on the same arm: two libraries LIBB and LIBB2 holding the identical BOX,
CLIENT1 over LIBB and CLIENT4 over LIBB2, failed the same way; had it not, placement (a) would have written `HOLD-BOX.dll` over the file
CLIENT1 depends on.

First fix (round 1 of the branch review refuted it): read the expansion's `AssemblyRef` table and import it only when it names the
assembly of every named class. That is a proxy: Roslyn emits no reference for an actual or a skeleton REPOSITORY entry the expansion's
code never uses, so such an expansion became unimportable by anyone (two classes in one run unit where ISO 9.3.12 requires one, and a
false COBOL2240 on an unchanged recompile of a program under a foreign compile state), and a set test is met by a home referenced for
a different named class (the CS0012 came back when two named classes shared an assembly in only one of the compilations).

Fix, at the root: the expansion PUBLISHES the homes it was made over. `ExpansionIdentity.Homes` (attribute `ExpansionHomes`) holds the
simple name of the assembly of each resolution, then each actual, empty for a class the standard supplies. The import arm of
`ExpansionPlacementPass.Place` takes an existing same-key expansion only when its recorded homes equal the ones this compilation
computes with `HomeOf` (an imported class's assembly, the file that holds a named expansion, this compilation's output name for a class
written here), position by position. The reference cycle of DESIGN-external-repository section 10.5 step (3) is the case where the home is
this group's own output. A same-key copy over other homes reports nothing (it is neither stale nor another class); when it holds the file
placement (a) would write, the expansion is made in the group's assembly instead of over that file, and the COBOL2240 wording of
`ReportForeignState` names which reason applies. `MetadataRepositoryProvider.IsCompiledAgainst` is deleted; so is
`PublishedExpansion.AssemblyName` (a duplicate of `From.AssemblyName`). An expansion published without homes is made again, never imported.
Section 10.5 and the Residual paragraph state the rule.

Witnesses: Unit `ExternalRepositoryExpansionTests` (the library/group and library/library cases, an unused formal, an unused skeleton
resolution and a foreign compile state each imported and run, two named classes sharing an assembly in only one compilation, and the
COBOL2240 reason text), and the conformance golden `12.3.8-REPOSITORY/pb4650_unused_actual_expansion_is_one_class` (one factory count
across two groups, accepted from 2002; refused at 85 by `pb4650_unused_actual_expansion_refused_at_85`). In the reverse client order
the second client finds the first client's `BOX` (its assembly is searched too) and so imports the expansion that assembly holds.
No inventory row is claimed or closed.

