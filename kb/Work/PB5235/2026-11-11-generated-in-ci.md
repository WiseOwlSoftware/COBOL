**Decision: the generated suite in CI's plan, made once per leg (2026-11-11, PB5235 round 2).** PB5231 landed on main (#1503): `conformance-suite` is a generated target, and Conformance's key takes its OUTPUT digest.
This branch merged main and carried the plan and the workflow over to the graph.

**The plan** (`scripts/ci_plan.py plan`) keys every target per leg in topological order. A leg that accepts records takes
the generator's digest from the record the ref holds at the generator's key. That record carries no facets field: the
key covers the toolchain facet. The Linux leg accepts no record, so its Conformance key is unresolved and it runs.

**Decision: generate once per leg in a job of its own, not once per shard.** Each of the 12 shard jobs could generate
for itself, at about 10 s each with no wait added before the shards. One `<leg>-generate` job per leg instead:
- gives every shard of the leg the same suite, with one digest that `handed` checks against;
- lets the job record the plan's digest in a private store before it generates, so a generation that does not reproduce
  the local host's digest is `Nondeterministic`;
- keeps the generator out of the Linux shards' inert watch, as PB5231's report asked.

The cost is one job (checkout, toolchain, about 10 s of generation) ahead of the shards on the critical path. Measure it
with the first merge groups and record the figure here.

**The checks.** After the hosts, `ci_plan.py handed` checks that each output still has its digest and that no host log
records `GeneratedTarget.own_mark`. `GeneratedTarget.recorded_own` is now the one test of the mark, which gate.py uses
too. `test_population.py audit-callers` lets a workflow set a generated target's variable only beside that check.
`ci_plan.ci_wiring` holds the workflow to every piece.
