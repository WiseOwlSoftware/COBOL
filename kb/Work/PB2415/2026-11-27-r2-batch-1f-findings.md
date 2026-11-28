## R2 batch 1f (pin 9e65164eb3a9): 1 finding on this note

### `listing-1--architecture--f1#6` (finding, move-and-rename, votes upheld/upheld/upheld)

**Finding:** The builder folder src/WiseOwl.Cobol.Compiler/Listing declares WiseOwl.Cobol.Listing.Building (census R0-0758); the folder rule applied against today root would name it WiseOwl.Cobol.Listing, the model assembly own root and layer, so the move must ride PB2415 compiler root flip to WiseOwl.Cobol.Compiler.Listing, which PB2415 does not list

**Rule:** folder equals namespace (DESIGN-architecture-review §8.2 rule 1, DESIGN-architecture-review §5.3) and the layers are namespaces (DESIGN-architecture-review §8.1: docs/rearchitecture/architecture-edges.json places layer listing at WiseOwl.Cobol.Listing and layer compiler.listing at WiseOwl.Cobol.Listing.Building)

**Scenario:** R0-0758 target reads move the files or rename the namespace so the two agree. A Move wave that renames the namespace to what the census rule computes today (RootNamespace WiseOwl.Cobol plus folder Listing) declares WiseOwl.Cobol.Listing, the namespace of the Listing assembly model and renderers: the 19 builder files and the model share one namespace, architecture-edges.json can no longer tell layer compiler.listing from layer listing, and the edge rule that keeps the model below the builder (the model references nothing, compiler.listing -> listing only) stops being checkable by namespace. Moving the folder instead (to Listing/Building) leaves the same reader confusion the census row names. PB2415 (open) lists every compiler namespace moving under WiseOwl.Cobol.Compiler but names no listing namespace, so the row has no wave that resolves it correctly.

**Target:** Under DESIGN-architecture-review §8.2 Shape A the builder namespace is WiseOwl.Cobol.Compiler.Listing (folder Listing under the root WiseOwl.Cobol.Compiler), done inside PB2415 compiler flip together with layer compiler.listing namespaces in architecture-edges.json and DESIGN-listing.md section 4.2 naming; until then no wave renames it. WiseOwl.Cobol.Symbols (src/WiseOwl.Cobol.Compiler/Symbols, layer compiler.symbols) flips with it to WiseOwl.Cobol.Compiler.Symbols.

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Listing/ListingBuilder.cs:7 (namespace WiseOwl.Cobol.Listing.Building); docs/rearchitecture/architecture-edges.json:5-13 (layer listing); docs/rearchitecture/architecture-edges.json:105-110 (layer compiler.listing)

The finding is about this note's own scope: the compiler root flip to WiseOwl.Cobol.Compiler does not list the builder folder src/WiseOwl.Cobol.Compiler/Listing (namespace WiseOwl.Cobol.Listing.Building, census row R0-0758) or src/WiseOwl.Cobol.Compiler/Symbols, and a rename to what the folder rule computes under today's root would collide with the Listing model assembly's own namespace. It is recorded here and not filed apart. The sites are the pin's.
