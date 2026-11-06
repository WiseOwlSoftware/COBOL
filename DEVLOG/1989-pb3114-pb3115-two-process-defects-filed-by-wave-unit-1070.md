## Entry 1989 — 2026-10-12 22:41 PDT — PB3114, PB3115: two process defects filed by wave unit 1070, and the main checkout's trend points committed

PB3114 (process/orchestrator, MINOR, silent): `plan_wave.py` reserves the wave's codes and records its groups in the dispatch ledger before `make_dispatch_specs.py` and `check_practices.py` run. A RED plan therefore exits 1 with its notes held as in flight, so the next plan skips them until someone runs `--release`, and with three codes per group reserved and never used. It also accepts a drive-relative `--scratch`. Wave unit 1070 hit both on its first run: Git Bash stripped the backslashes from the unit prompt's paths, the planner wrote its specs into `E:\COBOL\COBOL-coordscratch\` inside the main checkout, held all eight groups and burned `COBOLNET3581`-`COBOLNET3604`. The unit released the dispatch, removed the folder and re-planned from PowerShell.

PB3115 (process/ledger, MINOR, silent): `gen_ledger.py`'s `missed_points` lists `git log <last point>..HEAD`. When the last point is a merged branch's own commit (`d5396665`, PR 21's), the commits of a branch that landed beside it (PR 19's `693fa456` to `332dd4e0`) are not its ancestors and are appended a second time.

`docs/rearchitecture/evidence/ledger-trend.json` gains the 24 points the main checkout's renders had appended without a commit (`746006ae` to `57c36e5b`, the four PB3115 duplicates included, verbatim). Filed only; no code changed.

