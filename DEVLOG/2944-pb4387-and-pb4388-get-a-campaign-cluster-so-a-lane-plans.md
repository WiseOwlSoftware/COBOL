## Entry 2944 — 2026-11-01 05:57 PST — PB4387 and PB4388 get a campaign cluster, so a lane plans them

PB4278's landing gate went RED on SQ214A alone, and its finisher traced the red to two defects on main. PB4387: an
uncovered sequential writer takes and retires the sidecar entry mutex, a lock directory, on every WRITE. PB4388: the
test harness's `ProcessObserver` ignores its post-kill wait. Both are `process_only` notes with no cluster, and
`plan_wave` skips process-only notes in the fix lane, so no wave would ever plan them and PB4278 would wait forever. The
operator lane owns them under the owner's 2026-10-31 division. They now form cluster PB4387, which the operator's loop
plans as a campaign. PB4387's Sites line named two local predicates as members, so it now names their enclosing methods
(`JournalAppend`, `JournalImage`). This landing changes only the register.

