## Entry 2695 — 2026-10-28 11:36 PDT — The UTF-8 switch is idempotent, its COPY rule reads the parse tree, and the live trees have one listing (PB3823, PB3822, PB3821)

Three notes from PB3637's review, all in the Python tooling's standard-streams and live-tree code.

**PB3823 — `ensure_utf8` second call.** Re-probed: `sys.stdin.readline()` then `ensure_utf8()` raised
`io.UnsupportedOperation` (Python 3.14.7, stdin a file, cp1252 host). `ensure_utf8` now compares each stream with the
target (`codecs.lookup(stream.encoding).name`, `stream.errors`, `line_buffering`) and reconfigures only what differs, so a
stream already in the state is untouched and the second call is a no-op by construction. A stream that is not yet UTF-8
and has been read still raises, on purpose: those bytes were decoded wrongly, and swallowing the refusal would hide it.
Self-test arms run a child process with stdin piped.

**PB3822 — COPY rule on one physical line.** Re-probed: a `reconfigure(` call wrapped onto a second line, or passed
`**kw`, was not reported on the base. `_copy_lines` now reads the parse tree (a `Call` of `reconfigure` with `encoding=`,
`errors=` or `**`), and string constants (a `python -c` program) go through `COPY_RE`, which now also names `**`. A comment
naming the call is no copy; a method bound to another name is not followed (stated in the docstring). The live tree stays
at 0 findings (197 files, 133 entry points probed).

**PB3821 — one listing.** The note counted four readers of `git ls-files -z -- <live_tools.ROOTS>`; there were seven
(`self_tests`, `scratch_repo.audit`, `temp_names`, `standard_streams`, `gate_plan` twice, `line_endings`), each with its own
suffix filter, missing-file rule and environment. `live_tools.tracked(repo, suffixes, env)` is the one listing and all seven
call it; `live_tools.py --self-test` covers it. `SelfTestDiscoveryDriftTests` holds each reader to it and gains a net over
every live Python file that fails on a `git ls-files` list holding `ROOTS`. The note's second claim, two copies of
`scrubbed_env`, was wrong: one drops git's repository-selection variables (PB1719), the other the gate handshake and the
VSTest filters from a `dotnet test` child (PB1718). They are two jobs with one name, so the git one is now
`scratch_repo.git_env` (two callers, both changed) and `SelfTestFixtureDriftTests` refuses a second definition of either
name outside its home. One behaviour change: `self_tests.discover` no longer raises on a tracked file deleted from the working
tree (the other readers always skipped it); an unreadable file still raises.

No compiler, runtime or conformance change; the three notes claim no inventory row, so `closes_rows` stays empty.

