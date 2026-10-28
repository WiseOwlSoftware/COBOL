## PB3801 — remove the GUI-launch guard; the fix is not running Obsidian

Wave 20261737 group G. PB3798 had added rule 8 to the guard hook (`scripts/hooks/gui_launch.py`, 368 self-test cases)
to stop an agent opening Obsidian on the owner's screen; its reviews kept finding forms it missed (PB3800), and the
owner ruled the cause is fixed by not running Obsidian (PB3799 deleted the views that asked for it), not by a filter
over every command.

**What the re-probe showed.** `gui_launch.py` was read only by `forbidden_commands.py` rule 8, and `Step.shell`
(added to `shell_location.py` by PB3798's second review round) only by `gui_launch.py`.

**Fix.** Deleted `gui_launch.py`; removed rule 8, its import and its docstring entry from `forbidden_commands.py`;
removed the GUI case lists and their runner lines from `test_forbidden_commands.py` (232 cases remain); reverted
`Step.shell` and its two cases in `shell_location.py` and `test_shell_location.py`; updated the guardrails row of
`docs/DOC_INDEX.md`. PB3798 gains a `## Superseded` line; PB3801 is `landed` with `closes_rows: []` (process tooling, no
inventory row). The one-line agent rule stays text in MANDATORY-PRACTICES through PB3797.

**Sibling sweep.** `grep -rIn gui_launch` outside `DEVLOG/` and `kb/Work/` finds nothing; no other reader of `.shell`
on a `Step`. No goldens: no ISO rule governs the hook.
