## Entry 3462 — 2026-11-10 21:33 PST — PB5234 and PB5235: stages 5 and 6 of the build redesign are filed for the operator lane

The owner asked that the build redesign (kb/Work PB5207) be spread across the Mythos and operator lanes and done in
parallel. Mythos's plan gives the operator two stages. PB5234 is stage 5: measure every guard (the `scripts/guard-*.sh`
family and the hooks that parse a shell command to refuse it, which caused 77 % of 1,095 refusals in three days), then
delete it or replace it with a design in which its harm cannot happen. PB5235 is stage 6 with stage 1's CI proof: CI runs
the same target graph, computes each target's key at the merged tree, accepts a pass recorded on the owner's machine, and
fails a Linux shard when a test reads a path outside its target's declared inputs.

