## R2 batch 1b (pin 9e65164eb3a9): 1 finding on this note

### `parser-tree-4--modern-csharp--f1#4` (finding, move-and-rename, votes upheld/upheld/upheld)

**Finding:** The front-end driver type is named Frontend inside namespace WiseOwl.Cobol.Frontend, so every caller spells Frontend.Frontend (CA1724); the PB2246 namespace move keeps the clash

**Rule:** CA1724 (type names should not match namespaces) at AnalysisLevel=latest-all, DESIGN-architecture-review 5.5 analyzer-first; 8.2 layout and naming rules (folder equals namespace, R0-0762)

**Scenario:** Frontend.cs:29 declares class Frontend in namespace WiseOwl.Cobol.Frontend. From any namespace under WiseOwl.Cobol the simple name Frontend binds to the namespace first, so the compiler writes the stutter Frontend.Frontend (CompilerDriver.cs:224 and 226, ListingFacts.cs:103) and Frontend.CompilationInputs (CompilerDriver.cs:193-194, ParseNodeReader.cs:58), and a reader cannot tell a type from a namespace segment at a glance. PB2246 (open) moves Pipeline/ to namespace WiseOwl.Cobol.Frontend.Pipeline per 8.2; a type Frontend in WiseOwl.Cobol.Frontend.Pipeline still matches a namespace segment, so CA1724 survives that move and its callers would be re-edited by a second rename wave. R0-0072 (873 lines, 38 members) splits the type under 8.3, which renames its pieces anyway.

**Target:** In the PB2246 move-rename change, the type gets a name that is no namespace segment (for example FrontendDriver, or the names of the 8.3 split pieces when that lands first), and every caller (CompilerDriver, ParseNodeReader, ListingFacts, ListingBuilder, RoslynBackend, MetadataRepositoryProvider and the 22 test files that construct it) changes in the same change with no alias (CLAUDE.md rule 4). The analyzer then reports 0 CA1724; oracle: the whole population unchanged, no emitted .g.cs and no diagnostic names the type.

**Sites:** src/WiseOwl.Cobol.Frontend/Pipeline/Frontend.cs:13-29 (Frontend); src/WiseOwl.Cobol.Compiler/CompilerDriver.cs:224-226 (CompilerDriver.NewFrontend); src/WiseOwl.Cobol.Compiler/Listing/ListingFacts.cs:103 (ListingFacts.DepositFrontend)

The finding's own finder tied it to this note, and the note's mechanism holds it, so it is recorded here and not filed as a note of its own. The sites are the pin's.
