## Entry 2373 — 2026-10-22 16:33 PDT — A wave re-runs a gate its agent left running: filed as PB3536

In wave 1150 group A, a review finisher reached its turn cap while its gate was still running and returned SPLIT with no verdict. The workflow reads any non-DONE return as SPLIT and never looks at the gate run the agent named, so a second finisher had to be dispatched only to re-run the same gate at the same head. The owner called this a waste of tokens and a failure to fix at the source. PB3536 records the evidence and a two-part fix: the wave waits on a gate the agent left running (a GATE-PENDING return that the wave polls through the run's verdict file), and `build-local.ps1` answers from a recorded verdict for the same head and unchanged tree.

