## Entry 2715 — 2026-10-28 18:45 PDT — PB4007: CI and every host run the tool versions one manifest names, and the gate refuses any other

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
then runs `python scripts/toolchain.py check --scope ci`, which fails the job unless every tool it installed reports the
manifest's version. git is each runner image's (actions/checkout runs it before the manifest exists, and no publisher
ships a Linux git of a chosen version), named per image (`GIT_LINUX_VERSION`, `GIT_WINDOWS_VERSION`) and only warned on
in a job. `windows-latest` is now `windows-2025-vs2026`, and `toolchain-cache` replaces `dotnet-sdk-cache`.
`toolchain-freshness.yml` runs `toolchain.py latest` every day and is red while any publisher has a newer stable release
the manifest can take or cannot be read; its `images` jobs are red while an image's git differs from the manifest's.
`toolchain.py bump` moves the manifest and its cloud copy, never across a .NET major. On the hosts, the gate stops `NOT RUN [toolchain]` before
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

**Branch review, round 1.** Two blocking findings. R1-1: the first cut held every job's image git to one `GIT_VERSION`
as a hard failure, so GitHub rolling an image would stop every landing, and with the two images on different weeks no
value could clear it. Installing git in CI was ruled out (the `ubuntu-26.04` image has no zlib or libcurl headers for a
source build, and no publisher ships a Linux git binary), so the manifest now names each image's git, `latest` reads it
from the image's readme, a job on a moved image warns and runs, and the freshness run's `images` jobs hold each image
strictly (`check --scope image`). R1-2: the tool set lived in four places no test held together. `toolchain.py`'s
`TOOLS` is now the one table: the manifest may name no other key, `check` refuses an `install-tool.sh` whose
`TOOL_INSTALLABLE` differs from its install-tool rows, the action installs what `toolchain.py ci-installs` prints, the
host setups loop over the installer's own list, and the drift tests read the rows from `toolchain.py`'s text instead of
a hand-kept `ToolCommands`. The lead R1-3 was fixed in the same file: `latest` offers the newest SDK of the manifest's
own .NET major and reports a newer GA major as a warning, so the daily run does not stand red on an owner decision.

**Left, as leads.** The Windows box's Git for Windows revision trails its newest. The command audit stops at the step and
does not follow a repository script.

**Gate.** After the review round, run `000001-20261029T011321Z` (on 40cd15432): `LEG 1 ONLY (batched gating, PB2515):
GREEN — … Conformance 54/19,260 · Unit 5,602/38,309 · Characterization 36/36 cases ran (skipped 0) in 1 of 2 leg(s) ·
Linux half (WSL clone of HEAD) GREEN · … · toolchain dotnet-sdk 10.0.403, python 3.15.2, jdk 27.0.1, pwsh 7.6.9,
git-windows 2.56.0, gh 2.103.3, jq 1.8.2`. The Linux half passed its toolchain preflight, and every
`ToolchainManifestDriftTests` and `DotnetSdkInstallDriftTests` case, the new `EveryInstaller_InstallsExactlyTheTablesRows`
among them, passed in leg 1.

