## Entry 3666 — 2026-11-15 01:47 PST — PB5616: the toolchain probe keeps what happened, and a tool that did not answer is NOT RUN [toolchain-unread], never "absent"

A gate on the dev box at 2026-11-15 08:38Z ended `NOT RUN [toolchain] — ... dotnet-sdk absent (toolchain.env 10.0.404).
Install the manifest's versions` (run 000004-20261115T083827Z), between runs that read `dotnet-sdk 10.0.404` on the
same host and tree, under heavy load. `toolchain._run` answered None for four facts (not on PATH, a 60 s timeout, a failed
start, an answer with no version) and the check called each one "absent", so the gate advised installing an installed SDK
and the record kept nothing that said which fact it was.

The probe now answers a `Probe` (`Answered`, `NotOnPath`, `TimedOut`, `NoStart`), and `installed` answers `Reported`.
Only "not on PATH" is absent. A version that could not be read still fails the check, under its own name, for example
`dotnet-sdk did not answer within 60 s`, and with its own exit code (3). The gate stops on it as `NOT RUN
[toolchain-unread]`, a new ENVIRONMENT row of `gate_verdict.NOT_RUN_CAUSES`, with no install advice. `linux-gate.sh` maps
exit 3 to the same tag, and the readiness check says UNREAD instead of ASK-OWNER. `Host.toolchain` answers `HostTools`
instead of a bare (bool, str) tuple, and `gate_reuse`'s two readers changed with it.

Evidence: the self-test's planted `dotnet --version` timeout (the real `_run`, with `subprocess.run` planted) printed
`ABSENT dotnet-sdk (none)` and `dotnet-sdk absent (toolchain.env 10.0.404); install the manifest's version` at
origin/main. On the branch it prints `UNREAD dotnet-sdk did not answer within 60 s`, exit 3.

No retry was added and the 60 s limit was not changed. The 08:38Z cause is still unknown, and PB5616 stays `half` until
the next occurrence's record names the cause.

