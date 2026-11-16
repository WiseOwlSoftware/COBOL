## Entry 3724 — 2026-11-15 20:41 PST — PB5764: no recursive delete of a variable path; one tracked tool runs a probe in a fresh directory

The owner's instruction of 2026-11-15: agents that probe the compiler reset a run directory with a recursive delete
of a path built from variables, which is the filesystem root when a variable is empty, and the harness warned the
owner for every one. Re-probe: a search of `E:/COBOL-coord/scratch/gl1985/lane1/run/` and `my-20261115/open20/` finds
the six agent-written scripts the note names, each resetting `$R/$id`-style paths before a copy.

**The tool.** `scripts/orchestrator/run_probe.py` promotes the stopgap `E:/COBOL-coord/scratch/gl1985/run_case.py`:
one run is one NEW numbered directory under a literal `--root` that must be absolute and outside every git tree;
nothing is deleted or reused. `--std` is required (no default edition); the compiler is `--exe` or the built
`--tree <checkout>` (no default pin; an unbuilt one is refused). Beyond the stopgap: the process tree is held by
`process_job.ProcessJob`, so a timeout (exit 124) kills the program as well as the compiler; the run gets the suite's
WiseOwl adapter environment (the locale pins, the message-control directory) and `--expect` compares by the suite
runner's own `normalize`, so a MATCH is what the suite would compare; two inputs of one name and a non-positive
timeout are refused. Its cases live in the case layout (`scripts/selftests/orchestrator/run_probe/`, PB5428), which
refuses a new in-file `--self-test`.

**The practice.** MANDATORY-PRACTICES P17 (all roles): no recursive delete of a path holding a variable or a command
substitution, in a command or a script; a probe runs through the tool; a file that must go is removed by its literal
path. P12 names the hook's new rule. `check_practices.py` refuses a brief, template, rendered spec or instruction
surface (`.claude/` roles, skills and workflows, the loop units, CLAUDE.md, PROMPT.md) that writes such a delete; a
line citing PB5764 names the forms to forbid them. Its cases: `.claude/selftests/skills/workstream/check_practices/`.

**The hook.** `scripts/hooks/forbidden_commands.py` rule 7 refuses Bash `rm` with a recursive flag in any spelling,
cmd.exe `rmdir|rd /s` and PowerShell `Remove-Item` (and aliases) with `-Recurse` when a target word expands at run
time. The shared model `shell_location.Step` gained `expands` (from the one word scanner, so a single-quoted or
escaped `$` is literal) and `shell` (a `cmd /c` payload is read as cmd.exe). 30 Bash and 15 PowerShell cases, each
able to fail (a mutation that ignored `expands` turned eight of them red).

**The sweep.** Every template and role that tells an agent to run a program names the tool: the implementer spec's
re-probe, the finisher brief, the cloud adjudication brief, `build_lane1_inputs.py`, `wf_lane1_validate.js`,
`wf_lane3_adjudicate.js`, `wf_lane3_refute.js`, the two spec-grammar workflows (which compiled inside `E:/COBOL`),
the adjudicator, refuter and reviewer roles, the review skill and the workstream skill.

Gate: see the branch's report (tooling lane).

