## Entry 2243 — 2026-10-19 00:39 PDT — PB3402: the soft context cap no longer holds landings; PB3380 landed by PB3381; PB3403-PB3408 and PB3412-PB3415 filed

**PB3402 (MAJOR), fixed.** On 2026-10-18 the loop's enqueue unit passed its 200,000-token soft cap three times (19:11,
21:17, 23:47). The supervisor's `STOP-UNIT` was also one of the landing lane's stops, so every time, branches already
reviewed CLEAN waited for the capped unit to end: 26 minutes after 19:11, and 47 minutes after 21:17 (two branches
stamped CLEAN at 21:18 started landing at 22:05:08). A landing is its own process and adds nothing to the unit's
context. The owner (~23:58): "Stop the soft cap from holding landings".

Every stop meant to halt the loop already has a file of its own: the loop's `STOP`, the owner's global `STOP`, `HALT`,
and the hard cap's fleet stop `scratch\STOP-loop`, which the supervisor writes beside `STOP-UNIT`. So
`land_lane.stop_reason` reads the fleet stop in place of `STOP-UNIT`:
- the soft cap (and an operator's bare `STOP-UNIT`) holds no landing;
- the hard cap, `STOP`, the global `STOP` and `HALT` still do.
`STOP-UNIT` still tells the running unit to start no new step. The self-test's case 7 plants `STOP`, the hard cap's
pair and the global `STOP`; case 7b plants `STOP-UNIT` alone, and its WAITING TO LAND branch starts. With `STOP-UNIT`
put back, case 7b goes RED. The design doc (the file table, the context-policy table, section 4.9 and the self-test
list), `land_lane.py`'s docstrings and MANDATORY-PRACTICES O11 state the new rule.

PB3381 (PR #270) fixed PB3380, a successor chain whose predecessors' review fixes never reached the later links. A
successor now waits for its predecessor's whole review loop and merges the reviewed head. PB3381's note left the flip to
the operator, because PB3380's note was not on its branch. PB3380 is now landed, and its note says the three-link test
it asked for was not added.

New notes:
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
  checked by hand: all are carried on main, and on the owner's word they are deleted on origin. A remote whose local
  branch went any way other than `--apply` is never seen again. PB2708 found that `origin` is never surveyed and fixed
  only the `--apply` path.
- **PB3414 and PB3415**, the Mythos lane's two remaining leads from `-326-19`'s round-5 review, filed once PR #268
  (PB3334-PB3336) was on main. PB3414: GR-14.9.51.4-12 has no behavioural witness that a READ ONLY keyed writer runs
  under the store mutex. PB3415: two comments still say the sequential release asks `AdmitsAnotherWriter`. The
  `-326-31` review's three lows were filed by the enqueue unit's clerk (PB3410, PB3411, PB2749 witness (c)).

