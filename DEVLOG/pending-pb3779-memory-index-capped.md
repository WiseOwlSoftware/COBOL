## PB3779: the auto-memory index is capped at 6,000 characters and the cap is printed at every session start

The memory index (`MEMORY.md`) is in the turn-1 request of every dispatched agent and is re-read on every turn of every
session: 17,394 chars today (19.5K when the Mythos analysis of 2026-10-25 measured it, about 4.9K tokens a turn, about
$75 a week), two thirds of it dated owner decisions, resume notes and lane handoffs.

Re-probe: the note reproduced (`account.py --memory-index`, written for this change, would have printed 17,394 chars
against a 6,000 cap). The index is outside the repository, so the change has two halves.

The index itself: now 5,053 chars and 20 lines. It keeps the four anchors, the two START HERE lines that
`scripts/start-session.ps1` tells a restarted session to read, one pointer to `project_history_index.md` and 55
standing-rule links on seven lines. The history index carries the old index line for line, so every one of its 210
topic links is still reachable from one of the two files; no memory file was touched. The operator session edited the
START HERE line while the new index was being built, so the swap compares the file with the copy it was built from and
writes only if they are equal.

The cap: `scripts/orchestrator/account.py` owns the account's config dir and the repo key of the main checkout (where a
linked worktree's sessions keep their memory), so the index's path and `MEMORY_INDEX_CAP_CHARS` live there once;
`--memory-index` prints the size and exits 1 over the cap. `scripts/session-probe.ps1` prints the line at every session
start, the session-start skill names the remedy, and DESIGN-orchestrator-loop section 2.1 and the DOC_INDEX row
describe it. The account self-test pins the boundary (exactly at the cap is not over), the worktree-to-main-checkout
key, an absent index and a non-UTF-8 one. There is no new drift test: the file lives outside every tree CI can see, so
the probe line is the instrument.

Rows re-verdicted: none (process tooling; the note claims no inventory row). No compiler code changed.
