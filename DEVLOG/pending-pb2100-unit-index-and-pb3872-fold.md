## External repository slice 4: the schema, the renderer and the reader; the host entry pairs by the Annex C fold (PB2100, PB3872)

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

**PB2100, the signature attributes, the update option and the reader (wave 20261733 group A).** ISO 8.13: "The
implementor shall provide a mechanism that allows the user to specify whether to update the external repository when a
compilation unit is compiled." That mechanism is now `--repository-update on|off` (default on; `CompilerDriver.Options` and
`BackendOptions.RepositoryUpdate`, the `AssemblyName` remark corrected to the output file's base name). With it on, every host
entry carries its unit's record, transcribed from `RepositoryCodec.Of` by `RepositoryAttributeRenderer`: the unit facts on
`[CobolUnit]` (entry convention, DECIMAL-POINT IS COMMA, the currencies and the RAISING phrase as parallel arrays), the
record's tables on the class (`CobolTypeDeclarationAttribute`, `CobolStructureAttribute`, `CobolPrototypeAttribute`,
`CobolPrototypeFormalAttribute`, `CobolPrototypeDescriptionAttribute`), each formal's `[CobolFormal]` and `[CobolDescription]`
tree on its own `Call` parameter and the RETURNING tree on the return value; the assembly record says `HasSignatures`. The
description and formal fields are declared once, on two never-applied abstract bases, so every applied attribute stays sealed
(a correction of design 6.2's table). The reader is `RepositoryReader.Read(RepositoryIndex)`, through the index's own
decoding provider (`RepositoryIndex.SchemaAttributeName` / `DecodeSchemaAttribute`, not a copy): it rebuilds trees from
their ordinals and formals by position, and refuses every shape the renderer cannot write as a typed outcome
(`Malformed` with its words, `NoSignatures`, `OtherSchemaVersion`). Re-probe: the codec's fixture compiled by the CLI
carries the records, and the reader returns them equal to `Of` of each published unit. Tests: `RepositoryReaderTests`
(the module's records equal the codec's and still answer `PrototypeSignatures.Same`; the host entry's parameter and return
types are `HostSurface.TypeOf` of the decoded descriptions; `off` writes no signature attribute and reads `NoSignatures`;
two compiles are byte-identical; ten encoding fixtures, each structured value with it and without it; the malformed shapes);
`RepositoryRoundTripCorpusTests` now also renders every published record of the corpus into libraries, compiles them and
reads each back equal; `RepositorySchemaDriftTests` (every attribute sealed, not inherited and decodable without loading;
every settable property written by the renderer and read by its reader; metadata read only through `RepositoryIndex` and
`RepositoryReader`). A pseudo-mutation (the renderer dropping `DigitPositions`) failed an encoding fixture, the module
test and the corpus. The fourteen characterization snapshots are re-baselined for the attributes. Gate: run 000002 was red on
two tree sweeps (`CobolNameFoldDriftTests`: the option value's case-blind compare now carries `// not a COBOL word`, as
`--numeric-display`'s does; `HostWitnessTraitDriftTests`: the new schema drift test is a tree sweep and carries the trait);
the self-review widened the reader's catch to what `RepositoryIndex.Open` catches from a hostile blob. Run 000003 at
8e7d3c05c: `LEG 1 ONLY (batched gating, PB2515): GREEN`, Linux half GREEN, the 24 added cases `Passed`.

**PB2100, the OO arm (wave 20261734 group A).** The predecessor's thirteen commits cherry-picked cleanly onto main
40b853db4 and re-gated GREEN (run 000001-20261028T021719Z). Re-probe: before this part a class or an interface carried no
`[CobolUnit]` and no `Units` row, so a module holding only classes published nothing the repository could find, and no
method's description was anywhere in metadata; ISO §8.13 lists "the type of the source unit - program, function, class, or
interface", "the object properties of the source unit, if any" and "the methods contained in the source unit, if any, and
details about the method's externalized name, parameters, returning item, and entry convention". After: `cobol oo.cob` (the
codec's new OO fixture) writes `[CobolUnit(Class, "ROC", "RO-CLASS", …, BaseIdentity = "BASE", Implements = …, FactoryType =
"__FACTORY_ROC")]`, a `[CobolProperty]` per property on the type of its accessors, and `[CobolMethod]` on each method,
accessor and interface prototype with its formals on the carriers, and the program still prints `R= 0000002.50 TAG=ROC1`.
The fix is the model first: `ClassRecord`, `PropertyRecord`, `MethodRecord` and `RepositoryRecord.Class`, written by
`RepositoryCodec.Of(OoClassUnit)` / `Of(OoInterfaceSymbol, DataBinder)`; the renderer and the reader transcribe it
(`UnitRecord`'s class facts, `MethodRecord`, `PropertyRecords`; `RepositoryReader.Class` / `Methods`, the factory read from
the type `FactoryType` names). Two corrections of design 6.1 came with it. A description's type declaration is the one the
BINDER resolved where the TYPE clause was written: `DataItem.TypeDeclaration`, set by `DataBinder.ExpandType` beside `TypeName` and carried
by `CopyEntryDescription`, replaces the codec's own lookup by name in the publishing element's dictionary. That lookup was a
second statement of the type-name rule and was wrong twice: a method's formal typed by a method-local TYPEDEF (ISO §11.7.4 GR5;
the fixture's `LK-M TYPE MT` now names its row) and a referenced prototype's formal typed by the prototype's own TYPEDEF (the
predecessor's lead: RTONE's record described PRTA's `P TYPE PP` with no `TypeDeclaration`, because RTONE declares no PP; ISO
§8.13 "type declarations required for the description of parameters and returning items"; now pinned by
`TheRecord_StatesTheUnitFactsSection813Lists`). And an accessor's externalized name is its property's (ISO §12.3.8.4 GR2: "Literal-5
is the externalized name known to the operating environment for a method that implements the named property"), its `Name`
the roster's `__GET_<P>`; a property record carries `IsFactory` and is derived from the accessors. `OoSignatureOf` takes the
method's record and puts the formal's attributes on the carrier, the triple's first parameter (design 6.2 "Anchoring");
covariant adapters and the presented inherited prototypes of an interface pass none and carry no `[CobolMethod]`. The reader
also refuses an undefined `RepositoryUnitKind` in a constructor argument (`Fields.FixedKind`), which it had cast unchecked.
Tests: `RepositoryCodecTests.AClassAndAnInterfaceRecord_StateTheFactsSection813Lists` and every method round-tripped
(`RoundTripAll` through `OoRecordsOf`); `RepositoryReaderTests.ACompiledClassAndInterface_ReadBack_EqualToTheCodecsOf` (and
`off`: the index rows stay, no signature attribute); `RepositoryRoundTripCorpusTests` publishes every class and interface of
the corpus through metadata (it asserts more than 300 class and 50 interface records); the accessor pair in
`RepositoryVocabularyDriftTests`. All 28,806 unit drift cases and the 36 characterization cases pass (no snapshot holds a class).

**PB2100, the slice's remaining Facts, and the slice lands.** 17.2's SR2 pair: for every prototype the group also defines
(ISO §10.6.2 SR2: "the signatures of these two compilation units shall be the same"), `PrototypeSignatures.Same` of the two
DECODED records holds and agrees with the bound pair (`RepositoryRoundTripCorpusTests.PrototypeDefinitionPairs`, over the one
corpus enumeration `Corpus()` both corpus Facts read). 17.3's every-unit-has-its-entry Fact
(`HostEntryShapeDriftTests.EveryOutermostUnitOfTheCorpus_PublishesExactlyOneEntryAndRecord`): every case's module carries
exactly one `[CobolUnit]` per published unit, on its type, and its `Units` index lists exactly those in emission order. 6.4
part 3 end to end (`PrototypePointerTypeDriftTests.ADecodedPointer_IsTypedByItsPrototypesSignature_NeverByItsName`): a pointer
from another group's record, read through metadata and decoded into the importer, shares the group pointer's identity exactly
when the signatures are Same (ISO §13.18.60.4 GR25). 17.4 (`RepositoryBackendIndependenceTests`): a second backend writes the
same records with `MetadataBuilder` and the ECMA-335 custom-attribute encoders, and the reader reads them back equal to the
Roslyn module's. So that fixture is a second BACKEND rather than a second statement of the encoding, the renderer was split
into two layers: `AttributeApplication` values (type, constructor arguments, named arguments; every rule of design 6.2) and
their one C# spelling, `RepositoryAttributeRenderer.CSharp`; the emitted C# is byte-identical (the 36 characterization
snapshots and the OO probe's `oo.g.cs` compared). PB2100 flips to `landed` with `closes_rows: []` (its reason: slice 4 closes
no inventory row; the §12.3.8 rows are slices 7 and 8's), design section 21 marks item 4 LANDED, and the five R3 waves that
waited on PB2100 for a file of the in-flight set (PB2290, PB2319: `PB2103`; PB2398, PB2406, PB2407: `PB2104`) now wait on the
slice that changes that file next (`work.py check`). The self-review (silent-failure lens) added one refusal: a class record
that names no factory type, or an interface record that names one, is `Malformed` rather than read as a class without factory
methods (`RepositoryReaderTests.AClassRecordTheRendererCannotHaveWritten_IsMalformed`, with the missing factory type, the
factory method that does not say so and the formal on a CLR method without `[CobolMethod]`). Gate: run 000002-20261028T030809Z
at c24278e25, `LEG 1 ONLY (batched gating, PB2515): GREEN`, Linux half GREEN; run 000003-20261028T031610Z at a1f8d823e (the
hardening), `LEG 1 ONLY (batched gating, PB2515): GREEN`, Linux half GREEN; the note's Landing section re-gated after it.

**The branch review's findings (wave 20261739 group A).** R1-1 (blocking): the record kept an OCCURS DEPENDING ON object as
its bare data-name and the decoder linked the FIRST entry of that name in the record, kb/Work PB978's first-candidate shape. A
formal `01 REC. 05 A. 10 N PIC 99. 05 B. 10 N PIC 99. 05 T PIC X OCCURS 1 TO 10 DEPENDING ON N OF B.` is legal (ISO §8.4.2.2.3
SR1: "uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference") and
decoded with `T` counting by `A`'s `N`, which `DependingAdmission` also admits, so nothing refused it. Reproduced on this build
(`RepositoryCodecTests.AQualifiedDependingOnObject_AndARepeatedRedefinesName_DecodeToTheBoundEntries` failed against the old
lookup). The root fix records the link as a position, not a word: `OccursRecord.DependingEntry` (the attribute
`OccursDependingEntry`) is the preorder index of the object's entry in the description's tree, the index the attribute ordinals
already number, written by `Encoder.Table` exactly when the object is in the record, and `Decoder.Root` links the entry at that
position, refuses a record whose position is past the tree, whose word is not that entry's name, or that has a word of the
record with no position (or a position with none), and then asks the admission as before. The sibling sweep found the other
link decoded by name, REDEFINES: the decoder took the nearest preceding same-named sibling, so `05 X. 05 X REDEFINES X. 05 C
REDEFINES X.` decoded `C` over the second `X`, where §13.18.44.3 SR7 ("Multiple redefinitions of the same storage area shall
each specify as data-name-2 the data-name of the entry that originally defined the area") and NOTE 1 make it the first; it also
failed the same test against the old code. The placement rule the binder states (kb/Work PB1280) became ONE function,
`DataItem.RedefinedEntry`, which the binder and the decoder now both ask. `RepositoryCodecTests.Same` compares both links as
positions in the tree for every formal it round-trips, so the whole corpus checks them from now on. R1-2: a
`CliParserTests` case for `--repository-update` (default on, both spellings, case, refusals, no swallowed flag) and
`RunCompile` gets back its own summary. R1-3: the README option table lists `--repository-update`. Design 6.1 and its
table of read members say how the object is recorded. Gate: run 000002-20261028T191256Z at a0d9cf7cd,
`LEG 1 ONLY (batched gating, PB2515): GREEN`, Linux half (WSL clone of HEAD) GREEN; the four Facts above `Passed` in its
leg-1-Unit.trx.

**The branch review's second round (wave 20261739 group A, finisher).** R1-1 (blocking): the first round made the REDEFINES
placement rule one function, but only its first half. The binder linked `DataItem.OriginalDefinition(preceding) ??
preceding.LastOrDefault()`, so the fallback for the case where every same-named candidate is itself a redefinition (the
§13.18.44.3 SR7 chain COBOL1656 reports, an error strict and a warning under --permissive) lived only in the binder. For `05 A.
05 B REDEFINES A. 05 C REDEFINES B.` compiled --permissive, the renderer published `C` over `B` and the decoder refused the
record ("'C' REDEFINES 'B', which no preceding sibling originally defines"), against design 6.3's round-trip invariant. The
function is now `DataItem.RedefinedEntry` and holds both halves; the binder and the decoder both ask it, and the decoder
refuses only a word that names no preceding sibling. `RepositoryCodecTests.APermissiveRedefinesChain_DecodesToTheBoundEntry`
binds the chain --permissive, decodes `C` over `B`, round-trips it, and refuses a perturbed record whose word names no
sibling; it failed against the old decoder rule. Sweep: the clause pass in `DataBinder` is the only REDEFINES resolver (the
other two `SetRedefinition` writers are the file section's implicit redefinitions), and every other `RedefinesTargetName is
null` filter in `src/` is a storage walk.

**Self-review.** Architecture: one writer of attribute text, one reader of the index, one definition of the row encoding;
no C# text in a bound node. Code: the cap and the escapes are tested at their edges; `Open(Stream)` catches only the
exceptions a hostile image raises. Performance: the reader allocates only the rows. Duplication: the row encoding is not
restated by the renderer. Comment accuracy: `Edition` and `CompilerVersion` are documented as named in diagnostics and never
compared (design 5.4 item 2).
