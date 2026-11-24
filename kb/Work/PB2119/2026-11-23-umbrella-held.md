## 2026-11-23 - status set to `blocked`: the program note holds no work of its own (wave 20262147 group D)

Wave 20262147 dispatched this note itself as fix-lane group D under `-Cluster PB1754`. The implementer found nothing to
change, for two reasons.

1. **The note names no site.** It is the Delete program's umbrella. Its work is its member notes (`cluster: ["PB2119"]`,
   for example PB2155 to PB2162). Each of those is its own Delete wave, measured dead by the census. The note names no
   file and no member, so a group made of it alone has nothing to delete.
2. **The owner's hold still stands** (`2026-11-15-owner-wait.md`). The program waits until the 2002 and 1985 inventories
   read zero GAP. On this tree (base 223cd4fab5) `python scripts/spec/inventory_view.py --summary` reads
   `2002 ... 1161 GAP (1148 never adjudicated · 13 open) | 1985 3109 rules · 0 GAP`.

`status: blocked` records the hold where `work.waiting_on` reads it, so the planner reports the note as waiting and stops
dispatching it. Nothing in the note's body changed. When 2002 reads zero GAP, set `status: open` again. The note lands
when its last member lands.

The planner dispatched this note because of a wider problem: a note that leads a cluster with open members still counts
as READY. That is recorded as a lead in the wave's report
(`E:\COBOL-coord\scratch\reports\w20262147d-PB2119-report.md`) and is not fixed here.
