## Nightly 2026-11-18: the toolchain bump

The nightly (run 44365192584) was red on `latest`: toolchain.env was BEHIND by git-windows 2.56.0 -> 2.56.1, gh 2.104.0 ->
2.105.0, uv 0.13.8 -> 0.13.9, semgrep 1.185.0 -> 1.186.0. `python scripts/toolchain.py bump` moved toolchain.env and
scripts/cloud/setup-env.sh together (dotnet-sdk 11 left alone: the owner's decision). The same nightly's Shard spread red
landed separately (branch shard-weights-1118).
