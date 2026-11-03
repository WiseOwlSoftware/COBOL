## The toolchain moves to uv 0.13.7

The nightly's `latest` job went red on 2026-11-03 (run 41679939118): `BEHIND uv 0.13.6 — the newest release is 0.13.7`.
`python scripts/toolchain.py bump` moved `toolchain.env` and `scripts/cloud/setup-env.sh` to 0.13.7, and both hosts were
updated in the same change (winget on Windows, `uv self update` in WSL), so `toolchain.py check` is GREEN on this host.
The paste of `scripts/cloud/setup-env.sh` into the cloud environment is the owner's step.
