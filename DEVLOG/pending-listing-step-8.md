## PB5925: the source listing, step 8 - one sequence point per COBOL statement, a portable PDB, and the object-code map read back from them

Step 8 of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5925; the owner's order of 2026-11-18 to build the full listing) is
the map from each COBOL statement to its object code, read from what every backend produces, the assembly and its portable PDB
(the owner, 2026-11-18: nothing user-facing names or links emitted C#). Before it, the Roslyn backend wrote no PDB and no line
directive, so neither a debugger nor the listing could tell which IL a statement became.

**What was built.**
- Where a statement is written is a fact of the bound tree: `BoundStatement.Span` (`StatementSpan`: the physical file, the 1-based
  line and column of its first character, of the position after its last, and after its verb), stamped once by
  `StatementBinder.BindStatement` on the outermost node it returns, read from the statement's own tokens through the one
  resultant-to-physical mapping. The backend has no line map (the seam keeps the parse from it), and both backends read this span.
- The Roslyn backend, behind the seam: `CodeWriter.HideUnmappedLines` makes every line `#line hidden`, and `CodeWriter.MapStatement`
  (from `StatementEmitter.EmitStatement`) maps each statement's first code that Roslyn gives a sequence point
  (`CodeWriter.FirstSequencePoint`, read with Roslyn's parser) to the statement's span with C# 10's span form, the column offset at
  that code; anything after it on the line moves to a hidden line; a statement's code after a statement nested in it (a PERFORM's
  step and test) takes one more point spanning the verb. `RoslynBackend.Compile` writes `<out>.pdb` in the same emit
  (`BackendOptions.EmitPdb`, reserved until now), its documents named relative to the main source's directory
  (`BackendOptions.DocumentRoot`). A .NET debugger stops once per COBOL statement, in COBOL source.
- The listing: `ListingSection.ObjectMap` (`object-map`) and `ListingSection.ObjectCode` (`object-code`), in `all` and not in
  `standard`; `ListingObjectCodeMap`, `ListingMethod`, `ListingObjectRange`, `ListingInstruction`; `ObjectCodeMapReader` (the
  assembly and PDB with `System.Reflection.Metadata`), `IlDisassembler` (the runtime's `OpCodes` table, no opcode list),
  `MetadataNames`, `ObjectCodeMapBuilder` (each point placed through the one line index; a copybook copied twice paired by first
  points), `ObjectCodeText`; `ListingFacts.DepositObjectCode`; the schema regenerated (1.0, unreleased).
- Tests: `SequencePointCoverageDriftTests` (Unit: 374 NIST programs, each statement the parse built has its point and no point is
  elsewhere; red when MOVE is withheld its span, 431 misses on NC101A, and when `HideUnmappedLines` is removed);
  `ListingBackendNeutralityDriftTests` (RepoDrift: the Listing assembly references no Roslyn assembly, the builder's documents bind
  to no Roslyn or backend symbol by Roslyn's semantic model, no `.g.cs` is named; each rule red on a planted counterexample);
  `ListingObjectCodeTests` (Conformance: each statement's method and range equal the PDB's, read in the test; the PERFORM's second
  range; a copybook copied twice; the instructions; the statuses without an assembly); `IlDisassemblerTests` (Unit, ECMA-335
  partition III encodings); the object-code rows of `ListingSectionRenderingTests`.

**Corrections the code proved** (marked "(step 8)" in the design): the statement's position is stamped in the binder, not mapped
in the backend; a `#line` before each statement maps every later line by delta, so the first build gave points at columns no
statement starts at and several stops per statement, and the mapping is now one point per statement with the rest hidden; the
first stamping read the node's `At` after the funnel's wrappers, and a `BoundActivationSite` has none, which left every MOVE with a
sending-value temporary unmapped (the coverage test found it); full document paths made identical source compile to different
assemblies in two directories (the PE names its PDB by a hash of it), which the repository's byte-identity tests refused, so the
PDB names documents relative to the main source's directory and an expansion's text is written under the expansion's name; what
compiles to no code (CONTINUE, dead code, a CORRESPONDING with no pair, a condition the C# compiler folds) has no range; the
coverage test is in the Unit assembly over the NIST corpus, not every conformance-suite program, since a listing compilation is
never cached and the backend factory is internal.

**Measured** (the benchmark README's step-8 table): with no listing a full compile allocates +3.4 to +18.4 MB more than the parent,
most of it Roslyn's handling of the line directives (probe runs with the directives and the PDB switched off), so section 9's
budget now names the sequence points and the PDB; the standard text listing is +0.8 to +9.7 % over no listing; every section with
the object code is +35.5 to +38.6 %, over its 25 % (kb/Work PB5941). The first object-code build decoded each stretch from its
method's start, 2,657 MB for NC105A; fixed.

**Leads filed.** kb/Work PB5941 (the `All` row over budget); kb/Work PB5940 (the default listing of a compilation that fails before the backend is not written when the
output directory does not exist yet: found compiling NC101A into a new directory). PB5925's dated file `2026-11-18-step-8-built`
holds the measurements and the leads on the coverage test's flat-order exemption and its minute of Unit time.

**Review round 1.** An EVALUATE's second WHEN compiles to an `else if (...)` line, which the line reading took for no statement, so its test was listed under the first WHEN's last statement: the reading now parses a continuation line inside its head, and the writer tracks the blocks it opened so the brace ending a try, catch, finally, lock or using block (a point of its own in an optimized build) is the enclosing statement's code; both are checked against Roslyn's own PDB. A backend diagnostic on a mapped line named its file relative to the source's directory and was never placed: one decider, `SourceDocuments`, now names and resolves every PDB document, and the diagnostic names the full path. The coverage test's CORRESPONDING exemption reads the statement's own phrase, no longer any text holding CORR. kb/Work PB5925 dated file `2026-11-18-listing-step-8-review-r1`.