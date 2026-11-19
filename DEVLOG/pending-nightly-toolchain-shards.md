## Nightly 2026-11-18: toolchain bump and shard weights regenerated

The nightly (run 44365192584) was red on two checks. `latest`: toolchain.env was BEHIND by git-windows 2.56.0 -> 2.56.1,
gh 2.104.0 -> 2.105.0, uv 0.13.8 -> 0.13.9, semgrep 1.185.0 -> 1.186.0; `python scripts/toolchain.py bump` moved
toolchain.env and scripts/cloud/setup-env.sh together (dotnet-sdk 11 was left alone: owner's decision). Shard spread:
unweighted work 0.11 of a mean shard (> 0.10), STALE; `python scripts/shard_weights.py generate` rewrote
tests/_shared/test-weights.json (1223 classes from 5 observations). The dev host got uv 0.13.9 (winget) and semgrep
1.186.0 (uv tool); gh 2.105.0 (its MSI needs elevation) and git 2.56.1 (winget carries 2.56.0.4 at most) were not
installable from this session.
