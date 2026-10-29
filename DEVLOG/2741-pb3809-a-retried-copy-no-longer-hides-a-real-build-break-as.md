## Entry 2741 — 2026-10-29 15:01 PDT — PB3809 — a retried copy no longer hides a real build break as ENVIRONMENT

`abi_pin.failure` classified a failed pin-settle build or test run by one regex over the whole output, and that regex held the
phrases of MSBuild's transient `warning MSB3026` copy-retry notice ("being used by another process", "is locked by"). A merged
tree that really failed to compile, in a build where any copy had retried once, was therefore classed `merge-blocked`
(ENVIRONMENT): `enqueue.land` ended exit 2 with no report line, the lane retried once and the second exit 2 was handed to the
operator instead of a finisher.

Re-probe on the branch build: the MSB3026 warning plus `error CS0103` plus `Build FAILED.` returned `merge-blocked`. Fix:
`host_caused` reads the output line by line, `error MSB3027` and `error MSB3021` count as the host's, and the phrases count
only on a line that is not an MSBuild warning. The ending `error MSB3027` after the same retry stays the host's. The
`abi_pin.py` self-test carries the arm (either line order), declared in `MergeDriverDriftTests`. No second classifier exists
(grep of scripts/, .github/, tests/); `enqueue.land` and `run_gate_legs.NOT_RUN_CAUSES` read the tag and are unchanged.

