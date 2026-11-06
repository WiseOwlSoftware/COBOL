## Entry 3217 — 2026-11-06 15:20 PST — Wave 20261885's unit records PB4838's owner hold and files the survey's cost

Group E of wave 20261885 returned BLOCKED on PB4838 with no commits, so the note now reads `owner` and carries the reproduction, the checked citations (§13.18.60.3 SR14 and SR15, §13.18.60.4 GR9) and the question: what a file holds for a tag or pointer leaf, and what READ leaves in the slot. Its `rejects_legal_source` flag stays as filed, with the note saying that the harm seen is a run-time abort rather than a compile-time refusal.

PB4852 is new. `plan_wave.py` took about 34 minutes, and at least part of that was `git cherry` over branches the history rewrite left with no merge base on main; the link between the two is inferred and the note says so. PB4399 gains a second measurement: the workflow ran 4 h 35 min and the stall watchdog died at 2 h. No code changed.

