## PB5226: a gate reuses a self-test's clock proof when nothing that can change it changed

In the build-stage1 gate, 28 of 42 self-tests were clock-proved and took 6,419 of 10,022 summed seconds, about half of
it the second run with the clock moved forward 400 days. `enqueue.py`, `land_lane.py` and `gate.py` re-ran that second
run only because `inert_lane.py` and `buildgraph.py` changed, and neither imports a time module. The proof now has its
own key: the self-test's own file, the shim, and the inputs that import `datetime`, `time`, `calendar` or `zoneinfo`,
read a clock, carry a date, or cannot be read. A `--reuse` run whose key matches a recorded proof inside its horizon runs
the self-test once and says `clock proof reused`; the new PASS keeps the old proof's horizon. CI still runs both runs.
The residue is a window defined in a module that imports no time module and carries no date. `PASS_SCHEMA` is 4.
