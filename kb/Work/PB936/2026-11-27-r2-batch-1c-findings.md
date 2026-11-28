## R2 batch 1c (pin 9e65164eb3a9): 1 finding on this note

### `codegen-1--duplication--f1#4` (lead, unify, votes upheld/upheld/upheld)

**Finding:** The rule 'one DataEmitter, so one memoized PhysicalModel, per program class' is stated at ProgramEmitter and broken at eleven other construction sites, each of which rebuilds the physical model from empty

**Rule:** recomputation of what an earlier stage resolved (DESIGN-architecture-review §5.2); the rule itself is written at ProgramEmitter.cs:392-394 ('ONE DataEmitter per program class ... A second instance would build the physical model again for every program (DeepNestingTests pins one model per program; kb/Work PB2671)')

**Scenario:** DataEmitter's constructor (DataEmitter.cs:23-35) makes a new PhysicalModel whose memo (PhysicalModel.cs:20, the cache that keeps the ~49-level CCVS nesting linear) starts empty. Besides the unit's own instance (ProgramEmitter.cs:395) the pin constructs one at ProgramEmitter.cs:165 (host entry, per unit) and :630 (LOCAL-STORAGE re-seed), InitializeEmitter.cs:18, SetEmitter.cs:25, ReportWriterEmitter.cs:35 (lazily, per emitter), and OoEmitter.cs:143, :462, :1517, and TWO in one expression at :202-203 (AddressableCellInit, ImageInitOf and CellDynSeeds each on a fresh model). Every one re-runs BuildPhysicals for each group it touches, so a deeply nested record that is also INITIALIZEd, SET, ADDRESS-OF-taken in a class, or in LOCAL-STORAGE pays the model build once per instance; DeepNestingTests' one-model pin holds only for the path it compiles. Lead: no ListBuilds count or wall time was measured on the pin

**Target:** the unit's one DataEmitter (or its PhysicalModel) is created by the per-unit composition root and reached through EmitContext by every consumer; the per-call `new DataEmitter(ctx)` sites are deleted and DeepNestingTests' observer asserts one model per unit over a program that exercises INITIALIZE, SET, LOCAL-STORAGE and an OO addressable cell

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/CodeGen/DataDivision/DataEmitter.cs:23-35 (DataEmitter..ctor); src/WiseOwl.Cobol.Compiler/CodeGen/ProgramEmitter.cs:165,392-395,630; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/OoEmitter.cs:143,202-203,462,1517; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/InitializeEmitter.cs:18; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/SetEmitter.cs:25; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs:35

The finding's own finder tied it to this note, and the note's mechanism holds it (nine call sites each build their own DataEmitter, so each builds a fresh PhysicalModel); the finding counts eleven sites at the pin and adds the per-unit composition root as the target. It is recorded here and not filed as a note of its own. `codegen-1--architecture--f1#3` (PhysicalModel carries each field initializer text) also named this note but is another mechanism, the layering of Init under the declaration-only layout, and is filed as PB6279. The sites are the pin's.
