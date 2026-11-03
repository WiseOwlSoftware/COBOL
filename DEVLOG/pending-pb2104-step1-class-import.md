## PB2104 step 1: a class another compilation group defines is imported by identity (external repository slice 8)

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