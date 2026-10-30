## PB3278: a dotted inventory symbol must be declared in its type, and the note's comment half was already fixed

**The note.** PB3278 said `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` resolved a
`file#Member` code-location by a whole-file word match, so a deleted member kept resolving while a comment or a
`<see cref>` still spelled its name (witnesses: `SequentialConnector.cs#WriteBeforeAndAfter`, `RecordFraming.cs#WritePrefix`).

**Re-probe.** The note was written against a tree older than PB762's resolver. On this tree the comment half does not
reproduce: `SymbolResolves` searches a C# file's tokens, so `WriteBeforeAndAfter` (named only in comments at
`SequentialConnector.cs:1580` and `:1589`) does not resolve, the PB762 fixture refuses comment-only, doc-comment-only
and block-comment-only names, and the four claimed rows (GR-14.9.51.4-23, GR-12.4.5.11.4-1, GR-12.4.5.11.4-5,
DOC-A.1-151) cite live symbols and resolve. That half is discharged by PB762.

**What was still weak.** A dotted symbol was split into segments and each segment was searched anywhere in the file's
tokens, so `Type.Member` resolved when `Member` belonged to another type of the same file. A Roslyn walk now indexes
the file's declarations (types, namespaces, methods, constructors, properties, indexers, events, fields, enum members,
delegates, local functions, record-positional parameters) in every suffix of the qualified path, and a dotted name must
be one of them. A simple name keeps PB762's rule (present in the file's code, a string literal included).

**Measured and rejected.** Requiring a simple name to be declared in the file, not merely used, turns 39 locations
(81 rows) red. Each is a row that cites the file that uses a symbol declared elsewhere (`ReferenceFormatDiagnostics`,
`ListingDirective2002`, `TryUnscaledParts`). A use cannot go stale without breaking the build, so those rows are right
and were left alone. All 33 dotted spellings in the inventory resolve under the new rule, so no row was re-sited.

**Test.** The PB762 fixture in `TheseChecks_ActuallyFail_OnAFabricatedInventory` gained two negatives (a member
declared on the other type, in both directions) and positive controls (a fully qualified name, a type, a name the file
only uses).

**Register.** PB3278 landed with `closes_rows: []` (no verdict claimed or changed). Design text updated in
`DESIGN-spec-conformance-review.md` and the `symbol_of` docstring.
