## PB4395 item 1, the TOOLCHAIN family: five pipeline classes leave the compiler's test assembly for two script self-tests

**The slice.** Owner direction 2026-11-01 (kb/Work PB4395 item 1, split by script family): move
`ToolchainManifestDriftTests`, `DotnetSdkInstallDriftTests`, `PythonInterpreterDriftTests`, `LinuxGateDriftTests` and
`CiHarnessLogDriftTests` out of the Unit assembly into script self-tests that read the workflow, action, script and
`global.json` files as text, and delete each class (no forwarder).

**Re-probe.** On this branch's build (0 warnings, 0 errors) the five classes ran 26 cases, all green (Toolchain 9,
DotnetSdk 8, PythonInterpreter 3, LinuxGate 4, CiHarnessLog 2).

**The homes.**
- `scripts/toolchain_audit.py` (new; `--self-test`, and a bare run audits the tree) holds the toolchain classes and the
  interpreter class: every CI job sets up the toolchain right after its checkout, runs on a manifest image, installs no
  tool any other way and runs only manifest tools, repository scripts and the image's shell; the installers and host
  setups take every tool and version from `toolchain.py`'s TOOLS; the .NET SDK pin is one `global.json` accepts, its feeds
  avoid aka.ms, every `dotnet` job has the action first, the action restores, installs, saves and proves the SDK, and the
  cloud setup's install block matches install-tool.sh; every gate script runs the resolved interpreter, and the two
  resolvers match. `toolchain.py` was the nearest owner, but it runs in every sparse CI checkout, and importing the
  workflow reader there would add `ci_workflow.py` to each sparse list. So the audit imports `toolchain`, never the
  reverse. It reads TOOLS as Python objects, so the C# reader of `toolchain.py`'s text (`ReadTable`, and the rule that
  each row fits on one line) is gone. Its checks of the manifest, the cloud copy and TOOL_INSTALLABLE stay in
  `toolchain.py --self-test`, which already held them.
- `scripts/test_linux_gate.py` (new; a standalone `test_*.py`) holds the facts of `linux-gate.sh`'s text against the
  workflows: its legs equal CI's Linux test projects and scripts, with the guard leg among the defaults; `wgit` is the only
  git that reads the tree; no gate script starts the tool; every CI `dotnet test` step writes its harness log into its
  results directory, and its job uploads it. linux-gate.sh's own bash `--self-test` cannot read a workflow without a
  second reader (kb/Work PB3819).

**Arms.** Before, 26 C# cases. After, 61 self-test arms: toolchain_audit 43 (42 planted, 1 for the tree) and
test_linux_gate 18 (13 planted, 5 for the tree). The C# LinuxGate class planted nothing; every audit now fires on a
planted shape.

**Found on the way, fixed.**
- The SDK audit missed a toolchain step written `- uses: ./.github/actions/setup-toolchain` with no `name:` line: it
  compared the trimmed line with `uses: …`. Planted arm added.
- `ci_workflow.py` and `self_test_arms.py`, which this branch inherited, failed `standard_streams.py`'s ENTRY rule: their
  `__main__` block did not call `ensure_utf8()` first. That turned `standard_streams.py --self-test` RED.
- `LandingLaneDriftTests` still declared `land_lane.py`'s `classify_red` arm under the label that items 2 and 3 changed
  (the new label adds "or a tooling-lane record"). The declaration now matches.
- `scripts/inert_lane.py` gains one TOOLING_READS entry, `global.json`. Without it, `self_tests.py`'s population check
  refused the audit's PASS, and a `global.json`-only change would have skipped the SDK arm locally and failed in CI.

**Self-review.** One reader per job: workflows go through `ci_workflow.py`, the tool table is TOOLS, and the
installer rule is `toolchain.installer_problems`. No copy of a rule remains. Every test-runner literal is written so
`test_population.py`'s caller audit does not mistake it for an unscrubbed instruction. Semgrep: every count is level or
lower (raw-diagnostic-code-literal 225 to 216, the deleted classes).
