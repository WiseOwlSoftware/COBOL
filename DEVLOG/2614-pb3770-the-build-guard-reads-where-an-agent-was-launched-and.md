## Entry 2614 — 2026-10-26 10:13 PDT — PB3770: the build guard reads where an agent was launched, and a scratch probe's project graph, instead of guessing both

The mailbox note of the Mythos lane (20261025-135010) said a read-only PB3631 adjudicator waited about 90 minutes for a
quiet window: `scripts/hooks/fleet_active_build.py` refused its scratch probe build while sibling agents were live.
Re-probed on this build with the real probe directory and a live wave implementer's real transcript, it was two defects.

The scratch directory (`E:/COBOL-coord/scratch/...`) is in no working tree, which the guard deliberately answers
UNKNOWN (PB2599: a scratch project can `ProjectReference` a worktree's project), so every foreign agent denied. And
`agent_working_tree` inferred a foreign agent's tree from whether `<main>/.claude/worktrees/agent-<id>` exists, one of
the host's several ways to start an agent: a rolling wave's implementers work in `.claude/worktrees/wf_<run>-<n>`, so
each read as a main-checkout agent. A build in the implementer's OWN worktree therefore saw no sharing agent, and a
build in the main checkout was denied by it.

Fix: an agent's tree is the tree of the directory it was launched in, read from the `cwd` of the first record of its
own transcript (`launch_cwd`, `agent_place`). That is one mechanism for `agent-<id>`, `wf_<run>-<n>`, a named
worktree and the main checkout, and it deleted `main_repo_root`, `agent_id_of`, `agent_working_tree` and
`agents_sharing_tree`. A build outside every tree now writes where its project graph says (`build_places`): the
project or solution named, or the only one in the directory, and the closure of its `ProjectReference`s, solution
members and a file-based app's `#:project` directives. A reference into a worktree makes that worktree a place, so a
probe referencing `perf-gate` is still denied by an agent in `perf-gate`. A graph the guard cannot read to the end
(a missing project, `$(...)`, `<Import>`, an output-path property, a `Directory.Build.*` file in an ancestor) and an
agent whose launch directory is unreadable or gone stay UNKNOWN and deny, as before.

Tests: `fleet_active_build.py --self-test` went from 62 to 94 cases over real temp trees, scratch projects and
transcripts (wave, host and operator worktree names; scratch graphs with a reference, a transitive reference, a
solution, a file-based app; each UNKNOWN shape). Six mutations of the new code each fail it, and the real hook,
driven end to end with this session's live fleet, allows the adjudicator's probe build. Design: DESIGN-test-build-ci.md
section 1.7. No inventory row (process tooling). Self-review (silent failure, test coverage, comments): the one
`except OSError` that returns "" is the documented UNKNOWN path and denies; the in-tree arm still reads no project
graph (unchanged).

