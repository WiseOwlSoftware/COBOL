**PB5406's site block still claims work.py (wave 20262135 group G review lead R1-1).** The addendum
`2026-11-22-work-py-converted-by-pb3202.md` says the step's file set no longer includes `scripts/spec/work.py`, but the
**Sites** line still carries the glob `scripts/spec/w*.py`, which matches `scripts/spec/work.py`, so the computed file
set still claims it whole. The step's author narrows the glob (or names the remaining `w*.py` files) when the Sites are
next rewritten; until then `plan_wave.py` admits a change to work.py on this note.
