## Entry 2796 — 2026-10-30 12:06 PDT — The toolchain moves to semgrep 1.183.0 (toolchain freshness was red)

The daily `Toolchain freshness` run (41108565055, 2026-10-30 18:38Z) was red with `TOOLCHAIN: BEHIND — semgrep 1.182.1 →
1.183.0`. Every other tool in `toolchain.env` was at its publisher's newest release. `python scripts/toolchain.py bump`
moved `toolchain.env` and its byte-for-byte copy in `scripts/cloud/setup-env.sh`. Both hosts now run the manifest's
version, through the installer `scripts/wsl/setup-wsl.sh` runs:
`uv tool install --force "semgrep==1.183.0" --python 3.14.10 --managed-python`. That covers the Windows dev box and WSL,
and `toolchain.py check` is GREEN on both. semgrep is a host-scope tool, not one the gate runs, so in-flight branches
that still name 1.182.1 keep gating. The cloud environment takes the new `setup-env.sh` the next time it is pasted.

