## Entry 3630 — 2026-11-14 17:29 PST — Toolchain bump: gh 2.104.0, uv 0.13.8 and git-linux 2.56.1

The daily Nightly reported `toolchain.env` behind: `latest` on gh and uv, and `images (ubuntu-26.04)` on the runner
image's git (run 43612035301). `python scripts/toolchain.py bump` moved three values, in `toolchain.env` and in the
byte-identical copy in `scripts/cloud/setup-env.sh`:
- GH_VERSION 2.103.3 to 2.104.0 and UV_VERSION 0.13.7 to 0.13.8 (bump run of 2026-11-12, commit 505c6a9cd2);
- GIT_LINUX_VERSION 2.56.0 to 2.56.1 (bump run of 2026-11-14, after merging main). kb/Work PB5458 made `latest` and
  `bump` read the git of the newest actions/runner-images release tagged for the image (`ubuntu26/20261111.196`), the
  same release the `images` job checks against, so `bump` now sees 2.56.1; on the 2026-11-12 run it read the readme on
  `main`, which still named 2.56.0.

Left alone on purpose: .NET 11.0 (SDK 11.0.100) is GA. `bump` does not write it, and `latest` reports it as a warning,
not a red: moving to it changes `global.json` and every TargetFramework, the owner's decision (kb/Work PB1754).

The hosts were updated with this change, on 2026-11-14, after the orchestrator loop had stopped and no gate was running:
on the Windows dev box, winget installed gh 2.104.0 and uv 0.13.8; in WSL, `scripts/wsl/setup-wsl.sh` installed gh
2.104.0 and uv 0.13.8, and the git-core PPA's git 2.56.1. The gh MSI's UAC prompt and WSL's `sudo apt-get install git`
needed the owner, who ran them. The update waited for a stopped loop because a gate started on a branch whose manifest
differs from the host stops NOT RUN [toolchain] (kb/Work PB5459, still open: the procedure itself is not yet a loop
unit). `python scripts/toolchain.py check --scope host` then printed GREEN on both halves: Windows `... git-windows
2.56.0, gh 2.104.0, jq 1.8.2, uv 0.13.8, semgrep 1.185.0 ===` and WSL `... git-linux 2.56.1, gh 2.104.0, jq 1.8.2,
uv 0.13.8, semgrep 1.185.0 ===`.

Evidence: `python scripts/toolchain.py bump` printed `=== TOOLCHAIN: BUMPED — 1 tool(s) ... ===` (git-linux 2.56.0 to
2.56.1) on 2026-11-14; `python scripts/toolchain.py latest` then printed `=== TOOLCHAIN: LATEST — every tool is at its
newest release ===`; `python scripts/toolchain_audit.py` printed `=== TOOLCHAIN AUDIT: CLEAN (18 jobs, 18 runs-on lines,
286 step commands, 5 jobs that run dotnet, 18 scripts) ===`; `python scripts/toolchain.py --self-test` printed
`=== TOOLCHAIN SELF-TEST: PASS ===`.

