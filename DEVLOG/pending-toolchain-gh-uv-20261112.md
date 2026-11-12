## Toolchain bump: gh 2.104.0 and uv 0.13.8

The daily Nightly `latest` job reported `toolchain.env` behind on two tools. `python scripts/toolchain.py bump` moved
GH_VERSION from 2.103.3 to 2.104.0 and UV_VERSION from 0.13.7 to 0.13.8, in `toolchain.env` and in the byte-identical
copy in `scripts/cloud/setup-env.sh`. Nothing else changed.

Left alone on purpose:
- .NET 11.0 (SDK 11.0.100) is GA. `bump` does not write it; moving to it changes `global.json` and every TargetFramework,
  which is the owner's decision (kb/Work PB1754 holds the open SDK question).
- GIT_LINUX_VERSION stays 2.56.0. The Nightly `images (ubuntu-26.04)` job is red because the runner image now carries
  git 2.56.1, but `toolchain.py latest` reads the git version from the image readme on actions/runner-images `main`,
  which still describes image 20261101.186.1 (Git 2.56.0), while the run used image 20261111.196.1. `bump` therefore
  cannot see 2.56.1 yet. It follows when the readme catches up.

Evidence: `python scripts/toolchain.py bump` printed `=== TOOLCHAIN: BUMPED — 2 tool(s) ===`;
`python scripts/toolchain_audit.py` printed `=== TOOLCHAIN AUDIT: CLEAN (18 jobs, 18 runs-on lines, 283 step commands, 5 jobs that run dotnet, 18 scripts) ===`;
`python scripts/toolchain.py --self-test` printed `=== TOOLCHAIN SELF-TEST: PASS ===`.

The hosts are not updated in this change. A gate started on any branch whose manifest names the old gh refuses with
NOT RUN [toolchain] once the host has 2.104.0, and the reverse holds for a branch with the new manifest on an old host,
so the host update waits until the orchestrator loop's in-flight gates have finished.
