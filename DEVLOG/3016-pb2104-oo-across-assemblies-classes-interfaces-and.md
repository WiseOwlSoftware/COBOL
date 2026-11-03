## Entry 3016 — 2026-11-02 20:22 PST — PB2104: OO across assemblies — classes, interfaces and properties are imported by identity; parameterized classes and interfaces are published as skeletons and expanded across assemblies (external repository slice 8)

**What was wrong.** A REPOSITORY `CLASS` specifier could name only a class the compilation group defined: a class compiled in
another group (an assembly of the §8.13 external repository) could never be named, an unreferenced specifier no source answered
compiled clean (ISO §12.3.8.3 SR6), and the run-time description of an object reference spelled the importing element's WORD
for its class, so two groups that named one class by two words could not conform (§14.8.2.3.2 rule 3, "the same
object-class-name"). Re-probed on this build before the work: `CLASS ACCT AS "Ledger"` against `Ledger` in `lib.dll` was
COBOL0813 at the first reference and silent when unreferenced.

**The shape (DESIGN-external-repository 6.4, 8.4, 8.7, 10.1, 10.2, 14).**
- `ObjectRefDescriptor` carries the resolved `Identity` (the externalized name) and `ClrName` (null for a group class, the full
  `global::` name for an imported one), both functions of `Name` over the one class table and so excluded from equality;
  `DataBinder.OoDescribeNamedReference` sets them, the decoder sets them from the record, `ActivationDescriptions.Of(item, null)`
  spells the identity (the SIG identity of a prototype is now the same text in every group). `Binding.Model` still holds no symbol.
- `OoSymbolOrigin { Source, Standard, Imported, Clr }` replaced `OoClassSymbol.IsStandard` at both readers; an imported symbol has
  no `Ctx`, an `ImportedFrom(path, mvid, module namespace)`, and the table key `class "<identity>"`.
- `IdentityScope` is the renamed `OoRepositoryScope` (words map to identities; the reverse map and the prototype/structure rows
  the first design text gave it have no reader and were not built: design 6.4 and 8.7 say so).
- `IClassImporter` / `OoClassTable.ImportClass`: the group wins, then BASE, then the repository; registered before it is
  completed, so a method that takes its own class does not recurse. `MetadataRepositoryProvider.Class` builds the symbol from the
  `ClassRecord` (new `ClassRecord.ClrName`, the TypeDef name), the superclass by identity (transitive), the methods over the
  decoded forest, the overrides re-linked; `StorageFormPass.ClassifyImportedMethod` gives the decoded formals the storage form the
  publisher gave them, because that form IS the CLR signature (`ManagedPointer<string>` against `<long>`), found by the first run.
  `RepositoryCodec.Names` is the ONE derivation of the importing group's names for the identities a record names (the provider's
  REPO-11 and `--repository-check` both read it; the second copy in `RepositoryCheck` is deleted).
- REPO-6 is COBOL5976 (two needed assemblies of one module namespace, or one equal to the compilation's own output's); REPO-11
  covers an imported class's superclass, implemented interfaces, RAISING elements and methods' object references; REPO-1
  (COBOL5962) now covers an unanswered class specifier (SR6), used or not.
- `BackendOptions.RepositoryReferences` (= `OoClassTable.ImportedAssemblies`) is appended by `RoslynBackend.Compile`;
  CS0435/CS0436/CS0437 are errors on generated code. `ModuleNamespace`'s C# spelling moved to `CsNames.CSharpNamespace`,
  `NamingConvention.FactoryClassName` puts its prefix on the last segment of a qualified name.

**Interfaces and properties (step 2's first half, same mechanism).** The importer is `IOoImporter` (renamed from the class-only
interface); `OoClassTable.ImportInterface` / `ImportedInterfaces` / `ImportedInterfaceKey` mirror the class arm,
`OoInterfaceSymbol` gained `Origin` / `Imported` and a nullable `Ctx`, `MetadataRepositoryProvider.Interface` shares
`Locate` (entry, registrar namespace, REPO-6) with `Class`, an imported class's `IMPLEMENTS` and an imported interface's `INHERITS`
resolve by identity, and a method's `CLR` name is a fixed fact when it is imported (`OoMethodSymbol.HasFixedClrName`;
`NameMethodSlots` names a slot after it, so a group class that implements an imported interface takes the built interface's member
name). An interface specifier no source answers is REPO-1 (SR9, `UnansweredInterfaceSpecifier`); a PROPERTY specifier is REPO-1
unless a class or interface of its own REPOSITORY paragraph (group or imported, base chain and implemented interfaces included)
has an accessor of `literal-4 ?? word` (SR16, `OoClassTable.CheckPropertySpecifiers`, run once every roster is complete). Two
construct samples of the version matrix (`repository-class-2002`, `repository-interface-2002`, `repository-property-2002`) named
a class, an interface and a property nothing defined; they now define what they name. A PROPERTY specifier in a parameterized definition's paragraph (whose class and interface entries are its formals) is not asked:
each expansion's re-parsed paragraph is (found by gate 2 on `pb2937_skeleton_bodies_bind_as_themselves`). `--repository-check`
decodes a record with `RepositoryCodec.Names(…, import: false)`, so a comparison never makes the compilation reference an assembly
its source does not name. Not yet: skeletons and expansions (the
`ExpansionKey`, `CompileState`, the two placements, stale-expansion REPO-9), which are step 2's second half.

**Goldens.** `conformance-suite/wiseowl/cases/12.3.8-REPOSITORY/pb2104_import_interface_and_property` (and its refusal at 85) imports
an interface, a class that implements it and a class with a property, passes an imported and a group object through the imported
interface and sets and gets the property through the imported accessors; `.../pb2104_import_class_under_another_word` (2002+; a build step
compiles the class and a program that takes it, the main imports the class as ACCT through `AS`, runs NEW/DEPOSIT/BALANCE and
passes the object to the other group's program with EC-PROGRAM-ARG-MISMATCH checking on: values 0050 and "CONFORMS", derived from
§11.7 method semantics and §14.8.2.3.2 rule 3) and its refusal at 85 (`pb2104_import_class_refused_at_85`, COBOL0900);
`ExternalRepositoryClassImportTests` (import by identity and by word from a `--reference`, the chain INHERITS+OVERRIDE+SUPER,
REPO-1 used and unused, BASE and group classes need no entry, REPO-11, REPO-6); `ImportedTypeCollisionTests` (CS0435/6/7 and
the names). The REPO-11 test of slice 7 changed premise: a class its own assembly publishes is now imported, so it publishes the
class apart.

**Self-review.** Architecture: one derivation of the names (`RepositoryCodec.Names`), one rule for a method formal's channel
(`UsageCollectionPass.MethodFormalChannels`, read by the class forests and the importer), no C# text in a bound node beyond the
CLR name the contract already classes.
**Step 2's second half: parameterized classes and interfaces across assemblies (D3).** *Re-probe before the work:* a group that wrote
`CLASS HOLD-BOX EXPANDS HOLD USING BOX` with HOLD defined by another group got COBOL2240 ("no definition of that name is in this
compilation group"), and a library's parameterized class left nothing in the repository (the definition "emits nothing"). *After:*
`lib.cbl` (BOX and the skeleton HOLD USING ELEM) and a client that expands HOLD over BOX compile apart and print `BOX 042`.
- **Published.** `CompileState` (edition, `--permissive`, `--sign-encoding`, `--numeric-display`, `--flag-extensions`, and the
  `>>COBOL-WORDS`, directive-fold and `>>DEFINE` lines as directive text, so the one directive processor reads them again) is produced by
  one `CompileInputs.At(line)` and held exhaustive by `CompileStateDriftTests` over `CompilerDriver.Options` and `BindSession`. A
  parameterized definition is a `public static class` carrying `[CobolUnit(ParameterizedClass|Interface)]` and `[CobolSkeleton]`
  (source text from `SkeletonText`, formals and kinds, the identity each specifier of its own REPOSITORY stands for, the state); an
  expansion is `[CobolUnit(Kind = Expansion, ExpandsDefinition, ExpandsActuals, ExpansionKey, ExpansionParts, ExpansionOwnAssembly)]`.
  The model records the emitter writes (`CompileState`, `SkeletonRecord`, `ExpansionIdentity`, `ExpansionPlacements`, `RecordList`) moved
  to `Binding.Model`, because the layering ratchet (`ArchitectureEdgesDriftTests`) forbids the emitter a new pair into `Binding.Repository`.
- **Expanded.** `MetadataRepositoryProvider.Skeleton` reads the record; `FragmentParse.ParseUnit` lexes the recorded text again under the
  recorded edition and words map (the whole-group parse in miniature, with the reservation-gate fixpoint); `OoExpansion.ImportedSkeleton`
  makes it a skeleton and expansion goes on exactly as for a written one. Two defects of the in-group expansion surfaced and are fixed: a
  `CLASS N AS "lit" EXPANDS …` entry lost its `AS` literal (GR2), and the formal's own specifier did not take the actual's `AS`
  literal, so an expansion over an actual known by another word named the wrong class.
- **Placed (`ExpansionPlacementPass`).** The key (`ExpansionKey`: SHA-256 over the canonical text of the skeleton and the surface of
  everything named) needs bound surfaces, so the design's "import before expand" is import before EMIT: the group binds its copy, the pass
  finds an entry of that name, definition, actuals, key and surface (`MetadataRepositoryProvider.ExpansionsNamed`) and the module names the
  existing types by `using` aliases instead of emitting its own (including the skeleton's own group importing the expansion whose
  assembly references its previous build); otherwise an expansion that names no class of the group is compiled apart
  (`ExpansionAssemblies` through `CompilerDriver.Compile` with the internal `Options.PublishAs`), one that does is emitted in the group;
  a stale expansion is REPO-9 naming the first changed component (`ExpansionParts`) and its referencers (`RepositoryLocator.ReferencersOf`
  over the `AssemblyRef` tables); REPO-2, REPO-12 and COBOL2240 for what is not ours to overwrite. The key's first run found that a decoded
  imported class names itself `class "BOX"` where the group's says `BOX`: `ObjectRefName` is a word that only names and is not in the key.
  Found on the way and fixed: a numeric literal argument of a method of an IMPORTED class named the callee's internal `_P_n` profile
  (CS0117); the profile is spelled out. Residual (design 10.5): a skeleton published under another compile state whose expansion names a
  class of the expanding group is COBOL2240, and its surface is bound under the expanding group's state.
- **Goldens and tests.** `pb2104_expand_skeleton_from_repository` (BOX 042) and `pb2104_expansion_is_one_class_across_groups` (two groups
  expand one triple; the object crosses a CALL under EC-PROGRAM-ARG-MISMATCH checking: BOX 007 / 02 CONFORMS), each 2002/2014/2023, and the
  refusal `pb2104_expand_skeleton_refused_at_85`; `ExternalRepositoryExpansionTests` (skeleton record, own assembly, import by key without
  rewrite, the reference cycle, stale REPO-9 with its referencers, in-group, byte-identical assemblies across directories and groups
  with one key, AS literal, the interface arm, a foreign file, REPO-1) and `CompileStateDriftTests`.
- **Rows.** SR-12.3.8.3-6/-9/-16, GR-12.3.8.4-6/-9, DOC-A.1-161/-162 DIVERGES|PARTIAL → CONFORMS (GAP 19 → 12) with witnesses added to
  GR-12.3.8.4-5 and DOC-A.1-138; PB1086 keeps the program and function rows.
- **Gate 1** (6ef43bb, before the model-layer move): RED on five drift tests (layering ratchet, name-fold marker, edition-read inventory,
  host-entry shape for the new unit kinds, the ambient-read recorder), all fixed; the Conformance population was otherwise green
  (21,012 of 21,013).
- **Gate 2** (7617a45): RED on two `BackendContractDriftTests` — the placements record was keyed by `object` (the backend-input walk cannot see
  through it) and its text members were unclassified; the placements are now keyed by the CLR name the module gives the class or
  interface (unique across both) and carry plain data (no `Compiler.Oo` pair into the model layer), and `BackendContract` classifies
  every new member. **Gate 3** (36a1d33, run `000003-20261103T030227Z`): GREEN — Conformance 21,020/21,020, Unit 39,457/39,457,
  Characterization 37/37, including the new goldens at three editions and their refusal, `ExternalRepositoryExpansionTests` (11) and
  `CompileStateDriftTests` (5).
- **Self-review.** Architecture: the key is computed once, by one pass, from bound surfaces; `OoExpansion` stays the one expander for a
  written and a published skeleton; the second emission is a compilation of its own through the one pipeline, not a second code path in
  the backend. Full code: the pass implements the cited rule (§9.3.12 one class per externalized name, definition and actuals) and the
  design's import/own/in-group order; the foreign-state residual is stated and refused loudly, never silently bound. Performance: nothing
  runs for a group without a parameterized definition (`ExpansionPlacementPass` returns `None` first; `SkeletonText` is built only for a
  parameterized definition); an own-assembly expansion costs one small compilation, once per key. Duplication: `RecordList` has one home;
  `CompileInputs.At` is the only producer of a state. Looked for and found: the expansion entry's lost `AS` literal, the unspelled
  actual identity, the cross-assembly `_P_n` profile, the end marker of a skeleton known by another word.
- **Branch review, round 1 (three blocking findings, all fixed).** (1) `ExpansionAssemblies.FreeForm` split the expansion text at every
  space, so a space inside a literal that straddled the 200-position fill point broke the literal (COBOL5563, COBOL2633, COBOL0001) and a
  literal longer than a line stayed on one line: it now splits by words outside literals and continues a long literal with the floating
  literal continuation indicator (§6.2.3.1, §6.5 8)); tests read a 140-character spaced literal back through a real expansion and a
  500-character literal with doubled apostrophes through the lines themselves. (2) A same-name expansion of other actuals at the file
  placement (a) would overwrite was only warned about (REPO-2) and then overwritten, which broke the first group's already-compiled class
  with `MissingMethodException`; `ReportSameName` is now COBOL2240 where the other class lives in an assembly the group references (the
  skeleton's, an imported actual's or resolution's, or an imported expansion's) or is the target file, and REPO-2 elsewhere (design 10.5
  corrected: its "Landed" bullet had said the file was replaced). (3) `OwnerIsImported` knew only imported classes, so a numeric literal
  argument to a method of an expansion in another assembly still named that assembly's internal `_P_n` (CS0117); it now asks the
  placements (`ExpansionPlacements.LivesElsewhere`), and a new golden `pb2104_expansion_method_with_literal_argument` runs it at three
  editions. Fixed in the same files (I9): the §11.3.3 SR1 citation of the skeleton's identity is §11.3.4 GR1; an own-assembly expansion's
  diagnostic names the EXPANDS entry's line instead of a deleted temp file and the failed compilation's generated C# is returned;
  `ReportForeignState` names an INTERFACE entry as such; `OoMethodSymbol.Ctx` is nullable (null for BASE's, a property accessor's and an
  imported method's) with `Definition` as the one reader that binds source. Tests added for the REPO-12 overwrite, REPO-2, the foreign-state
  refusal for a class and an interface, and both COBOL2240 arms. Not fixed (stay leads): `FragmentParse.ParseUnit` repeats
  `Frontend.LexAndParse`'s steps with no drift test tying them (R1-8); an own-assembly expansion's sub-compilation error position has no
  test because every error reachable there is reported first by the expanding group's own bind.

