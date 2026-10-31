## Filed two leads from the PB2556 and PB3245 reports as PB4183 and PB4184

The lead-filing clerk (units/enqueue.md step 5) turned two review and witness leads into `kb/Work/` notes. No code changed.

- PB4183 (defect, MINOR, process_only): `docs/CONFORMANCE.md` row DOC-A.1-159 says `FileRegistry.RegisterReport` registering every report connector as not line sequential is what makes the line end the host newline. `SequentialConnector.SelectLineEnd` takes the host newline from `recordDescribed: false`; `lineSequential: false` keeps the connector off the line sequential arm, so the refusal is '91' and not '71'. The `RegisterReport` comment chains the two effects in a milder form and is a site. From the PB2556 report, R1-1.
- PB4184 (defect, MINOR, process_only): `enqueue.py land` decides NOTHING TO LAND by ancestry alone, so a branch whose work a carrier landed re-merges main, re-gates in the shared slot and ends exit 6 NO ENTRY (a false STOPPED row), while `prune_worktrees.py` reads the same branch LANDED by content. The note records the measured witness (branch f7f-29, attempt 000444, PR #856) and the sweep of the other "is this branch's work on main" questions the fixer owes. From the PB3245 report.

PB4051 (stale RETRY rows for deleted branches) is a different mechanism and was not extended.

Four more notes from the PB4147 report (pull request #860, the registry split):

- PB4185 (TRIVIAL): `generate.py` `repository_inputs` and `generate` still annotate the problems as `list[str]` though they are `Problem` tuples, and `conformance-suite/README.md` line 75 omits the `repository-update` option that `WISEOWL_OPTIONS` accepts (R1-1 and R1-3, one note, two sites).
- PB4186 (MINOR): `check_cases` checks test ids unique with exact case only, but each refusal is now its own `wiseowl/diagnostics/<id>.json`, so two ids that differ in case collide on NTFS and APFS (R1-2).
- PB4187 (MINOR): `impacted_tests.reach_of` names no test for a deleted registry file, right for `diagnostics/`, wrong for `options/` (R2-1).
- PB4188 (MINOR, silent): `review_findings.with_leads` dedupes leads by bare id, which is numbered per findings file, so a lead from a second file is dropped (R2-2).

Not filed: the report's `constructs.json` driver-hold lead. Of 89 DRIVER FILE QUEUED stops in the landing lane's logs, 41 name `diagnostics.json` (PB4147), 6 the traceability inventory (PB4148) and none `constructs.json`; PB3642 (landed) owns the hold itself, so the id PB4189 stays unused.

PB3719 gained a Sibling paragraph: `enqueue.driver_file_holders` holds a branch for every driver-covered file when a pull request's file list is cut off at 100 (pull request #860 lists 100 of 3,144 files and held two unrelated landings); the root fix is one complete file list per pull request in `open_pull_requests`.
