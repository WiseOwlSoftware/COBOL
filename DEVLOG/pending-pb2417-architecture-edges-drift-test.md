## PB2417: the architecture drift test lands as a metadata walk and one edges file; PB4319 removes the new repository row

The first wave of the R3 program (DESIGN-architecture-review §8.1) puts the layering under a gate. Until now the layers
and their allowed edges lived only in §8.1's proposed JSON block, and nothing failed when a landing added a cross-layer
dependency.

**The walk.** `TypeReferenceWalk` (tests/WiseOwl.Cobol.Tests.Unit/Architecture/) reads the five built product
assemblies with System.Reflection.Metadata and each type's documents from its portable PDB. It applies the census's
population rule: the synthesized types are left out, and a type is generated only when every document that declares it
is. It counts the distinct (outermost authored type, outermost product type) pairs named by base types, interfaces,
constraints, signatures, locals, catch types, IL operand tokens and custom attributes, `typeof` arguments included. Two
defects surfaced while seeding. First, `il.Offset += 4 * il.ReadInt32()` reads `Offset` before the count advances it,
so every `switch` misaligned the decoder by four bytes. The count is now read first. Second, the PDB names only
generated documents for `CobolParserCore` itself, because its hand partial declares only a member of its nested
`CompilationUnitContext`. A type's documents now include its nested types', since a nested type is always declared
inside its container. After both fixes the walk agrees with census `7cb79581e` on the generated flag of every type both
name.

**The file and its checker.** `docs/rearchitecture/architecture-edges.json` is the one place the rule is written.
`ArchitectureEdges.CheckWalk` fails on:
- an unplaced or doubly placed namespace;
- a cross-layer edge that is neither allowed nor tolerated;
- a pair inside a tolerated row that the row does not list (J1);
- a grown or shrunk row, or a vanished pair (the ratchet only falls);
- a pair with no remover;
- a row that the layers allow or that measures zero;
- a dead allowance.

`CheckCensus` runs the same file model over the newest census record's `namespaceEdges`. It requires exact `uses` and
the file keyed to that record, and `ArchCensusDriftTests` gains that assertion. The what-if
(`WISEOWL_COBOL_ARCH_EDGES_WHATIF`: a move map and a cut list) re-keys the pairs before they aggregate. The seed
wrote the rows once from both observers; it is now the ratchet (below). The self-test plants every violation and
the clean input.

**The removers.** The targets file carries no type-to-namespace move maps, so `scripts/arch/edges.py --assign`
replays their file-level image. A row's plan names candidate notes. A candidate removes a pair A → B when its computed
file set (`member_index.note_file_set`) holds A's declaring file and it names A or B among what it moves or changes. If
no candidate names either type, every candidate that reaches the pair removes it. Each remover's Removes line must name
the row.

**Measured on this tree.** The file has 28 rows and 359 pairs, and 330 of the pairs name removers from R1's notes. Since
`a02b165da` the census grew eight rows. Two namespaces R1 never saw now cross a layer:
- `Binding.Repository`, placed in compiler.binding because its reader reads the binder.
- The CLI's `NumericDisplayForm`. `cli → compiler.binding` is now allowed, by the composition-root rule.

The new row `CodeGen → Binding.Repository` holds 29 pairs (three emitters read the external repository's records), and
none of them had a remover. Under §8.1 such a pair blocks this note until a note is filed, so the first gate was red on
exactly those 29 pairs. The design doc §8.1 now describes the drift test as landed.

**The remover, PB4319.** The implementer's lead was to move the record family and the codec to compiler.model. The
types say otherwise. The records (`RepositoryRecord.cs`: `RepositoryRecord`, fifteen sub-records and `RecordList<T>`)
name only the runtime, so they can move. The codec cannot: its encoder reads `DataBinder` and `OoClassTable`, and its
decoder reads `PrototypeSignatureClasses`. In compiler.model it would trade this row for compiler.model →
compiler.binding. PB4319 therefore moves the records and takes the encode out of the backend: the binder writes each
published unit's record at bind time with the same `RepositoryCodec.Of`, and `KindOf` becomes a `BoundUnit` fact.
`RepositoryReader` and `RepositoryVocabulary` stay where they are. PB4319 waits for this note and for slice 8 (PB2104),
the last external-repository slice that edits its files. The newest committed member index (`65e78707f`) predates the
repository's types, so `work.py check` could not resolve the note's `**Moves or changes:**` names. The index of this
tree is now recorded beside it, and every open R3 note resolves against it. The row's `plan` names PB4319, and
`edges.py --assign` names it on all 29 pairs (359 of 359 assigned). DESIGN-backend-abstraction §1.3 L6 now lists the
emit-time encode as a seam leak. Self-review covered the architecture (one checker, one file model, one census reader moved out of
`ArchCensusDriftTests`) and cost (the walk and checks take about half a second).

**The ratchet, after the branch review (R1-1).** The review found that the seed admitted new dependencies. The
vanished-pair remedy and every census recording re-seeded the file, the seed listed every measured pair and overwrote
`uses` with the census count, and the assigner's reach rule named a remover for any new pair whose from type already
had a pair in the row: the reviewer's plant `CodeGen.CSharpEmitter → Binding.SomeNewBinderType` was assigned PB2290.
So a move that deleted one pair and added another went green after the prescribed command, and census growth was
absorbed at the next recording. §8.1 approved a file seeded once and then ratcheted, so the seed mode is gone.
`WISEOWL_COBOL_ARCH_EDGES_RATCHET=1` runs `ArchitectureEdges.Ratchet`, which deletes vanished pairs and dead rows and
lowers counts, and never adds a pair or a row, raises a count or names a remover. Admitting a dependency is a hand
edit in the change that files the note removing it. `scripts/arch/edges.py` did its one job and is deleted. Its
Removes-line check moved into the gate as `ArchitectureEdges.RemoverDefects`, which also refuses a remover kb/Work
does not hold and a landed or retired one (`work.py`'s `TERMINAL_STATUSES`, read from there). Ratcheting the seeded
file changes no byte of it. Two new self-tests plant the reviewer's swap, an added pair, a new row and census growth
(each still red after the ratchet), every fall, and each remover defect.

**A census-only row names its remover (R1-2).** `Binding.Bound → Frontend.Parsing` has one census use and no walked
pair, so it passed with no remover at all. A row with no pair now names its own removers in `removedBy`. This one
names PB2347: the use is `StatementBinder.BindSizeError` reading the enum constant
`ConditionalPhraseFamily.SizeError`, which the compiler inlines. PB4319's Removes line now counts 26 record pairs and
three codec pairs, one per emitter (R1-3).
