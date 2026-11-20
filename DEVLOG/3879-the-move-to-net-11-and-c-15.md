## Entry 3879 — 2026-11-20 15:04 PST — The move to .NET 11 and C# 15

The owner decided on 2026-11-20 to move the repository from .NET 10 to .NET 11 (SDK 11.0.100, GA), and the move landed as one change
(kb/Work PB5971). The manifest, `global.json`, the target framework and the packages that ride the release train moved together:
`System.CommandLine` 3.0.0, and Roslyn 5.12.0, the compiler the 11.0.100 SDK ships. At the owner's word the language version moved
with it, from C# 14 to C# 15, the newest that compiler accepts (it refuses 16). The solution built with 0 warnings and 0 errors on
the first try, with warnings as errors.

The part worth keeping is how the target framework is written. A dozen scripts spelled `bin/Debug/net10.0/` themselves: the guards, the
differential drivers, the perf baseline, the probe runner. A move meant finding every one by grep, and a missed one would have
pointed a gate at an empty directory. Now `Directory.Build.props` is the only place the framework is written; the Python scripts read it
through `toolchain.target_framework()` and the shell guards through `guard_target_framework`. `toolchain_audit.py` fails the tree if the
props do not name the manifest SDK's major, or if a script spells a framework other than the current one, so the next move edits three
files and the audit names whatever it missed. `guard-run-group.sh` lost its default runtime path, which had been a second copy of the answer.

The owner still has to paste the changed toolchain block into the cloud environment's setup script and refresh WSL.

