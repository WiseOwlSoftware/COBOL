## Entry 2733 — 2026-10-29 13:14 PDT — The sparse-checkout audit follows a function body's import and shares the one workflow job reader (PB3810)

`SparseCheckoutAudit.ModuleLevelImports` skipped every import inside a `def` body, so a repository module a CI script
imports lazily (from `main()`, say) was never demanded of a sparse-checkout list: the job would fail on the runner with
`ModuleNotFoundError` and `SparseCheckoutDriftTests` stayed green, the PB3787 failure again. The repository's scripts hold
176 such imports, so the idiom is the common one. The reader is now `Imports` and takes every `import` and `from ... import`
statement at any depth (a docstring's text and a comment are still not code); over-approximating the closure fails loudly in
the test, under-approximating it failed silently on the runner. The first run on the real workflow named two files the note
had not: `standard_streams.live_python` imports `live_tools`, which imports `scratch_repo`. Both joined all six sparse lists
(`supersede`, `toolchain-cache`, `ci-gate`, and the two jobs of toolchain-freshness.yml).

The second half: `SparseCheckoutAudit.Jobs` ran the shared `JobShape` regex over the whole workflow and returned the `on:`
triggers `push`, `pull_request` and `merge_group` as jobs, while `DotnetSdkInstallDriftTests.Jobs` sliced to the text under
`jobs:` first. Sweeping for the siblings found `ToolchainManifestDriftTests.Jobs`, a verbatim copy of the slicing. All three
now read through `DotnetSdkInstallDriftTests.Jobs`, and `JobShape` is private again. Four further job readers
(`InertLaneDriftTests`, `SelfTestDiscoveryDriftTests`, `FilterPopulationGuardDriftTests`, `CiHarnessLogDriftTests`) have other
output shapes and are registered as a lead, not folded in here.

Arms: a lazily imported module missing from the list is flagged with its chain; the real workflow's job reader returns no
trigger as a job (and a planted `on:` + `jobs:` workflow returns only its job); the lazy arm of the old
`ALazyImport_...` test became a reached arm; `TheSupersede...` asserts `live_tools.py` is reached. The closure test now prints
its problems instead of a truncated collection. DESIGN-test-build-ci.md 3.17.8 and the PB3787 note carry the corrected
reading (the first said a function body's import "is lazy and not followed").

