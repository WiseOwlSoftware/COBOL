## PB5303: a rolling wave's concurrency is the gate pool's cap in force

`plan_wave.py wave_concurrency` used to return the gate cap less the slots held and tickets waiting when the wave was
planned (PB5100). An implementer codes far longer than it gates, so those holds say nothing about the slots free when its
gates arrive: wave 20261999 was planned at 1 from a busy moment, ran 8 compiler groups serially and starved about 25 notes
for 216+ minutes. It now returns the cap in force (3), never fewer than 1; the slot queue already serializes the gates.
The planner's printed reason, the plan_wave self-test (cap 3 with 2 held and 1 waiting gives 3; the old formula gave 1),
the workstream skill and references, `wf_rolling_wave.js` and DESIGN-test-build-ci §3.14.6 state the new rule.
