## Nightly 2026-11-27: the toolchain bump (gh, uv, semgrep)

The nightly (run 46053146312, and the two before it, 45880109906 and 45683044579) was red on `latest` alone:
`python scripts/toolchain.py latest` printed `BEHIND — gh 2.105.0 → 2.106.0; uv 0.13.9 → 0.13.10; semgrep 1.186.0 →
1.187.0`. `python scripts/toolchain.py bump` moved toolchain.env and scripts/cloud/setup-env.sh together. The dev box
follows in the same change: uv by `winget upgrade --id astral-sh.uv`, semgrep by `uv tool install --force
semgrep==1.187.0`, gh by `winget upgrade --id GitHub.cli` (an elevated install, under Program Files).
