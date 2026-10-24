## PB3662 filed: the landing lane stops a branch whose own gate is still running

After the 2026-10-24 reboot, the `resume` unit merged main into `worktree-wf_1973801d-4fe-29` (PB2974) to bring in the
PB3612 gate and started a gate run. The landing lane launched `enqueue.py land` at the new head before that run had
written its verdict. Since PB3612 the landing reads gate records instead of gating, so the running gate counted only as
"no verdict file", and the attempt ended exit 14 UNTESTED. The lane treats exit 14 as a terminal STOP. The gate read GREEN
about ten minutes later, and the branch stayed stopped until the operator re-armed it by hand. Before PB3612 the same
case was exit 12 GATE BUSY, which the lane retries once the gate is free.

PB3662 records the case and the bar for the fix. A live gate run that would cover the tip is a wait, and the lane retries
that head once the run writes its verdict. One reader decides liveness: the gate driver's. Cluster PB1957.
