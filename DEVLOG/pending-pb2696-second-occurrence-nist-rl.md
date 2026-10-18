## PB2696: a second occurrence, where NIST relative and indexed file programs never ran in a runtime format change's leg 1

The operator adds a dated section to kb/Work PB2696, from the Mythos lane's PB3333 finisher lead, checked against the
run's own files. The PB3330 engine's link changed relative and indexed files to the paged format. Its Windows gate (run
`20261018T175021Z-266291`) was GREEN with `map_state: none`, so every case was tier 1. The NIST rows of
`RL101A`-`RL213A`, `RL302M` and `IX106A` sat in leg 1's not-run list. At the landing the Linux gate went RED on 31 NIST
programs, `IX106A` and 30 of the RL programs (not `RL119A`), because their written-file goldens still held the old
format. The finisher re-recorded the 37 written files (GREEN on both hosts at `46934aa87`). The root cause is PB2696's
item 1. The note also records the finisher's untested hypothesis that a recorded map would still not attribute the
runtime's I/O code to the out-of-process NIST rows, the measurement that settles it, and the static rule needed if it
holds. Notes only.
