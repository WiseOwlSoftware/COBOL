## A sparse-checkout CI job can no longer lose an import silently (PB3787)

The merge-group `supersede` job (PB3769) checks out only `scripts/orchestrator` and `scripts/standard_streams.py` and
runs `cancel_superseded.py` in a `continue-on-error` step. PB3637 had to add `standard_streams.py` to that list by hand
after the fact; the next import of another `scripts/` module by `cancel_superseded.py`, `queue_facts.py` or `ruleset.py`
would have made the job cancel nothing while the run stayed green, and no test read the list.

Re-probe on this build: a copy of `scripts/orchestrator` alone fails `cancel_superseded.py --self-test` with
`ModuleNotFoundError: No module named 'standard_streams'`; the same copy with `scripts/standard_streams.py` passes.

Fix at the root, for every sparse job rather than for `supersede` alone: `SparseCheckoutDriftTests` (reader
`SparseCheckoutAudit`) takes each workflow job whose checkout has a `sparse-checkout` list (`supersede` and
`dotnet-sdk-cache` today) and requires the list to cover every repository file the job reaches: a token of its steps that
names an existing file, a local action's `action.yml` and what that names, the tokens of a shell script, and each
repository module a Python script imports as it starts, transitively (module-level `import` and `from ... import`,
relative and package forms; a function body's import is lazy and is not followed, so `standard_streams.live_python`'s
`import live_tools` is not demanded of a job that never calls it). The list is read as git reads it in non-cone mode, so a
job without `sparse-checkout-cone-mode: false` is refused (the action defaults to cone mode, where the same lines name
directories only) and a `!` pattern is refused as unmodelled. `DotnetSdkInstallDriftTests.JobShape` (the workflow job
reader) is now shared rather than copied. Limits of the static read, written in the audit: an `importlib` import and a
script path held in a Python string are not seen.

Arms: the real workflow without `scripts/standard_streams.py` in the supersede list is flagged naming the file and the
script that imports it (the note's acceptance); the real closures are measured so a reader that found nothing cannot
pass; planted arms hold a transitive import with its chain, the script the job runs, a lazy import, a docstring and a
comment, a class body and an `if` block, relative imports and packages, a local action with a commented-out mention, the
pattern forms, a cone-mode job, a negation and a job without a sparse list. A comment above the `supersede` list now
names the test; DESIGN-test-build-ci.md section 3.17.2 step 4 and section 3.17.8 and DOC_INDEX.md say so.

No inventory row is claimed or closed (process tooling). Self-review: the pattern matcher follows git's non-cone
semantics (a name covers a file or directory and everything beneath it; `*` does not cross `/`); a repository file that
shadows a standard-library module name (`scripts/orchestrator/mailbox.py`) would resolve ahead of it exactly as Python
resolves it from the script's directory.
