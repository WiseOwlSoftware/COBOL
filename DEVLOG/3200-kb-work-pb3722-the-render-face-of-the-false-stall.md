## Entry 3200 — 2026-11-06 10:34 PST — kb/Work PB3722: the render face of the false STALL

Wave 20261884 (PB4815, PB4817) had its specs rendered at about 01:55 by a Mythos session that ended before the Workflow call; this session launched it at 09:58. The operator's loop meanwhile posted a STALL for both groups every ten minutes, reading up to 491 minutes on running, because `progress.py` ages a group from the dispatch ledger's `at`, which `make_dispatch_specs.py` writes at render time. The face, its self-test and the new site are added to PB3722, the open note on the progress line's false STALLs; no code changes here.

