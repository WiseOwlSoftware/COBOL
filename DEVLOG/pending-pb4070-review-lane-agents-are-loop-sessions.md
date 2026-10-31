## PB4070: the review lane's agents are loop sessions, so they exit at their result

The review lane starts each reviewer and conflict finisher as a headless `claude -p` session, and until now it exported no
`COBOL_LOOP_UNIT`. So every hook read those sessions as the attended operator. The inbox Stop hook refused their turn end until they
armed the operator's watcher, and that watcher kept them alive after their result: each ledger-publish message woke them for another
paid turn, and they held a lane slot until their ceiling (90 minutes for a reviewer, 240 for a finisher). This evening three idle
sessions for PB4147's branch held all three slots. The landing lane stopped PB4147 at 18:15 and PB4148 at 18:45 on conflicts, the
landing campaign's two bottleneck branches, and the lane could start a conflict finisher for neither. It was the fourth time in two
days; the first three ended the same way, with a unit killing the sessions by hand and filing a witness.

`run_agent` now marks each session `COBOL_LOOP_UNIT=review-lane`, the mark `orchestrate.ps1` gives a unit. The agent answers no inbox,
the Stop hook passes it, and the SessionStart hook gives it a line of its own: its prompt and its role's definition are the whole task,
it loads no skill and arms no watcher. The runner's self-test now has its stub record the mark it was started with. Without the fix
that check failed, reading `resume`, this unit's own mark leaking through the environment.
