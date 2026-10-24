## PB1957 takes ten stranded loop and landing defects; PB3636 files the no-lane check

At 11:0x PDT on 2026-10-24 the owner asked which notes were not being enacted. A scratch classifier over the register
found 848 open notes: 7 in the campaigns the loop runs, 31 in the fix lane (paused until the planned work lands),
173 blocked, 202 in campaigns nobody runs, and 435 with no lane at all, because they carry no harm flag and belong to no
running campaign. Nothing in the probe, `work.py` or the planners reports that last group.

The owner approved moving the loop and landing MAJORs among them into the PB1957 process campaign: PB3628 at tier 1
(a re-armed NOT QUEUED branch is never landed again; it is why PR #467 sat 13 hours), and PB3625, PB3315, PB3310,
PB3267, PB3275, PB3413, PB2974 and PB3170 at tier 2. PB2699 joined at tier 1 in its own branch.

PB3636 files the missing check: one lane classifier in `work.py`, a `lanes` line in the probe, and a ratchet that refuses
a new MAJOR note with no lane. PB3637 records a defect found on the way: `work.py --help` dies on a cp1252 console
because the script switches stdout to UTF-8 only after argparse prints, and 70 other scripts share the ordering; its fix
is one UTF-8 mechanism for every entry point, not 71 moved lines.
