---
name: gate
description: Use before every commit and before every merge to choose and run the correct test gate - wave-local filtered (~2 min) per commit versus the comprehensive battery per accumulated batch - and to read the verdict without producing a false green.
---

# Gate

**Self-check first: is this a single wave/commit, or the batch's pre-merge?** Single wave means FILTERED. The owner
has corrected over-gating repeatedly; the full suite per fix is the bottleneck this model exists to remove.

## Always first

```
dotnet build CobolSharp.sln -c Debug
```

Build the **solution**, not one project. Building only the compiler project and then running `dotnet test
--no-build` tests whatever compiler was copied into the test bin at its LAST full build — a stale compiler. Local
goes green while CI fails.

## Per commit — wave-local (~2 min)

1. `dotnet test tests/Cobol.Net.Tests.Characterization` (full — it is seconds)
2. `dotnet test tests/Cobol.Net.Tests.Conformance --filter "FullyQualifiedName~<Area or the fix's own test class>"`
   - add `--filter "FullyQualifiedName~VersionMatrix"` for an edition gate
3. `dotnet test tests/Cobol.Net.Tests.Unit --filter "<the wave's tests>"`
4. A `cobol` CLI compile-and-run probe

**Do NOT run per commit:** the full Conformance suite, the full Unit suite, or the serial `scripts/guard.sh`.

## Per accumulated batch / pre-merge — comprehensive

- Full greenfield Conformance + full characterization
- The GnuCOBOL external differential, before AND after, diffing PER-CASE verdicts
- `scripts/guard-fast.sh` (parallel) when a legacy-shared seam was touched — never the serial `guard.sh`

`guard-fast.sh` is **not** CI-complete: CI builds Release, local builds Debug. Any change whose semantics differ by
build configuration gets a local `-c Release` leg before push. Better: do not write configuration-divergent
compiler behavior.

SERIAL does not mean comprehensive-per-change. The grammar batch is serial (one shared parser, so implement
constructs one after another) but still gets ONE comprehensive gate for the whole batch.

## Reading the verdict — where false greens come from

1. **Redirect the FULL output to a file.** Never `| tail -N` — it drops the failing test NAME, the one thing you
   need. Then grep the file for the summary line and for `crash|abort|Failed: *[1-9]`.
2. **Never `&&`-chain `git commit` or `git push` onto a test run or its tail.** The exit code of `tail` is not the
   verdict. Read the verdict, THEN commit as a separate call. This has been violated after the rule was written.
3. **Never edit source files while a gate is running.** The parallel legs compile from the WORKING TREE, so mid-run
   edits manufacture phantom failures. Staging first does not protect you. Prep only docs and commit messages.
4. **Never call a failure a "flake" without naming the test.** Get the name, reproduce in isolation with
   `--filter`, then disposition. A flake verdict requires a clean serial re-run of THAT test — never an inference
   from other suites being green.

## Read the failure before diagnosing it

Stack trace = emitter or runtime bug. `error CS####` = generated-code problem. A COBOLNET diagnostic = front-end
reject. Timeout = infinite loop. These are different problems; check which one you have first.
