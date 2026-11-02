## PB4452 follow-up: multiple editors of one file within a wave

The owner, 2026-11-02: "operator is still not allowing multiple editors to a file". PB4452 changed how in-flight work
holds a group, but two groups of ONE wave on one file were still refused. `check_practices.py` O2 used the file-level
`inflight.collision`, and the planner made the rest of a too-large file family overflow that waits a wave. As a result
the operator's parallel DataBinder groups (PB2352-PB2360) were refused on DataBinder.cs.

- O2 now refuses two groups of a wave only when they edit the same member (`inflight.hold`). A shared file, or a file
  either group claims whole, never refuses a pair.
- `plan_wave.plan_partition` still fuses one file family into one group while one implementer can hold it (PB4396).
  Past that size, the rest becomes a second group of the same wave, side by side, unless the two edit the same member.
- `test_plan_wave.py` pins both rules (521/521).
