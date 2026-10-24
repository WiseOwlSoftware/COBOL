## Entry 2465 — 2026-10-24 11:49 PDT — PB2699: the owner decides the loop adopts landed, CI-green changes by itself

At 10:5x PDT on 2026-10-24 the owner asked whether the loop already ran the more efficient process. It did not. PB3603
and PB3611 had landed on origin/main, but the main checkout the supervisor runs from was still at `e5cf90436`, and
`orchestrate.ps1` loads its own script once, at start. Nothing between units fast-forwards the checkout; only
`autostart.ps1` does, once, before the loop starts.

The owner: "fix the loop so it picks up landed changes itself. We should adopt efficiency improvements as soon as they
are available and stable". PB2699 already asked for a fast-forward before every unit. Its new section adds the owner's
bar: adopt only a main commit whose CI run succeeded, restart the supervisor in place when its own code changes, and
make the planners refuse a checkout behind the adopted commit, each with a drift test. The note joins the PB1957
campaign at tier 1, beside PB3612. Until it lands, the operator fast-forwards and restarts the loop by hand after
each process landing.

