## Filed two leads from the PB2556 and PB3245 reports as PB4183 and PB4184

The lead-filing clerk (units/enqueue.md step 5) turned two review and witness leads into `kb/Work/` notes. No code changed.

- PB4183 (defect, MINOR, process_only): `docs/CONFORMANCE.md` row DOC-A.1-159 says `FileRegistry.RegisterReport` registering every report connector as not line sequential is what makes the line end the host newline. `SequentialConnector.SelectLineEnd` takes the host newline from `recordDescribed: false`; `lineSequential: false` keeps the connector off the line sequential arm, so the refusal is '91' and not '71'. The `RegisterReport` comment chains the two effects in a milder form and is a site. From the PB2556 report, R1-1.
- PB4184 (defect, MINOR, process_only): `enqueue.py land` decides NOTHING TO LAND by ancestry alone, so a branch whose work a carrier landed re-merges main, re-gates in the shared slot and ends exit 6 NO ENTRY (a false STOPPED row), while `prune_worktrees.py` reads the same branch LANDED by content. The note records the measured witness (branch f7f-29, attempt 000444, PR #856) and the sweep of the other "is this branch's work on main" questions the fixer owes. From the PB3245 report.

PB4051 (stale RETRY rows for deleted branches) is a different mechanism and was not extended.
