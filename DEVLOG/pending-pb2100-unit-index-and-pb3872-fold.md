## External repository slice 4, first part: the unit index and the unit records; the host entry pairs by the Annex C fold (PB2100, PB3872)

**What this change is.** Two things. The first is the PB1086 slice notes' `**Sites:**` blocks (the earlier branch
`notes-pb1086-sites`, merged here): slices 4, 5, 7 and 8 each declare the files and members the design (sections 12 and
21) says they change, and PB2100 says NEEDS-OPUS for the restricted-pointer typing of design 6.4. `plan_wave.py --cluster
PB1086` had planned PB2100 UNPARTITIONED, free to collide with any parallel group. The branch's review (four blocking
under-claims, two leads) was answered here: PB2100 now names the OO emission sites (`OoEmitter.EmitClassUnit`,
`EmitTypeHalf`, `EmitMethod`, `EmitInterfaceUnit`, `OoSignatureOf`), `HostEntryEmitter.EmitEntry` and `ParameterText`, and
`PrototypeSignatureClasses.cs` whole (its nested `Namespace` and the `RestrictedPrototype` record change with the keying);
PB2101 claims `ProgramTable.cs` whole (the `CallName` dictionary, the registrar-fault classification in `RegisterModule`,
`RunOnThisThread` and `CallFromHost`, the `n.Factory` catch in `Activate` and `ActivateMain`) and the suite runner and
manifest; PB2103 and PB2104 gained the binder, runtime and version-file members design 12 assigns them (`BinderDriver.Bind`
and `BindUnitProcedure`, `StatementBinder.ProgramRestrictionIdentityOf`, `ActivationRelations.SamePointerRestriction`,
`OoClassSymbol.cs`, `FragmentParse.ParseTokens`, `DataBinder.OoBindObjectRefDescriptor`, `RuntimeAbi.pin`); every block now
names the docs files its slice edits (docs is a campaign root). The over-claims came out: `scripts/abi_pin.py` (run, not
edited), `ObjectRefDescriptor.cs` and `OptionsModelConsumerDriftTests` under PB2100 (slice 4 reads `RuntimeKind`; the
`Target` field is slice 8's; `--repository-update` adds no `OptionsModel` field).

**PB3872 (the host-entry pairing).** `HostEntryEmitter.EntryUnits` withheld a prototype's host entry when its group also
defines it, but compared the two externalized names ordinally, so `PROGRAM-ID. psub IS PROTOTYPE` beside
`PROGRAM-ID. PSUB` published the classes `psub` AND `PSUB`. It now pairs by `ExternalizedNames.Same`, the Annex C fold, as
every other comparison of an externalized name does (ISO 8.3.2.2 2): `cite.py --check` OK). Re-probe: the ordinal compare
published `PMAIN`, `psub`, `PSUB`; the fold publishes `PMAIN`, `PSUB`. `HostEntryShapeDriftTests` gained the case-differing
pair (red with the ordinal compare). No conformance-suite golden: the observable is the public surface of the compiled
assembly, not COBOL output.

**PB2100 (slice 4), the first part.** The design's slice 4 is the schema, the renderer and the reader; it is several
mechanisms, and this change lands the one the rest stand on, the module's header and unit index:

- Runtime: the persisted enums of design 6.2 rule 1 (`Repository/RepositoryEnums.cs`, an explicit value on every member,
  `int`-backed); `CobolRepositoryAttribute` at `CurrentSchemaVersion = 2` with `Units`, `CompilerVersion`, `Edition`,
  `HasSignatures`; `CobolUnitAttribute`; `RepositoryIndex`, the one reader of a module's header and `Units` rows and of the
  `AssemblyRef` names, over one prefetched `PEReader` and `MetadataReader`, the file read into memory and closed, the
  65,536-unit cap and every failure a typed `RepositoryReadFailure` (design 7.4, 15.1, 15.2). `RepositoryIndex.EncodeUnit` /
  `TryDecodeUnit` are the one definition of the `<kind>:<name>:<word>` row with its `\:` and `\\` escapes.
- Compiler: `RepositoryAttributeRenderer` is the one place attribute syntax is written (`ModuleRecord`, `UnitRecord`,
  `IndexOf`); `RuntimeApi.ModuleRecordAttribute` moved into it and is gone. `BoundCompilation` carries the `Edition` it was
  bound under. Every host entry carries `[CobolUnit]`, and the assembly's `Units` is built from `HostEntryEmitter.EntryUnits`,
  the same list the entries are emitted from, so the index cannot disagree with the records. The `Published` filter is
  deleted (a group item `PublishedRecordEmitter.Renders` refuses is an `InvalidOperationException` naming the unit).
- Tests: `RepositoryIndexTests` (the header and the index of a compiled group read without loading it, the index equal to
  the `[CobolUnit]` record set, the row encoding round-tripped over names holding `:` and `\`, malformed rows refused, the
  typed outcomes for a non-assembly, a plain assembly, a malformed row and the unit cap, no handle left on the file);
  `RuntimeAbiPinDriftTests` states schema 2; the 14 characterization snapshots re-baselined (a `{COMPILER-VERSION}` token
  beside the runtime's).

**Left open in PB2100, and why this is a first part.** `HasSignatures` is written only with the signature attributes, which
do not exist yet, so it is never written and decodes false (the design makes it true exactly for `--repository-update on`).
Not yet written: the rest of the attribute family and `RepositoryVocabulary`, `RepositoryRecord` with `Of(BoundUnit)` and its
inverse, the description attributes on host entries and OO types with `--repository-update`, `RepositoryReader`, and
`PrototypeSignatureClasses` keyed by origin and name (design 6.4 parts 1-3, the subtle part). Classes and interfaces are not
yet units of the index either (their `[CobolUnit]` comes with the OO emission). The note says so and stays open.

**Self-review.** Architecture: one writer of attribute text, one reader of the index, one definition of the row encoding;
no C# text in a bound node. Code: the cap and the escapes are tested at their edges; `Open(Stream)` catches only the
exceptions a hostile image raises. Performance: the reader allocates only the rows. Duplication: the row encoding is not
restated by the renderer. Comment accuracy: `Edition` and `CompilerVersion` are documented as named in diagnostics and never
compared (design 5.4 item 2).
