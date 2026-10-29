## PB4007: CI and every host run the tool versions one manifest names, and the gate refuses any other

**Why.** Pull request #734 was GREEN on every local leg and RED in its merge group (run 40754265548): CI's Windows jobs
ran the runner image's Python 3.12, while every local host ran 3.14, and the self-test's `Path.exists` stub behaves
differently on the two. The owner, 2026-10-28: "CI tooling should match local tooling versions - always", then "CI and
local should always be using the latest version". The hosts were brought to the latest releases by hand the same
afternoon (the note's table); this change makes that the mechanism.

**Re-probe.** On this branch's base, `python --version` gave 3.15.2, `java -version` 27.0.1, pwsh 7.6.9, git
2.56.0.windows.3, gh 2.103.3, jq 1.8.2, uv 0.13.6 and dotnet 10.0.403. The workflow still ran `windows-latest` and every
image tool but the SDK. The runner images' readmes give Python 3.12.10, JDK 17 (default), pwsh 7.6.7, git
2.56.0.windows.2 and gh 2.103.0 on `windows-2025-vs2026`, and pwsh 7.6.9, JDK 25, jq 1.8.1 and gh 2.103.2 on
`ubuntu-26.04`. So the defect reproduced as filed.

**The shape.** `toolchain.env` names every tool's exact version and the two runner images, between `toolchain` markers
that `scripts/cloud/setup-env.sh` carries byte for byte. It is the only place a version is written.
`scripts/install-tool.sh` replaces `install-dotnet-sdk.sh` as the one installer. It installs the SDK, the Oracle JDK,
pwsh, gh, jq and uv from their publishers, checking each against the publisher's checksum file (a single hash, or a
list; PowerShell's is UTF-16). Every job's step after its checkout is `.github/actions/setup-toolchain`, which replaces
`setup-dotnet`. It installs Python with `actions/setup-python` and the rest from one cache entry keyed on the manifest,
then runs `python scripts/toolchain.py check --scope ci`, which fails the job unless every tool reports the manifest's
version (git included, which actions/checkout runs from the image before the manifest exists). `windows-latest` is now
`windows-2025-vs2026`, and `toolchain-cache` replaces `dotnet-sdk-cache`. `toolchain-freshness.yml` runs `toolchain.py
latest` every day and is red while any publisher has a newer stable release or cannot be read. `toolchain.py bump` moves
the manifest and its cloud copy, never across a .NET major. On the hosts, the gate stops `NOT RUN [toolchain]` before
its audits when a tool it runs differs (Windows half: `run_gate_legs.Host.toolchain`; Linux half: `linux-gate.sh`), and
its verdict line names the toolchain. `python-resolve.sh` and `.ps1` accept exactly `PYTHON_VERSION` (the 3.12 floor is
gone), and `tooling_check.py` prints the host comparison at session start. `setup-wsl.sh` and `setup-env.sh` install from
the manifest. semgrep keeps its own uv-managed CPython 3.14.10 (no cp315 wheel for ruamel.yaml.clib yet), recorded as
`SEMGREP_PYTHON_VERSION`.

**Witnesses.** `ToolchainManifestDriftTests` (new; Unit, tree sweep) holds the manifest's shape, its cloud copy, the
action after every checkout, the images, no other install route, and every command a CI step runs being a manifest tool,
a repository script or the image's shell. A step that runs `node`, `yq` or `az` fails it, so a new tool must arrive
pinned. `DotnetSdkInstallDriftTests`, `PythonInterpreterDriftTests` and `SparseCheckoutDriftTests` moved to the new
names. The self-tests of `install-tool.sh`, `toolchain.py`, `run_gate_legs.py` (two new arms: NOT RUN [toolchain]
before anything runs; the verdict names the toolchain) and `tooling_check.py` cover the scripts. Real installs of all
six tools from their publishers ran on the Windows box, and each executable reported the manifest's version.

**PB1957.** Rows 8, 12, 13, 14, 15, 52, 53 and 56 are now (a), identical.

**Left, as leads.** CI's git is verified, not installed. The Windows box's Git for Windows revision trails its newest. The
command audit stops at the step and does not follow a repository script.

**Gate.** See the report: `TestResults/build-local/<run>` and its verdict line.
