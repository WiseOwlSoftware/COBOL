## PB3774: a standalone tick reports no conflict STOP

With the loop stopped, the operator landed both lanes' branches from a background `land_lane.py tick` every 5 minutes.
Three attempts ended in exit 4 that evening, and none of them reached the tick log or either inbox. `reap()` speaks
only for a runner that died and for the exit-8 halt, so a CONFLICT STOP shows only in `land_lane.py status`, and only
the loop's enqueue unit reads that. PB3774 gains the measurement and a second half for its fix: a tick prints one line
per newly finished attempt and posts a STOPPED branch's conflict brief to the owning lane, once per head.
