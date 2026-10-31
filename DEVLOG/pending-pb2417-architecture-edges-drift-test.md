## PB2417: the architecture drift test lands as a metadata walk and one edges file (blocked on one remover note)

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
(`WISEOWL_COBOL_ARCH_EDGES_SEED=1`) rewrites the rows from both observers. The self-test plants every violation and
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
none of them has a remover. Under §8.1 such a pair blocks this note until a note is filed, so the drift test is red on
exactly those 29 pairs and the branch waits for that note. The design doc §8.1 now describes the drift test as
landed. Self-review covered the architecture (one checker, one file model, one census reader moved out of
`ArchCensusDriftTests`), cost (the walk and checks take about half a second) and duplication (the C# seed and the
Python assigner write byte-identical JSON).
