## The session probe prints every stop file and its age (PB6058)

The loop's STOP, STOP-UNIT and scratch\STOP-loop of 2026-11-16 were still present on 2026-11-22, and nothing showed
them. The owner: "We should have no STOP files from 6 days ago." The stale stops were removed, ten wave stops and
archived copies with them. `coord.py stops` now lists every stop present (the land lane's HALT among them) with its age and marks one older than 24 h
with ⛔. The session probe prints it as its `stops` line. `coord.py --self-test` GREEN.
