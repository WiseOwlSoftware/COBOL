## PB3380 landed by PB3381; PB3402-PB3408, PB3412 and PB3413 filed: the soft cap's stop holds the landing lane, a DEVLOG pre-landing check, PB3381's five open leads, a prune self-test hang, and remote-only branches the survey never sees

PB3381 (PR #270) fixed PB3380, a successor chain whose predecessors' review fixes never reached the later links. A
successor now waits for its predecessor's whole review loop and merges the reviewed head. PB3381's note left the flip to
the operator, because PB3380's note was not on its branch. PB3380 is now landed, and its note says the three-link test
it asked for was not added.

Seven new notes:
- **PB3402 (MAJOR).** On 2026-10-18 the enqueue unit passed its 200,000-token soft cap at 21:17:53. The supervisor's
  `STOP-UNIT` is also one of the landing lane's stops, so two branches reviewed CLEAN at 21:18 waited until the unit
  ended and started landing at 22:05:08, 47 minutes later. The same thing happened from 19:11 to 19:37. A landing is its
  own process, so a unit's context is no reason to hold it.
- **PB3403.** `devlog.py check` refuses every pending entry, and only the landing stamps, so a finished branch cannot
  confirm its entry before landing. It needs a pre-landing mode (the Mythos lane's lead).
- **PB3404-PB3408**, PB3381's open review leads:
  - PB3404: an UNANSWERED predecessor's findings have two owners (the successor's reviewer and the predecessor's next
    finisher);
  - PB3405: the survey and the wave look for findings files in different places;
  - PB3406: `last_verdict` can raise on a parser failure that is not an `OSError`;
  - PB3407: an unlistable wave directory drops its findings files silently;
  - PB3408: a relative stamped findings path reads TORN from another directory.
- **PB3412 (analysis).** A standalone Windows run of the prune self-test hung for more than 3 minutes in process
  creation under full CPU load, while the gate's run of it passed. The Mythos lane reported it during the `-326-31`
  bounce verification. It is unexplained, so it is a defect to root-cause.
- **PB3413 (MAJOR).** The branch survey lists only local branches and detached worktrees. Eleven branches of 10-09 to
  10-11 that exist only on origin surfaced only through a hand query during the drain. The owner asked why. Each was
  checked by hand: all are carried on main. A remote whose local branch went any way other than `--apply` is never
  seen again. PB2708 found that `origin` is never surveyed and fixed only the `--apply` path.
