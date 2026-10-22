## Entry 2343 — 2026-10-22 01:46 PDT — PB3504: an ANTLR runtime defect hung EVALUATE on nested function calls; the compiler now runs our fork's fixed 4.13.2 runtime

**What.** PB3489's sibling sweep found a hang on LEGAL source: four nested FUNCTION calls in any EVALUATE operand
(subject, WHEN object, condition, THRU range) never finished compiling, and three took seconds. MOVE, COMPUTE, IF and
the rest stayed flat.

**Is it ours?** The owner asked whether it was an ANTLR bug or a grammar problem, since reshaping the grammar only to
avoid a tool defect would be a workaround. The measurements answered it. ANTLR's profiler showed the EVALUATE decision
looking ahead only 10–14 tokens in pure SLL mode, while its time grew about 12-fold per level. The graph of prediction
contexts stayed small (361 nodes at depth 3), but the stack paths it stands for reached 12 million, and time followed
the paths. The parser's own serialized ATN and token stream, run on ANTLR's `ParserInterpreter` with no compiler code,
reproduced it in the C# runtime and in the Java reference runtime, which ran out of memory at depth 5. Counters in a
source build found the place. `PredictionMode.HasSLLConflictTerminatingPrediction` copies predicated configurations
with their predicates stripped and merges them WITHOUT a merge cache, so every shared subgraph is re-merged once per
path. Our operand rules carry semantic predicates and our first parse pass is SLL, which is how we met it.

**Fix.** One argument: the copy loop gets a merge cache, as every other merge in the simulator has. On the owner's
direction the fix lives in our fork, https://github.com/WiseOwlSoftware/antlr4, on a branch made of upstream `4.13.2`
plus the fix in both the C# and Java runtimes. The compiler restores `Antlr4.Runtime.Standard 4.13.2-wiseowl.1` from
the repo-local feed `nuget-feed/`, which also brings the C# runtime up to the 4.13.2 tool jar, a version upstream never
published to nuget.org. The grammar is unchanged. Depths 4, 6 and 10 now compile in about 2 seconds, the same as MOVE.

**Witness.** `EvaluateNestedOperandPredictionTests` measures the compiling thread's allocation at depths 1–3, not the
clock: the fixed runtime allocates 48 MB then 31 MB per added level, and pristine 4.13.2 allocates 192 MB then 2,335 MB.
Six-deep operands in all four EVALUATE positions compile.

**Next.** kb/Work PB3507 tracks the upstream pull request, whose text goes to the owner before it is opened, and the
return to an upstream release.

