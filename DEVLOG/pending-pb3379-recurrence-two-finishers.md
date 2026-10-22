## PB3379 recurs: the loop and the Mythos lane both sent a finisher to one blocked branch

The Mythos lane's PB3511 branch went red at its landing (Linux, one golden), and both the Mythos session and the
loop's enqueue unit dispatched a finisher for it. The two fixes stacked without loss, but both accounts paid for one fix
and two agents wrote one worktree. It is the class PB3379 already records: a branch has no owning orchestrator, so two
lanes work it at once. The note gains this recurrence and one case for its fix: a blocked report is finished only by
the lane that owns the branch, and a finisher dispatch records a claim that every other dispatcher reads first.

The reviews doubled as well, and when both lanes then ran the landing script on the branch within a minute, the second
run found the gate lock held and reported it as a red gate, marking the healthy branch blocked. That is filed separately
as PB3526: a held gate lock is not a red gate.

The landing of this branch then stopped on a Linux gate red that was not its content, filed and fixed here as PB3529.
`ConformanceSuiteRunnerDriftTests` found the `cobol` apphost missing ("No such file or directory", "Text file busy")
because the guard leg's own build of the CLI recompiled it while the conformance leg ran it. A second build of a fresh
tree was not a no-op: the frontend's `EnsureGeneratedFiles` target appended the generated parser to Compile after the
SDK had hashed `@(Compile)` into `CoreCompileInputs.cache`, so the next build, whose glob finds those files mid-list,
recompiled the frontend, the compiler and the CLI. The target now runs before `BeforeCompile` and removes and appends
the generated files on every build; on a fresh WSL clone the second build now changes nothing.
`AntlrGenerationDriftTests` pins the shape, and `linux-gate.sh` reports `build-rewritten` if the CLI's output moves
while the legs run.
