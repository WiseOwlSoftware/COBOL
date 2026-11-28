## R2 batch 1g (pin 9e65164eb3a9): 1 finding on this note

### `procedure-binding-6--performance--f1#1` (finding, extract, votes refuted/upheld/upheld)

**Finding:** SEARCH ALL is bound with no key model on BoundSearch, so it is emitted as a serial probe: O(n) WHEN evaluations on the ordered table where the format exists to be O(log n)

**Rule:** Performance dimension: complexity at a stated input size (DESIGN-architecture-review §5.4 and DESIGN-architecture-review §4 item 5; tools/claude-skills/skills/review/references/dimensions.md 3). Conformance is not in question: ISO/IEC 1989:2023 14.9.37.4 GR9 leaves the technique to the implementor (kb/Work PB905, PB445).

**Scenario:** SearchBinder.BindSearchAll builds the OCCURS KEY / WHEN-conjunct model only inside the SR7-SR13 screen (StatementValidation.CheckSearchAllFormat2 -> SearchAllFormat2Rules) and discards it; BoundSearch gets only IsAll, so ControlFlowEmitter.EmitAllScan sets the index to 1 and advances it by 1 per probe, re-rendering every WHEN subscript at each probe (GR1). A SEARCH ALL over an OCCURS 10000 ASCENDING KEY table that misses costs 10000 WHEN evaluations where a binary probe costs ceil(log2 10000)+1 = 15; a program that runs that SEARCH ALL once per input record over 100000 records runs 1e9 condition evaluations instead of 1.5e6.

**Target:** The binder hands the Format-2 key model it already builds (keys with ASCENDING/DESCENDING, each WHEN conjunct as key = operand) to BoundSearch, and EmitAllScan emits a bounded binary probe over it (index never outside 1..bound, GR9), keeping the serial probe only where the model cannot be built; CONTROL_FLOW_DESIGN D12 is updated in the same change, as PB905 records.

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SearchBinder.cs:273-298 (SearchBinder.BindSearchAll); src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs:901-903 (StatementValidation.CheckSearchAllFormat2); src/WiseOwl.Cobol.Compiler/Binding/Validation/SearchAllFormat2Rules.cs:71 (SearchAllFormat2Rules.Check); src/WiseOwl.Cobol.Compiler/Binding/Bound/BoundTree.cs:2158 (BoundSearch); src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs:737-748 (ControlFlowEmitter.EmitAllScan)

The finding is the same mechanism as this note: SEARCH ALL is emitted as a serial probe because BoundSearch carries no key model, although SearchBinder.BindSearchAll builds the Format 2 key model inside the screen and discards it. The collector named PB905 and it is that note's own measurement. Recorded here and not filed apart. The members are still declared in the files the sites name on the tree after the pin. The sites are the pin's.
