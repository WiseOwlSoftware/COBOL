## Entry 3878 — 2026-11-20 13:48 PST — Toolchain: Git for Windows 2.56.1; shard weights regenerated; the listing's follow-up notes

The nightly of 2026-11-20 (run 44786485038) was red on three jobs. `latest` and `images (windows-2025-vs2026)` both
named git-windows 2.56.0 against the published 2.56.1 (the image win25-vs2026/20261116.315 carries 2.56.1). The owner
installed 2.56.1 on the Windows host, and `python scripts/toolchain.py bump` moved `toolchain.env` and
`scripts/cloud/setup-env.sh` to it; `toolchain.py check` reads GREEN on the host. `Shard spread` was red because the
listing work added 22 test classes the weights file did not name (0.10 of a mean shard unweighted);
`shard_weights.py generate` rewrote `tests/_shared/test-weights.json` (1250 classes) and the report now reads 0 classes
unnamed.

With the full source listing landed (PB5925, PB5932), plan §0 says so, and the leads it left are filed: the four 9b
review leads (PB5963 a method's CALL listed as external, PB5964 a test comment's citation, PB5965 the program-name scope
rule written three times, PB5966 a second lookup in INITIALIZE) and four gate-speed defects found while landing it
(PB5967 the test_planner self-test keyed on every tracked file name, PB5968 the 22-minute filter_population self-test,
PB5969 the program differential carrying 0 cases on most listing gates, PB5970 NIST timeouts under three concurrent
gates). One branch, one gate, one landing for all of it (the owner's grouping rule).

