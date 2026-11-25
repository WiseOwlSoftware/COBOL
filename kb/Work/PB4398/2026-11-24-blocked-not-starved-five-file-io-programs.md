**2026-11-24, sighting by the CONFLICT finisher of worktree-wf_c37a519f-ba4-15 (PB5707).** Gate run
`000005-20261124T132925Z` of that branch (merged head 7afbaa2164, a binder-only change: CURRENCY SIGN literal-8 rules, no
runtime or file I/O code) went RED on five HARNESS NON-OBSERVATIONs, each TimedOut twice at 120 s: NIST IC114A, SQ204A,
SQ214A, OBSQ5A and suite case `pb3350_open_reversed_varying_span_85`, every one a sequential/indexed file program.

The datum this sighting adds to the 2026-11-23 one: the host was NOT saturated. Each attempt used 1.6 s to 3.3 s of
processor time over 120 s while the host was 57-64 % busy (32 logical processors), and the run volume's durable flush median
was 1.3-3.7 ms (one attempt 130 ms). A starved process would show the host near 100 % busy; this one sat BLOCKED. That
points at a wait inside the program's runtime, a file lock or the sidecar entry mutex (PB4387's lock directory), held or
contended under the gate's concurrency, rather than at host load. The same four NIST cases pass alone on the same build in
19 s (`dotnet test --filter` over the four, 4/4 passed).

Repro path: the observation log `TestResults/build-local/000005-20261124T132925Z/Conformance.observations.log` in that
worktree (lines 5-18). Code site to start from: the runtime's file lock / sidecar mutex acquisition
(`src/WiseOwl.Cobol.Runtime` file connectors; PB4387's mutex), sampled with a stack dump of a live hung child
(the observer kills it at 120 s, so a dump must be taken before the kill).
