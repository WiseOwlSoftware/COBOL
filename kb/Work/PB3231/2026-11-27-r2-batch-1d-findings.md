## R2 batch 1d (pin 9e65164eb3a9): 1 finding on this note

### `reference-resolution-1--architecture--f1#5` (lead, unify, votes upheld/refuted/upheld)

**Finding:** The resolver re-shapes a written reference by synthesizing new parse nodes, while its one-report ledger is keyed by parse-node identity

**Rule:** DESIGN-architecture-review §8.3 (4) (PB3230): one report per written reference is ONE ledger, ReferenceReports, keyed by the written node's identity (ReferenceReports.cs:21-40)

**Scenario:** Four builders construct fresh DataReferenceContext, QualificationContext, DataReferenceSuffixContext or PropertyObjectContext nodes that adopt the original children: ReferenceResolver.WithoutTrailingSuffix (ReferenceResolver.cs:104-122), ObjectPropertyResolver.ViewSubjectChain (ObjectPropertyResolver.cs:114-139), PropertyObjectReference (329-367) and WithoutResultRefMods (377-395). Each ask builds a new node, and the committing resolution of it (refs.ResolveImpl(subject, report) at 106, refs.Resolve(objRef) at 211) asks ReportOnce of that node, so a report about the identifier-3 or view subject is keyed to an object no second ask can present again: a statement binder that resolves the same written reference twice (the case ReferenceReports exists for, ReferenceReports.cs:10) would report it twice. Not reproduced on the pin: a probe program over a subscript fault inside identifier-3 of a property reference (`P OF T (0)`) bound by a verb that resolves its operand twice would settle it.

**Target:** The resolver's input after the decomposition is WrittenReference (or a slice of it) plus the original node as the report key, so a re-shaped reference is a value, not a fabricated parse node, and the ledger key is always the node the programmer wrote.

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Binding/ReferenceResolver.cs:104-122 (ReferenceResolver.WithoutTrailingSuffix); src/WiseOwl.Cobol.Compiler/Binding/ObjectPropertyResolver.cs:102-139 (ObjectPropertyResolver.ViewOfQualifiedItem, ViewSubjectChain); src/WiseOwl.Cobol.Compiler/Binding/ObjectPropertyResolver.cs:329-395 (ObjectPropertyResolver.PropertyObjectReference, WithoutResultRefMods)

The finding is a lead (no measurement) of this note's mechanism: the resolver re-shapes a written reference by synthesizing parse nodes while its one-report ledger is keyed by parse-node identity. It is recorded here for the wave that deletes the forged contexts, which must keep or re-key the ledger. The sites are the pin's.
