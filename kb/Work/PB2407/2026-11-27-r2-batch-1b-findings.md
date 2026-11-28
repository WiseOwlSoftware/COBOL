## R2 batch 1b (pin 9e65164eb3a9): 1 finding on this note

### `parser-tree-4--architecture--f1#5` (finding, move-and-rename, votes upheld/upheld/upheld)

**Finding:** R0-0105 judged by DESIGN-architecture-review §8.7: CompilationInputs.FilesRead, Probes and EnvironmentReads are an observation seam (rule b), not dead code; keep them, and correct the summary that names a build tool as their reader

**Rule:** the test-only judgment of DESIGN-architecture-review §8.7 (a table enumeration keeps; (b) a seam that exists for observation moves to the test project where the product allows it, else is named as a seam; (c) the rest is deleted); docs describe the current compiler (CLAUDE.md rule 6)

**Scenario:** The three getters are the read side of the ambient-input record every compile writes (CompilationInputs.cs:23-38). Their readers are all test-side: the Conformance CompiledProgramCache keys and re-validates its entries by them (tests/WiseOwl.Cobol.Tests.Conformance/CompiledProgramCache.cs:403,485-488), ArchOracle decides whether a case is hermetic by them (ArchOracle.cs:281-293), and the compile benchmark witness counts FilesRead (CompileBenchmarks.cs:85). Deleting them by rule (c), as the census target proposes, breaks the cache invalidation that keeps a Conformance run from serving a stale compile after a copybook or environment variable changes. Moving them to the test project is not possible: the lists are private product state. Meanwhile the class summary says the record is the dependency list a build tool needs (the gcc -MD depfile shape, CompilationInputs.cs:12-14), but no product path writes a depfile, so a reader takes the record for a shipped feature.

**Target:** keep the three members (rule (b), the product does not allow the move: their lists are private state the gateway fills); with PB2407 they move to frontend.common unchanged; the summary names their consumers as they are (the Conformance compiled-program cache and the hermeticity oracle) and states that no product depfile output exists; the census row R0-0105 is closed as judged (b)

**Sites:** src/WiseOwl.Cobol.Frontend/Pipeline/CompilationInputs.cs:9-38 (CompilationInputs, FilesRead, Probes, EnvironmentReads)

The finding's own finder tied it to this note, and the note's mechanism holds it, so it is recorded here and not filed as a note of its own. The sites are the pin's.
