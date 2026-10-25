## Owner decision: the landing lane becomes its own process, stopped only by STOP (PB3703, option 3)

Since PB3639 the landing lane ticks for as long as the supervisor lives, but a circuit-breaker trip (exit 4) or an
owner question (exit 5) still ends the supervisor, and with it all landing of branches already reviewed and gated.
The operator laid out three options and recommended a separate lane process. The owner chose it ("Option 3").
PB3703 is open at tier 1 of PB1957 with the bar. The lane runs as `land_lane.py run` under its own lock, the
supervisor starts it and no longer ticks it, only STOP ends it, and the supervisor restarts it when its code changes.
