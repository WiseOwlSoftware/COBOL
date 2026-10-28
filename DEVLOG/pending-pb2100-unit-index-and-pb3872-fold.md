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
  beside the runtime's); `RuntimeApiGuardTests` (the ratchet that every emitted runtime name routes through `RuntimeApi`)
  found the renderer's `typeof`/`nameof`-anchored names bare and now lists `RepositoryAttributeRenderer.cs` as an anchored
  writer beside the façade, with the reason (a rename of an attribute member still breaks that file at compile time).

**PB2100, the restricted-pointer classes (design 6.4 parts 1-3).** `PrototypeSignatureClasses` keyed its members by the
externalized name alone, which is right inside one group (one externalized name, one program, ISO 8.3.2.2) and wrong the
moment a second source of prototypes exists: a decoded prototype `P` met the group's `P` and took its class whatever its
decoded signature, typing the pointer by NAME, which 13.18.60.4 GR25 forbids. Members are now keyed by origin and name
(`PrototypeKey`; `RestrictedPrototype.Origin` null for the group, a `PrototypeOrigin` for a decoded record, compared by
reference); `Admit(decodedPointers)` adds the decoded prototypes and the whole graph their signatures reach, then RE-RUNS
the refinement over every member of both namespaces from ONE starting block, group members first in their original order
(an incremental append would give a decoded `B` whose signature is `Same` as the group's `B` a block of its own: two types
for one signature); a block a decoded member founds is spelled `DECODED k TO name`, `k` the founder's admission number from
1, so no group spelling (`TO name`) and no `EXTERNAL TO` can equal it whatever an `AS` literal holds. `Classify` is the same
`Take`/`Run` over the group's pointers, so every group identity is what it was. `PrototypeSignatureClassesTests` (seven
Facts) drives the class directly over five bound signatures, because no source reaches a decoded prototype until the reader
and codec exist: a decoded prototype of the group's signature joins the group's block and moves no group identity; one named
like a group prototype with another signature is not the group's type; two decoded `X` of different records get two
identities and one record's `X`, in another case, is one member; a group prototype named `DECODED 1 TO B` or `B #1` never
shares an identity with a decoded block; an unadmitted decoded prototype and a group pointer given to `Admit` are refused.

**PB2100, the codec (wave 20261731 group B; design 6.1-6.4, 17.2).** ISO 8.13 stores "the description of the parameters
of the source unit, if any, and the manner of receiving parameters (by reference or by value) and whether they are optional
or not" (`cite.py --check` OK) as the unit's signature. `Binding/Repository/RepositoryRecord.cs` is that record in the
persisted vocabulary, `RepositoryVocabulary` the one enum translation (a `Persisted` / `FromPersisted` pair per enum, each
with an `UnreachableException` discard), and `RepositoryCodec.Of(BoundUnit)` / `RepositoryCodec.Decode` the one translation
between a bound unit and its record: the formals and the returning item as description trees, every type declaration,
dynamic-length structure and referenced prototype they name as rows of the record's own tables (an index is assigned before
a row is described, so a prototype naming itself records as one row). `Decode` binds a record into a throw-away `DataItem`
forest, rebuilds `RestrictedPrototype` per prototype row with a `PrototypeOrigin` of its own and calls `Admit` on the group's
signature classes (`BoundCompilation.SignatureClasses`, new), so a decoded pointer is typed by its decoded signature.
Re-probe (the slice's own shape: no code existed): the fixture of nine formals of every 6.3 shape and the whole corpus round
trip. The design's model was wrong in five places, each fixed in the design: SYNCHRONIZED is the clause's form
(`RepositorySynchronized`; kb/Work PB538 made LEFT and RIGHT two clauses), OCCURS needs the whole `OccursSpec`
(`OccursRecord`), BASED and ALIGNED-as-level-1 ride the description copy, a structure's name is still compared today, and a
GR10 c) prototype row has no signature. The decoder refuses a malformed record by name (`RepositoryRecordException`), and an
OCCURS DEPENDING ON link only through the binder's one admission, now a value both ask (`DependingAdmission`, kb/Work PB3510:
`OdoDependingAdmissionDriftTests` caught the decoder as a third writer of the link, and now pins that both writers ask the
one admission). The program's whole RAISING phrase now rides the bound program (`BoundProgram.Raising`, which replaced
`RaisingObjects`), since 8.13 records "the exceptions that may be raised". Tests: `RepositoryCodecTests` (the fixture round
trip; the unit facts; two encodings equal; a referenced prototype missing a formal fails `Same` and founds a `DECODED` block;
a data-name change is inert and a TYPE-name change is seen, ISO 13.7.3 SR2 / 8.5.3.1; malformed records refused);
`RepositoryRoundTripCorpusTests` (every accepted case of the WiseOwl manifest at every edition, 3,011 binds: the activation
description both ways, `SameElementaryLayout`, `SameType`, the widths, the host type, the normalized `PicInfo` equality and
the restriction identities); `RepositoryVocabularyDriftTests`; `RepositoryRecordConverseDriftTests` (every `PicInfo` member
and every carried `DataItem` member has a record field or a stated reason). A pseudo-mutation (the decoder dropping
SYNCHRONIZED) failed the fixture at `SameElementaryLayout`. `DescriptionCopyCompletenessDriftTests` exempts the decoder's
`Item`, a translation from a persisted record and not a `DataItem` copy.

**Left open in PB2100, and why this is a first part.** `HasSignatures` is written only with the signature attributes, which
do not exist yet, so it is never written and decodes false (the design makes it true exactly for `--repository-update on`).
Not yet written: the rest of the attribute family, the description attributes on host entries and OO types with
`--repository-update`, `RepositoryReader` (which decodes the attributes into the codec's `RepositoryRecord`), and the
round-trip-through-metadata tests (17.2's encoding Facts, 17.4, 17.7). Classes and interfaces are not yet units of the index
either (their `[CobolUnit]` and the class half of the record come with the OO emission). The note says so and stays open.

**Self-review.** Architecture: one writer of attribute text, one reader of the index, one definition of the row encoding;
no C# text in a bound node. Code: the cap and the escapes are tested at their edges; `Open(Stream)` catches only the
exceptions a hostile image raises. Performance: the reader allocates only the rows. Duplication: the row encoding is not
restated by the renderer. Comment accuracy: `Edition` and `CompilerVersion` are documented as named in diagnostics and never
compared (design 5.4 item 2).
