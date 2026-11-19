## Nightly 2026-11-18: the shard weights regenerated

The nightly (run 44365192584) was red on Shard spread: unweighted work 0.11 of a mean shard (limit 0.10), 58 classes the
weights file did not name (the heaviest RepoDrift.AnalyzerBuildsOnceDriftTests, 99 s). `python scripts/shard_weights.py
generate` rewrote tests/_shared/test-weights.json (1223 classes from 5 observations). The same nightly's `latest` red
(git-windows 2.56.1, gh 2.105.0, uv 0.13.9, semgrep 1.186.0) is a separate change, branch nightly-toolchain-shards: the dev
host needs elevated installs of gh and git first, and a host bump mid-run would refuse the listing build's gates.
