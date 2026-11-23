## The Delete program note PB2119 is marked blocked

Wave 20262147 dispatched kb/Work PB2119 by itself as group D. PB2119 is the Delete program's umbrella. Its work lives in
its member notes (`cluster: ["PB2119"]`), and it names no file or member of its own. The owner's 2026-11-15 hold
(`kb/Work/PB2119/2026-11-15-owner-wait.md`) also still applies: the program waits until 2002 and 1985 read zero GAP, and
on base 223cd4fab5 `inventory_view.py --summary` reads 2002 at 1161 GAP.

The note's `status` is now `blocked`, which records the hold where `work.waiting_on` reads it, so the planner stops
dispatching the note. The reasoning is in `kb/Work/PB2119/2026-11-23-umbrella-held.md`. No code changed and no inventory
row moved. The root cause, that a note leading a cluster with open members still counts as ready, is a lead in the wave's
report and is not fixed here.
